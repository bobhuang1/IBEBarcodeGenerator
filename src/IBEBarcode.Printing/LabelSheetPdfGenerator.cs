using IBEBarcode.Templates;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace IBEBarcode.Printing;

public static class LabelSheetPdfGenerator
{
    private const double PointsPerMillimeter = 72.0 / 25.4;

    /// <summary>
    /// Lays out label images across one or more sheet pages, wrapping onto a new page once
    /// a page's <see cref="PaperTemplate.LabelCount"/> positions are filled. Any number of
    /// images is accepted -- a 1000-image batch on a 30-label-per-sheet template produces
    /// 34 pages.
    /// </summary>
    public static byte[] Generate(PaperTemplate template, IReadOnlyList<byte[]> labelPngImages)
    {
        var document = new PdfDocument();
        var index = 0;

        while (index < labelPngImages.Count || index == 0)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(template.PageWidthMm * PointsPerMillimeter);
            page.Height = XUnit.FromPoint(template.PageHeightMm * PointsPerMillimeter);

            using var gfx = XGraphics.FromPdfPage(page);

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
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }
}
