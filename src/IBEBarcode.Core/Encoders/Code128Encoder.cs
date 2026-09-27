namespace IBEBarcode.Core.Encoders;

public sealed class Code128Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code128;

    private const int StartB = 104;
    private const int Stop = 106;

    private static readonly int[][] Patterns =
    {
        new[] { 2, 1, 2, 2, 2, 2 }, new[] { 2, 2, 2, 1, 2, 2 }, new[] { 2, 2, 2, 2, 2, 1 }, new[] { 1, 2, 1, 2, 2, 3 }, new[] { 1, 2, 1, 3, 2, 2 },
        new[] { 1, 3, 1, 2, 2, 2 }, new[] { 1, 2, 2, 2, 1, 3 }, new[] { 1, 2, 2, 3, 1, 2 }, new[] { 1, 3, 2, 2, 1, 2 }, new[] { 2, 2, 1, 2, 1, 3 },
        new[] { 2, 2, 1, 3, 1, 2 }, new[] { 2, 3, 1, 2, 1, 2 }, new[] { 1, 1, 2, 2, 3, 2 }, new[] { 1, 2, 2, 1, 3, 2 }, new[] { 1, 2, 2, 2, 3, 1 },
        new[] { 1, 1, 3, 2, 2, 2 }, new[] { 1, 2, 3, 1, 2, 2 }, new[] { 1, 2, 3, 2, 2, 1 }, new[] { 2, 2, 3, 2, 1, 1 }, new[] { 2, 2, 1, 1, 3, 2 },
        new[] { 2, 2, 1, 2, 3, 1 }, new[] { 2, 1, 3, 2, 1, 2 }, new[] { 2, 2, 3, 1, 1, 2 }, new[] { 3, 1, 2, 1, 3, 1 }, new[] { 3, 1, 1, 2, 2, 2 },
        new[] { 3, 2, 1, 1, 2, 2 }, new[] { 3, 2, 1, 2, 2, 1 }, new[] { 3, 1, 2, 2, 1, 2 }, new[] { 3, 2, 2, 1, 1, 2 }, new[] { 3, 2, 2, 2, 1, 1 },
        new[] { 2, 1, 2, 1, 2, 3 }, new[] { 2, 1, 2, 3, 2, 1 }, new[] { 2, 3, 2, 1, 2, 1 }, new[] { 1, 1, 1, 3, 2, 3 }, new[] { 1, 3, 1, 1, 2, 3 },
        new[] { 1, 3, 1, 3, 2, 1 }, new[] { 1, 1, 2, 3, 1, 3 }, new[] { 1, 3, 2, 1, 1, 3 }, new[] { 1, 3, 2, 3, 1, 1 }, new[] { 2, 1, 1, 3, 1, 3 },
        new[] { 2, 3, 1, 1, 1, 3 }, new[] { 2, 3, 1, 3, 1, 1 }, new[] { 1, 1, 2, 1, 3, 3 }, new[] { 1, 1, 2, 3, 3, 1 }, new[] { 1, 3, 2, 1, 3, 1 },
        new[] { 1, 1, 3, 1, 2, 3 }, new[] { 1, 1, 3, 3, 2, 1 }, new[] { 1, 3, 3, 1, 2, 1 }, new[] { 3, 1, 3, 1, 2, 1 }, new[] { 2, 1, 1, 3, 3, 1 },
        new[] { 2, 3, 1, 1, 3, 1 }, new[] { 2, 1, 3, 1, 1, 3 }, new[] { 2, 1, 3, 3, 1, 1 }, new[] { 2, 1, 3, 1, 3, 1 }, new[] { 3, 1, 1, 1, 2, 3 },
        new[] { 3, 1, 1, 3, 2, 1 }, new[] { 3, 3, 1, 1, 2, 1 }, new[] { 3, 1, 2, 1, 1, 3 }, new[] { 3, 1, 2, 3, 1, 1 }, new[] { 3, 3, 2, 1, 1, 1 },
        new[] { 3, 1, 4, 1, 1, 1 }, new[] { 2, 2, 1, 4, 1, 1 }, new[] { 4, 3, 1, 1, 1, 1 }, new[] { 1, 1, 1, 2, 2, 4 }, new[] { 1, 1, 1, 4, 2, 2 },
        new[] { 1, 2, 1, 1, 2, 4 }, new[] { 1, 2, 1, 4, 2, 1 }, new[] { 1, 4, 1, 1, 2, 2 }, new[] { 1, 4, 1, 2, 2, 1 }, new[] { 1, 1, 2, 2, 1, 4 },
        new[] { 1, 1, 2, 4, 1, 2 }, new[] { 1, 2, 2, 1, 1, 4 }, new[] { 1, 2, 2, 4, 1, 1 }, new[] { 1, 4, 2, 1, 1, 2 }, new[] { 1, 4, 2, 2, 1, 1 },
        new[] { 2, 4, 1, 2, 1, 1 }, new[] { 2, 2, 1, 1, 1, 4 }, new[] { 4, 1, 3, 1, 1, 1 }, new[] { 2, 4, 1, 1, 1, 2 }, new[] { 1, 3, 4, 1, 1, 1 },
        new[] { 1, 1, 1, 2, 4, 2 }, new[] { 1, 2, 1, 1, 4, 2 }, new[] { 1, 2, 1, 2, 4, 1 }, new[] { 1, 1, 4, 2, 1, 2 }, new[] { 1, 2, 4, 1, 1, 2 },
        new[] { 1, 2, 4, 2, 1, 1 }, new[] { 4, 1, 1, 2, 1, 2 }, new[] { 4, 2, 1, 1, 1, 2 }, new[] { 4, 2, 1, 2, 1, 1 }, new[] { 2, 1, 2, 1, 4, 1 },
        new[] { 2, 1, 4, 1, 2, 1 }, new[] { 4, 1, 2, 1, 2, 1 }, new[] { 1, 1, 1, 1, 4, 3 }, new[] { 1, 1, 1, 3, 4, 1 }, new[] { 1, 3, 1, 1, 4, 1 },
        new[] { 1, 1, 4, 1, 1, 3 }, new[] { 1, 1, 4, 3, 1, 1 }, new[] { 4, 1, 1, 1, 1, 3 }, new[] { 4, 1, 1, 3, 1, 1 }, new[] { 1, 1, 3, 1, 4, 1 },
        new[] { 1, 1, 4, 1, 3, 1 }, new[] { 3, 1, 1, 1, 4, 1 }, new[] { 4, 1, 1, 1, 3, 1 }, new[] { 2, 1, 1, 4, 1, 2 }, new[] { 2, 1, 1, 2, 1, 4 },
        new[] { 2, 1, 1, 2, 3, 2 }, new[] { 2, 3, 3, 1, 1, 1, 2 },
    };

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var values = new int[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (ch < ' ' || ch > (char)127)
            {
                error = $"Character '{ch}' is outside the ASCII 32-127 range Code 128 Set B supports.";
                return false;
            }

            values[i] = ch - ' ';
        }

        var checkSum = StartB;
        var weight = 1;

        foreach (var v in values)
        {
            checkSum += v * weight;
            weight++;
        }

        checkSum %= 103;

        var segments = new List<BarSegment>();
        AppendSymbol(segments, StartB);

        foreach (var v in values)
        {
            AppendSymbol(segments, v);
        }

        AppendSymbol(segments, checkSum);
        AppendSymbol(segments, Stop);

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }

    private static void AppendSymbol(List<BarSegment> segments, int symbolValue)
    {
        var widths = Patterns[symbolValue];
        var isBar = true;

        foreach (var width in widths)
        {
            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }
}
