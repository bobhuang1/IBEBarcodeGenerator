namespace IBEBarcode.Templates;

public sealed class PaperTemplate
{
    public required string Vendor { get; init; }
    public required string Code { get; init; }
    public required double PageWidthMm { get; init; }
    public required double PageHeightMm { get; init; }
    public required int Columns { get; init; }
    public required int Rows { get; init; }
    public required double LabelWidthMm { get; init; }
    public required double LabelHeightMm { get; init; }
    public required double TopMarginMm { get; init; }
    public required double LeftMarginMm { get; init; }
    public required double HorizontalGapMm { get; init; }
    public required double VerticalGapMm { get; init; }

    public int LabelCount => Columns * Rows;

    public (double X, double Y) LabelPosition(int column, int row)
    {
        if (column < 0 || column >= Columns)
            throw new ArgumentOutOfRangeException(nameof(column));

        if (row < 0 || row >= Rows)
            throw new ArgumentOutOfRangeException(nameof(row));

        var x = LeftMarginMm + column * (LabelWidthMm + HorizontalGapMm);
        var y = TopMarginMm + row * (LabelHeightMm + VerticalGapMm);
        return (x, y);
    }

    public override string ToString() => $"{Vendor} {Code} ({Columns}x{Rows}, {LabelCount} labels)";
}
