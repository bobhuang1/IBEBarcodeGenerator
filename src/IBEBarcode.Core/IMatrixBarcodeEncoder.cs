namespace IBEBarcode.Core;

public interface IMatrixBarcodeEncoder
{
    BarcodeSymbology Symbology { get; }

    bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error);
}
