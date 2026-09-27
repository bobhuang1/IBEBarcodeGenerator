using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering.Tests;

public class MatrixRendererTests
{
    private static BarcodeMatrix TinyCheckerboard()
    {
        var modules = new bool[2, 2];
        modules[0, 0] = true;
        modules[1, 0] = false;
        modules[0, 1] = false;
        modules[1, 1] = true;
        return BarcodeMatrix.Create("test", modules);
    }

    [Fact]
    public void Render_ProducesExpectedDimensions()
    {
        var matrix = TinyCheckerboard();
        var options = new MatrixRenderOptions { ModuleSizePixels = 3, QuietZoneModules = 0 };

        using var bitmap = MatrixRenderer.Render(matrix, options);

        Assert.Equal(6, bitmap.Width);
        Assert.Equal(6, bitmap.Height);
    }

    [Fact]
    public void Render_DrawsEachModuleAsASolidSquare()
    {
        var matrix = TinyCheckerboard();
        var options = new MatrixRenderOptions { ModuleSizePixels = 2, QuietZoneModules = 0 };

        using var bitmap = MatrixRenderer.Render(matrix, options);

        // module (0,0) dark -> pixels (0,0) and (1,1) within it are black
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(1, 1));
        // module (1,0) light -> white
        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 0));
        Assert.Equal(SKColors.White, bitmap.GetPixel(3, 1));
        // module (1,1) dark -> black
        Assert.Equal(SKColors.Black, bitmap.GetPixel(2, 2));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 3));
    }

    [Fact]
    public void Render_AppliesQuietZone()
    {
        var matrix = TinyCheckerboard();
        var options = new MatrixRenderOptions { ModuleSizePixels = 1, QuietZoneModules = 2 };

        using var bitmap = MatrixRenderer.Render(matrix, options);

        Assert.Equal(6, bitmap.Width);
        Assert.Equal(6, bitmap.Height);
        Assert.Equal(SKColors.White, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(2, 2));
    }

    [Fact]
    public void RenderToPng_ProducesValidPngBytes()
    {
        var matrix = TinyCheckerboard();

        var bytes = MatrixRenderer.RenderToPng(matrix);

        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Render_RealQrCode_ProducesExpectedSize()
    {
        var encoder = new IBEBarcode.Core.Encoders.QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        var options = new MatrixRenderOptions { ModuleSizePixels = 4, QuietZoneModules = 4 };
        using var bitmap = MatrixRenderer.Render(matrix!, options);

        var expected = (matrix!.Width + options.QuietZoneModules * 2) * options.ModuleSizePixels;
        Assert.Equal(expected, bitmap.Width);
        Assert.Equal(expected, bitmap.Height);
    }
}
