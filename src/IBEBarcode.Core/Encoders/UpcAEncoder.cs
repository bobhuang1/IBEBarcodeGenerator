namespace IBEBarcode.Core.Encoders;

public sealed class UpcAEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.UpcA;

    private readonly Ean13Encoder _ean13 = new();

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value) || (value.Length != 11 && value.Length != 12))
        {
            error = "UPC-A requires 11 data digits, optionally followed by the check digit (12 total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; UPC-A encodes digits only.";
                return false;
            }
        }

        if (!_ean13.TryEncode("0" + value, out var ean13Pattern, out var innerError))
        {
            error = innerError?.Replace("EAN-13", "UPC-A");
            return false;
        }

        var upcValue = ean13Pattern!.Value[1..];
        pattern = BarcodePattern.Create(upcValue, ean13Pattern.Segments, upcValue);
        error = null;
        return true;
    }
}
