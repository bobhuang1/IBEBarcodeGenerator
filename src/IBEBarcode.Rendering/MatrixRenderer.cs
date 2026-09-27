using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering;

public static class MatrixRenderer
{
    public static SKBitmap Render(BarcodeMatrix matrix, MatrixRenderOptions? options = null)
    {
        options ??= new MatrixRenderOptions();

        var quietZonePixels = options.QuietZoneModules * options.ModuleSizePixels;
        var totalWidth = (matrix.Width * options.ModuleSizePixels) + quietZonePixels * 2;
        var totalHeight = (matrix.Height * options.ModuleSizePixels) + quietZonePixels * 2;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.LightColor);

        using var paint = new SKPaint { Color = options.DarkColor, Style = SKPaintStyle.Fill };

        for (var moduleY = 0; moduleY < matrix.Height; moduleY++)
        {
            for (var moduleX = 0; moduleX < matrix.Width; moduleX++)
            {
                if (!matrix[moduleX, moduleY])
                {
                    continue;
                }

                var x = quietZonePixels + moduleX * options.ModuleSizePixels;
                var y = quietZonePixels + moduleY * options.ModuleSizePixels;
                canvas.DrawRect(SKRect.Create(x, y, options.ModuleSizePixels, options.ModuleSizePixels), paint);
            }
        }

        return bitmap;
    }

    public static byte[] RenderToPng(BarcodeMatrix matrix, MatrixRenderOptions? options = null)
    {
        using var bitmap = Render(matrix, options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
