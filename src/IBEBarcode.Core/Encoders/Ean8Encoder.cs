namespace IBEBarcode.Core.Encoders;

public sealed class Ean8Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Ean8;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value) || (value.Length != 7 && value.Length != 8))
        {
            error = "EAN-8 requires 7 data digits, optionally followed by the check digit (8 total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; EAN-8 encodes digits only.";
                return false;
            }
        }

        var dataDigits = value[..7];
        var checkDigit = EanUpcDigitPatterns.ComputeCheckDigit(dataDigits);

        if (value.Length == 8 && value[7] != checkDigit)
        {
            error = $"Invalid EAN-8 check digit: expected '{checkDigit}', got '{value[7]}'.";
            return false;
        }

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        for (var i = 0; i < 4; i++)
        {
            EanUpcDigitPatterns.AppendLeftDigit(segments, dataDigits[i] - '0', useGCode: false);
        }

        EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1, 1, 1, 1);

        for (var i = 4; i < 7; i++)
        {
            EanUpcDigitPatterns.AppendRightDigit(segments, dataDigits[i] - '0');
        }

        EanUpcDigitPatterns.AppendRightDigit(segments, checkDigit - '0');
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        var fullValue = dataDigits + checkDigit;
        pattern = BarcodePattern.Create(fullValue, segments, fullValue);
        error = null;
        return true;
    }
}
