using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class EanUpcDigitPatternsTests
{
    [Fact]
    public void AppendLeftDigit_DigitZero_LCode_ProducesKnownPattern()
    {
        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendLeftDigit(segments, 0, useGCode: false);

        Assert.Equal(new[] { 3, 2, 1, 1 }, segments.Select(s => s.WidthUnits).ToArray());
        Assert.Equal(new[] { false, true, false, true }, segments.Select(s => s.IsBar).ToArray());
    }

    [Fact]
    public void AppendLeftDigit_DigitZero_GCode_ProducesKnownPattern()
    {
        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendLeftDigit(segments, 0, useGCode: true);

        Assert.Equal(new[] { 1, 1, 2, 3 }, segments.Select(s => s.WidthUnits).ToArray());
        Assert.Equal(new[] { false, true, false, true }, segments.Select(s => s.IsBar).ToArray());
    }

    [Fact]
    public void AppendRightDigit_DigitZero_ProducesKnownPattern()
    {
        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendRightDigit(segments, 0);

        Assert.Equal(new[] { 3, 2, 1, 1 }, segments.Select(s => s.WidthUnits).ToArray());
        Assert.Equal(new[] { true, false, true, false }, segments.Select(s => s.IsBar).ToArray());
    }

    [Fact]
    public void ParityForFirstDigit_KnownValues_MatchStandardTable()
    {
        Assert.Equal("LLLLLL", EanUpcDigitPatterns.ParityForFirstDigit(0));
        Assert.Equal("LLGLGG", EanUpcDigitPatterns.ParityForFirstDigit(1));
        Assert.Equal("LGGLGL", EanUpcDigitPatterns.ParityForFirstDigit(9));
    }

    [Theory]
    [InlineData("03600029145", '2')]
    [InlineData("978030640615", '7')]
    [InlineData("9638507", '4')]
    public void ComputeCheckDigit_KnownValues_ReturnsExpectedDigit(string dataDigits, char expected)
    {
        Assert.Equal(expected, EanUpcDigitPatterns.ComputeCheckDigit(dataDigits));
    }
}
