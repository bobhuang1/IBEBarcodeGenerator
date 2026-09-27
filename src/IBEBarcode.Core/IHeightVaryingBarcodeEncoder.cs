namespace IBEBarcode.Core;

public interface IHeightVaryingBarcodeEncoder
{
    BarcodeSymbology Symbology { get; }

    bool TryEncode(string value, out HeightBarPattern? pattern, out string? error);
}
