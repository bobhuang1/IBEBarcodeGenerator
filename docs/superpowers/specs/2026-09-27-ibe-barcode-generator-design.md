# IBE Barcode Generator — Architecture Design

Date: 2026-09-27
Status: Approved

## Goal

Recreate the lost "IBE Barcode Studio" (VB, Windows-only, commercial) as
**IBE Barcode Generator**: free, open source (MIT), cross-platform, with
full feature parity to the old Professional Edition plus QR/common modern
barcodes. Source of truth for legacy feature scope:
`docs/IBE-BarcodeStudioProfessional.htm`, `docs/IBE-BarcodeStudioStandard.htm`.

## Requirements (from rewrite.md)

1. Runs on Windows/Linux/Mac as installable/executable, and as a web site
   (ibebarcode.com, eventually free Azure hosting). Desktop and web share
   the same code and live in a single solution file.
2. Targets .NET 10.
3. Supports all commonly used barcode formats, especially QR codes.
4. No database dependency.
5. MIT license.

## Legacy feature scope (Professional Edition, to match/exceed)

- 16 barcode formats: Code 39, Extended Code 39, Code 93, MSI Plessey,
  Interleaved 2 of 5, Codabar, Code 128, EAN-13, EAN-8, UCC/EAN-128,
  UPC-A, UPC-E, UPC 2 (supplement), UPC 5 (supplement), Postnet, ISBN.
- Spreadsheet-like barcode designer.
- Mix and match / combine multiple barcode labels on one sheet.
- Define row/column placement on the page.
- Two user-definable fields per barcode.
- Auto-increment on barcode field and second user field.
- Batch print of sequential barcodes (legacy: up to ~1,000 labels).
- Export label to BMP/GIF/JPG/TIF; copy to clipboard.
- Paper template support: 1,000+ vendor templates (Avery, APLI, Devauzet,
  ERO, Formtec, Herma, Hisago, Kokuyo, MACO, Pimaco, Rank Xerox,
  Zweckform), plus user-defined templates.
- Zoom in/out, barcode rotation (90/180/270).

New scope beyond legacy: QR Code (priority), Data Matrix, PDF417, Aztec.

## Decisions made during brainstorming

- **Desktop UI**: Avalonia (native XAML), not Blazor Hybrid/MAUI —
  chosen for true native Windows/Linux/Mac support. Desktop and web UI
  are **not** code-shared at the UI layer; only the Core/Rendering/
  Printing/Templates libraries are shared.
- **Web UI**: Blazor WebAssembly (standalone), not Blazor Server — fully
  static after build, hosts free on Azure Static Web Apps, no server
  compute, fits "no database" and free-tier hosting.
- **Barcode encoding**: all custom-written encoders, no third-party
  barcode library (e.g. no ZXing.Net dependency). Applies to every
  linear format and to QR/DataMatrix/PDF417/Aztec.
- **Rendering**: SkiaSharp (MIT), shared by Avalonia desktop (Avalonia
  already uses Skia internally) and Blazor WASM (SkiaSharp has a WASM
  target). One draw routine feeds screen preview, image export, and PDF
  page composition.
- **Printing**: no unified .NET print API across platforms, so printing
  is PDF-first. Label sheet is rendered to a PDF via PdfSharp (MIT);
  Windows uses `PrintDocument` to print the PDF, Linux/Mac hand off to
  system PDF/print handling; web offers PDF download and browser print.
- **Repo/licensing**: MIT license for the whole repo. All chosen
  dependencies (Avalonia, SkiaSharp, PdfSharp) are MIT-licensed —
  deliberately avoided QuestPDF (restrictive commercial tiers) and
  ZXing.Net (would have been fine license-wise but user chose fully
  custom encoders instead).
- **GitHub**: local `git init` only for now; remote/GitHub repo creation
  is deferred to the user.

## Solution structure

Single `.sln` at repo root.

```
/src
  IBEBarcode.Core        barcode data models + custom encoders (pure logic, no graphics dep)
  IBEBarcode.Rendering    SkiaSharp: draw barcode+labels to canvas/bitmap; export PNG/BMP/GIF/JPG/TIF; clipboard bytes
  IBEBarcode.Printing     PdfSharp: label-sheet PDF generation from template+layout; print dispatch
  IBEBarcode.Templates    paper template catalog (JSON) + lookup + user-defined templates
  IBEBarcode.Desktop      Avalonia app (Win/Linux/Mac) — spreadsheet-style label designer UI
  IBEBarcode.Web          Blazor WebAssembly app (ibebarcode.com) — same feature set, browser UI
/tests
  IBEBarcode.Core.Tests
  IBEBarcode.Rendering.Tests
  IBEBarcode.Templates.Tests
/docs                     existing legacy marketing docs, kept as reference
LICENSE                   MIT
README.md
```

Desktop and Web both reference Core/Rendering/Printing/Templates. UI code
is not shared between them (Avalonia XAML vs Razor).

## Core data model

- `LabelCell`: barcode symbology, encoded value, two user-definable
  fields, auto-increment rule (applies to barcode value and/or second
  user field), rotation (0/90/180/270).
- `LabelSheet`: collection of `LabelCell`s positioned on a `PaperTemplate`
  grid — supports mixing different barcode types/cells on one sheet.
- `PaperTemplate`: vendor, template code, page size, rows, columns, label
  width/height, top/left margin, horizontal/vertical gap, corner radius.
  Seeded with a broad common Avery/APLI/Herma/Formtec/Zweckform set;
  reaching full "1,000+" template coverage is incremental data entry, not
  an architectural blocker. Users can also define custom templates.

## Barcode formats to implement

Linear (custom encoders): Code 39, Extended Code 39, Code 93, Codabar,
Code 128 A/B/C, Interleaved 2 of 5, MSI Plessey, EAN-13, EAN-8, UPC-A,
UPC-E, UPC 2-digit supplement, UPC 5-digit supplement, UCC/EAN-128
(GS1-128), Postnet, ISBN (EAN-13/Bookland check digit).

2D (custom encoders, sequenced by complexity in the implementation plan):
QR Code first (requires Reed-Solomon error correction + mask pattern
selection), then Data Matrix, PDF417, Aztec as follow-on additions.

## Out of scope for this design doc

Exact implementation order/task breakdown (handled by the implementation
plan), detailed UI wireframes for the spreadsheet designer, and the full
1,000+ template data set (seeded incrementally).
