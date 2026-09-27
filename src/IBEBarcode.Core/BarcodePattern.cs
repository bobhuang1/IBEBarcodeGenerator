namespace IBEBarcode.Core;

public sealed class BarcodePattern
{
    public string Value { get; }
    public IReadOnlyList<BarSegment> Segments { get; }
    public string? HumanReadableText { get; }

    private BarcodePattern(string value, IReadOnlyList<BarSegment> segments, string? humanReadableText)
    {
        Value = value;
        Segments = segments;
        HumanReadableText = humanReadableText;
    }

    public static BarcodePattern Create(string value, IReadOnlyList<BarSegment> segments, string? humanReadableText = null)
    {
        if (segments.Count == 0)
            throw new ArgumentException("Pattern must contain at least one segment.", nameof(segments));

        for (var i = 1; i < segments.Count; i++)
        {
            if (segments[i].IsBar == segments[i - 1].IsBar)
            {
                throw new ArgumentException(
                    $"Segments must alternate bar/space; segments {i - 1} and {i} are both {(segments[i].IsBar ? "bars" : "spaces")}.",
                    nameof(segments));
            }
        }

        return new BarcodePattern(value, segments, humanReadableText ?? value);
    }

    public int TotalWidthUnits => Segments.Sum(s => s.WidthUnits);
}
