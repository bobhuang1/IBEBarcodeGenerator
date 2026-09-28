using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Tests.Encoders.Aztec;

public class AztecReedSolomonEncoderTests
{
    [Fact]
    public void Encode_Gf16_ProducesDeterministicCheckWords()
    {
        var field = new AztecGaloisField(0x13, 16, 1);
        var rs = new AztecReedSolomonEncoder(field);

        var toEncode = new[] { 3, 5, 0, 0, 0, 0, 0 };
        rs.Encode(toEncode, 5);

        var toEncodeAgain = new[] { 3, 5, 0, 0, 0, 0, 0 };
        rs.Encode(toEncodeAgain, 5);

        Assert.Equal(toEncode, toEncodeAgain);

        foreach (var word in toEncode)
        {
            Assert.InRange(word, 0, 15);
        }
    }

    [Fact]
    public void Encode_Gf256_MatchesDataMatrixFieldPrimitive()
    {
        var field = new AztecGaloisField(0x12D, 256, 1);
        var rs = new AztecReedSolomonEncoder(field);

        var toEncode = new int[10];
        toEncode[0] = 65;
        toEncode[1] = 66;
        rs.Encode(toEncode, 8);

        for (var i = 2; i < toEncode.Length; i++)
        {
            Assert.InRange(toEncode[i], 0, 255);
        }
    }

    [Fact]
    public void BitBuffer_AppendAndGet_RoundTrips()
    {
        var buffer = new AztecBitBuffer();
        buffer.AppendBits(0b101, 3);
        buffer.AppendBits(0b11001, 5);

        Assert.Equal(8, buffer.Count);
        Assert.True(buffer.Get(0));
        Assert.False(buffer.Get(1));
        Assert.True(buffer.Get(2));
        Assert.True(buffer.Get(3));
        Assert.True(buffer.Get(4));
        Assert.False(buffer.Get(5));
        Assert.False(buffer.Get(6));
        Assert.True(buffer.Get(7));
    }
}
