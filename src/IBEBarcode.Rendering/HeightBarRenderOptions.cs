using SkiaSharp;

namespace IBEBarcode.Rendering;

public sealed class HeightBarRenderOptions
{
    public int BarWidthPixels { get; init; } = 2;
    public int GapPixels { get; init; } = 2;
    public int TallBarHeightPixels { get; init; } = 30;
    public int ShortBarHeightPixels { get; init; } = 15;
    public SKColor BarColor { get; init; } = SKColors.Black;
    public SKColor BackgroundColor { get; init; } = SKColors.White;
}
