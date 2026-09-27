namespace IBEBarcode.Templates.Tests;

public class PaperTemplateCatalogTests
{
    [Fact]
    public void Avery5160_ExactlyFillsUsLetterPageWidth()
    {
        var t = PaperTemplateCatalog.Avery5160;

        var totalWidth = t.LeftMarginMm + t.Columns * t.LabelWidthMm + (t.Columns - 1) * t.HorizontalGapMm + t.LeftMarginMm;

        Assert.Equal(t.PageWidthMm, totalWidth, precision: 4);
    }

    [Fact]
    public void Avery5160_ExactlyFillsUsLetterPageHeight()
    {
        var t = PaperTemplateCatalog.Avery5160;

        var totalHeight = t.TopMarginMm + t.Rows * t.LabelHeightMm + (t.Rows - 1) * t.VerticalGapMm + t.TopMarginMm;

        Assert.Equal(t.PageHeightMm, totalHeight, precision: 4);
    }

    [Fact]
    public void Avery5163_ExactlyFillsUsLetterPage()
    {
        var t = PaperTemplateCatalog.Avery5163;

        var totalWidth = t.LeftMarginMm + t.Columns * t.LabelWidthMm + (t.Columns - 1) * t.HorizontalGapMm + t.LeftMarginMm;
        var totalHeight = t.TopMarginMm + t.Rows * t.LabelHeightMm + (t.Rows - 1) * t.VerticalGapMm + t.TopMarginMm;

        Assert.Equal(t.PageWidthMm, totalWidth, precision: 4);
        Assert.Equal(t.PageHeightMm, totalHeight, precision: 4);
    }

    [Fact]
    public void Find_KnownVendorAndCode_ReturnsTemplate()
    {
        var found = PaperTemplateCatalog.Find("avery", "5160");

        Assert.NotNull(found);
        Assert.Equal(30, found!.LabelCount);
    }

    [Fact]
    public void Find_UnknownCode_ReturnsNull()
    {
        Assert.Null(PaperTemplateCatalog.Find("Avery", "99999"));
    }

    [Fact]
    public void AllTemplates_ContainsSeededTemplates()
    {
        Assert.Contains(PaperTemplateCatalog.AllTemplates, t => t.Code == "5160");
        Assert.Contains(PaperTemplateCatalog.AllTemplates, t => t.Code == "5163");
    }
}
