using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Tests.Encoders.Qr;

public class QrMaskUtilTests
{
    // Cross-checked against this project's own already-verified, previously-hardcoded
    // mask-0 format strings (used throughout the original QR implementation before mask
    // scoring existed) -- these are independently known-correct values, not derived from
    // the same BCH code being tested here.
    [Theory]
    [InlineData('L', "111011111000100")]
    [InlineData('M', "101010000010010")]
    [InlineData('Q', "011010101011111")]
    [InlineData('H', "001011010001001")]
    public void ComputeFormatString_Mask0_MatchesPreviouslyHardcodedValues(char level, string expected)
    {
        var formatString = QrMaskUtil.ComputeFormatString(level, 0);

        Assert.Equal(expected, formatString);
    }

    [Fact]
    public void GetDataMaskBit_Mask0_MatchesXorParityFormula()
    {
        // mask 0: (x+y) % 2 == 0 -> masked (this project's original single-mask formula).
        Assert.True(QrMaskUtil.GetDataMaskBit(0, 0, 0));
        Assert.False(QrMaskUtil.GetDataMaskBit(0, 1, 0));
        Assert.True(QrMaskUtil.GetDataMaskBit(0, 1, 1));
    }

    [Fact]
    public void ApplyMaskPenaltyRule4_PerfectlyBalancedMatrix_ScoresZero()
    {
        var modules = new bool[10, 10];

        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                modules[x, y] = true;
            }
        }

        var penalty = QrMaskUtil.ApplyMaskPenaltyRule4(modules);

        Assert.Equal(0, penalty);
    }

    [Fact]
    public void ApplyMaskPenaltyRule1_LongRunOfSameColor_IsPenalized()
    {
        var modules = new bool[10, 1];

        for (var x = 0; x < 6; x++)
        {
            modules[x, 0] = true;
        }

        var penalty = QrMaskUtil.ApplyMaskPenaltyRule1(modules);

        // Run of 6: N1(3) + (6-5) = 4.
        Assert.Equal(4, penalty);
    }
}
