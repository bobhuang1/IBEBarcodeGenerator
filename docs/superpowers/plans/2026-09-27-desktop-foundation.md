# Desktop Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up `IBEBarcode.Desktop` — the first real UI, an Avalonia MVVM app (Windows/Linux/macOS) that lets someone type a value, pick a barcode symbology, and see the rendered barcode on screen. This is the first point in the project where all the layers (Core, Rendering) come together into something a person actually uses.

**Architecture:** A standard Avalonia MVVM app (`Avalonia.Templates`' `avalonia.mvvm` template) with one view model, `MainWindowViewModel`: an `InputText` string property, a `SelectedSymbology` enum-backed property, a computed/refreshed `PreviewImage` (an Avalonia `Bitmap` decoded from `BarcodeRenderer.RenderToPng`/`MatrixRenderer.RenderToPng` output), and an `ErrorMessage` string for encode failures. `MainWindow.axaml` binds a `TextBox`, a `ComboBox` (symbologies), an `Image`, and an error `TextBlock` to these. Generation re-runs whenever input or symbology changes (no separate "Generate" button — instant feedback is simpler to implement and use for a first cut).

**Scope decision:** Only the symbologies that take a plain string and a fixed `IBarcodeEncoder`-style interface are wired into the picker for this first pass: `Code39`, `Codabar`, `Interleaved2Of5`, `MsiPlessey`, `Code93`, `Code39Extended`, `Code128` (Set B), and `QrCode` (via `IMatrixBarcodeEncoder`, rendered with `MatrixRenderer`). The EAN/UPC/ISBN family (fixed-length numeric with mandatory check digits) and Postnet (a `HeightBarPattern`, which has no renderer yet) need slightly different input UX (a length hint, or a renderer that doesn't exist yet) and are follow-up work, not silently unsupported forever.

**Tech Stack:** .NET 10, Avalonia (MVVM template, `CommunityToolkit.Mvvm` for `[ObservableProperty]`/`[RelayCommand]`-style boilerplate reduction, which the template pulls in by default), referencing `IBEBarcode.Core` and `IBEBarcode.Rendering`.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) `net10.0`, MIT license, no database, single solution file.

---

### Task 1: Scaffold `IBEBarcode.Desktop` and wire up live barcode preview

**Files:**
- Create: `src/IBEBarcode.Desktop/` (full Avalonia MVVM app, from the `avalonia.mvvm` template)
- Modify: `src/IBEBarcode.Desktop/ViewModels/MainWindowViewModel.cs`
- Modify: `src/IBEBarcode.Desktop/Views/MainWindow.axaml`

**Interfaces:**
- Consumes: `IBEBarcode.Core.IBarcodeEncoder` implementations, `IBEBarcode.Core.IMatrixBarcodeEncoder` (`QrEncoder`), `IBEBarcode.Rendering.BarcodeRenderer`/`MatrixRenderer`.
- Produces: a runnable desktop app (`dotnet run` from `src/IBEBarcode.Desktop`).

- [ ] **Step 1: Scaffold the project**

Run from the repo root:

```bash
dotnet new avalonia.mvvm -o src/IBEBarcode.Desktop -n IBEBarcode.Desktop --Font Inter
dotnet sln add src/IBEBarcode.Desktop/IBEBarcode.Desktop.csproj
dotnet add src/IBEBarcode.Desktop/IBEBarcode.Desktop.csproj reference src/IBEBarcode.Core/IBEBarcode.Core.csproj src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj
```

- [ ] **Step 2: Verify the scaffolded app builds**

Run: `dotnet build src/IBEBarcode.Desktop/IBEBarcode.Desktop.csproj`
Expected: builds cleanly (this confirms the template + new project references resolve before any custom code is added).

- [ ] **Step 3: Replace the view model**

Replace `src/IBEBarcode.Desktop/ViewModels/MainWindowViewModel.cs` with:

```csharp
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
    QrCode,
}

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = "HELLO123";

    [ObservableProperty]
    private SupportedSymbology _selectedSymbology = SupportedSymbology.Code39;

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private string? _errorMessage;

    public IReadOnlyList<SupportedSymbology> AvailableSymbologies { get; } =
        Enum.GetValues<SupportedSymbology>();

    public MainWindowViewModel()
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
```

- [ ] **Step 4: Replace the main window view**

Replace the `<StackPanel>` (or equivalent root content) inside `src/IBEBarcode.Desktop/Views/MainWindow.axaml` with:

```xml
<StackPanel Margin="16" Spacing="12">
    <TextBlock Text="IBE Barcode Generator" FontSize="20" FontWeight="Bold" />

    <TextBox Watermark="Value to encode" Text="{Binding InputText}" />

    <ComboBox ItemsSource="{Binding AvailableSymbologies}" SelectedItem="{Binding SelectedSymbology}" />

    <TextBlock Text="{Binding ErrorMessage}" Foreground="Red" TextWrapping="Wrap" IsVisible="{Binding ErrorMessage, Converter={x:Static ObjectConverters.IsNotNull}}" />

    <Border BorderBrush="Gray" BorderThickness="1" Padding="8" HorizontalAlignment="Left">
        <Image Source="{Binding PreviewImage}" MaxWidth="600" />
    </Border>
</StackPanel>
```

Make sure the `<Window>` root element in that file has the `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` namespace (the template includes this by default) so `x:Static` resolves, and add `Width="700" Height="500"` on the `<Window>` element if not already sized.

- [ ] **Step 5: Build and smoke-run the app**

Run: `dotnet build src/IBEBarcode.Desktop/IBEBarcode.Desktop.csproj`
Expected: builds cleanly.

If a display is available in this environment, run `dotnet run --project src/IBEBarcode.Desktop` and confirm the window opens showing a Code 39 barcode for "HELLO123" by default, and that changing the text box or the symbology picker updates the preview live. If no display is available, building cleanly is the achievable bar for this step — note in the commit/summary that a manual run wasn't possible here.

- [ ] **Step 6: Commit**

```bash
git add IBEBarcodeGenerator.slnx src/IBEBarcode.Desktop
git commit -m "$(cat <<'EOF'
Add IBEBarcode.Desktop Avalonia app with live barcode preview

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

EAN/UPC/ISBN and Postnet in the symbology picker (need input-length
hints / a Postnet renderer first). Batch/sequential printing UI wired to
`IBEBarcode.Printing` + `IBEBarcode.Templates` (paper template picker,
label grid designer). Export to BMP/GIF/JPG/TIFF and clipboard copy.
Then `IBEBarcode.Web` (Blazor WebAssembly) — a separate UI over the same
Core/Rendering libraries. Data Matrix, PDF417, Aztec remain outstanding
from the barcode-format side.
