using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;
using IBEBarcode.Rendering;
using IBEBarcode.Templates;

namespace IBEBarcode.Printing.Tests;

public class LabelSheetPdfGeneratorTests
{
    // A real PNG produced by the already-tested rendering pipeline, rather than
    // a hand-typed byte array (which is easy to get subtly wrong).
    private static readonly byte[] TinyPng = RenderRealBarcodePng();

    private static byte[] RenderRealBarcodePng()
    {
        var encoder = new Code39Encoder();
        encoder.TryEncode("A", out var pattern, out _);
        return BarcodeRenderer.RenderToPng(pattern!, new BarcodeRenderOptions { ModuleWidthPixels = 1, QuietZoneModules = 0, BarHeightPixels = 10 });
    }

    private static PaperTemplate SmallTemplate() => new()
    {
        Vendor = "Test",
        Code = "T1",
        PageWidthMm = 100,
        PageHeightMm = 100,
        Columns = 2,
        Rows = 2,
        LabelWidthMm = 40,
        LabelHeightMm = 30,
        TopMarginMm = 10,
        LeftMarginMm = 5,
        HorizontalGapMm = 5,
        VerticalGapMm = 5,
    };

    [Fact]
    public void Generate_ProducesValidPdfBytes()
    {
        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), new[] { TinyPng });

        Assert.True(pdfBytes.Length > 100);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public void Generate_WithNoImages_StillProducesValidPdf()
    {
        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), Array.Empty<byte[]>());

        Assert.True(pdfBytes.Length > 50);
        Assert.Equal((byte)'%', pdfBytes[0]);
    }

    [Fact]
    public void Generate_FullSheet_Succeeds()
    {
        var images = new[] { TinyPng, TinyPng, TinyPng, TinyPng };

        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), images);

        Assert.True(pdfBytes.Length > 100);
    }

    [Fact]
    public void Generate_MoreImagesThanLabelPositions_WrapsOntoASecondPage()
    {
        // The 2x2 template holds 4 labels per page; a 5th image must start a new page.
        var images = new[] { TinyPng, TinyPng, TinyPng, TinyPng, TinyPng };

        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), images);

        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.Equal(2, document.PageCount);
    }

    [Fact]
    public void Generate_LargeBatch_ProducesEnoughPagesForAllImages()
    {
        // 1000 images on a 4-label-per-page template needs 250 pages -- exercises the
        // "up to 1000 sequential labels" batch-printing path end to end.
        var images = new byte[1000][];
        Array.Fill(images, TinyPng);

        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), images);

        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.Equal(250, document.PageCount);
    }
}
