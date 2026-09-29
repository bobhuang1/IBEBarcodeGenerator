using SkiaSharp;

namespace IBEBarcode.Rendering;

/// <summary>
/// Wraps an already-rendered barcode PNG with an optional caption above it (User Field 1)
/// and below it (User Field 2), then applies an optional rotation -- all symbology-agnostic,
/// so it works uniformly on linear, matrix, and height-bar renderer output alike.
/// </summary>
public static class LabelComposer
{
    public static byte[] Compose(byte[] barcodePngBytes, LabelComposeOptions options)
    {
        using var barcodeBitmap = SKBitmap.Decode(barcodePngBytes);

        var hasPrefix = !string.IsNullOrEmpty(options.PrefixText);
        var hasSuffix = !string.IsNullOrEmpty(options.SuffixText);

        if (!hasPrefix && !hasSuffix && options.RotationDegrees == 0)
        {
            return barcodePngBytes;
        }

        var style = (options.FontBold, options.FontItalic) switch
        {
            (true, true) => SKFontStyle.BoldItalic,
            (true, false) => SKFontStyle.Bold,
            (false, true) => SKFontStyle.Italic,
            _ => SKFontStyle.Normal,
        };

        using var typeface = SKTypeface.FromFamilyName(options.FontFamily, style);
        var textStripHeight = (int)(options.FontSize * 1.8);
        var prefixHeight = hasPrefix ? textStripHeight : 0;
        var suffixHeight = hasSuffix ? textStripHeight : 0;

        var totalWidth = barcodeBitmap.Width;
        var totalHeight = barcodeBitmap.Height + prefixHeight + suffixHeight;

        using var composed = new SKBitmap(totalWidth, totalHeight);

        using (var canvas = new SKCanvas(composed))
        using (var font = new SKFont(typeface, options.FontSize))
        using (var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true })
        {
            canvas.Clear(SKColors.White);

            if (hasPrefix)
            {
                DrawCenteredText(canvas, options.PrefixText!, font, paint, totalWidth, prefixHeight * 0.75f, options.FontUnderline);
            }

            canvas.DrawBitmap(barcodeBitmap, 0, prefixHeight, SKSamplingOptions.Default, paint: null);

            if (hasSuffix)
            {
                DrawCenteredText(canvas, options.SuffixText!, font, paint, totalWidth, prefixHeight + barcodeBitmap.Height + suffixHeight * 0.75f, options.FontUnderline);
            }
        }

        using var rotated = Rotate(composed, options.RotationDegrees);
        using var image = SKImage.FromBitmap(rotated);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawCenteredText(SKCanvas canvas, string text, SKFont font, SKPaint paint, int containerWidth, float baselineY, bool underline)
    {
        var textWidth = font.MeasureText(text);
        var textX = Math.Max(0f, (containerWidth - textWidth) / 2f);
        canvas.DrawText(text, textX, baselineY, SKTextAlign.Left, font, paint);

        if (underline)
        {
            var underlineY = baselineY + font.Size * 0.12f;
            canvas.DrawLine(textX, underlineY, textX + textWidth, underlineY, paint);
        }
    }

    private static SKBitmap Rotate(SKBitmap source, int degrees)
    {
        var normalized = ((degrees % 360) + 360) % 360;
        var (width, height) = normalized is 0 or 180 ? (source.Width, source.Height) : (source.Height, source.Width);
        var rotated = new SKBitmap(width, height);

        using var canvas = new SKCanvas(rotated);
        canvas.Clear(SKColors.White);
        canvas.Translate(width / 2f, height / 2f);
        canvas.RotateDegrees(normalized);
        canvas.Translate(-source.Width / 2f, -source.Height / 2f);
        canvas.DrawBitmap(source, 0, 0, SKSamplingOptions.Default, paint: null);

        return rotated;
    }
}
