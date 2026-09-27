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
        var totalHeight = options.BarHeightPixels;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.BackgroundColor);

        using var paint = new SKPaint { Color = options.BarColor, Style = SKPaintStyle.Fill };

        var x = quietZonePixels;
        foreach (var segment in pattern.Segments)
        {
            var widthPixels = segment.WidthUnits * options.ModuleWidthPixels;

            if (segment.IsBar)
            {
                canvas.DrawRect(SKRect.Create(x, 0, widthPixels, totalHeight), paint);
            }

            x += widthPixels;
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
