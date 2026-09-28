namespace IBEBarcode.Templates;

public static class PaperTemplateCatalog
{
    public static readonly PaperTemplate Avery5160 = new()
    {
        Vendor = "Avery",
        Code = "5160",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 3,
        Rows = 10,
        LabelWidthMm = 66.675,
        LabelHeightMm = 25.4,
        TopMarginMm = 12.7,
        LeftMarginMm = 4.7625,
        HorizontalGapMm = 3.175,
        VerticalGapMm = 0,
    };

    // Margins are computed (solved for an exact US Letter fit), not directly
    // vendor-sourced — see the plan doc for the reasoning and cross-check.
    public static readonly PaperTemplate Avery5163 = new()
    {
        Vendor = "Avery",
        Code = "5163",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 2,
        Rows = 5,
        LabelWidthMm = 101.6,
        LabelHeightMm = 50.8,
        TopMarginMm = 12.7,
        LeftMarginMm = 6.35,
        HorizontalGapMm = 0,
        VerticalGapMm = 0,
    };

    // Margins are computed (solved for an exact US Letter fit), same method as 5163.
    // Cross-check: the computed left margin matches 5163's (same 2-column, zero-gap
    // width layout) and the computed top margin matches 5160's vendor-sourced value
    // (same 1"-tall label, zero vertical gap layout) -- both were established
    // independently before this template was added, so the match is a genuine
    // consistency check, not circular reasoning.
    public static readonly PaperTemplate Avery5161 = new()
    {
        Vendor = "Avery",
        Code = "5161",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 2,
        Rows = 10,
        LabelWidthMm = 101.6,
        LabelHeightMm = 25.4,
        TopMarginMm = 12.7,
        LeftMarginMm = 6.35,
        HorizontalGapMm = 0,
        VerticalGapMm = 0,
    };

    private static readonly PaperTemplate[] All = { Avery5160, Avery5161, Avery5163 };

    public static IReadOnlyList<PaperTemplate> AllTemplates => All;

    public static PaperTemplate? Find(string vendor, string code) =>
        All.FirstOrDefault(t =>
            string.Equals(t.Vendor, vendor, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
}
