# IBE Barcode Generator

Free, open-source, cross-platform barcode label generator — a from-scratch
recreation of the discontinued "IBE Barcode Studio" (Windows/VB), released
under the MIT license.

## Status

Every barcode format from the legacy Professional Edition's original
16-format list is implemented and tested, plus QR Code. A working desktop
app (Avalonia) and web app (Blazor WebAssembly) both offer live barcode
preview and paper-template PDF label sheet export. See
`docs/superpowers/specs/` for the architecture design and
`docs/superpowers/plans/` for implementation plans (one per subsystem).

## Solution layout

- `src/IBEBarcode.Core` — barcode data models and custom-written encoders
  (no graphics dependency, no third-party barcode library). Formats: Code 39,
  Extended Code 39, Codabar, Interleaved 2 of 5, MSI Plessey, Code 93,
  Code 128 (Set A/B/C, or auto mid-message subset switching), EAN-13,
  EAN-8, UPC-A, UPC-E, UPC 2-digit and 5-digit supplements, GS1-128
  (numeric, or the full official GS1 Application Identifier element-string
  notation), ISBN (Bookland), Postnet, QR Code (auto Numeric/
  Alphanumeric/Byte mode selection, all 40 versions, multi-block
  Reed-Solomon, mask-pattern scoring), Data Matrix (ASCII mode, all 9
  square sizes plus 2 rectangular sizes, multi-region and multi-block
  up to 1304 bytes), PDF417 (Byte Compaction by
  default, plus optional Compact/Numeric Compaction/Text Compaction
  modes), and Aztec Code (Binary Shift mode, all layers 1-32).
- `src/IBEBarcode.Rendering` — SkiaSharp renderers for all three pattern
  shapes: linear (`BarcodeRenderer`), 2D grid (`MatrixRenderer` for QR),
  and height-varying (`HeightBarRenderer` for Postnet).
- `src/IBEBarcode.Templates` — `PaperTemplate` model and a catalog of
  real-world label sheet layouts (Avery 5160, 5161, 5163, ...).
- `src/IBEBarcode.Printing` — PdfSharp-based label sheet PDF generation
  from a `PaperTemplate` and a set of rendered label images. Works
  identically on desktop .NET and in the browser under Blazor
  WebAssembly.
- `src/IBEBarcode.Desktop` — Avalonia MVVM app (Windows/Linux/macOS): live
  barcode preview and PDF label sheet export via a native save dialog.
- `src/IBEBarcode.Web` — Blazor WebAssembly standalone app: the same live
  preview and PDF export, running entirely client-side in the browser (no
  server, deployable as a static site).
- `tests/` — one xUnit test project per library project above.

## Build and test

```bash
dotnet build
dotnet test
```

Run the desktop app:

```bash
dotnet run --project src/IBEBarcode.Desktop
```

Run the web app:

```bash
dotnet run --project src/IBEBarcode.Web
```

The web app's `WasmBuildNative` build step (needed for SkiaSharp's native
code to run under WebAssembly) requires the `wasm-tools` workload:

```bash
dotnet workload install wasm-tools
```

## Deployment

`.github/workflows/azure-static-web-apps.yml` deploys `src/IBEBarcode.Web`
to Azure Static Web Apps on push to `master`. It needs an
`AZURE_STATIC_WEB_APPS_API_TOKEN` repository secret, which Azure adds
automatically when you connect this repo to a Static Web App resource in
the Azure portal.

## License

MIT — see `LICENSE`.
