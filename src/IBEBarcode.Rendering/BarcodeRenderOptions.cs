using SkiaSharp;

namespace IBEBarcode.Rendering;

public sealed class BarcodeRenderOptions
{
    public int ModuleWidthPixels { get; init; } = 2;
    public int BarHeightPixels { get; init; } = 80;
    public int QuietZoneModules { get; init; } = 10;
    public SKColor BarColor { get; init; } = SKColors.Black;
    public SKColor BackgroundColor { get; init; } = SKColors.White;
    public bool ShowHumanReadableText { get; init; } = false;
    public int TextHeightPixels { get; init; } = 20;
}
