using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering.Tests;

public class BarcodeRendererTests
{
    [Fact]
    public void Render_SimplePattern_ProducesExpectedPixels()
    {
        var pattern = BarcodePattern.Create("test", new[]
        {
            new BarSegment(true, 2),
            new BarSegment(false, 1),
            new BarSegment(true, 1),
        });

        var options = new BarcodeRenderOptions
        {
            ModuleWidthPixels = 1,
            QuietZoneModules = 0,
            BarHeightPixels = 10,
        };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(4, bitmap.Width);
        Assert.Equal(10, bitmap.Height);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 5));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(1, 5));
        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 5));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 5));
    }

    [Fact]
    public void Render_AppliesQuietZoneOnBothSides()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) });
        var options = new BarcodeRenderOptions { ModuleWidthPixels = 1, QuietZoneModules = 3, BarHeightPixels = 5 };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(7, bitmap.Width);
        Assert.Equal(SKColors.White, bitmap.GetPixel(0, 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 2));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(4, 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(6, 2));
    }

    [Fact]
    public void Render_ScalesByModuleWidth()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) });
        var options = new BarcodeRenderOptions { ModuleWidthPixels = 5, QuietZoneModules = 0, BarHeightPixels = 5 };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(5, bitmap.Width);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 2));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(4, 2));
    }

    [Fact]
    public void Render_UsesDefaultOptionsWhenNoneGiven()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) });

        using var bitmap = BarcodeRenderer.Render(pattern);

        // default ModuleWidthPixels=2, QuietZoneModules=10 -> (1 + 10*2) * 2 = 42
        Assert.Equal(42, bitmap.Width);
        Assert.Equal(80, bitmap.Height);
    }

    [Fact]
    public void RenderToPng_ProducesValidPngBytes()
    {
        var pattern = BarcodePattern.Create("test", new[]
        {
            new BarSegment(true, 1),
            new BarSegment(false, 1),
            new BarSegment(true, 1),
        });

        var bytes = BarcodeRenderer.RenderToPng(pattern);

        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Render_RealEncoderOutput_ProducesNonEmptyImage()
    {
        var encoder = new IBEBarcode.Core.Encoders.Code39Encoder();
        encoder.TryEncode("HELLO", out var pattern, out _);

        var options = new BarcodeRenderOptions { ModuleWidthPixels = 2, QuietZoneModules = 10, BarHeightPixels = 60 };
        using var bitmap = BarcodeRenderer.Render(pattern!, options);

        var expectedWidth = (pattern!.TotalWidthUnits + options.QuietZoneModules * 2) * options.ModuleWidthPixels;
        Assert.Equal(expectedWidth, bitmap.Width);
        Assert.Equal(60, bitmap.Height);
    }
}
