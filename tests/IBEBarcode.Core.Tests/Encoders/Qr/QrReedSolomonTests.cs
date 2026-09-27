using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Tests.Encoders.Qr;

public class QrReedSolomonTests
{
    [Fact]
    public void ComputeEccCodewords_PublishedHelloWorldExample_MatchesKnownOutput()
    {
        var data = new byte[] { 32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17 };
        var expectedEcc = new byte[] { 196, 35, 39, 119, 235, 215, 231, 226, 93, 23 };

        var ecc = QrReedSolomon.ComputeEccCodewords(data, 10);

        Assert.Equal(expectedEcc, ecc);
    }
}
