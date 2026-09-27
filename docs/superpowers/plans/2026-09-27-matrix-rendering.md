# Matrix Rendering Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render a `BarcodeMatrix` (QR Code, and later Data Matrix/Aztec) to an actual image, the same way `BarcodeRenderer` already does for linear `BarcodePattern`s.

**Architecture:** `MatrixRenderer` in `IBEBarcode.Rendering`, mirroring `BarcodeRenderer`'s shape: each matrix module becomes a square of `ModuleSizePixels` pixels, with a quiet zone of `QuietZoneModules` blank modules on all four sides. Reuses `BarcodeRenderOptions`' color properties via a new small `MatrixRenderOptions` type (module size instead of separate width/height, since matrix modules are square, unlike linear bars).

**Tech Stack:** Same as the rendering foundation plan — SkiaSharp, pixel-verified tests.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.)

---

### Task 1: `MatrixRenderOptions` and `MatrixRenderer`

**Files:**
- Create: `src/IBEBarcode.Rendering/MatrixRenderOptions.cs`
- Create: `src/IBEBarcode.Rendering/MatrixRenderer.cs`
- Test: `tests/IBEBarcode.Rendering.Tests/MatrixRendererTests.cs`

**Interfaces:**
- Consumes: `IBEBarcode.Core.BarcodeMatrix`.
- Produces:
  - `sealed class MatrixRenderOptions` — `int ModuleSizePixels { get; init; } = 8`, `int QuietZoneModules { get; init; } = 4`, `SKColor DarkColor { get; init; } = SKColors.Black`, `SKColor LightColor { get; init; } = SKColors.White`.
  - `static class MatrixRenderer` — `SKBitmap Render(BarcodeMatrix matrix, MatrixRenderOptions? options = null)`, `byte[] RenderToPng(BarcodeMatrix matrix, MatrixRenderOptions? options = null)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Rendering.Tests/MatrixRendererTests.cs`:

```csharp
using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering.Tests;

public class MatrixRendererTests
{
    private static BarcodeMatrix TinyCheckerboard()
    {
        var modules = new bool[2, 2];
        modules[0, 0] = true;
        modules[1, 0] = false;
        modules[0, 1] = false;
        modules[1, 1] = true;
        return BarcodeMatrix.Create("test", modules);
    }

    [Fact]
    public void Render_ProducesExpectedDimensions()
    {
        var matrix = TinyCheckerboard();
        var options = new MatrixRenderOptions { ModuleSizePixels = 3, QuietZoneModules = 0 };

        using var bitmap = MatrixRenderer.Render(matrix, options);

        Assert.Equal(6, bitmap.Width);
        Assert.Equal(6, bitmap.Height);
    }

    [Fact]
    public void Render_DrawsEachModuleAsASolidSquare()
    {
        var matrix = TinyCheckerboard();
        var options = new MatrixRenderOptions { ModuleSizePixels = 2, QuietZoneModules = 0 };

        using var bitmap = MatrixRenderer.Render(matrix, options);

        // module (0,0) dark -> pixels (0,0) and (1,1) within it are black
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(1, 1));
        // module (1,0) light -> white
        Assert.Equal(SKColors.White, bitmap.GetPixel(2, 0));
        Assert.Equal(SKColors.White, bitmap.GetPixel(3, 1));
        // module (1,1) dark -> black
        Assert.Equal(SKColors.Black, bitmap.GetPixel(2, 2));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(3, 3));
    }

    [Fact]
    public void Render_AppliesQuietZone()
    {
        var matrix = TinyCheckerboard();
        var options = new MatrixRenderOptions { ModuleSizePixels = 1, QuietZoneModules = 2 };

        using var bitmap = MatrixRenderer.Render(matrix, options);

        Assert.Equal(6, bitmap.Width);
        Assert.Equal(6, bitmap.Height);
        Assert.Equal(SKColors.White, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(2, 2));
    }

    [Fact]
    public void RenderToPng_ProducesValidPngBytes()
    {
        var matrix = TinyCheckerboard();

        var bytes = MatrixRenderer.RenderToPng(matrix);

        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Render_RealQrCode_ProducesExpectedSize()
    {
        var encoder = new IBEBarcode.Core.Encoders.QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        var options = new MatrixRenderOptions { ModuleSizePixels = 4, QuietZoneModules = 4 };
        using var bitmap = MatrixRenderer.Render(matrix!, options);

        var expected = (matrix!.Width + options.QuietZoneModules * 2) * options.ModuleSizePixels;
        Assert.Equal(expected, bitmap.Width);
        Assert.Equal(expected, bitmap.Height);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `MatrixRenderOptions`/`MatrixRenderer` do not exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Rendering/MatrixRenderOptions.cs`:

```csharp
using SkiaSharp;

namespace IBEBarcode.Rendering;

public sealed class MatrixRenderOptions
{
    public int ModuleSizePixels { get; init; } = 8;
    public int QuietZoneModules { get; init; } = 4;
    public SKColor DarkColor { get; init; } = SKColors.Black;
    public SKColor LightColor { get; init; } = SKColors.White;
}
```

Create `src/IBEBarcode.Rendering/MatrixRenderer.cs`:

```csharp
using IBEBarcode.Core;
using SkiaSharp;

namespace IBEBarcode.Rendering;

public static class MatrixRenderer
{
    public static SKBitmap Render(BarcodeMatrix matrix, MatrixRenderOptions? options = null)
    {
        options ??= new MatrixRenderOptions();

        var quietZonePixels = options.QuietZoneModules * options.ModuleSizePixels;
        var totalWidth = (matrix.Width * options.ModuleSizePixels) + quietZonePixels * 2;
        var totalHeight = (matrix.Height * options.ModuleSizePixels) + quietZonePixels * 2;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.LightColor);

        using var paint = new SKPaint { Color = options.DarkColor, Style = SKPaintStyle.Fill };

        for (var moduleY = 0; moduleY < matrix.Height; moduleY++)
        {
            for (var moduleX = 0; moduleX < matrix.Width; moduleX++)
            {
                if (!matrix[moduleX, moduleY])
                {
                    continue;
                }

                var x = quietZonePixels + moduleX * options.ModuleSizePixels;
                var y = quietZonePixels + moduleY * options.ModuleSizePixels;
                canvas.DrawRect(SKRect.Create(x, y, options.ModuleSizePixels, options.ModuleSizePixels), paint);
            }
        }

        return bitmap;
    }

    public static byte[] RenderToPng(BarcodeMatrix matrix, MatrixRenderOptions? options = null)
    {
        using var bitmap = Render(matrix, options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `MatrixRendererTests` green, full solution test suite green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Rendering/MatrixRenderOptions.cs src/IBEBarcode.Rendering/MatrixRenderer.cs tests/IBEBarcode.Rendering.Tests/MatrixRendererTests.cs
git commit -m "$(cat <<'EOF'
Add MatrixRenderOptions and MatrixRenderer for QR/2D barcodes

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

`HeightBarPattern` (Postnet) still has no renderer. Then Code 128 Set
A/C + GS1-128, Data Matrix, PDF417, Aztec. Then `IBEBarcode.Templates`,
`IBEBarcode.Printing`, `IBEBarcode.Desktop`, `IBEBarcode.Web`.
