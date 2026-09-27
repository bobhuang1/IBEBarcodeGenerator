namespace IBEBarcode.Core.Encoders;

public sealed class IsbnEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Isbn;

    private readonly Ean13Encoder _ean13 = new();

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        var digitsOnly = value.Replace("-", "");

        if (digitsOnly.Length != 9 && digitsOnly.Length != 10)
        {
            error = "ISBN requires the first 9 digits of an ISBN-10 (its own check character is ignored and replaced by a freshly computed EAN-13 check digit).";
            return false;
        }

        var isbnCore = digitsOnly[..9];

        foreach (var ch in isbnCore)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; the first 9 ISBN characters must be digits.";
                return false;
            }
        }

        if (!_ean13.TryEncode("978" + isbnCore, out var ean13Pattern, out error))
        {
            return false;
        }

        pattern = ean13Pattern;
        error = null;
        return true;
    }
}
