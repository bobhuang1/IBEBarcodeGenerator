using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering.Tests;

public class HeightBarRendererTests
{
    private static HeightBarPattern TallShortTallPattern() =>
        HeightBarPattern.Create("test", new[] { new HeightBar(true), new HeightBar(false), new HeightBar(true) });

    [Fact]
    public void Render_ProducesExpectedDimensions()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        // 3 bars * 2px + 2 gaps * 1px = 8
        Assert.Equal(8, bitmap.Width);
        Assert.Equal(10, bitmap.Height);
    }

    [Fact]
    public void Render_TallBarSpansFullHeight()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 9));
    }

    [Fact]
    public void Render_ShortBarIsBottomAlignedAndShorterThanTall()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        // short bar (index 1, x=3..4): empty above, filled at the bottom
        Assert.Equal(SKColors.White, bitmap.GetPixel(3, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 7));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 9));
    }

    [Fact]
    public void Render_GapBetweenBarsIsBackground()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 5));
    }

    [Fact]
    public void RenderToPng_ProducesValidPngBytes()
    {
        var pattern = TallShortTallPattern();

        var bytes = HeightBarRenderer.RenderToPng(pattern);

        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Render_RealPostnetEncoderOutput_ProducesExpectedWidth()
    {
        var encoder = new IBEBarcode.Core.Encoders.PostnetEncoder();
        encoder.TryEncode("12345", out var pattern, out _);

        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1 };
        using var bitmap = HeightBarRenderer.Render(pattern!, options);

        var expectedWidth = pattern!.Bars.Count * options.BarWidthPixels + (pattern.Bars.Count - 1) * options.GapPixels;
        Assert.Equal(expectedWidth, bitmap.Width);
    }
}
