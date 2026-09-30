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

## Languages

Both apps ship in four languages — English, Simplified Chinese (简体中文),
Traditional Chinese (繁體中文) and Japanese (日本語). The Desktop app picks
one from the operating system's UI language and lets you change it from the
**Language** menu; the Web app picks one from the browser's preferred
languages and remembers your choice, with `?lang=ja`, `?lang=zh-Hans` or
`?lang=zh-Hant` as an explicit override. Interface text changes language;
the values you encode, the label captions you type and the paper template
names do not.

All four tables live in one shared library, `src/IBEBarcode.Localization`,
as a `StringTable` record with `required` members, so a table that is missing
a key does not compile and `tests/IBEBarcode.Localization.Tests` fails the
build if a translation is left blank. Adding a string means adding it to all
four tables — there is no resource-file tooling and no third-party
i18n package.

## Solution layout

- `src/IBEBarcode.Core` — barcode data models and custom-written encoders
  (no graphics dependency, no third-party barcode library). Formats: Code 39,
  Extended Code 39, Codabar, Interleaved 2 of 5, MSI Plessey, Code 93,
  Code 128 (Set A/B/C, or auto mid-message subset switching), EAN-13,
  EAN-8, UPC-A, UPC-E, UPC 2-digit and 5-digit supplements, GS1-128
  (numeric, or the full official GS1 Application Identifier element-string
  notation), ISBN (Bookland), Postnet, QR Code (auto Numeric/
  Alphanumeric/Byte/Kanji mode selection, all 40 versions, multi-block
  Reed-Solomon, mask-pattern scoring), Data Matrix (ASCII mode by default,
  plus optional C40/Text/X12/Base256 encodation modes, all 9 square sizes
  plus 2 rectangular sizes, multi-region and multi-block up to 1304
  bytes), PDF417 (Byte Compaction by default, plus optional Compact/
  Numeric Compaction/Text Compaction modes), and Aztec Code (Binary Shift
  mode by default, plus optional text compaction across Upper/Lower/
  Digit/Mixed/Punct submodes, all layers 1-32).
- `src/IBEBarcode.Rendering` — SkiaSharp renderers for all three pattern
  shapes: linear (`BarcodeRenderer`), 2D grid (`MatrixRenderer` for QR),
  and height-varying (`HeightBarRenderer` for Postnet).
- `src/IBEBarcode.Localization` — the four-language interface strings
  (`StringTable` + `Strings.Get(language)`), the `AppLanguage` enum and the
  culture-name detection shared by both apps. No graphics or platform
  dependency.
- `src/IBEBarcode.Templates` — `PaperTemplate` model and a catalog of
  real-world label sheet layouts (Avery 5160, 5161, 5163, ...).
- `src/IBEBarcode.Printing` — PdfSharp-based label sheet PDF generation
  from a `PaperTemplate` and a set of rendered label images. Works
  identically on desktop .NET and in the browser under Blazor
  WebAssembly.
- `src/IBEBarcode.Desktop` — Avalonia MVVM app (Windows/Linux/macOS): live
  barcode preview and PDF label sheet export via a native save dialog, in
  all four languages.
- `src/IBEBarcode.Web` — Blazor WebAssembly standalone app: the same live
  preview and PDF export, running entirely client-side in the browser (no
  server, deployable as a static site), with a language selector and the
  choice kept in `localStorage`.
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

`.github/workflows/desktop-release.yml` builds the Desktop app for five
targets — Windows x64, Windows x86, Linux x64, macOS x64, and macOS
ARM64 — as self-contained, single-file executables. Each target gets a
portable archive (`.zip`/`.tar.gz`, just unzip and run) and an installer
(Windows `.msi` via WiX, macOS `.dmg` containing a `.app` bundle, Linux
`.deb`). It runs on every push to `master` (build-only, to catch
breakage early) and, when the push is a `vX.Y.Z` tag, also publishes
all ten artifacts to a GitHub Release. No secrets are required. The
Windows MSI is built with WiX v5, pinned below WiX v7's paid Open
Source Maintenance Fee requirement.

## License

MIT — see `LICENSE`.
