namespace IBEBarcode.Rendering;

public sealed class LabelComposeOptions
{
    public string? PrefixText { get; init; }
    public string? SuffixText { get; init; }
    public string FontFamily { get; init; } = "Arial";
    public int FontSize { get; init; } = 10;
    public bool FontBold { get; init; }
    public bool FontItalic { get; init; }
    public bool FontUnderline { get; init; }

    /// <summary>Clockwise rotation applied last, after prefix/suffix compositing. Must be 0, 90, 180, or 270.</summary>
    public int RotationDegrees { get; init; }
}
