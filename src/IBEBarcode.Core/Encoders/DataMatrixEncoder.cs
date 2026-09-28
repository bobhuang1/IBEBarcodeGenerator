using IBEBarcode.Core.Encoders.DataMatrix;

namespace IBEBarcode.Core.Encoders;

public sealed class DataMatrixEncoder : IMatrixBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.DataMatrix;

    public bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error)
    {
        matrix = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var bytes = new byte[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 127)
            {
                error = $"Character '{value[i]}' is outside the ASCII 0-127 range this Data Matrix encoder supports.";
                return false;
            }

            bytes[i] = (byte)(value[i] + 1);
        }

        DataMatrixSymbols.SymbolSize? size = null;

        foreach (var candidate in DataMatrixSymbols.Sizes)
        {
            if (bytes.Length <= candidate.DataCapacity)
            {
                size = candidate;
                break;
            }
        }

        if (size is null)
        {
            error = $"Value is too large to encode: this Data Matrix encoder supports up to {DataMatrixSymbols.Sizes[^1].DataCapacity} ASCII characters.";
            return false;
        }

        var dataCodewords = new byte[size.DataCapacity];
        Array.Copy(bytes, dataCodewords, bytes.Length);

        if (bytes.Length < size.DataCapacity)
        {
            dataCodewords[bytes.Length] = 129;

            for (var i = bytes.Length + 1; i < size.DataCapacity; i++)
            {
                var position = i - bytes.Length;
                var pseudoRandom = ((149 * position) % 253) + 1;
                var temp = 129 + pseudoRandom;
                dataCodewords[i] = (byte)(temp <= 254 ? temp : temp - 254);
            }
        }

        var eccCodewords = DataMatrixErrorCorrection.ComputeEcc(dataCodewords, size.ErrorCodewords, size.EccPoly);

        var allCodewords = new byte[size.DataCapacity + size.ErrorCodewords];
        Array.Copy(dataCodewords, allCodewords, size.DataCapacity);
        Array.Copy(eccCodewords, 0, allCodewords, size.DataCapacity, size.ErrorCodewords);

        var placement = new DataMatrixPlacement(allCodewords, size.InteriorSize, size.InteriorSize);
        placement.Place();

        var totalSize = size.InteriorSize + 2;
        var modules = new bool[totalSize, totalSize];

        for (var col = 0; col < totalSize; col++)
        {
            modules[col, 0] = col % 2 == 0;
            modules[col, totalSize - 1] = true;
        }

        for (var y = 0; y < size.InteriorSize; y++)
        {
            var outputRow = y + 1;
            modules[0, outputRow] = true;

            for (var x = 0; x < size.InteriorSize; x++)
            {
                modules[x + 1, outputRow] = placement.GetBit(x, y);
            }

            modules[size.InteriorSize + 1, outputRow] = y % 2 == 0;
        }

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }
}
