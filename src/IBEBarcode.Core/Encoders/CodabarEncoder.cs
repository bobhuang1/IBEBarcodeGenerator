namespace IBEBarcode.Core.Encoders;

public sealed class CodabarEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Codabar;

    private const string Alphabet = "0123456789-$:/.+ABCD";

    private static readonly int[] CharacterPatterns =
    {
        0x003, 0x006, 0x009, 0x060, 0x012, 0x042, 0x021, 0x024, 0x030, 0x048,
        0x00C, 0x018, 0x045, 0x051, 0x054, 0x015, 0x01A, 0x029, 0x00B, 0x00E,
    };

    private const int NarrowWidth = 1;
    private const int WideWidth = 2;
    private const int InterCharacterGapWidth = 1;
    private const char StartStopChar = 'A';

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var upper = value.ToUpperInvariant();
        var segments = new List<BarSegment>();

        AppendCharacter(segments, LookupPattern(StartStopChar)!.Value);
        segments.Add(new BarSegment(false, InterCharacterGapWidth));

        foreach (var ch in upper)
        {
            var bitPattern = LookupPattern(ch);
            if (bitPattern is null)
            {
                error = $"Character '{ch}' is not valid in Codabar.";
                return false;
            }

            AppendCharacter(segments, bitPattern.Value);
            segments.Add(new BarSegment(false, InterCharacterGapWidth));
        }

        AppendCharacter(segments, LookupPattern(StartStopChar)!.Value);

        pattern = BarcodePattern.Create(value, segments, $"{StartStopChar}{upper}{StartStopChar}");
        error = null;
        return true;
    }

    private static int? LookupPattern(char ch)
    {
        var index = Alphabet.IndexOf(ch);
        return index < 0 ? null : CharacterPatterns[index];
    }

    private static void AppendCharacter(List<BarSegment> segments, int bitPattern)
    {
        for (var bitIndex = 6; bitIndex >= 0; bitIndex--)
        {
            var isWide = (bitPattern & (1 << bitIndex)) != 0;
            var isBar = bitIndex % 2 == 0;
            segments.Add(new BarSegment(isBar, isWide ? WideWidth : NarrowWidth));
        }
    }
}
