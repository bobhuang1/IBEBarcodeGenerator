namespace IBEBarcode.Core.Encoders;

public sealed class Upc2DigitSupplementEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Upc2DigitSupplement;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 2 })
        {
            error = "The 2-digit UPC/EAN supplement requires exactly 2 digits.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; the 2-digit supplement encodes digits only.";
                return false;
            }
        }

        var numericValue = (value[0] - '0') * 10 + (value[1] - '0');
        var parityBits = numericValue % 4;

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 2);

        for (var i = 0; i < 2; i++)
        {
            var digit = value[i] - '0';
            var bitPosition = 1 - i;
            var useGCode = ((parityBits >> bitPosition) & 1) != 0;
            EanUpcDigitPatterns.AppendLeftDigit(segments, digit, useGCode);

            if (i < 1)
            {
                EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1);
            }
        }

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }
}
