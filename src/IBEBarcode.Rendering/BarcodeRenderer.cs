using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering;

public static class BarcodeRenderer
{
    public static SKBitmap Render(BarcodePattern pattern, BarcodeRenderOptions? options = null)
    {
        options ??= new BarcodeRenderOptions();

        var quietZonePixels = options.QuietZoneModules * options.ModuleWidthPixels;
        var barsWidthPixels = pattern.TotalWidthUnits * options.ModuleWidthPixels;
        var totalWidth = barsWidthPixels + quietZonePixels * 2;

        var showText = options.ShowHumanReadableText && !string.IsNullOrEmpty(pattern.HumanReadableText);
        var textStripHeight = showText ? options.TextHeightPixels : 0;
        var totalHeight = options.BarHeightPixels + textStripHeight;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.BackgroundColor);

        using var paint = new SKPaint { Color = options.BarColor, Style = SKPaintStyle.Fill, IsAntialias = true };

        var x = quietZonePixels;
        foreach (var segment in pattern.Segments)
        {
            var widthPixels = segment.WidthUnits * options.ModuleWidthPixels;

            if (segment.IsBar)
            {
                canvas.DrawRect(SKRect.Create(x, 0, widthPixels, options.BarHeightPixels), paint);
            }

            x += widthPixels;
        }

        if (showText)
        {
            using var font = new SKFont(SKTypeface.Default, textStripHeight * 0.7f);
            var textWidth = font.MeasureText(pattern.HumanReadableText);
            var textX = Math.Max(0f, (totalWidth - textWidth) / 2f);
            var textY = options.BarHeightPixels + textStripHeight * 0.8f;
            canvas.DrawText(pattern.HumanReadableText, textX, textY, SKTextAlign.Left, font, paint);
        }

        return bitmap;
    }

    public static byte[] RenderToPng(BarcodePattern pattern, BarcodeRenderOptions? options = null)
    {
        using var bitmap = Render(pattern, options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
