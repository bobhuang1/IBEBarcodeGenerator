namespace IBEBarcode.Core.Encoders;

internal static class EanUpcDigitPatterns
{
    public static readonly int[][] Widths =
    {
        new[] { 3, 2, 1, 1 },
        new[] { 2, 2, 2, 1 },
        new[] { 2, 1, 2, 2 },
        new[] { 1, 4, 1, 1 },
        new[] { 1, 1, 3, 2 },
        new[] { 1, 2, 3, 1 },
        new[] { 1, 1, 1, 4 },
        new[] { 1, 3, 1, 2 },
        new[] { 1, 2, 1, 3 },
        new[] { 3, 1, 1, 2 },
    };

    private static readonly string[] FirstDigitParity =
    {
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL",
    };

    public static string ParityForFirstDigit(int firstDigit) => FirstDigitParity[firstDigit];

    public static void AppendLeftDigit(List<BarSegment> segments, int digit, bool useGCode)
    {
        var widths = Widths[digit];
        var isBar = false;

        for (var i = 0; i < 4; i++)
        {
            var width = useGCode ? widths[3 - i] : widths[i];
            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }

    public static void AppendRightDigit(List<BarSegment> segments, int digit)
    {
        var widths = Widths[digit];
        var isBar = true;

        for (var i = 0; i < 4; i++)
        {
            segments.Add(new BarSegment(isBar, widths[i]));
            isBar = !isBar;
        }
    }

    public static void AppendGuard(List<BarSegment> segments, bool startsWithBar, params int[] widths)
    {
        var isBar = startsWithBar;

        foreach (var width in widths)
        {
            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }

    public static char ComputeCheckDigit(string dataDigits)
    {
        var sum = 0;
        var weight = 3;

        for (var i = dataDigits.Length - 1; i >= 0; i--)
        {
            sum += (dataDigits[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        return (char)('0' + (10 - sum % 10) % 10);
    }
}
