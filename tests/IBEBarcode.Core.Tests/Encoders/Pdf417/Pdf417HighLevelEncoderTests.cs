using IBEBarcode.Core.Encoders.Pdf417;

namespace IBEBarcode.Core.Tests.Encoders.Pdf417;

public class Pdf417HighLevelEncoderTests
{
    [Fact]
    public void EncodeBytes_LengthMultipleOfSix_UsesLatch924()
    {
        var codewords = Pdf417HighLevelEncoder.EncodeBytes(new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.Equal(924, codewords[0]);
        Assert.Equal(6, codewords.Length);
    }

    [Fact]
    public void EncodeBytes_LengthNotMultipleOfSix_UsesLatch901()
    {
        var codewords = Pdf417HighLevelEncoder.EncodeBytes(new byte[] { 65, 66, 67 });

        Assert.Equal(901, codewords[0]);
        Assert.Equal(4, codewords.Length);
        Assert.Equal(65, codewords[1]);
        Assert.Equal(66, codewords[2]);
        Assert.Equal(67, codewords[3]);
    }

    [Fact]
    public void EncodeBytes_SixPackCodewords_AreAllWithinBase900Range()
    {
        var codewords = Pdf417HighLevelEncoder.EncodeBytes(new byte[] { 200, 201, 202, 203, 204, 205 });

        for (var i = 1; i < codewords.Length; i++)
        {
            Assert.InRange(codewords[i], 0, 899);
        }
    }
}
