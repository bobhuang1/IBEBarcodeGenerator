using IBEBarcode.Core.Encoders.DataMatrix;

namespace IBEBarcode.Core.Encoders;

public sealed class DataMatrixEncoder : IMatrixBarcodeEncoder
{
    private readonly bool _preferRectangular;

    public DataMatrixEncoder(bool preferRectangular = false)
    {
        _preferRectangular = preferRectangular;
    }

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
        var smallestFittingCapacity = int.MaxValue;

        foreach (var candidate in DataMatrixSymbols.Sizes)
        {
            if (bytes.Length > candidate.DataCapacity || candidate.DataCapacity > smallestFittingCapacity)
            {
                continue;
            }

            var candidateIsRectangular = candidate.InteriorWidth != candidate.InteriorHeight;

            if (candidate.DataCapacity < smallestFittingCapacity || candidateIsRectangular == _preferRectangular)
            {
                size = candidate;
                smallestFittingCapacity = candidate.DataCapacity;
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

        var allCodewords = DataMatrixErrorCorrection.EncodeEcc200(dataCodewords, size);

        // DefaultPlacement always spans the *combined* data area across every region --
        // confirmed directly from ZXing's DataMatrixWriter, which passes
        // symbolInfo.getSymbolDataWidth()/Height() (region count * per-region interior
        // size) to a single DefaultPlacement instance, not one per region. The ISO
        // placement algorithm itself needs no per-region awareness.
        var symbolDataWidth = size.RegionsHorizontal * size.InteriorWidth;
        var symbolDataHeight = size.RegionsVertical * size.InteriorHeight;

        var placement = new DataMatrixPlacement(allCodewords, symbolDataWidth, symbolDataHeight);
        placement.Place();

        var totalWidth = symbolDataWidth + (size.RegionsHorizontal * 2);
        var totalHeight = symbolDataHeight + (size.RegionsVertical * 2);
        var modules = new bool[totalWidth, totalHeight];

        var matrixY = 0;

        for (var y = 0; y < symbolDataHeight; y++)
        {
            if (y % size.InteriorHeight == 0)
            {
                for (var x = 0; x < totalWidth; x++)
                {
                    modules[x, matrixY] = x % 2 == 0;
                }

                matrixY++;
            }

            var matrixX = 0;

            for (var x = 0; x < symbolDataWidth; x++)
            {
                if (x % size.InteriorWidth == 0)
                {
                    modules[matrixX, matrixY] = true;
                    matrixX++;
                }

                modules[matrixX, matrixY] = placement.GetBit(x, y);
                matrixX++;

                if (x % size.InteriorWidth == size.InteriorWidth - 1)
                {
                    modules[matrixX, matrixY] = y % 2 == 0;
                    matrixX++;
                }
            }

            matrixY++;

            if (y % size.InteriorHeight == size.InteriorHeight - 1)
            {
                for (var x = 0; x < totalWidth; x++)
                {
                    modules[x, matrixY] = true;
                }

                matrixY++;
            }
        }

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }
}
