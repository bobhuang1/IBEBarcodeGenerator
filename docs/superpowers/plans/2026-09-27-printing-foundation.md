# Printing Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up `IBEBarcode.Printing` — takes a `PaperTemplate` and a set of already-rendered label PNG images, lays them out on a PDF page at the exact millimeter positions the template specifies. This is the "print" half of the PDF-first printing decision in the design spec.

**Architecture:** `LabelSheetPdfGenerator.Generate(PaperTemplate template, IReadOnlyList<byte[]> labelPngImages)` is a single static entry point. It deliberately does **not** depend on `IBEBarcode.Core` or `IBEBarcode.Rendering` — it only knows paper geometry (`IBEBarcode.Templates`) and how to place pre-rendered images (PdfSharp). The caller (a future desktop or web app) renders each barcode to PNG via `IBEBarcode.Rendering` first, then hands the bytes here. This keeps the printing layer simple and reusable regardless of what's being printed.

**Scope decision:** Only image placement — no human-readable text drawn separately (the existing `BarcodeRenderer`/`MatrixRenderer` don't draw text under bars yet either, so there's nothing to place text from). Fewer images than label positions is fine (partial sheets); more images than positions is a caller error and throws.

**Tech Stack:** .NET 10, xUnit, PdfSharp (MIT licensed, cross-platform — the modern, actively maintained fork used by the design spec's PDF-first printing decision). Millimeter-to-point conversion uses the exact constant `72/25.4` rather than any PdfSharp unit-conversion helper, since verifying that helper's exact API surface added avoidable risk for no benefit — the conversion itself is a fixed, well-known ratio.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) `net10.0`, MIT license, no database, `Nullable`/`ImplicitUsings` enabled.

---

### Task 1: `LabelSheetPdfGenerator`

**Files:**
- Create: `src/IBEBarcode.Printing/IBEBarcode.Printing.csproj`
- Create: `src/IBEBarcode.Printing/LabelSheetPdfGenerator.cs`
- Create: `tests/IBEBarcode.Printing.Tests/IBEBarcode.Printing.Tests.csproj`
- Test: `tests/IBEBarcode.Printing.Tests/LabelSheetPdfGeneratorTests.cs`

**Interfaces:**
- Consumes: `IBEBarcode.Templates.PaperTemplate`.
- Produces: `static class LabelSheetPdfGenerator` — `byte[] Generate(PaperTemplate template, IReadOnlyList<byte[]> labelPngImages)`, throws `ArgumentException` if `labelPngImages.Count > template.LabelCount`.

- [ ] **Step 1: Create the projects**

Run from the repo root:

```bash
dotnet new classlib -n IBEBarcode.Printing -o src/IBEBarcode.Printing -f net10.0
dotnet new xunit -n IBEBarcode.Printing.Tests -o tests/IBEBarcode.Printing.Tests -f net10.0
dotnet sln add src/IBEBarcode.Printing/IBEBarcode.Printing.csproj tests/IBEBarcode.Printing.Tests/IBEBarcode.Printing.Tests.csproj
dotnet add src/IBEBarcode.Printing/IBEBarcode.Printing.csproj reference src/IBEBarcode.Templates/IBEBarcode.Templates.csproj
dotnet add tests/IBEBarcode.Printing.Tests/IBEBarcode.Printing.Tests.csproj reference src/IBEBarcode.Printing/IBEBarcode.Printing.csproj
dotnet add tests/IBEBarcode.Printing.Tests/IBEBarcode.Printing.Tests.csproj reference src/IBEBarcode.Templates/IBEBarcode.Templates.csproj
dotnet add src/IBEBarcode.Printing/IBEBarcode.Printing.csproj package PdfSharp
```

Delete the template stub files: `src/IBEBarcode.Printing/Class1.cs`, `tests/IBEBarcode.Printing.Tests/UnitTest1.cs`.

Edit `src/IBEBarcode.Printing/IBEBarcode.Printing.csproj`'s `<PropertyGroup>` to add `<RootNamespace>IBEBarcode.Printing</RootNamespace>`.

Edit `tests/IBEBarcode.Printing.Tests/IBEBarcode.Printing.Tests.csproj`'s `<PropertyGroup>` to add `<IsTestProject>true</IsTestProject>` and `<RootNamespace>IBEBarcode.Printing.Tests</RootNamespace>`.

- [ ] **Step 2: Write the failing tests**

A real PNG produced by `IBEBarcode.Rendering`'s already-tested pipeline stands in
for a rendered label image, rather than a hand-typed byte array (which is easy
to get subtly wrong — a hand-typed PNG in an earlier draft of this plan turned
out to have a corrupted IDAT chunk that PdfSharp's decoder rejected). This
means the test project also references `IBEBarcode.Core` and
`IBEBarcode.Rendering`:

```bash
dotnet add tests/IBEBarcode.Printing.Tests/IBEBarcode.Printing.Tests.csproj reference src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj src/IBEBarcode.Core/IBEBarcode.Core.csproj
```

Create `tests/IBEBarcode.Printing.Tests/LabelSheetPdfGeneratorTests.cs`:

```csharp
using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;
using IBEBarcode.Rendering;
using IBEBarcode.Templates;

namespace IBEBarcode.Printing.Tests;

public class LabelSheetPdfGeneratorTests
{
    private static readonly byte[] TinyPng = RenderRealBarcodePng();

    private static byte[] RenderRealBarcodePng()
    {
        var encoder = new Code39Encoder();
        encoder.TryEncode("A", out var pattern, out _);
        return BarcodeRenderer.RenderToPng(pattern!, new BarcodeRenderOptions { ModuleWidthPixels = 1, QuietZoneModules = 0, BarHeightPixels = 10 });
    }

    private static PaperTemplate SmallTemplate() => new()
    {
        Vendor = "Test",
        Code = "T1",
        PageWidthMm = 100,
        PageHeightMm = 100,
        Columns = 2,
        Rows = 2,
        LabelWidthMm = 40,
        LabelHeightMm = 30,
        TopMarginMm = 10,
        LeftMarginMm = 5,
        HorizontalGapMm = 5,
        VerticalGapMm = 5,
    };

    [Fact]
    public void Generate_ProducesValidPdfBytes()
    {
        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), new[] { TinyPng });

        Assert.True(pdfBytes.Length > 100);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public void Generate_WithNoImages_StillProducesValidPdf()
    {
        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), Array.Empty<byte[]>());

        Assert.True(pdfBytes.Length > 50);
        Assert.Equal((byte)'%', pdfBytes[0]);
    }

    [Fact]
    public void Generate_FullSheet_Succeeds()
    {
        var images = new[] { TinyPng, TinyPng, TinyPng, TinyPng };

        var pdfBytes = LabelSheetPdfGenerator.Generate(SmallTemplate(), images);

        Assert.True(pdfBytes.Length > 100);
    }

    [Fact]
    public void Generate_MoreImagesThanLabelPositions_Throws()
    {
        var images = new[] { TinyPng, TinyPng, TinyPng, TinyPng, TinyPng };

        Assert.Throws<ArgumentException>(() => LabelSheetPdfGenerator.Generate(SmallTemplate(), images));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `LabelSheetPdfGenerator` does not exist yet.

- [ ] **Step 4: Implement**

Create `src/IBEBarcode.Printing/LabelSheetPdfGenerator.cs`:

```csharp
using IBEBarcode.Templates;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace IBEBarcode.Printing;

public static class LabelSheetPdfGenerator
{
    private const double PointsPerMillimeter = 72.0 / 25.4;

    public static byte[] Generate(PaperTemplate template, IReadOnlyList<byte[]> labelPngImages)
    {
        if (labelPngImages.Count > template.LabelCount)
        {
            throw new ArgumentException(
                $"Template '{template.Vendor} {template.Code}' has {template.LabelCount} label positions but {labelPngImages.Count} images were provided.",
                nameof(labelPngImages));
        }

        var document = new PdfDocument();
        var page = document.AddPage();
        page.Width = template.PageWidthMm * PointsPerMillimeter;
        page.Height = template.PageHeightMm * PointsPerMillimeter;

        using var gfx = XGraphics.FromPdfPage(page);

        var index = 0;

        for (var row = 0; row < template.Rows && index < labelPngImages.Count; row++)
        {
            for (var column = 0; column < template.Columns && index < labelPngImages.Count; column++)
            {
                var (xMm, yMm) = template.LabelPosition(column, row);

                using var stream = new MemoryStream(labelPngImages[index]);
                using var image = XImage.FromStream(stream);

                gfx.DrawImage(
                    image,
                    xMm * PointsPerMillimeter,
                    yMm * PointsPerMillimeter,
                    template.LabelWidthMm * PointsPerMillimeter,
                    template.LabelHeightMm * PointsPerMillimeter);

                index++;
            }
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `LabelSheetPdfGeneratorTests` green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add IBEBarcodeGenerator.slnx src/IBEBarcode.Printing tests/IBEBarcode.Printing.Tests
git commit -m "$(cat <<'EOF'
Add IBEBarcode.Printing with LabelSheetPdfGenerator

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Human-readable text under barcodes (needs a font-drawing pass in
`IBEBarcode.Rendering` first). OS print-dialog dispatch of the generated
PDF (Windows `PrintDocument`, Linux/Mac system PDF handling) —
out of scope for a library project; that's a desktop-app-layer concern.
Then `IBEBarcode.Desktop` (Avalonia) and `IBEBarcode.Web` (Blazor
WebAssembly), which are the first real consumers tying Core + Rendering
+ Templates + Printing together into an actual application. Data Matrix,
PDF417, and Aztec remain outstanding from the barcode-format side.
