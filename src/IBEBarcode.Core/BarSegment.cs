namespace IBEBarcode.Core;

public readonly struct BarSegment : IEquatable<BarSegment>
{
    public bool IsBar { get; }
    public int WidthUnits { get; }

    public BarSegment(bool isBar, int widthUnits)
    {
        if (widthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthUnits), "Segment width must be positive.");

        IsBar = isBar;
        WidthUnits = widthUnits;
    }

    public bool Equals(BarSegment other) => IsBar == other.IsBar && WidthUnits == other.WidthUnits;

    public override bool Equals(object? obj) => obj is BarSegment other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(IsBar, WidthUnits);
}
