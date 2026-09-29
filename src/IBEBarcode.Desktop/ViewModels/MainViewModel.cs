using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    Gs1_128,
    Upc2DigitSupplement,
    Upc5DigitSupplement,
}

public partial class MainViewModel : ViewModelBase
{
    private const int MaxQuantity = 1000;
    private const int MinZoomPercent = 25;
    private const int MaxZoomPercent = 400;

    [ObservableProperty]
    public partial string InputText { get; set; } = "34738";

    [ObservableProperty]
    public partial SupportedSymbology SelectedSymbology { get; set; } = SupportedSymbology.Code39;

    [ObservableProperty]
    public partial Bitmap? PreviewImage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial PaperTemplate? SelectedTemplate { get; set; }

    [ObservableProperty]
    public partial bool ShowBarcodeValue { get; set; } = true;

    [ObservableProperty]
    public partial int BarcodeIncrement { get; set; }

    [ObservableProperty]
    public partial int Quantity { get; set; } = 2;

    [ObservableProperty]
    public partial string UserField1 { get; set; } = "IBE Group, Inc";

    [ObservableProperty]
    public partial string UserField2 { get; set; } = "20050112001";

    [ObservableProperty]
    public partial int UserField2Increment { get; set; }

    [ObservableProperty]
    public partial string FontFamily { get; set; } = "Arial";

    [ObservableProperty]
    public partial int FontSize { get; set; } = 10;

    [ObservableProperty]
    public partial bool FontBold { get; set; }

    [ObservableProperty]
    public partial bool FontItalic { get; set; }

    [ObservableProperty]
    public partial bool FontUnderline { get; set; }

    [ObservableProperty]
    public partial int RotationDegrees { get; set; }

    [ObservableProperty]
    public partial int ZoomPercent { get; set; } = 100;

    public double ZoomScale => ZoomPercent / 100.0;

    private const double BasePreviewWidth = 600;
    private const double BasePreviewHeight = 350;

    public double PreviewBoxWidth => BasePreviewWidth * ZoomScale;
    public double PreviewBoxHeight => BasePreviewHeight * ZoomScale;

    public IReadOnlyList<SupportedSymbology> AvailableSymbologies { get; } =
        Enum.GetValues<SupportedSymbology>();

    public IReadOnlyList<PaperTemplate> AvailableTemplates { get; } =
        PaperTemplateCatalog.AllTemplates;

    public IReadOnlyList<string> AvailableFontFamilies { get; } =
        new[] { "Arial", "Times New Roman", "Courier New", "Verdana", "Consolas" };

    public string StatusPageSize => SelectedTemplate is { } t
        ? $"{t.PageWidthMm / 25.4:0.##} inch(es) * {t.PageHeightMm / 25.4:0.##} inch(es)"
        : string.Empty;

    public string StatusLabelSize => SelectedTemplate is { } t
        ? $"{t.LabelWidthMm / 25.4:0.##} inch(es) * {t.LabelHeightMm / 25.4:0.##} inch(es)"
        : string.Empty;

    public string StatusSymbology => SelectedSymbology.ToString();

    public string StatusDate => DateTime.Today.ToString("yyyy-M-d");

    private byte[]? _lastPngBytes;

    public MainViewModel()
    {
        SelectedTemplate = AvailableTemplates.Count > 0 ? AvailableTemplates[0] : null;
        Regenerate();
    }

    public bool TryGenerateLabelSheetPdf(out byte[]? pdfBytes, out string? error)
    {
        pdfBytes = null;

        if (SelectedTemplate is null)
        {
            error = "Select a paper template before exporting a label sheet.";
            return false;
        }

        var clampedQuantity = Math.Clamp(Quantity, 1, MaxQuantity);
        var images = new List<byte[]>();

        for (var i = 0; i < clampedQuantity; i++)
        {
            if (!TryRenderLabel(
                    IncrementNumericSuffix(InputText, BarcodeIncrement * i),
                    IncrementNumericSuffix(UserField2, UserField2Increment * i),
                    out var pngBytes,
                    out error))
            {
                return false;
            }

            images.Add(pngBytes!);
        }

        pdfBytes = LabelSheetPdfGenerator.Generate(SelectedTemplate, images);
        error = null;
        return true;
    }

    [RelayCommand]
    private void RotateLeft() => RotationDegrees = (RotationDegrees + 270) % 360;

    [RelayCommand]
    private void RotateRight() => RotationDegrees = (RotationDegrees + 90) % 360;

    [RelayCommand]
    private void ZoomIn() => ZoomPercent = Math.Min(MaxZoomPercent, ZoomPercent + 25);

    [RelayCommand]
    private void ZoomOut() => ZoomPercent = Math.Max(MinZoomPercent, ZoomPercent - 25);

    partial void OnInputTextChanged(string value) => Regenerate();

    partial void OnSelectedSymbologyChanged(SupportedSymbology value) => Regenerate();

    partial void OnSelectedTemplateChanged(PaperTemplate? value)
    {
        OnPropertyChanged(nameof(StatusPageSize));
        OnPropertyChanged(nameof(StatusLabelSize));
    }

    partial void OnQuantityChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, MaxQuantity);

        if (clamped != value)
        {
            Quantity = clamped;
        }
    }

    partial void OnShowBarcodeValueChanged(bool value) => Regenerate();

    partial void OnUserField1Changed(string value) => Regenerate();

    partial void OnUserField2Changed(string value) => Regenerate();

    partial void OnFontFamilyChanged(string value) => Regenerate();

    partial void OnFontSizeChanged(int value) => Regenerate();

    partial void OnFontBoldChanged(bool value) => Regenerate();

    partial void OnFontItalicChanged(bool value) => Regenerate();

    partial void OnFontUnderlineChanged(bool value) => Regenerate();

    partial void OnRotationDegreesChanged(int value) => Regenerate();

    partial void OnZoomPercentChanged(int value)
    {
        OnPropertyChanged(nameof(ZoomScale));
        OnPropertyChanged(nameof(PreviewBoxWidth));
        OnPropertyChanged(nameof(PreviewBoxHeight));
    }

    private static string IncrementNumericSuffix(string value, int delta)
    {
        if (delta == 0 || string.IsNullOrEmpty(value))
        {
            return value;
        }

        var i = value.Length;

        while (i > 0 && char.IsDigit(value[i - 1]))
        {
            i--;
        }

        if (i == value.Length)
        {
            return value;
        }

        var prefix = value[..i];
        var digits = value[i..];
        var width = digits.Length;

        if (!long.TryParse(digits, out var number))
        {
            return value;
        }

        var next = Math.Max(0, number + delta);
        var nextText = next.ToString();

        if (nextText.Length < width)
        {
            nextText = nextText.PadLeft(width, '0');
        }

        return prefix + nextText;
    }

    private void Regenerate()
    {
        ErrorMessage = null;
        PreviewImage = null;
        _lastPngBytes = null;

        if (!TryRenderLabel(InputText, UserField2, out var pngBytes, out var error))
        {
            ErrorMessage = error;
            return;
        }

        _lastPngBytes = pngBytes;
        using var stream = new MemoryStream(pngBytes!);
        PreviewImage = new Bitmap(stream);
        OnPropertyChanged(nameof(StatusSymbology));
    }

    private bool TryRenderLabel(string barcodeValue, string userField2Value, out byte[]? pngBytes, out string? error)
    {
        pngBytes = null;

        if (string.IsNullOrEmpty(barcodeValue))
        {
            error = "Enter a value to encode.";
            return false;
        }

        try
        {
            byte[] rawPngBytes;

            if (SelectedSymbology is SupportedSymbology.QrCode or SupportedSymbology.DataMatrix or SupportedSymbology.Pdf417 or SupportedSymbology.Aztec)
            {
                IMatrixBarcodeEncoder matrixEncoder = SelectedSymbology switch
                {
                    SupportedSymbology.QrCode => new QrEncoder(),
                    SupportedSymbology.DataMatrix => new DataMatrixEncoder(),
                    SupportedSymbology.Pdf417 => new Pdf417Encoder(),
                    _ => new AztecEncoder(),
                };

                if (!matrixEncoder.TryEncode(barcodeValue, out var matrix, out error))
                {
                    return false;
                }

                rawPngBytes = MatrixRenderer.RenderToPng(matrix!, new MatrixRenderOptions { ModuleSizePixels = 12, QuietZoneModules = 4 });
            }
            else if (SelectedSymbology == SupportedSymbology.Postnet)
            {
                var postnetEncoder = new PostnetEncoder();

                if (!postnetEncoder.TryEncode(barcodeValue, out var heightPattern, out error))
                {
                    return false;
                }

                rawPngBytes = HeightBarRenderer.RenderToPng(heightPattern!, new HeightBarRenderOptions { BarWidthPixels = 5, GapPixels = 3 });
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
                    SupportedSymbology.Code128 => new Code128Encoder(Code128Set.Auto),
                    SupportedSymbology.Ean13 => new Ean13Encoder(),
                    SupportedSymbology.Ean8 => new Ean8Encoder(),
                    SupportedSymbology.UpcA => new UpcAEncoder(),
                    SupportedSymbology.UpcE => new UpcEEncoder(),
                    SupportedSymbology.Isbn => new IsbnEncoder(),
                    SupportedSymbology.Gs1_128 => new Gs1_128Encoder(),
                    SupportedSymbology.Upc2DigitSupplement => new Upc2DigitSupplementEncoder(),
                    SupportedSymbology.Upc5DigitSupplement => new Upc5DigitSupplementEncoder(),
                    _ => throw new ArgumentOutOfRangeException(),
                };

                if (!encoder.TryEncode(barcodeValue, out var pattern, out error))
                {
                    return false;
                }

                rawPngBytes = BarcodeRenderer.RenderToPng(pattern!, new BarcodeRenderOptions
                {
                    ModuleWidthPixels = 4,
                    QuietZoneModules = 10,
                    BarHeightPixels = 160,
                    ShowHumanReadableText = ShowBarcodeValue,
                    TextHeightPixels = Math.Max(32, FontSize * 4),
                    FontFamily = FontFamily,
                    FontBold = FontBold,
                    FontItalic = FontItalic,
                    FontUnderline = FontUnderline,
                });
            }

            pngBytes = LabelComposer.Compose(rawPngBytes, new LabelComposeOptions
            {
                PrefixText = UserField1,
                SuffixText = userField2Value,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontBold = FontBold,
                FontItalic = FontItalic,
                FontUnderline = FontUnderline,
                RotationDegrees = RotationDegrees,
            });

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Unexpected error: {ex.Message}";
            return false;
        }
    }
}
