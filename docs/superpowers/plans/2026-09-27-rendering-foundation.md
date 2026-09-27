# Rendering Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up `IBEBarcode.Rendering` — a SkiaSharp-based renderer that turns any `IBEBarcode.Core` `BarcodePattern` into an actual pixel image (bitmap and PNG bytes). This is the first point in the project where a barcode becomes something you can actually look at.

**Architecture:** One project, `IBEBarcode.Rendering`, referencing `IBEBarcode.Core` and the `SkiaSharp` NuGet package (MIT licensed, cross-platform, works identically on the future Avalonia desktop app and Blazor WebAssembly web app per the design spec). Two public types: `BarcodeRenderOptions` (module width, bar height, quiet zone, colors — all with sane defaults) and `BarcodeRenderer` (draws a `BarcodePattern`'s alternating bar/space `Segments` left to right into an `SKBitmap`, with a quiet zone of blank space on each side, and can also encode that bitmap to PNG bytes).

**Tech Stack:** .NET 10, xUnit, SkiaSharp. Tests inspect actual pixel colors (`SKBitmap.GetPixel`) at hand-computed coordinates rather than just checking image dimensions, so a geometry bug (wrong bar position, wrong quiet zone) fails a test rather than passing silently.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans, plus:) `IBEBarcode.Rendering` depends only on `IBEBarcode.Core` and `SkiaSharp` — no other third-party packages, no UI framework references (Avalonia/Blazor come later and depend on this project, not the other way around).

---

### Task 1: `IBEBarcode.Rendering` project scaffolding

**Files:**
- Create: `src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj`
- Create: `tests/IBEBarcode.Rendering.Tests/IBEBarcode.Rendering.Tests.csproj`

**Interfaces:**
- Consumes: `IBEBarcode.Core` (project reference).
- Produces: a buildable, testable project added to `IBEBarcodeGenerator.slnx`.

- [ ] **Step 1: Create the projects and wire references**

Run from the repo root:

```bash
dotnet new classlib -n IBEBarcode.Rendering -o src/IBEBarcode.Rendering -f net10.0
dotnet new xunit -n IBEBarcode.Rendering.Tests -o tests/IBEBarcode.Rendering.Tests -f net10.0
dotnet sln add src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj tests/IBEBarcode.Rendering.Tests/IBEBarcode.Rendering.Tests.csproj
dotnet add src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj reference src/IBEBarcode.Core/IBEBarcode.Core.csproj
dotnet add tests/IBEBarcode.Rendering.Tests/IBEBarcode.Rendering.Tests.csproj reference src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj
dotnet add src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj package SkiaSharp
```

Delete the template stub files: `src/IBEBarcode.Rendering/Class1.cs` and
`tests/IBEBarcode.Rendering.Tests/UnitTest1.cs`.

- [ ] **Step 2: Set project properties**

Edit `src/IBEBarcode.Rendering/IBEBarcode.Rendering.csproj` to add `RootNamespace` inside the existing `<PropertyGroup>`:

```xml
<RootNamespace>IBEBarcode.Rendering</RootNamespace>
```

Edit `tests/IBEBarcode.Rendering.Tests/IBEBarcode.Rendering.Tests.csproj` to add inside its `<PropertyGroup>`:

```xml
<IsTestProject>true</IsTestProject>
<RootNamespace>IBEBarcode.Rendering.Tests</RootNamespace>
```

- [ ] **Step 3: Write a smoke test**

Create `tests/IBEBarcode.Rendering.Tests/SmokeTests.cs`:

```csharp
namespace IBEBarcode.Rendering.Tests;

public class SmokeTests
{
    [Fact]
    public void TestProjectIsWiredUpCorrectly()
    {
        Assert.Equal(4, 2 + 2);
    }
}
```

- [ ] **Step 4: Build and run tests, verify green**

Run: `dotnet test`
Expected: build succeeds (SkiaSharp restores its native runtime package for the current platform without extra configuration), smoke test passes.

- [ ] **Step 5: Commit**

```bash
git add IBEBarcodeGenerator.slnx src/IBEBarcode.Rendering tests/IBEBarcode.Rendering.Tests
git commit -m "$(cat <<'EOF'
Scaffold IBEBarcode.Rendering project

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: `BarcodeRenderOptions` and `BarcodeRenderer`

**Files:**
- Create: `src/IBEBarcode.Rendering/BarcodeRenderOptions.cs`
- Create: `src/IBEBarcode.Rendering/BarcodeRenderer.cs`
- Test: `tests/IBEBarcode.Rendering.Tests/BarcodeRendererTests.cs`

**Interfaces:**
- Consumes: `IBEBarcode.Core.BarcodePattern`, `BarSegment` (foundation plan); `SkiaSharp` types.
- Produces:
  - `sealed class BarcodeRenderOptions` — `int ModuleWidthPixels { get; init; } = 2`, `int BarHeightPixels { get; init; } = 80`, `int QuietZoneModules { get; init; } = 10`, `SKColor BarColor { get; init; } = SKColors.Black`, `SKColor BackgroundColor { get; init; } = SKColors.White`.
  - `static class BarcodeRenderer` — `SKBitmap Render(BarcodePattern pattern, BarcodeRenderOptions? options = null)` and `byte[] RenderToPng(BarcodePattern pattern, BarcodeRenderOptions? options = null)`.

Image width = quiet zone (both sides) + sum of all segment widths, each scaled by `ModuleWidthPixels`. Height = `BarHeightPixels`. Only `IsBar == true` segments are painted (in `BarColor`); everything else stays `BackgroundColor` (spaces and quiet zone are visually identical — both are just "not ink").

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Rendering.Tests/BarcodeRendererTests.cs`:

```csharp
using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering.Tests;

public class BarcodeRendererTests
{
    [Fact]
    public void Render_SimplePattern_ProducesExpectedPixels()
    {
        var pattern = BarcodePattern.Create("test", new[]
        {
            new BarSegment(true, 2),
            new BarSegment(false, 1),
            new BarSegment(true, 1),
        });

        var options = new BarcodeRenderOptions
        {
            ModuleWidthPixels = 1,
            QuietZoneModules = 0,
            BarHeightPixels = 10,
        };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(4, bitmap.Width);
        Assert.Equal(10, bitmap.Height);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 5));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(1, 5));
        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 5));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 5));
    }

    [Fact]
    public void Render_AppliesQuietZoneOnBothSides()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) });
        var options = new BarcodeRenderOptions { ModuleWidthPixels = 1, QuietZoneModules = 3, BarHeightPixels = 5 };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(7, bitmap.Width);
        Assert.Equal(SKColors.White, bitmap.GetPixel(0, 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 2));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(4, 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(6, 2));
    }

    [Fact]
    public void Render_ScalesByModuleWidth()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) });
        var options = new BarcodeRenderOptions { ModuleWidthPixels = 5, QuietZoneModules = 0, BarHeightPixels = 5 };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(5, bitmap.Width);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 2));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(4, 2));
    }

    [Fact]
    public void Render_UsesDefaultOptionsWhenNoneGiven()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) });

        using var bitmap = BarcodeRenderer.Render(pattern);

        // default ModuleWidthPixels=2, QuietZoneModules=10 -> (1 + 10*2) * 2 = 42
        Assert.Equal(42, bitmap.Width);
        Assert.Equal(80, bitmap.Height);
    }

    [Fact]
    public void RenderToPng_ProducesValidPngBytes()
    {
        var pattern = BarcodePattern.Create("test", new[]
        {
            new BarSegment(true, 1),
            new BarSegment(false, 1),
            new BarSegment(true, 1),
        });

        var bytes = BarcodeRenderer.RenderToPng(pattern);

        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Render_RealEncoderOutput_ProducesNonEmptyImage()
    {
        var encoder = new IBEBarcode.Core.Encoders.Code39Encoder();
        encoder.TryEncode("HELLO", out var pattern, out _);

        var options = new BarcodeRenderOptions { ModuleWidthPixels = 2, QuietZoneModules = 10, BarHeightPixels = 60 };
        using var bitmap = BarcodeRenderer.Render(pattern!, options);

        var expectedWidth = (pattern!.TotalWidthUnits + options.QuietZoneModules * 2) * options.ModuleWidthPixels;
        Assert.Equal(expectedWidth, bitmap.Width);
        Assert.Equal(60, bitmap.Height);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `BarcodeRenderOptions`/`BarcodeRenderer` do not exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Rendering/BarcodeRenderOptions.cs`:

```csharp
using SkiaSharp;

namespace IBEBarcode.Rendering;

public sealed class BarcodeRenderOptions
{
    public int ModuleWidthPixels { get; init; } = 2;
    public int BarHeightPixels { get; init; } = 80;
    public int QuietZoneModules { get; init; } = 10;
    public SKColor BarColor { get; init; } = SKColors.Black;
    public SKColor BackgroundColor { get; init; } = SKColors.White;
}
```

Create `src/IBEBarcode.Rendering/BarcodeRenderer.cs`:

```csharp
using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering;

public static class BarcodeRenderer
{
    public static SKBitmap Render(BarcodePattern pattern, BarcodeRenderOptions? options = null)
    {
        options ??= new BarcodeRenderOptions();

        var quietZonePixels = options.QuietZoneModules * options.ModuleWidthPixels;
        var barsWidthPixels = pattern.TotalWidthUnits * options.ModuleWidthPixels;
        var totalWidth = barsWidthPixels + quietZonePixels * 2;
        var totalHeight = options.BarHeightPixels;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.BackgroundColor);

        using var paint = new SKPaint { Color = options.BarColor, Style = SKPaintStyle.Fill };

        var x = quietZonePixels;
        foreach (var segment in pattern.Segments)
        {
            var widthPixels = segment.WidthUnits * options.ModuleWidthPixels;

            if (segment.IsBar)
            {
                canvas.DrawRect(SKRect.Create(x, 0, widthPixels, totalHeight), paint);
            }

            x += widthPixels;
        }

        return bitmap;
    }

    public static byte[] RenderToPng(BarcodePattern pattern, BarcodeRenderOptions? options = null)
    {
        using var bitmap = Render(pattern, options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `BarcodeRendererTests` green, full solution test suite green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Rendering/BarcodeRenderOptions.cs src/IBEBarcode.Rendering/BarcodeRenderer.cs tests/IBEBarcode.Rendering.Tests/BarcodeRendererTests.cs
git commit -m "$(cat <<'EOF'
Add BarcodeRenderOptions and BarcodeRenderer

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Human-readable text drawn beneath the bars (needs font metrics/layout —
its own task once a font strategy is picked), `IBEBarcode.Templates`
(paper templates), `IBEBarcode.Printing` (PdfSharp), then
`IBEBarcode.Desktop` (Avalonia) and `IBEBarcode.Web` (Blazor WebAssembly)
which are the first consumers that will actually show a barcode on
screen. Also still pending: the non-linear pattern redesign for Postnet
and QR/Data Matrix/PDF417/Aztec.
