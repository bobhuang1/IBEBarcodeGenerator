using SkiaSharp;

namespace IBEBarcode.Rendering.Tests;

public class LabelComposerTests
{
    private static byte[] MakeBarcodePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black };
        canvas.DrawRect(SKRect.Create(0, 0, width, 1), paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void Compose_NoOptions_ReturnsOriginalBytesUnchanged()
    {
        var original = MakeBarcodePng(20, 10);

        var result = LabelComposer.Compose(original, new LabelComposeOptions());

        Assert.Same(original, result);
    }

    [Fact]
    public void Compose_PrefixOnly_GrowsHeightByTextStrip()
    {
        var original = MakeBarcodePng(40, 20);

        var result = LabelComposer.Compose(original, new LabelComposeOptions { PrefixText = "IBE Group, Inc", FontSize = 10 });

        using var decoded = SKBitmap.Decode(result);
        Assert.Equal(40, decoded.Width);
        Assert.True(decoded.Height > 20);
    }

    [Fact]
    public void Compose_PrefixAndSuffix_GrowsHeightByBothStrips()
    {
        var original = MakeBarcodePng(40, 20);

        var prefixOnly = LabelComposer.Compose(original, new LabelComposeOptions { PrefixText = "Top", FontSize = 10 });
        var both = LabelComposer.Compose(original, new LabelComposeOptions { PrefixText = "Top", SuffixText = "Bottom", FontSize = 10 });

        using var prefixOnlyBitmap = SKBitmap.Decode(prefixOnly);
        using var bothBitmap = SKBitmap.Decode(both);
        Assert.True(bothBitmap.Height > prefixOnlyBitmap.Height);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(270)]
    public void Compose_QuarterRotation_SwapsWidthAndHeight(int degrees)
    {
        var original = MakeBarcodePng(40, 20);

        var result = LabelComposer.Compose(original, new LabelComposeOptions { RotationDegrees = degrees });

        using var decoded = SKBitmap.Decode(result);
        Assert.Equal(20, decoded.Width);
        Assert.Equal(40, decoded.Height);
    }

    [Fact]
    public void Compose_180Rotation_KeepsSameDimensions()
    {
        var original = MakeBarcodePng(40, 20);

        var result = LabelComposer.Compose(original, new LabelComposeOptions { RotationDegrees = 180 });

        using var decoded = SKBitmap.Decode(result);
        Assert.Equal(40, decoded.Width);
        Assert.Equal(20, decoded.Height);
    }
}
