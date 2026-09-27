# Web Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up `IBEBarcode.Web` — a Blazor WebAssembly standalone app (per the design spec's hosting decision: fully static after build, free Azure Static Web Apps hosting, no server compute) offering the same core experience as the desktop app: type a value, pick a symbology, see the barcode.

**Architecture:** `dotnet new blazorwasm` standalone template, replacing its default counter/weather pages with a single `Home.razor` page that mirrors `IBEBarcode.Desktop`'s `MainViewModel` logic directly in a Razor component's code-behind (no separate ViewModel layer — Blazor components are already the "view + state" unit, so introducing an extra ViewModel class here would just be indirection with no consumer). Barcode encoding runs entirely client-side in the browser via WebAssembly (`IBEBarcode.Core`), and rendering produces a PNG via `IBEBarcode.Rendering` (SkiaSharp has a WASM-compatible build), which is displayed via a base64 data URI `<img>` tag — no server round-trip at any point.

**Tech Stack:** .NET 10, Blazor WebAssembly, referencing `IBEBarcode.Core` and `IBEBarcode.Rendering` directly (both already pure .NET class libraries with no server-only dependencies, so they should run under WASM without changes — this plan's build/run step is the actual verification of that assumption).

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) `net10.0`, MIT license, no database, single solution file.

---

### Task 1: Scaffold `IBEBarcode.Web` and wire up live barcode preview

**Files:**
- Create: `src/IBEBarcode.Web/` (full Blazor WebAssembly standalone app)
- Modify: `src/IBEBarcode.Web/Pages/Home.razor` (or wherever the template puts the default landing page)
- Delete: template sample pages that don't apply (Counter, Weather) if present

**Interfaces:**
- Consumes: `IBEBarcode.Core.IBarcodeEncoder`/`IMatrixBarcodeEncoder` implementations, `IBEBarcode.Rendering.BarcodeRenderer`/`MatrixRenderer`.
- Produces: a buildable, publishable static web app (`dotnet build`/`dotnet publish` from `src/IBEBarcode.Web`).

- [ ] **Step 1: Scaffold the project**

Run from the repo root:

```bash
dotnet new blazorwasm -o src/IBEBarcode.Web -n IBEBarcode.Web
dotnet sln add src/IBEBarcode.Web/IBEBarcode.Web.csproj
dotnet add src/IBEBarcode.Web/IBEBarcode.Web.csproj reference src/IBEBarcode.Core/IBEBarcode.Core.csproj src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj
```

- [ ] **Step 2: Verify the scaffolded app builds before any custom code**

Run: `dotnet build src/IBEBarcode.Web/IBEBarcode.Web.csproj`
Expected: builds cleanly — this confirms `IBEBarcode.Core`/`IBEBarcode.Rendering` (and SkiaSharp's WASM native assets) actually resolve for the `browser-wasm` target before customizing anything. If SkiaSharp fails to restore a WASM-compatible native asset here, that's a real finding to report, not something to route around silently.

- [ ] **Step 3: Inspect the actual scaffolded page structure**

Look at whatever `src/IBEBarcode.Web/Pages/` (or `src/IBEBarcode.Web/Components/Pages/`, depending on the exact template version) contains, and note the actual namespace/routing convention used, before writing the replacement page — template output has drifted before in this project (see the desktop plan's `MainViewModel` vs. assumed `MainWindowViewModel` naming).

- [ ] **Step 4: Replace the home page**

Replace the default landing page's content with:

```razor
@page "/"

<PageTitle>IBE Barcode Generator</PageTitle>

<h1>IBE Barcode Generator</h1>

<div class="mb-3">
    <label>Value to encode</label>
    <input class="form-control" @bind="InputText" @bind:event="oninput" />
</div>

<div class="mb-3">
    <label>Symbology</label>
    <select class="form-select" @bind="SelectedSymbology">
        @foreach (var symbology in AvailableSymbologies)
        {
            <option value="@symbology">@symbology</option>
        }
    </select>
</div>

@if (ErrorMessage is not null)
{
    <p style="color:red">@ErrorMessage</p>
}

@if (PreviewImageDataUri is not null)
{
    <img src="@PreviewImageDataUri" style="max-width:600px;border:1px solid gray;padding:8px" />
}

@code {
    private string _inputText = "HELLO123";
    private SupportedSymbology _selectedSymbology = SupportedSymbology.Code39;

    private string InputText
    {
        get => _inputText;
        set { _inputText = value; Regenerate(); }
    }

    private SupportedSymbology SelectedSymbology
    {
        get => _selectedSymbology;
        set { _selectedSymbology = value; Regenerate(); }
    }

    private string? ErrorMessage;
    private string? PreviewImageDataUri;

    public IReadOnlyList<SupportedSymbology> AvailableSymbologies { get; } =
        Enum.GetValues<SupportedSymbology>();

    protected override void OnInitialized() => Regenerate();

    private void Regenerate()
    {
        ErrorMessage = null;
        PreviewImageDataUri = null;

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

            PreviewImageDataUri = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
    }

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
}
```

Add `@using IBEBarcode.Core`, `@using IBEBarcode.Core.Encoders`, and `@using IBEBarcode.Rendering` either at the top of this file or to `_Imports.razor`.

Delete the template's sample pages (`Counter.razor`, `Weather.razor` or equivalents) and their nav-menu entries if the template scaffolded a nav layout referencing them, so the build doesn't carry unused sample code.

- [ ] **Step 5: Build and confirm the WASM app runs**

Run: `dotnet build src/IBEBarcode.Web/IBEBarcode.Web.csproj`
Expected: builds cleanly.

Run `dotnet run --project src/IBEBarcode.Web` and, if a browser can reach the printed localhost URL in this environment, confirm the page loads and shows a Code 39 barcode for "HELLO123" by default, updating live as the input or symbology selection changes. If browser verification isn't possible in this environment, a clean build plus a successful `dotnet publish` (which fully exercises the WASM/AOT toolchain) is the achievable bar — say so explicitly rather than claiming it was verified in a browser.

- [ ] **Step 6: Commit**

```bash
git add IBEBarcodeGenerator.slnx src/IBEBarcode.Web
git commit -m "$(cat <<'EOF'
Add IBEBarcode.Web Blazor WebAssembly app with live barcode preview

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Styling/layout polish, EAN/UPC/ISBN and Postnet in the symbology picker
(same follow-up noted in the desktop plan), deployment to Azure Static
Web Apps, PDF export via `IBEBarcode.Printing` (note: PdfSharp's
WASM compatibility is unverified — may need a server-side fallback or a
different in-browser PDF path). Data Matrix, PDF417, Aztec remain
outstanding from the barcode-format side.
