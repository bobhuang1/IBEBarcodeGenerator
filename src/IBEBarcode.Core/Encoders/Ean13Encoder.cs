namespace IBEBarcode.Core.Encoders;

public sealed class Ean13Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Ean13;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value) || (value.Length != 12 && value.Length != 13))
        {
            error = "EAN-13 requires 12 data digits, optionally followed by the check digit (13 total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; EAN-13 encodes digits only.";
                return false;
            }
        }

        var dataDigits = value[..12];
        var checkDigit = EanUpcDigitPatterns.ComputeCheckDigit(dataDigits);

        if (value.Length == 13 && value[12] != checkDigit)
        {
            error = $"Invalid EAN-13 check digit: expected '{checkDigit}', got '{value[12]}'.";
            return false;
        }

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        var firstDigit = dataDigits[0] - '0';
        var parity = EanUpcDigitPatterns.ParityForFirstDigit(firstDigit);

        for (var i = 1; i < 7; i++)
        {
            EanUpcDigitPatterns.AppendLeftDigit(segments, dataDigits[i] - '0', parity[i - 1] == 'G');
        }

        EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1, 1, 1, 1);

        for (var i = 7; i < 12; i++)
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
