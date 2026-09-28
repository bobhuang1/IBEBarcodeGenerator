# Height-Bar (Postnet) Rendering Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render a `HeightBarPattern` (Postnet) to an actual image — the encoder has existed since early in this project but had no renderer, unlike every other format.

**Architecture:** `HeightBarRenderer` in `IBEBarcode.Rendering`, structurally simpler than `BarcodeRenderer`/`MatrixRenderer` since Postnet has no "space" concept — every position is a bar, only its height varies. Bars are bottom-aligned (the real-world look of Postnet: all bars share a common baseline, tall bars reach further up), each `BarWidthPixels` wide with `GapPixels` between them.

**Tech Stack:** SkiaSharp, same pattern as the other two renderers.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.)

---

### Task 1: `HeightBarRenderOptions` and `HeightBarRenderer`

**Files:**
- Create: `src/IBEBarcode.Rendering/HeightBarRenderOptions.cs`
- Create: `src/IBEBarcode.Rendering/HeightBarRenderer.cs`
- Test: `tests/IBEBarcode.Rendering.Tests/HeightBarRendererTests.cs`

**Interfaces:**
- Consumes: `IBEBarcode.Core.HeightBarPattern`, `HeightBar`.
- Produces:
  - `sealed class HeightBarRenderOptions` — `int BarWidthPixels { get; init; } = 2`, `int GapPixels { get; init; } = 2`, `int TallBarHeightPixels { get; init; } = 30`, `int ShortBarHeightPixels { get; init; } = 15`, `SKColor BarColor { get; init; } = SKColors.Black`, `SKColor BackgroundColor { get; init; } = SKColors.White`.
  - `static class HeightBarRenderer` — `SKBitmap Render(HeightBarPattern pattern, HeightBarRenderOptions? options = null)`, `byte[] RenderToPng(HeightBarPattern pattern, HeightBarRenderOptions? options = null)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Rendering.Tests/HeightBarRendererTests.cs`:

```csharp
using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering.Tests;

public class HeightBarRendererTests
{
    private static HeightBarPattern TallShortTallPattern() =>
        HeightBarPattern.Create("test", new[] { new HeightBar(true), new HeightBar(false), new HeightBar(true) });

    [Fact]
    public void Render_ProducesExpectedDimensions()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        // 3 bars * 2px + 2 gaps * 1px = 8
        Assert.Equal(8, bitmap.Width);
        Assert.Equal(10, bitmap.Height);
    }

    [Fact]
    public void Render_TallBarSpansFullHeight()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 9));
    }

    [Fact]
    public void Render_ShortBarIsBottomAlignedAndShorterThanTall()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        // short bar (index 1, x=3..4): empty above, filled at the bottom
        Assert.Equal(SKColors.White, bitmap.GetPixel(3, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 7));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 9));
    }

    [Fact]
    public void Render_GapBetweenBarsIsBackground()
    {
        var pattern = TallShortTallPattern();
        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1, TallBarHeightPixels = 10, ShortBarHeightPixels = 5 };

        using var bitmap = HeightBarRenderer.Render(pattern, options);

        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 5));
    }

    [Fact]
    public void RenderToPng_ProducesValidPngBytes()
    {
        var pattern = TallShortTallPattern();

        var bytes = HeightBarRenderer.RenderToPng(pattern);

        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Render_RealPostnetEncoderOutput_ProducesExpectedWidth()
    {
        var encoder = new IBEBarcode.Core.Encoders.PostnetEncoder();
        encoder.TryEncode("12345", out var pattern, out _);

        var options = new HeightBarRenderOptions { BarWidthPixels = 2, GapPixels = 1 };
        using var bitmap = HeightBarRenderer.Render(pattern!, options);

        var expectedWidth = pattern!.Bars.Count * options.BarWidthPixels + (pattern.Bars.Count - 1) * options.GapPixels;
        Assert.Equal(expectedWidth, bitmap.Width);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `HeightBarRenderOptions`/`HeightBarRenderer` do not exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Rendering/HeightBarRenderOptions.cs`:

```csharp
using SkiaSharp;

namespace IBEBarcode.Rendering;

public sealed class HeightBarRenderOptions
{
    public int BarWidthPixels { get; init; } = 2;
    public int GapPixels { get; init; } = 2;
    public int TallBarHeightPixels { get; init; } = 30;
    public int ShortBarHeightPixels { get; init; } = 15;
    public SKColor BarColor { get; init; } = SKColors.Black;
    public SKColor BackgroundColor { get; init; } = SKColors.White;
}
```

Create `src/IBEBarcode.Rendering/HeightBarRenderer.cs`:

```csharp
using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering;

public static class HeightBarRenderer
{
    public static SKBitmap Render(HeightBarPattern pattern, HeightBarRenderOptions? options = null)
    {
        options ??= new HeightBarRenderOptions();

        var barCount = pattern.Bars.Count;
        var totalWidth = barCount * options.BarWidthPixels + Math.Max(0, barCount - 1) * options.GapPixels;
        var totalHeight = options.TallBarHeightPixels;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.BackgroundColor);

        using var paint = new SKPaint { Color = options.BarColor, Style = SKPaintStyle.Fill };

        var x = 0;
        foreach (var bar in pattern.Bars)
        {
            var barHeight = bar.IsTall ? options.TallBarHeightPixels : options.ShortBarHeightPixels;
            var y = totalHeight - barHeight;
            canvas.DrawRect(SKRect.Create(x, y, options.BarWidthPixels, barHeight), paint);
            x += options.BarWidthPixels + options.GapPixels;
        }

        return bitmap;
    }

    public static byte[] RenderToPng(HeightBarPattern pattern, HeightBarRenderOptions? options = null)
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
Expected: PASS — all `HeightBarRendererTests` green, full solution test suite green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Rendering/HeightBarRenderOptions.cs src/IBEBarcode.Rendering/HeightBarRenderer.cs tests/IBEBarcode.Rendering.Tests/HeightBarRendererTests.cs
git commit -m "$(cat <<'EOF'
Add HeightBarRenderOptions and HeightBarRenderer for Postnet

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Wire Postnet into the desktop/web symbology pickers now that it has a
renderer. Data Matrix, PDF417, Aztec. GS1-128 AI parsing. More paper
templates. OS print-dialog integration. PDF export in Web. Azure
deployment. GitHub remote creation.
