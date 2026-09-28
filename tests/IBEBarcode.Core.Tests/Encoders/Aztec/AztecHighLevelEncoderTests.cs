using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Tests.Encoders.Aztec;

public class AztecHighLevelEncoderTests
{
    [Fact]
    public void EncodeBinaryShift_ShortRun_UsesSingleFiveBitHeader()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[] { 65, 66, 67 });

        Assert.Equal(34, bits.Count);
    }

    [Fact]
    public void EncodeBinaryShift_ExactlyThirtyOneBytes_UsesSingleHeader()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[31]);

        Assert.Equal(10 + 31 * 8, bits.Count);
    }

    [Fact]
    public void EncodeBinaryShift_ThirtyTwoToSixtyTwoBytes_UsesTwoHeaders()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[40]);

        Assert.Equal(20 + 40 * 8, bits.Count);
    }

    [Fact]
    public void EncodeBinaryShift_MoreThanSixtyTwoBytes_UsesExtendedHeader()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[100]);

        Assert.Equal(21 + 100 * 8, bits.Count);
    }
}
