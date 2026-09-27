namespace IBEBarcode.Templates.Tests;

public class PaperTemplateTests
{
    private static PaperTemplate SimpleGrid() => new()
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
    public void LabelCount_IsColumnsTimesRows()
    {
        Assert.Equal(4, SimpleGrid().LabelCount);
    }

    [Fact]
    public void LabelPosition_FirstLabel_IsAtTopLeftMargin()
    {
        var (x, y) = SimpleGrid().LabelPosition(0, 0);

        Assert.Equal(5, x);
        Assert.Equal(10, y);
    }

    [Fact]
    public void LabelPosition_SecondColumn_AccountsForLabelWidthAndGap()
    {
        var (x, _) = SimpleGrid().LabelPosition(1, 0);

        Assert.Equal(5 + 40 + 5, x);
    }

    [Fact]
    public void LabelPosition_SecondRow_AccountsForLabelHeightAndGap()
    {
        var (_, y) = SimpleGrid().LabelPosition(0, 1);

        Assert.Equal(10 + 30 + 5, y);
    }

    [Fact]
    public void LabelPosition_ColumnOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimpleGrid().LabelPosition(2, 0));
    }

    [Fact]
    public void LabelPosition_RowOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimpleGrid().LabelPosition(0, 2));
    }
}
