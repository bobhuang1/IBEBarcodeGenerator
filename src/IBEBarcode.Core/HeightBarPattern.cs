namespace IBEBarcode.Core;

public sealed class HeightBarPattern
{
    public string Value { get; }
    public IReadOnlyList<HeightBar> Bars { get; }

    private HeightBarPattern(string value, IReadOnlyList<HeightBar> bars)
    {
        Value = value;
        Bars = bars;
    }

    public static HeightBarPattern Create(string value, IReadOnlyList<HeightBar> bars)
    {
        if (bars.Count == 0)
            throw new ArgumentException("Pattern must contain at least one bar.", nameof(bars));

        return new HeightBarPattern(value, bars);
    }
}
