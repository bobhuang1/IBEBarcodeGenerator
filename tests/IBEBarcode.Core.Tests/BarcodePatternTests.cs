namespace IBEBarcode.Core.Tests;

public class BarcodePatternTests
{
    [Fact]
    public void BarSegment_ThrowsOnNonPositiveWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarSegment(true, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarSegment(true, -1));
    }

    [Fact]
    public void Create_WithAlternatingSegments_Succeeds()
    {
        var segments = new[]
        {
            new BarSegment(true, 1),
            new BarSegment(false, 2),
            new BarSegment(true, 1),
        };

        var pattern = BarcodePattern.Create("123", segments);

        Assert.Equal("123", pattern.Value);
        Assert.Equal(segments, pattern.Segments);
        Assert.Equal(4, pattern.TotalWidthUnits);
        Assert.Equal("123", pattern.HumanReadableText);
    }

    [Fact]
    public void Create_WithCustomHumanReadableText_UsesIt()
    {
        var segments = new[] { new BarSegment(true, 1) };

        var pattern = BarcodePattern.Create("123", segments, "*123*");

        Assert.Equal("*123*", pattern.HumanReadableText);
    }

    [Fact]
    public void Create_WithConsecutiveBars_Throws()
    {
        var segments = new[]
        {
            new BarSegment(true, 1),
            new BarSegment(true, 1),
        };

        Assert.Throws<ArgumentException>(() => BarcodePattern.Create("x", segments));
    }

    [Fact]
    public void Create_WithNoSegments_Throws()
    {
        Assert.Throws<ArgumentException>(() => BarcodePattern.Create("x", Array.Empty<BarSegment>()));
    }
}
