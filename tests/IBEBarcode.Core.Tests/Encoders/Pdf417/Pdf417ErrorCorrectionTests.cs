using IBEBarcode.Core.Encoders.Pdf417;

namespace IBEBarcode.Core.Tests.Encoders.Pdf417;

public class Pdf417ErrorCorrectionTests
{
    [Fact]
    public void CodewordTable_HasThreeClustersOf929Patterns()
    {
        Assert.Equal(3, Pdf417CodewordTable.Patterns.Length);

        foreach (var cluster in Pdf417CodewordTable.Patterns)
        {
            Assert.Equal(929, cluster.Length);
        }
    }

    [Fact]
    public void ErrorCorrectionCoefficients_HasLevelsZeroToFiveWithCorrectLengths()
    {
        var expected = new[] { 2, 4, 8, 16, 32, 64 };

        Assert.Equal(6, Pdf417ErrorCorrectionCoefficients.Levels.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], Pdf417ErrorCorrectionCoefficients.Levels[i].Length);
        }
    }

    [Fact]
    public void Generate_ProducesCorrectCountAndIsDeterministic()
    {
        var data = new[] { 5, 900, 12, 34 };

        var ec = Pdf417ErrorCorrection.Generate(data, 2);
        var ecAgain = Pdf417ErrorCorrection.Generate(data, 2);

        Assert.Equal(8, ec.Length);
        Assert.Equal(ec, ecAgain);

        foreach (var codeword in ec)
        {
            Assert.InRange(codeword, 0, 928);
        }
    }
}
