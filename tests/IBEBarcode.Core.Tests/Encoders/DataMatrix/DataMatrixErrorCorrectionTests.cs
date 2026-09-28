using IBEBarcode.Core.Encoders.DataMatrix;

namespace IBEBarcode.Core.Tests.Encoders.DataMatrix;

public class DataMatrixErrorCorrectionTests
{
    [Fact]
    public void ComputeEcc_KnownDataCodewords_ProducesDeterministicOutputOfCorrectLength()
    {
        var pseudoRandom = ((149 * 1) % 253) + 1;
        var padWord = 129 + pseudoRandom;
        var data = new byte[] { 66, 129, (byte)(padWord <= 254 ? padWord : padWord - 254) };

        var ecc = DataMatrixErrorCorrection.ComputeEcc(data, 5, DataMatrixSymbols.Sizes[0].EccPoly);

        Assert.Equal(5, ecc.Length);
        var eccAgain = DataMatrixErrorCorrection.ComputeEcc(data, 5, DataMatrixSymbols.Sizes[0].EccPoly);
        Assert.Equal(ecc, eccAgain);
    }

    [Fact]
    public void Sizes_AreOrderedByNonDescendingCapacity()
    {
        // Ties are allowed (a rectangular size can share a square size's dataCapacity,
        // e.g. both the square 10x10 and rectangular 16x6 sizes have capacity 5), but the
        // list must never go backwards, and DataMatrixEncoder relies on the square variant
        // being listed first within a tie so it wins by default.
        var sizes = DataMatrixSymbols.Sizes;

        for (var i = 1; i < sizes.Count; i++)
        {
            Assert.True(sizes[i].DataCapacity >= sizes[i - 1].DataCapacity);

            if (sizes[i].DataCapacity == sizes[i - 1].DataCapacity)
            {
                var previousIsSquare = sizes[i - 1].InteriorWidth == sizes[i - 1].InteriorHeight;
                var currentIsSquare = sizes[i].InteriorWidth == sizes[i].InteriorHeight;
                Assert.True(previousIsSquare && !currentIsSquare);
            }
        }
    }

    [Fact]
    public void Sizes_EccPolyLengthMatchesErrorCodewordCount()
    {
        foreach (var size in DataMatrixSymbols.Sizes)
        {
            Assert.Equal(size.ErrorCodewords, size.EccPoly.Length);
        }
    }
}
