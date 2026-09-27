namespace IBEBarcode.Core.Encoders;

public sealed class Code39Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code39;

    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    private static readonly int[] CharacterPatterns =
    {
        0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064,
        0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C,
        0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016,
        0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x0A8,
        0x0A2, 0x08A, 0x02A,
    };

    private const int StartStopPattern = 0x094;
    private const int NarrowWidth = 1;
    private const int WideWidth = 2;
    private const int InterCharacterGapWidth = 1;

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

        AppendCharacter(segments, StartStopPattern);
        segments.Add(new BarSegment(false, InterCharacterGapWidth));

        foreach (var ch in upper)
        {
            var index = Alphabet.IndexOf(ch);
            if (index < 0)
            {
                error = $"Character '{ch}' is not valid in Code 39.";
                return false;
            }

            AppendCharacter(segments, CharacterPatterns[index]);
            segments.Add(new BarSegment(false, InterCharacterGapWidth));
        }

        AppendCharacter(segments, StartStopPattern);

        pattern = BarcodePattern.Create(value, segments, $"*{upper}*");
        error = null;
        return true;
    }

    private static void AppendCharacter(List<BarSegment> segments, int bitPattern)
    {
        for (var bitIndex = 8; bitIndex >= 0; bitIndex--)
        {
            var isWide = (bitPattern & (1 << bitIndex)) != 0;
            var isBar = bitIndex % 2 == 0;
            segments.Add(new BarSegment(isBar, isWide ? WideWidth : NarrowWidth));
        }
    }
}
