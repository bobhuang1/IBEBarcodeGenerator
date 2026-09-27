# Human-Readable Text Rendering Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Draw the encoded value as text beneath the bars in `BarcodeRenderer` — every `BarcodePattern` already carries a `HumanReadableText` (set by every linear encoder), it just wasn't being drawn.

**Architecture:** `BarcodeRenderOptions` gains `ShowHumanReadableText` (bool) and `TextHeightPixels` (int), both opt-in-friendly. When enabled, the bitmap grows by `TextHeightPixels` and the bars are confined to the original `BarHeightPixels` region at the top, with the pattern's `HumanReadableText` centered in the new strip below using SkiaSharp's `SKFont`/`canvas.DrawText`.

**Scope decision — default `ShowHumanReadableText = false`:** every existing `BarcodePattern.Create(value, segments)` call (used throughout the already-merged test suite) defaults `HumanReadableText` to `value` itself — it's never null. If text-drawing defaulted to *on*, every existing `BarcodeRendererTests` assertion about bitmap dimensions would break, since the library can't tell "an old caller that never thought about text" apart from "a new caller who wants bars only." Defaulting to off preserves all existing behavior exactly; `IBEBarcode.Desktop` and `IBEBarcode.Web` opt in explicitly since a real product should show the human-readable line. `MatrixRenderer` (QR) is untouched — `BarcodeMatrix` has no `HumanReadableText` concept, and QR codes are conventionally shown without a text line since the code itself, not a human, is the reader.

**Tech Stack:** SkiaSharp `SKFont`/`SKCanvas.DrawText`, already a dependency of `IBEBarcode.Rendering`.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.)

---

### Task 1: Text rendering in `BarcodeRenderer`

**Files:**
- Modify: `src/IBEBarcode.Rendering/BarcodeRenderOptions.cs`
- Modify: `src/IBEBarcode.Rendering/BarcodeRenderer.cs`
- Test: `tests/IBEBarcode.Rendering.Tests/BarcodeRendererTests.cs` (new tests appended; existing tests must keep passing unchanged)
- Modify: `src/IBEBarcode.Desktop/ViewModels/MainViewModel.cs` (opt in)
- Modify: `src/IBEBarcode.Web/Pages/Home.razor` (opt in)

**Interfaces:**
- `BarcodeRenderOptions` gains `bool ShowHumanReadableText { get; init; } = false;`, `int TextHeightPixels { get; init; } = 20;`.
- `BarcodeRenderer.Render`'s output height becomes `BarHeightPixels + (ShowHumanReadableText && pattern.HumanReadableText is not empty ? TextHeightPixels : 0)`.

- [ ] **Step 1: Write the failing tests**

Add to `tests/IBEBarcode.Rendering.Tests/BarcodeRendererTests.cs` (append inside the existing class):

```csharp
    [Fact]
    public void Render_TextDisabledByDefault_MatchesPriorBehavior()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) });
        var options = new BarcodeRenderOptions { ModuleWidthPixels = 1, QuietZoneModules = 0, BarHeightPixels = 10 };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(10, bitmap.Height);
    }

    [Fact]
    public void Render_TextEnabled_GrowsBitmapByTextHeight()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) }, "ABC");
        var options = new BarcodeRenderOptions
        {
            ModuleWidthPixels = 1,
            QuietZoneModules = 0,
            BarHeightPixels = 10,
            ShowHumanReadableText = true,
            TextHeightPixels = 20,
        };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        Assert.Equal(30, bitmap.Height);
    }

    [Fact]
    public void Render_TextEnabled_BarsStayConfinedToBarHeightRegion()
    {
        var pattern = BarcodePattern.Create("test", new[] { new BarSegment(true, 1) }, "ABC");
        var options = new BarcodeRenderOptions
        {
            ModuleWidthPixels = 1,
            QuietZoneModules = 0,
            BarHeightPixels = 10,
            ShowHumanReadableText = true,
            TextHeightPixels = 20,
        };

        using var bitmap = BarcodeRenderer.Render(pattern, options);

        // Below the bar region (in the text strip), the pixel directly under the
        // bar's x-position should no longer be forced black by the bar itself.
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 5));
        Assert.Equal(SKColors.White, bitmap.GetPixel(0, 15));
    }
```

- [ ] **Step 2: Run tests to verify the new ones fail**

Run: `dotnet test`
Expected: `Render_TextEnabled_*` tests FAIL (feature not implemented yet); `Render_TextDisabledByDefault_MatchesPriorBehavior` passes trivially since it matches current behavior already.

- [ ] **Step 3: Implement**

Edit `src/IBEBarcode.Rendering/BarcodeRenderOptions.cs` — add two properties:

```csharp
    public bool ShowHumanReadableText { get; init; } = false;
    public int TextHeightPixels { get; init; } = 20;
```

Replace `src/IBEBarcode.Rendering/BarcodeRenderer.cs` with:

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

        var showText = options.ShowHumanReadableText && !string.IsNullOrEmpty(pattern.HumanReadableText);
        var textStripHeight = showText ? options.TextHeightPixels : 0;
        var totalHeight = options.BarHeightPixels + textStripHeight;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.BackgroundColor);

        using var paint = new SKPaint { Color = options.BarColor, Style = SKPaintStyle.Fill, IsAntialias = true };

        var x = quietZonePixels;
        foreach (var segment in pattern.Segments)
        {
            var widthPixels = segment.WidthUnits * options.ModuleWidthPixels;

            if (segment.IsBar)
            {
                canvas.DrawRect(SKRect.Create(x, 0, widthPixels, options.BarHeightPixels), paint);
            }

            x += widthPixels;
        }

        if (showText)
        {
            using var font = new SKFont(SKTypeface.Default, textStripHeight * 0.7f);
            var textWidth = font.MeasureText(pattern.HumanReadableText);
            var textX = Math.Max(0f, (totalWidth - textWidth) / 2f);
            var textY = options.BarHeightPixels + textStripHeight * 0.8f;
            canvas.DrawText(pattern.HumanReadableText, textX, textY, font, paint);
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
Expected: PASS — all `BarcodeRendererTests` green (old and new), full solution test suite green.

- [ ] **Step 5: Opt in from the desktop and web apps**

In `src/IBEBarcode.Desktop/ViewModels/MainViewModel.cs`, add `ShowHumanReadableText = true` to the `BarcodeRenderOptions` object initializer used for linear symbologies (not the QR/`MatrixRenderOptions` one, which has no equivalent option).

Do the same in `src/IBEBarcode.Web/Pages/Home.razor`'s equivalent `BarcodeRenderOptions` initializer.

Rebuild both (`dotnet build src/IBEBarcode.Desktop/IBEBarcode.Desktop.csproj` and `dotnet build src/IBEBarcode.Web/IBEBarcode.Web.csproj`) to confirm they still compile cleanly.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Rendering/BarcodeRenderOptions.cs src/IBEBarcode.Rendering/BarcodeRenderer.cs tests/IBEBarcode.Rendering.Tests/BarcodeRendererTests.cs src/IBEBarcode.Desktop/ViewModels/MainViewModel.cs src/IBEBarcode.Web/Pages/Home.razor
git commit -m "$(cat <<'EOF'
Add human-readable text rendering under linear barcodes

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Data Matrix, PDF417, Aztec. GS1-128 AI parsing. Mid-message Code128
subset switching. A `HeightBarPattern` (Postnet) renderer. More
`PaperTemplateCatalog` entries. OS print-dialog integration in Desktop.
PDF export in Web. Azure deployment. GitHub remote creation.
