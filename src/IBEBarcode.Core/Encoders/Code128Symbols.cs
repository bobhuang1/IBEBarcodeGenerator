namespace IBEBarcode.Core.Encoders;

internal static class Code128Symbols
{
    public const int CodeC = 99;
    public const int CodeB = 100;
    public const int CodeA = 101;
    public const int Fnc1 = 102;
    public const int StartA = 103;
    public const int StartB = 104;
    public const int StartC = 105;
    public const int Stop = 106;

    public static readonly int[][] Patterns =
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

    public static void AppendSymbol(List<BarSegment> segments, int symbolValue)
    {
        var widths = Patterns[symbolValue];
        var isBar = true;

        foreach (var width in widths)
        {
            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }

    public static bool TryEncodeSetA(string value, out int[] values, out string? error)
    {
        values = new int[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];

            if (ch is >= (char)32 and <= (char)95)
            {
                values[i] = ch - 32;
            }
            else if (ch is >= (char)0 and <= (char)31)
            {
                values[i] = ch + 64;
            }
            else
            {
                error = $"Character '{ch}' is outside the ASCII 0-31/32-95 range Code 128 Set A supports.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public static bool TryEncodeSetB(string value, out int[] values, out string? error)
    {
        values = new int[value.Length];

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

        error = null;
        return true;
    }

    public static bool TryEncodeSetC(string value, out int[] values, out string? error)
    {
        if (value.Length % 2 != 0)
        {
            values = Array.Empty<int>();
            error = "Code 128 Set C requires an even number of digits (each symbol encodes a pair).";
            return false;
        }

        values = new int[value.Length / 2];

        for (var i = 0; i < value.Length; i += 2)
        {
            var a = value[i];
            var b = value[i + 1];

            if (a is < '0' or > '9' || b is < '0' or > '9')
            {
                error = $"Characters '{a}{b}' are not both digits; Code 128 Set C encodes digit pairs only.";
                return false;
            }

            values[i / 2] = (a - '0') * 10 + (b - '0');
        }

        error = null;
        return true;
    }

    public static int ComputeChecksum(int startValue, int[] dataValues)
    {
        var checkSum = startValue;
        var weight = 1;

        foreach (var v in dataValues)
        {
            checkSum += v * weight;
            weight++;
        }

        return checkSum % 103;
    }
}
