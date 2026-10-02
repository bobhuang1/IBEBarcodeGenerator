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
        if (template.Rows <= 0 || template.Columns <= 0)
        {
            throw new ArgumentException(
                $"Template {template.Code} must have at least one row and one column.", nameof(template));
        }

        var document = new PdfDocument();
        var index = 0;

        // Always at least one page, so an empty batch still yields a valid (blank) sheet.
        // Counting pages up front keeps the loop bounded: the previous
        // "index < Count || index == 0" condition never advanced for an empty list.
        var pageCount = Math.Max(1, (labelPngImages.Count + template.LabelCount - 1) / template.LabelCount);

        for (var pageNumber = 0; pageNumber < pageCount; pageNumber++)
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
