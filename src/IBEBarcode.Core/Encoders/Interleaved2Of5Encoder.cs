namespace IBEBarcode.Core.Encoders;

public sealed class Interleaved2Of5Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Interleaved2Of5;

    private static readonly bool[][] DigitPatterns =
    {
        new[] { false, false, true, true, false },
        new[] { true, false, false, false, true },
        new[] { false, true, false, false, true },
        new[] { true, true, false, false, false },
        new[] { false, false, true, false, true },
        new[] { true, false, true, false, false },
        new[] { false, true, true, false, false },
        new[] { false, false, false, true, true },
        new[] { true, false, false, true, false },
        new[] { false, true, false, true, false },
    };

    private const int NarrowWidth = 1;
    private const int WideWidth = 2;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        if (value.Length % 2 != 0)
        {
            error = "Interleaved 2 of 5 requires an even number of digits; pad with a leading zero.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; Interleaved 2 of 5 encodes digits only.";
                return false;
            }
        }

        var segments = new List<BarSegment>
        {
            new(true, NarrowWidth),
            new(false, NarrowWidth),
            new(true, NarrowWidth),
            new(false, NarrowWidth),
        };

        for (var i = 0; i < value.Length; i += 2)
        {
            var barDigit = DigitPatterns[value[i] - '0'];
            var spaceDigit = DigitPatterns[value[i + 1] - '0'];

            for (var element = 0; element < 5; element++)
            {
                segments.Add(new BarSegment(true, barDigit[element] ? WideWidth : NarrowWidth));
                segments.Add(new BarSegment(false, spaceDigit[element] ? WideWidth : NarrowWidth));
            }
        }

        segments.Add(new BarSegment(true, WideWidth));
        segments.Add(new BarSegment(false, NarrowWidth));
        segments.Add(new BarSegment(true, NarrowWidth));

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }
}
