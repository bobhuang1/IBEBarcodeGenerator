using IBEBarcode.Core.Encoders.Pdf417;

namespace IBEBarcode.Core.Tests.Encoders.Pdf417;

public class Pdf417NumericCompactionTests
{
    [Fact]
    public void EncodeDigits_ShortDigitString_MatchesHandComputedCodewords()
    {
        // "1234" -> prepend "1" -> 11234 -> base 900: 11234 = 12*900 + 434.
        var codewords = Pdf417NumericCompaction.EncodeDigits("1234");

        Assert.Equal(new[] { 902, 12, 434 }, codewords);
    }

    [Fact]
    public void EncodeDigits_LongDigitString_SplitsIntoFortyFourDigitChunks()
    {
        var digits = new string('7', 50);

        var codewords = Pdf417NumericCompaction.EncodeDigits(digits);

        // Latch codeword, then at least 2 chunks worth of codewords (44 + 6 digits).
        Assert.Equal(902, codewords[0]);
        Assert.True(codewords.Length > 1);

        foreach (var codeword in codewords.Skip(1))
        {
            Assert.InRange(codeword, 0, 899);
        }
    }
}
