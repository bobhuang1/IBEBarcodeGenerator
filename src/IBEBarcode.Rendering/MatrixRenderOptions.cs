using SkiaSharp;

namespace IBEBarcode.Rendering;

public sealed class MatrixRenderOptions
{
    public int ModuleSizePixels { get; init; } = 8;
    public int QuietZoneModules { get; init; } = 4;
    public SKColor DarkColor { get; init; } = SKColors.Black;
    public SKColor LightColor { get; init; } = SKColors.White;
}
