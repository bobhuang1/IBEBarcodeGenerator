using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Code128AutoSegmenterTests
{
    [Fact]
    public void TrySegment_NoQualifyingDigitRun_StaysOneSetBSegment()
    {
        var success = Code128AutoSegmenter.TrySegment("AB12CD34", out var segments, out var error);

        Assert.True(success, error);
        Assert.Single(segments);
        Assert.Equal(Code128Set.B, segments[0].Set);
        Assert.Equal("AB12CD34", segments[0].Text);
    }

    [Fact]
    public void TrySegment_FourOrMoreDigitsInMiddle_SwitchesToSetC()
    {
        var success = Code128AutoSegmenter.TrySegment("AB1234CD", out var segments, out var error);

        Assert.True(success, error);
        Assert.Equal(3, segments.Count);
        Assert.Equal((Code128Set.B, "AB"), (segments[0].Set, segments[0].Text));
        Assert.Equal((Code128Set.C, "1234"), (segments[1].Set, segments[1].Text));
        Assert.Equal((Code128Set.B, "CD"), (segments[2].Set, segments[2].Text));
    }

    [Fact]
    public void TrySegment_OddLengthDigitRun_LeavesOneDigitOutOfSetC()
    {
        var success = Code128AutoSegmenter.TrySegment("12345", out var segments, out var error);

        Assert.True(success, error);
        Assert.Equal(2, segments.Count);
        Assert.Equal((Code128Set.C, "1234"), (segments[0].Set, segments[0].Text));
        Assert.Equal((Code128Set.B, "5"), (segments[1].Set, segments[1].Text));
    }

    [Fact]
    public void TrySegment_ControlCharacter_UsesSetA()
    {
        var success = Code128AutoSegmenter.TrySegment("A\u0001B", out var segments, out var error);

        Assert.True(success, error);
        Assert.Single(segments);
        Assert.Equal(Code128Set.A, segments[0].Set);
    }

    [Fact]
    public void TrySegment_ControlCharacterMixedWithLowercase_ReturnsError()
    {
        var success = Code128AutoSegmenter.TrySegment("a\u0001b", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TrySegment_CharacterAbove127_ReturnsError()
    {
        var success = Code128AutoSegmenter.TrySegment("café", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }
}
