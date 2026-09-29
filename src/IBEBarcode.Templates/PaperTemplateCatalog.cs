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

    // Sourced from a structured Avery-dimensions dataset (columns: page W/H, top/bottom
    // margin, left/right margin, rows, columns, horizontal/vertical gap -- reverse
    // engineered by matching known-good 5164 figures against an independent search
    // result before trusting the rest of the dataset) plus independent WebSearch
    // corroboration for 5164 specifically. All three below were cross-checked by solving
    // for an exact US Letter (215.9x279.4mm) fit using the dataset's own margin/gap
    // fractions (5/32in margin, 3/16in gap for the 4-inch-wide templates; 0.3in for
    // 5167) -- every one closes to the millimeter, not just approximately.
    public static readonly PaperTemplate Avery5162 = new()
    {
        Vendor = "Avery",
        Code = "5162",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 2,
        Rows = 7,
        LabelWidthMm = 101.6,
        LabelHeightMm = 33.8667,
        TopMarginMm = 21.1667,
        LeftMarginMm = 3.96875,
        HorizontalGapMm = 4.7625,
        VerticalGapMm = 0,
    };

    public static readonly PaperTemplate Avery5164 = new()
    {
        Vendor = "Avery",
        Code = "5164",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 2,
        Rows = 3,
        LabelWidthMm = 101.6,
        LabelHeightMm = 84.6667,
        TopMarginMm = 12.7,
        LeftMarginMm = 3.96875,
        HorizontalGapMm = 4.7625,
        VerticalGapMm = 0,
    };

    public static readonly PaperTemplate Avery5167 = new()
    {
        Vendor = "Avery",
        Code = "5167",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 4,
        Rows = 20,
        LabelWidthMm = 44.45,
        LabelHeightMm = 12.7,
        TopMarginMm = 12.7,
        LeftMarginMm = 7.62,
        HorizontalGapMm = 7.62,
        VerticalGapMm = 0,
    };

    // Avery 22805 ("Print-to-the-Edge Square Labels", 1-1/2"x1-1/2", 24/sheet) was
    // looked up but NOT added: no source gave its column/row layout or margins, and the
    // one layout guess found (6 columns x 4 rows) is geometrically impossible on US
    // Letter (6 x 1.5in = 9in exceeds the 8.5in page width) -- left out rather than
    // guessed, matching this catalog's existing standard for 5162 (originally) and 22805.

    private static readonly PaperTemplate[] All = { Avery5160, Avery5161, Avery5162, Avery5163, Avery5164, Avery5167 };

    public static IReadOnlyList<PaperTemplate> AllTemplates => All;

    public static PaperTemplate? Find(string vendor, string code) =>
        All.FirstOrDefault(t =>
            string.Equals(t.Vendor, vendor, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
}
