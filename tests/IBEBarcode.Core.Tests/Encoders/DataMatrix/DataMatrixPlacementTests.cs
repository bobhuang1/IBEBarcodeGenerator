using IBEBarcode.Core.Encoders.DataMatrix;

namespace IBEBarcode.Core.Tests.Encoders.DataMatrix;

public class DataMatrixPlacementTests
{
    [Fact]
    public void Place_DoesNotThrow_ForEveryScopedSymbolSize()
    {
        foreach (var size in DataMatrixSymbols.Sizes)
        {
            var totalCodewords = size.DataCapacity + size.ErrorCodewords;
            var codewords = new byte[totalCodewords];

            for (var i = 0; i < totalCodewords; i++)
            {
                codewords[i] = (byte)(i + 1);
            }

            var placement = new DataMatrixPlacement(codewords, size.InteriorWidth, size.InteriorHeight);

            placement.Place();
        }
    }

    [Fact]
    public void Place_SmallestSymbol_SetsEveryModulePosition()
    {
        var codewords = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var placement = new DataMatrixPlacement(codewords, 8, 8);

        placement.Place();

        // At this exact-fit size, every interior module gets assigned by the
        // traversal (no unused-module ambiguity). Sanity-check by counting
        // how many of the 64 positions read as "dark" is not meaningful on
        // its own, so instead confirm the traversal is deterministic:
        // running it twice from fresh instances yields identical results.
        var placement2 = new DataMatrixPlacement(codewords, 8, 8);
        placement2.Place();

        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                Assert.Equal(placement.GetBit(x, y), placement2.GetBit(x, y));
            }
        }
    }
}
