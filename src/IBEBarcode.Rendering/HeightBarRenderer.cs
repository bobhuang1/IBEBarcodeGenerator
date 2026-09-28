using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering;

public static class HeightBarRenderer
{
    public static SKBitmap Render(HeightBarPattern pattern, HeightBarRenderOptions? options = null)
    {
        options ??= new HeightBarRenderOptions();

        var barCount = pattern.Bars.Count;
        var totalWidth = barCount * options.BarWidthPixels + Math.Max(0, barCount - 1) * options.GapPixels;
        var totalHeight = options.TallBarHeightPixels;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.BackgroundColor);

        using var paint = new SKPaint { Color = options.BarColor, Style = SKPaintStyle.Fill };

        var x = 0;
        foreach (var bar in pattern.Bars)
        {
            var barHeight = bar.IsTall ? options.TallBarHeightPixels : options.ShortBarHeightPixels;
            var y = totalHeight - barHeight;
            canvas.DrawRect(SKRect.Create(x, y, options.BarWidthPixels, barHeight), paint);
            x += options.BarWidthPixels + options.GapPixels;
        }

        return bitmap;
    }

    public static byte[] RenderToPng(HeightBarPattern pattern, HeightBarRenderOptions? options = null)
    {
        using var bitmap = Render(pattern, options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
