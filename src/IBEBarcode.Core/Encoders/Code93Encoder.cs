namespace IBEBarcode.Core.Encoders;

public sealed class Code93Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code93;

    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    private static readonly int[] CharacterPatterns =
    {
        0x114, 0x148, 0x144, 0x142, 0x128, 0x124, 0x122, 0x150, 0x112, 0x10A,
        0x1A8, 0x1A4, 0x1A2, 0x194, 0x192, 0x18A, 0x168, 0x164, 0x162, 0x134,
        0x11A, 0x158, 0x14C, 0x146, 0x12C, 0x116, 0x1B4, 0x1B2, 0x1AC, 0x1A6,
        0x196, 0x19A, 0x16C, 0x166, 0x136, 0x13A,
        0x12E, 0x1D4, 0x1D2, 0x1CA, 0x16E, 0x176, 0x1AE,
    };

    private const int StartStopPattern = 0x15E;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var upper = value.ToUpperInvariant();
        var values = new int[upper.Length];

        for (var i = 0; i < upper.Length; i++)
        {
            var index = Alphabet.IndexOf(upper[i]);
            if (index < 0)
            {
                error = $"Character '{upper[i]}' is not valid in Code 93.";
                return false;
            }

            values[i] = index;
        }

        var cValue = WeightedChecksum(values, 20);
        var withC = values.Append(cValue).ToArray();
        var kValue = WeightedChecksum(withC, 15);

        var segments = new List<BarSegment>();
        AppendCharacter(segments, StartStopPattern);

        foreach (var v in values)
        {
            AppendCharacter(segments, CharacterPatterns[v]);
        }

        AppendCharacter(segments, CharacterPatterns[cValue]);
        AppendCharacter(segments, CharacterPatterns[kValue]);
        AppendCharacter(segments, StartStopPattern);

        var humanReadable = $"*{upper}{Alphabet[cValue]}{Alphabet[kValue]}*";
        pattern = BarcodePattern.Create(upper, segments, humanReadable);
        error = null;
        return true;
    }

    private static int WeightedChecksum(int[] values, int maxWeight)
    {
        var sum = 0;
        var weight = 1;

        for (var i = values.Length - 1; i >= 0; i--)
        {
            sum += values[i] * weight;
            weight = weight == maxWeight ? 1 : weight + 1;
        }

        return sum % 47;
    }

    private static void AppendCharacter(List<BarSegment> segments, int bitPattern)
    {
        var isBar = true;
        var bitIndex = 8;

        while (bitIndex >= 0)
        {
            var expected = isBar ? 1 : 0;
            var width = 0;

            while (bitIndex >= 0 && ((bitPattern >> bitIndex) & 1) == expected)
            {
                width++;
                bitIndex--;
            }

            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }
}
