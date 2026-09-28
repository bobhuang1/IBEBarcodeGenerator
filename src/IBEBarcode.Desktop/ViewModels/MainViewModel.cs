using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;
using IBEBarcode.Printing;
using IBEBarcode.Rendering;
using IBEBarcode.Templates;

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
    UpcE,
    Isbn,
    QrCode,
    Postnet,
    DataMatrix,
    Pdf417,
    Aztec,
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

    [ObservableProperty]
    public partial PaperTemplate? SelectedTemplate { get; set; }

    public IReadOnlyList<SupportedSymbology> AvailableSymbologies { get; } =
        Enum.GetValues<SupportedSymbology>();

    public IReadOnlyList<PaperTemplate> AvailableTemplates { get; } =
        PaperTemplateCatalog.AllTemplates;

    private byte[]? _lastPngBytes;

    public MainViewModel()
    {
        SelectedTemplate = AvailableTemplates.Count > 0 ? AvailableTemplates[0] : null;
        Regenerate();
    }

    public bool TryGenerateLabelSheetPdf(out byte[]? pdfBytes, out string? error)
    {
        pdfBytes = null;

        if (_lastPngBytes is null)
        {
            error = "Generate a valid barcode before exporting a label sheet.";
            return false;
        }

        if (SelectedTemplate is null)
        {
            error = "Select a paper template before exporting a label sheet.";
            return false;
        }

        var images = new List<byte[]>();

        for (var i = 0; i < SelectedTemplate.LabelCount; i++)
        {
            images.Add(_lastPngBytes);
        }

        pdfBytes = LabelSheetPdfGenerator.Generate(SelectedTemplate, images);
        error = null;
        return true;
    }

    partial void OnInputTextChanged(string value) => Regenerate();

    partial void OnSelectedSymbologyChanged(SupportedSymbology value) => Regenerate();

    private void Regenerate()
    {
        ErrorMessage = null;
        PreviewImage = null;
        _lastPngBytes = null;

        if (string.IsNullOrEmpty(InputText))
        {
            ErrorMessage = "Enter a value to encode.";
            return;
        }

        try
        {
            byte[] pngBytes;

            if (SelectedSymbology is SupportedSymbology.QrCode or SupportedSymbology.DataMatrix or SupportedSymbology.Pdf417 or SupportedSymbology.Aztec)
            {
                IMatrixBarcodeEncoder matrixEncoder = SelectedSymbology switch
                {
                    SupportedSymbology.QrCode => new QrEncoder(),
                    SupportedSymbology.DataMatrix => new DataMatrixEncoder(),
                    SupportedSymbology.Pdf417 => new Pdf417Encoder(),
                    _ => new AztecEncoder(),
                };

                if (!matrixEncoder.TryEncode(InputText, out var matrix, out var error))
                {
                    ErrorMessage = error;
                    return;
                }

                pngBytes = MatrixRenderer.RenderToPng(matrix!, new MatrixRenderOptions { ModuleSizePixels = 8, QuietZoneModules = 4 });
            }
            else if (SelectedSymbology == SupportedSymbology.Postnet)
            {
                var postnetEncoder = new PostnetEncoder();

                if (!postnetEncoder.TryEncode(InputText, out var heightPattern, out var error))
                {
                    ErrorMessage = error;
                    return;
                }

                pngBytes = HeightBarRenderer.RenderToPng(heightPattern!, new HeightBarRenderOptions { BarWidthPixels = 3, GapPixels = 2 });
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
                    SupportedSymbology.UpcE => new UpcEEncoder(),
                    SupportedSymbology.Isbn => new IsbnEncoder(),
                    _ => throw new ArgumentOutOfRangeException(),
                };

                if (!encoder.TryEncode(InputText, out var pattern, out var error))
                {
                    ErrorMessage = error;
                    return;
                }

                pngBytes = BarcodeRenderer.RenderToPng(pattern!, new BarcodeRenderOptions { ModuleWidthPixels = 2, QuietZoneModules = 10, BarHeightPixels = 80, ShowHumanReadableText = true, TextHeightPixels = 24 });
            }

            _lastPngBytes = pngBytes;
            using var stream = new MemoryStream(pngBytes);
            PreviewImage = new Bitmap(stream);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
    }
}
