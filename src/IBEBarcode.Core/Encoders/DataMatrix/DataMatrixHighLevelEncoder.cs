namespace IBEBarcode.Core.Encoders.DataMatrix;

// C40/Text/X12/Base256 encodation per ISO/IEC 16022 Annex C. Each mode always encodes
// the WHOLE message in that single mode (no per-character mixed-mode optimizer, matching
// this project's existing PDF417/Aztec text-mode scope decisions) -- if any character in
// the input isn't representable in the requested mode, encoding fails outright rather than
// silently falling back.
//
// EDIFACT mode is deliberately not implemented: unlike C40/Text/X12 (which unlatch via a
// plain byte-aligned ASCII codeword, 254) EDIFACT signals its unlatch as an in-band 6-bit
// value inside its packed bitstream, then pads to the next byte boundary -- a genuinely
// distinct mechanism, and EDIFACT is the least practically useful of the 5 modes for a
// general-purpose barcode generator (mirrors the one-special-case-skipped pattern already
// used for DataMatrixSymbolInfo144).
internal static class DataMatrixHighLevelEncoder
{
    public const int LatchToC40 = 230;
    public const int LatchToText = 239;
    public const int LatchToAnsiX12 = 238;
    public const int LatchToBase256 = 231;
    private const int Unlatch = 254;

    public static bool TryEncodeC40(string text, out byte[] codewords, out string? error) =>
        TryEncodeTriplet(text, LatchToC40, c => GetC40OrTextValues(c, textMode: false), out codewords, out error);

    public static bool TryEncodeText(string text, out byte[] codewords, out string? error) =>
        TryEncodeTriplet(text, LatchToText, c => GetC40OrTextValues(c, textMode: true), out codewords, out error);

    public static bool TryEncodeX12(string text, out byte[] codewords, out string? error) =>
        TryEncodeTriplet(text, LatchToAnsiX12, GetX12Values, out codewords, out error);

    public static byte[] EncodeBase256(byte[] data)
    {
        var result = new List<byte> { LatchToBase256 };
        var position = 2;

        if (data.Length <= 249)
        {
            result.Add(Randomize255(data.Length, position));
            position++;
        }
        else
        {
            result.Add(Randomize255((data.Length / 250) + 249, position));
            position++;
            result.Add(Randomize255(data.Length % 250, position));
            position++;
        }

        foreach (var b in data)
        {
            result.Add(Randomize255(b, position));
            position++;
        }

        return result.ToArray();
    }

    private static byte Randomize255(int value, int position)
    {
        var pseudoRandom = ((149 * position) % 255) + 1;
        var temp = value + pseudoRandom;
        return (byte)(temp <= 255 ? temp : temp - 256);
    }

    // Returns null if the character can't be represented in this mode; otherwise 1 value
    // (base set) or 2 values (a shift escape followed by the shifted value).
    private static int[]? GetC40OrTextValues(char c, bool textMode)
    {
        if (c == ' ')
        {
            return new[] { 3 };
        }

        if (c is >= '0' and <= '9')
        {
            return new[] { c - '0' + 4 };
        }

        if (!textMode && c is >= 'A' and <= 'Z')
        {
            return new[] { c - 'A' + 14 };
        }

        if (textMode && c is >= 'a' and <= 'z')
        {
            return new[] { c - 'a' + 14 };
        }

        if (c <= 31)
        {
            return new[] { 0, (int)c };
        }

        const string shift2Chars = "!\"#$%&'()*+,-./:;<=>?@[\\]^_";
        var shift2Index = shift2Chars.IndexOf(c);

        if (shift2Index >= 0)
        {
            return new[] { 1, shift2Index };
        }

        if (!textMode && c is >= 'a' and <= 'z')
        {
            return new[] { 2, c - 'a' };
        }

        if (textMode && c is >= 'A' and <= 'Z')
        {
            return new[] { 2, c - 'A' };
        }

        var shift3Extra = c switch
        {
            '{' => 26,
            '|' => 27,
            '}' => 28,
            '~' => 29,
            (char)127 => 30,
            _ => -1,
        };

        return shift3Extra >= 0 ? new[] { 2, shift3Extra } : null;
    }

    private static int[]? GetX12Values(char c) => c switch
    {
        '\r' => new[] { 0 },
        '*' => new[] { 1 },
        '>' => new[] { 2 },
        ' ' => new[] { 3 },
        >= '0' and <= '9' => new[] { c - '0' + 4 },
        >= 'A' and <= 'Z' => new[] { c - 'A' + 14 },
        _ => null,
    };

    private static bool TryEncodeTriplet(string text, int latchCodeword, Func<char, int[]?> getValues, out byte[] codewords, out string? error)
    {
        codewords = Array.Empty<byte>();
        var valueCostPerChar = new int[text.Length];
        var totalValues = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var values = getValues(text[i]);

            if (values is null)
            {
                error = $"Character '{text[i]}' (0x{(int)text[i]:X2}) is not representable in this Data Matrix encodation mode.";
                return false;
            }

            valueCostPerChar[i] = values.Length;
            totalValues += values.Length;
        }

        // Find the shortest trailing run of characters to exclude from triplet packing so
        // the remaining prefix's value count is an exact multiple of 3 (see file header:
        // a shift-escape's 2 values are never split across the triplet/ASCII-fallback
        // boundary this way, since the boundary is chosen at a character boundary).
        var trailingCharCount = 0;
        var trailingValueCount = 0;

        while ((totalValues - trailingValueCount) % 3 != 0)
        {
            trailingCharCount++;
            trailingValueCount += valueCostPerChar[text.Length - trailingCharCount];
        }

        var packedCharCount = text.Length - trailingCharCount;
        var values2 = new List<int>();

        for (var i = 0; i < packedCharCount; i++)
        {
            values2.AddRange(getValues(text[i])!);
        }

        var result = new List<byte> { (byte)latchCodeword };

        for (var i = 0; i < values2.Count; i += 3)
        {
            var codeword16 = (1600 * values2[i]) + (40 * values2[i + 1]) + values2[i + 2] + 1;
            result.Add((byte)(codeword16 / 256));
            result.Add((byte)(codeword16 % 256));
        }

        result.Add(Unlatch);

        for (var i = packedCharCount; i < text.Length; i++)
        {
            result.Add((byte)(text[i] + 1));
        }

        codewords = result.ToArray();
        error = null;
        return true;
    }
}
