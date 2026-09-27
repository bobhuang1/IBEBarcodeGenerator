using IBEBarcode.Templates;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace IBEBarcode.Printing;

public static class LabelSheetPdfGenerator
{
    private const double PointsPerMillimeter = 72.0 / 25.4;

    public static byte[] Generate(PaperTemplate template, IReadOnlyList<byte[]> labelPngImages)
    {
        if (labelPngImages.Count > template.LabelCount)
        {
            throw new ArgumentException(
                $"Template '{template.Vendor} {template.Code}' has {template.LabelCount} label positions but {labelPngImages.Count} images were provided.",
                nameof(labelPngImages));
        }

        var document = new PdfDocument();
        var page = document.AddPage();
        page.Width = template.PageWidthMm * PointsPerMillimeter;
        page.Height = template.PageHeightMm * PointsPerMillimeter;

        using var gfx = XGraphics.FromPdfPage(page);

        var index = 0;

        for (var row = 0; row < template.Rows && index < labelPngImages.Count; row++)
        {
            for (var column = 0; column < template.Columns && index < labelPngImages.Count; column++)
            {
                var (xMm, yMm) = template.LabelPosition(column, row);

                using var stream = new MemoryStream(labelPngImages[index]);
                using var image = XImage.FromStream(stream);

                gfx.DrawImage(
                    image,
                    xMm * PointsPerMillimeter,
                    yMm * PointsPerMillimeter,
                    template.LabelWidthMm * PointsPerMillimeter,
                    template.LabelHeightMm * PointsPerMillimeter);

                index++;
            }
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }
}
