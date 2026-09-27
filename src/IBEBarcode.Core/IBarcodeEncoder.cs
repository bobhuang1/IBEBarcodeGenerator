namespace IBEBarcode.Core;

public interface IBarcodeEncoder
{
    BarcodeSymbology Symbology { get; }

    bool TryEncode(string value, out BarcodePattern? pattern, out string? error);
}
