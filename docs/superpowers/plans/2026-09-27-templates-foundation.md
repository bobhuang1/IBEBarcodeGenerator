# Templates Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up `IBEBarcode.Templates` — the paper-template data model the legacy product's "1,000+ label paper templates" feature is built on — with a `PaperTemplate` type, position math, and a small seeded catalog of real, verified templates.

**Architecture:** `PaperTemplate` is a plain data record: page size, grid (rows × columns), label size, margins, and gaps, all in millimeters (a metric-only internal unit keeps math simple; UI layers convert to inches/points for display). `LabelPosition(column, row)` computes a label's top-left corner. `PaperTemplateCatalog` holds a small, growable set of named templates (`Vendor` + `Code`, e.g. `"Avery"` + `"5160"`) with a lookup method. Reaching full "1,000+" catalog coverage is incremental data entry (each new template is a one-line addition to the catalog), not an architectural task — this plan seeds it with 2 real, dimensionally-verified US Letter templates and documents the pattern for adding more. Users can also construct a `PaperTemplate` directly for one-off custom layouts — no separate "custom template" type is needed since `PaperTemplate` has no vendor-catalog-specific behavior.

**Tech Stack:** .NET 10, xUnit. No dependency on `IBEBarcode.Core`/`Rendering` — this project only knows about paper geometry, not barcodes.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) `net10.0`, MIT license, no database, `Nullable`/`ImplicitUsings` enabled.

## Verified reference values used by this plan

- **Avery 5160** (US Letter, 215.9mm × 279.4mm): 3 columns × 10 rows, label 66.675mm × 25.4mm, top/bottom margin 12.7mm, left/right margin 4.7625mm, horizontal gap 3.175mm, no vertical gap. Independently sourced (Avery's own published spec) and self-consistency-checked: `4.7625 + 3×66.675 + 2×3.175 + 4.7625 = 215.9` and `12.7 + 10×25.4 + 12.7 = 279.4` — both exact.
- **Avery 5163** (US Letter): 2 columns × 5 rows, label 101.6mm × 50.8mm (landscape — a 4"×2" shipping label laid out 4" wide), sourced label/grid size only; margins (top/left 12.7mm/6.35mm, no gaps) were *computed* by solving for the values that make the grid exactly fill a Letter page, following the same round-number pattern (12.7mm = 0.5") independently confirmed for 5160 and 5164 — not directly vendor-sourced, so flagged as computed in code comments. Self-consistency check: `6.35 + 2×101.6 + 6.35 = 215.9` and `12.7 + 5×50.8 + 12.7 = 279.4` — both exact.

---

### Task 1: `PaperTemplate` and `PaperTemplateCatalog`

**Files:**
- Create: `src/IBEBarcode.Templates/IBEBarcode.Templates.csproj`
- Create: `src/IBEBarcode.Templates/PaperTemplate.cs`
- Create: `src/IBEBarcode.Templates/PaperTemplateCatalog.cs`
- Create: `tests/IBEBarcode.Templates.Tests/IBEBarcode.Templates.Tests.csproj`
- Test: `tests/IBEBarcode.Templates.Tests/PaperTemplateTests.cs`
- Test: `tests/IBEBarcode.Templates.Tests/PaperTemplateCatalogTests.cs`

**Interfaces:**
- Produces:
  - `sealed class PaperTemplate` — required `Vendor`, `Code` (strings); `PageWidthMm`, `PageHeightMm`, `LabelWidthMm`, `LabelHeightMm`, `TopMarginMm`, `LeftMarginMm`, `HorizontalGapMm`, `VerticalGapMm` (doubles); computed `Columns`, `Rows` (ints, required init); `int LabelCount => Columns * Rows`; `(double X, double Y) LabelPosition(int column, int row)`.
  - `static class PaperTemplateCatalog` — `PaperTemplate Avery5160`, `PaperTemplate Avery5163`, `IReadOnlyList<PaperTemplate> AllTemplates`, `PaperTemplate? Find(string vendor, string code)`.

- [ ] **Step 1: Create the projects**

Run from the repo root:

```bash
dotnet new classlib -n IBEBarcode.Templates -o src/IBEBarcode.Templates -f net10.0
dotnet new xunit -n IBEBarcode.Templates.Tests -o tests/IBEBarcode.Templates.Tests -f net10.0
dotnet sln add src/IBEBarcode.Templates/IBEBarcode.Templates.csproj tests/IBEBarcode.Templates.Tests/IBEBarcode.Templates.Tests.csproj
dotnet add tests/IBEBarcode.Templates.Tests/IBEBarcode.Templates.Tests.csproj reference src/IBEBarcode.Templates/IBEBarcode.Templates.csproj
```

Delete the template stub files: `src/IBEBarcode.Templates/Class1.cs`, `tests/IBEBarcode.Templates.Tests/UnitTest1.cs`.

Edit `src/IBEBarcode.Templates/IBEBarcode.Templates.csproj`'s `<PropertyGroup>` to add `<RootNamespace>IBEBarcode.Templates</RootNamespace>`.

Edit `tests/IBEBarcode.Templates.Tests/IBEBarcode.Templates.Tests.csproj`'s `<PropertyGroup>` to add `<IsTestProject>true</IsTestProject>` and `<RootNamespace>IBEBarcode.Templates.Tests</RootNamespace>`.

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Templates.Tests/PaperTemplateTests.cs`:

```csharp
namespace IBEBarcode.Templates.Tests;

public class PaperTemplateTests
{
    private static PaperTemplate SimpleGrid() => new()
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
    public void LabelCount_IsColumnsTimesRows()
    {
        Assert.Equal(4, SimpleGrid().LabelCount);
    }

    [Fact]
    public void LabelPosition_FirstLabel_IsAtTopLeftMargin()
    {
        var (x, y) = SimpleGrid().LabelPosition(0, 0);

        Assert.Equal(5, x);
        Assert.Equal(10, y);
    }

    [Fact]
    public void LabelPosition_SecondColumn_AccountsForLabelWidthAndGap()
    {
        var (x, _) = SimpleGrid().LabelPosition(1, 0);

        Assert.Equal(5 + 40 + 5, x);
    }

    [Fact]
    public void LabelPosition_SecondRow_AccountsForLabelHeightAndGap()
    {
        var (_, y) = SimpleGrid().LabelPosition(0, 1);

        Assert.Equal(10 + 30 + 5, y);
    }

    [Fact]
    public void LabelPosition_ColumnOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimpleGrid().LabelPosition(2, 0));
    }

    [Fact]
    public void LabelPosition_RowOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimpleGrid().LabelPosition(0, 2));
    }
}
```

Create `tests/IBEBarcode.Templates.Tests/PaperTemplateCatalogTests.cs`:

```csharp
namespace IBEBarcode.Templates.Tests;

public class PaperTemplateCatalogTests
{
    [Fact]
    public void Avery5160_ExactlyFillsUsLetterPageWidth()
    {
        var t = PaperTemplateCatalog.Avery5160;

        var totalWidth = t.LeftMarginMm + t.Columns * t.LabelWidthMm + (t.Columns - 1) * t.HorizontalGapMm + t.LeftMarginMm;

        Assert.Equal(t.PageWidthMm, totalWidth, precision: 4);
    }

    [Fact]
    public void Avery5160_ExactlyFillsUsLetterPageHeight()
    {
        var t = PaperTemplateCatalog.Avery5160;

        var totalHeight = t.TopMarginMm + t.Rows * t.LabelHeightMm + (t.Rows - 1) * t.VerticalGapMm + t.TopMarginMm;

        Assert.Equal(t.PageHeightMm, totalHeight, precision: 4);
    }

    [Fact]
    public void Avery5163_ExactlyFillsUsLetterPage()
    {
        var t = PaperTemplateCatalog.Avery5163;

        var totalWidth = t.LeftMarginMm + t.Columns * t.LabelWidthMm + (t.Columns - 1) * t.HorizontalGapMm + t.LeftMarginMm;
        var totalHeight = t.TopMarginMm + t.Rows * t.LabelHeightMm + (t.Rows - 1) * t.VerticalGapMm + t.TopMarginMm;

        Assert.Equal(t.PageWidthMm, totalWidth, precision: 4);
        Assert.Equal(t.PageHeightMm, totalHeight, precision: 4);
    }

    [Fact]
    public void Find_KnownVendorAndCode_ReturnsTemplate()
    {
        var found = PaperTemplateCatalog.Find("avery", "5160");

        Assert.NotNull(found);
        Assert.Equal(30, found!.LabelCount);
    }

    [Fact]
    public void Find_UnknownCode_ReturnsNull()
    {
        Assert.Null(PaperTemplateCatalog.Find("Avery", "99999"));
    }

    [Fact]
    public void AllTemplates_ContainsSeededTemplates()
    {
        Assert.Contains(PaperTemplateCatalog.AllTemplates, t => t.Code == "5160");
        Assert.Contains(PaperTemplateCatalog.AllTemplates, t => t.Code == "5163");
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `PaperTemplate`/`PaperTemplateCatalog` do not exist yet.

- [ ] **Step 4: Implement**

Create `src/IBEBarcode.Templates/PaperTemplate.cs`:

```csharp
namespace IBEBarcode.Templates;

public sealed class PaperTemplate
{
    public required string Vendor { get; init; }
    public required string Code { get; init; }
    public required double PageWidthMm { get; init; }
    public required double PageHeightMm { get; init; }
    public required int Columns { get; init; }
    public required int Rows { get; init; }
    public required double LabelWidthMm { get; init; }
    public required double LabelHeightMm { get; init; }
    public required double TopMarginMm { get; init; }
    public required double LeftMarginMm { get; init; }
    public required double HorizontalGapMm { get; init; }
    public required double VerticalGapMm { get; init; }

    public int LabelCount => Columns * Rows;

    public (double X, double Y) LabelPosition(int column, int row)
    {
        if (column < 0 || column >= Columns)
            throw new ArgumentOutOfRangeException(nameof(column));

        if (row < 0 || row >= Rows)
            throw new ArgumentOutOfRangeException(nameof(row));

        var x = LeftMarginMm + column * (LabelWidthMm + HorizontalGapMm);
        var y = TopMarginMm + row * (LabelHeightMm + VerticalGapMm);
        return (x, y);
    }
}
```

Create `src/IBEBarcode.Templates/PaperTemplateCatalog.cs`:

```csharp
namespace IBEBarcode.Templates;

public static class PaperTemplateCatalog
{
    public static readonly PaperTemplate Avery5160 = new()
    {
        Vendor = "Avery",
        Code = "5160",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 3,
        Rows = 10,
        LabelWidthMm = 66.675,
        LabelHeightMm = 25.4,
        TopMarginMm = 12.7,
        LeftMarginMm = 4.7625,
        HorizontalGapMm = 3.175,
        VerticalGapMm = 0,
    };

    // Margins are computed (solved for an exact US Letter fit), not directly
    // vendor-sourced — see the plan doc for the reasoning and cross-check.
    public static readonly PaperTemplate Avery5163 = new()
    {
        Vendor = "Avery",
        Code = "5163",
        PageWidthMm = 215.9,
        PageHeightMm = 279.4,
        Columns = 2,
        Rows = 5,
        LabelWidthMm = 101.6,
        LabelHeightMm = 50.8,
        TopMarginMm = 12.7,
        LeftMarginMm = 6.35,
        HorizontalGapMm = 0,
        VerticalGapMm = 0,
    };

    private static readonly PaperTemplate[] All = { Avery5160, Avery5163 };

    public static IReadOnlyList<PaperTemplate> AllTemplates => All;

    public static PaperTemplate? Find(string vendor, string code) =>
        All.FirstOrDefault(t =>
            string.Equals(t.Vendor, vendor, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `PaperTemplateTests`/`PaperTemplateCatalogTests` green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add IBEBarcodeGenerator.slnx src/IBEBarcode.Templates tests/IBEBarcode.Templates.Tests
git commit -m "$(cat <<'EOF'
Add IBEBarcode.Templates with PaperTemplate and a seeded catalog

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

More catalog entries (the "1,000+" templates are incremental data
entry — each new vendor/code is a one-line `PaperTemplate` addition to
`PaperTemplateCatalog`, verified the same way as this plan's two).
`IBEBarcode.Printing` (PdfSharp) consumes `PaperTemplate` to lay out a
label sheet as a PDF page. Then `IBEBarcode.Desktop` (Avalonia) and
`IBEBarcode.Web` (Blazor WebAssembly). Data Matrix, PDF417, and Aztec
remain outstanding from the barcode-format side.
