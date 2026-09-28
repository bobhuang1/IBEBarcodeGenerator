using IBEBarcode.Core.Encoders.Pdf417;

namespace IBEBarcode.Core.Tests.Encoders.Pdf417;

public class Pdf417TextCompactionTests
{
    [Fact]
    public void TryEncodeText_TwoUppercaseLetters_MatchesHandComputedCodewords()
    {
        // 'A'->0, 'B'->1 (Alpha submode offsets); packed as 0*30+1=1.
        var success = Pdf417TextCompaction.TryEncodeText("AB", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(new[] { 900, 1 }, codewords);
    }

    [Fact]
    public void TryEncodeText_UppercaseThenDigit_MatchesHandComputedCodewords()
    {
        // 'A'->0 (Alpha). '1' isn't alpha, is Mixed -> latch-to-mixed(28), then '1'->1 (Mixed
        // table index 1). tmp=[0,28,1]: pair(0,28)->28; trailing 1 padded with ps(29):
        // 1*30+29=59.
        var success = Pdf417TextCompaction.TryEncodeText("A1", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(new[] { 900, 28, 59 }, codewords);
    }

    [Fact]
    public void TryEncodeText_UnsupportedCharacter_ReturnsError()
    {
        var success = Pdf417TextCompaction.TryEncodeText("café", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncodeText_LowercaseAndMixedPunctuation_ProducesValidCodewordRange()
    {
        var success = Pdf417TextCompaction.TryEncodeText("Hello, World! 123.45", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(900, codewords[0]);

        foreach (var codeword in codewords.Skip(1))
        {
            Assert.InRange(codeword, 0, 899);
        }
    }
}
