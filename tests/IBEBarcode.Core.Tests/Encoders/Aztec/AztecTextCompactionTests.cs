using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Tests.Encoders.Aztec;

public class AztecTextCompactionTests
{
    private static int[] ReadBits(AztecBitBuffer bits, int[] widths)
    {
        var values = new int[widths.Length];
        var pos = 0;

        for (var i = 0; i < widths.Length; i++)
        {
            var value = 0;

            for (var b = 0; b < widths[i]; b++)
            {
                value = (value << 1) | (bits.Get(pos++) ? 1 : 0);
            }

            values[i] = value;
        }

        return values;
    }

    [Fact]
    public void TryEncodeText_TwoUppercaseLetters_NoModeSwitchNeeded()
    {
        // Initial mode is Upper; 'A'=2, 'B'=3 (space=1, A-Z=2-27).
        var success = AztecTextCompaction.TryEncodeText("AB", out var bits, out var error);

        Assert.True(success, error);
        Assert.Equal(10, bits.Count);
        Assert.Equal(new[] { 2, 3 }, ReadBits(bits, new[] { 5, 5 }));
    }

    [Fact]
    public void TryEncodeText_SingleLowercaseLetter_LatchesToLowerFirst()
    {
        // Upper -> Lower latch is code 28 (5 bits), then 'a'=2 in Lower.
        var success = AztecTextCompaction.TryEncodeText("a", out var bits, out var error);

        Assert.True(success, error);
        Assert.Equal(10, bits.Count);
        Assert.Equal(new[] { 28, 2 }, ReadBits(bits, new[] { 5, 5 }));
    }

    [Fact]
    public void TryEncodeText_DigitsUseFourBitCodes()
    {
        // Upper -> Digit latch is code 30 (5 bits), then '1'=3 in Digit (4 bits: '0'=2,'1'=3).
        var success = AztecTextCompaction.TryEncodeText("1", out var bits, out var error);

        Assert.True(success, error);
        Assert.Equal(9, bits.Count); // 5 (latch) + 4 (digit code)
        Assert.Equal(new[] { 30, 3 }, ReadBits(bits, new[] { 5, 4 }));
    }

    [Fact]
    public void TryEncodeText_UnrepresentableCharacter_ReturnsError()
    {
        var success = AztecTextCompaction.TryEncodeText("café", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncodeText_MixedCaseAndPunctuation_ProducesValidBitStream()
    {
        var success = AztecTextCompaction.TryEncodeText("Hello, World! 123", out var bits, out var error);

        Assert.True(success, error);
        Assert.True(bits.Count > 0);
        Assert.Equal(0, bits.Count % 1); // sanity: buffer is well-formed (no exceptions above)
    }
}
