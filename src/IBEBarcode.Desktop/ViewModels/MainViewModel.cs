using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;
using IBEBarcode.Rendering;

namespace IBEBarcode.Desktop.ViewModels;

public enum SupportedSymbology
{
    Code39,
    Codabar,
    Interleaved2Of5,
    MsiPlessey,
    Code93,
    Code39Extended,
    Code128,
    Ean13,
    Ean8,
    UpcA,
    Isbn,
    QrCode,
}

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string InputText { get; set; } = "HELLO123";

    [ObservableProperty]
    public partial SupportedSymbology SelectedSymbology { get; set; } = SupportedSymbology.Code39;

    [ObservableProperty]
    public partial Bitmap? PreviewImage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public IReadOnlyList<SupportedSymbology> AvailableSymbologies { get; } =
        Enum.GetValues<SupportedSymbology>();

    public MainViewModel()
    {
        Regenerate();
    }

    partial void OnInputTextChanged(string value) => Regenerate();

    partial void OnSelectedSymbologyChanged(SupportedSymbology value) => Regenerate();

    private void Regenerate()
    {
        ErrorMessage = null;
        PreviewImage = null;

        if (string.IsNullOrEmpty(InputText))
        {
            ErrorMessage = "Enter a value to encode.";
            return;
        }

        try
        {
            byte[] pngBytes;

            if (SelectedSymbology == SupportedSymbology.QrCode)
            {
                var qrEncoder = new QrEncoder();

                if (!qrEncoder.TryEncode(InputText, out var matrix, out var error))
                {
                    ErrorMessage = error;
                    return;
                }

                pngBytes = MatrixRenderer.RenderToPng(matrix!, new MatrixRenderOptions { ModuleSizePixels = 8, QuietZoneModules = 4 });
            }
            else
            {
                IBarcodeEncoder encoder = SelectedSymbology switch
                {
                    SupportedSymbology.Code39 => new Code39Encoder(),
                    SupportedSymbology.Codabar => new CodabarEncoder(),
                    SupportedSymbology.Interleaved2Of5 => new Interleaved2Of5Encoder(),
                    SupportedSymbology.MsiPlessey => new MsiPlesseyEncoder(),
                    SupportedSymbology.Code93 => new Code93Encoder(),
                    SupportedSymbology.Code39Extended => new ExtendedCode39Encoder(),
                    SupportedSymbology.Code128 => new Code128Encoder(),
                    SupportedSymbology.Ean13 => new Ean13Encoder(),
                    SupportedSymbology.Ean8 => new Ean8Encoder(),
                    SupportedSymbology.UpcA => new UpcAEncoder(),
                    SupportedSymbology.Isbn => new IsbnEncoder(),
                    _ => throw new ArgumentOutOfRangeException(),
                };

                if (!encoder.TryEncode(InputText, out var pattern, out var error))
                {
                    ErrorMessage = error;
                    return;
                }

                pngBytes = BarcodeRenderer.RenderToPng(pattern!, new BarcodeRenderOptions { ModuleWidthPixels = 2, QuietZoneModules = 10, BarHeightPixels = 80 });
            }

            using var stream = new MemoryStream(pngBytes);
            PreviewImage = new Bitmap(stream);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
    }
}
