namespace IBEBarcode.Core.Encoders;

public sealed class PostnetEncoder : IHeightVaryingBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Postnet;

    private static readonly bool[][] DigitPatterns =
    {
        new[] { true, true, false, false, false },
        new[] { false, false, false, true, true },
        new[] { false, false, true, false, true },
        new[] { false, false, true, true, false },
        new[] { false, true, false, false, true },
        new[] { false, true, false, true, false },
        new[] { false, true, true, false, false },
        new[] { true, false, false, false, true },
        new[] { true, false, false, true, false },
        new[] { true, false, true, false, false },
    };

    public bool TryEncode(string value, out HeightBarPattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 5 or 9 or 11 })
        {
            error = "Postnet requires 5 (ZIP), 9 (ZIP+4), or 11 (delivery point) digits.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; Postnet encodes digits only.";
                return false;
            }
        }

        var sum = 0;
        foreach (var ch in value)
        {
            sum += ch - '0';
        }

        var checkDigit = (10 - sum % 10) % 10;

        var bars = new List<HeightBar> { new(true) };

        foreach (var ch in value)
        {
            AppendDigit(bars, ch - '0');
        }

        AppendDigit(bars, checkDigit);
        bars.Add(new HeightBar(true));

        var fullValue = value + checkDigit;
        pattern = HeightBarPattern.Create(fullValue, bars);
        error = null;
        return true;
    }

    private static void AppendDigit(List<HeightBar> bars, int digit)
    {
        foreach (var isTall in DigitPatterns[digit])
        {
            bars.Add(new HeightBar(isTall));
        }
    }
}
