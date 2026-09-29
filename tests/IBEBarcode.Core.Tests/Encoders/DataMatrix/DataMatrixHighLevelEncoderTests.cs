using IBEBarcode.Core.Encoders.DataMatrix;

namespace IBEBarcode.Core.Tests.Encoders.DataMatrix;

public class DataMatrixHighLevelEncoderTests
{
    [Fact]
    public void TryEncodeC40_OneCompleteTriplet_MatchesHandComputedCodewords()
    {
        // "ABC" -> values 14,15,16 (A-Z = 14-39). codeword16 = 1600*14+40*15+16+1 = 23017
        // = 89*256+233. No trailing chars (0 % 3 == 0), so always unlatch afterward.
        var success = DataMatrixHighLevelEncoder.TryEncodeC40("ABC", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(new byte[] { 230, 89, 233, 254 }, codewords);
    }

    [Fact]
    public void TryEncodeC40_TwoTriplets_MatchesHandComputedCodewords()
    {
        // "ABCDEF": triplet1 (14,15,16) -> 89,233 (as above). triplet2 (17,18,19):
        // codeword16 = 1600*17+40*18+19+1 = 27940 = 109*256+36.
        var success = DataMatrixHighLevelEncoder.TryEncodeC40("ABCDEF", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(new byte[] { 230, 89, 233, 109, 36, 254 }, codewords);
    }

    [Fact]
    public void TryEncodeC40_TrailingPartialCharacters_FallBackToAsciiAfterUnlatch()
    {
        // "AB" (2 base-set values) can never form a complete triplet -- both characters
        // fall back to plain ASCII (byte+1) after latch+immediate unlatch.
        var success = DataMatrixHighLevelEncoder.TryEncodeC40("AB", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(new byte[] { 230, 254, 'A' + 1, 'B' + 1 }, codewords);
    }

    [Fact]
    public void TryEncodeC40_UnsupportedCharacter_ReturnsError()
    {
        var success = DataMatrixHighLevelEncoder.TryEncodeC40("café", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncodeText_LowercaseTriplet_MatchesHandComputedCodewords()
    {
        // Text mode's base set is lowercase (unlike C40's uppercase): "abc" -> same
        // values 14,15,16 as C40's "ABC", so the same codeword math applies.
        var success = DataMatrixHighLevelEncoder.TryEncodeText("abc", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(new byte[] { 239, 89, 233, 254 }, codewords);
    }

    [Fact]
    public void TryEncodeX12_OneCompleteTriplet_MatchesHandComputedCodewords()
    {
        // X12 has no shift sets; "ABC" -> same base values 14,15,16 as C40.
        var success = DataMatrixHighLevelEncoder.TryEncodeX12("ABC", out var codewords, out var error);

        Assert.True(success, error);
        Assert.Equal(new byte[] { 238, 89, 233, 254 }, codewords);
    }

    [Fact]
    public void TryEncodeX12_LowercaseCharacter_ReturnsError()
    {
        // X12 has no shift sets at all -- lowercase isn't representable in any form.
        var success = DataMatrixHighLevelEncoder.TryEncodeX12("abc", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void EncodeBase256_ShortMessage_MatchesHandComputedRandomizedBytes()
    {
        // Latch(231) at position1 (not randomized). Length byte (3, <=249) at position2:
        // pseudoRandom = (149*2 % 255)+1 = 44 -> 3+44=47. Data bytes at positions 3,4,5:
        // pos3: (149*3%255)+1=193 -> 1+193=194. pos4: (149*4%255)+1=87 -> 2+87=89.
        // pos5: (149*5%255)+1=236 -> 3+236=239.
        var codewords = DataMatrixHighLevelEncoder.EncodeBase256(new byte[] { 1, 2, 3 });

        Assert.Equal(new byte[] { 231, 47, 194, 89, 239 }, codewords);
    }
}
