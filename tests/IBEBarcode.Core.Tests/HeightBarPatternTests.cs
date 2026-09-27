namespace IBEBarcode.Core.Tests;

public class HeightBarPatternTests
{
    [Fact]
    public void Create_WithBars_Succeeds()
    {
        var bars = new[] { new HeightBar(true), new HeightBar(false), new HeightBar(true) };

        var pattern = HeightBarPattern.Create("123", bars);

        Assert.Equal("123", pattern.Value);
        Assert.Equal(bars, pattern.Bars);
    }

    [Fact]
    public void Create_WithNoBars_Throws()
    {
        Assert.Throws<ArgumentException>(() => HeightBarPattern.Create("x", Array.Empty<HeightBar>()));
    }
}
