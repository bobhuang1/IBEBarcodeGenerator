# Height-Varying Pattern Foundation and Postnet Encoder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a second, height-varying pattern abstraction to `IBEBarcode.Core` (Postnet bars are all the same width and evenly spaced — only their height carries data, which the existing width-varying `BarcodePattern`/`BarSegment`/`IBarcodeEncoder` contract cannot represent), then implement Postnet on top of it.

**Architecture:** `HeightBar` (a single `bool IsTall` value) and `HeightBarPattern` (an ordered, non-empty list of `HeightBar`s plus the original `Value`) are new, minimal types — deliberately not reusing `BarSegment`, since Postnet has no bar/space alternation to encode, only per-position height. `IHeightVaryingBarcodeEncoder` parallels `IBarcodeEncoder`'s `TryEncode` shape. `PostnetEncoder` implements it using the standard 7-4-2-1-0 weighted two-of-five digit code plus a mod-10 check digit, framed by a full-height guard bar on each end.

**Tech Stack:** Same as prior plans. The digit encoding table (each digit as 5 bars with exactly 2 tall) was fetched and cross-checked against an independent source; the mod-10 check digit is the same weighted-sum style algorithm already proven correct for MSI Plessey and Code 39's digit-only relatives in this codebase.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) Every project targets `net10.0`; MIT license; no third-party barcode-encoding library; no database; single solution file; `Nullable`/`ImplicitUsings` enabled everywhere.

## Verified reference values used by this plan's tests

Postnet digit-to-bar-height table (bar positions weighted 7-4-2-1-0, `1`=tall, `0`=short), independently published and cross-checked:

| Digit | 7 4 2 1 0 |
|---|---|
| 0 | 1 1 0 0 0 |
| 1 | 0 0 0 1 1 |
| 2 | 0 0 1 0 1 |
| 3 | 0 0 1 1 0 |
| 4 | 0 1 0 0 1 |
| 5 | 0 1 0 1 0 |
| 6 | 0 1 1 0 0 |
| 7 | 1 0 0 0 1 |
| 8 | 1 0 0 1 0 |
| 9 | 1 0 1 0 0 |

Every row has exactly two `1`s (two-of-five code), which the tests assert
structurally as a sanity check on top of the exact per-digit values.

---

### Task 1: `HeightBar`, `HeightBarPattern`, `IHeightVaryingBarcodeEncoder`

**Files:**
- Create: `src/IBEBarcode.Core/HeightBar.cs`
- Create: `src/IBEBarcode.Core/HeightBarPattern.cs`
- Create: `src/IBEBarcode.Core/IHeightVaryingBarcodeEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/HeightBarPatternTests.cs`

**Interfaces:**
- Produces (consumed by Task 2 and, later, `IBEBarcode.Rendering`):
  - `readonly struct HeightBar(bool isTall)`.
  - `sealed class HeightBarPattern` with `string Value`, `IReadOnlyList<HeightBar> Bars`, and factory `HeightBarPattern.Create(string value, IReadOnlyList<HeightBar> bars)` — throws `ArgumentException` if `bars` is empty.
  - `interface IHeightVaryingBarcodeEncoder` with `BarcodeSymbology Symbology { get; }` and `bool TryEncode(string value, out HeightBarPattern? pattern, out string? error)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/HeightBarPatternTests.cs`:

```csharp
namespace IBEBarcode.Core.Tests;

public class HeightBarPatternTests
{
    [Fact]
    public void Create_WithBars_Succeeds()
    {
        var bars = new[] { new HeightBar(true), new HeightBar(false), new HeightBar(true) };

        var pattern = HeightBarPattern.Create("123", bars);

        Assert.Equal("123", pattern.Value);
        Assert.Equal(bars, pattern.Bars);
    }

    [Fact]
    public void Create_WithNoBars_Throws()
    {
        Assert.Throws<ArgumentException>(() => HeightBarPattern.Create("x", Array.Empty<HeightBar>()));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `HeightBar`/`HeightBarPattern` do not exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Core/HeightBar.cs`:

```csharp
namespace IBEBarcode.Core;

public readonly struct HeightBar : IEquatable<HeightBar>
{
    public bool IsTall { get; }

    public HeightBar(bool isTall)
    {
        IsTall = isTall;
    }

    public bool Equals(HeightBar other) => IsTall == other.IsTall;

    public override bool Equals(object? obj) => obj is HeightBar other && Equals(other);

    public override int GetHashCode() => IsTall.GetHashCode();
}
```

Create `src/IBEBarcode.Core/HeightBarPattern.cs`:

```csharp
namespace IBEBarcode.Core;

public sealed class HeightBarPattern
{
    public string Value { get; }
    public IReadOnlyList<HeightBar> Bars { get; }

    private HeightBarPattern(string value, IReadOnlyList<HeightBar> bars)
    {
        Value = value;
        Bars = bars;
    }

    public static HeightBarPattern Create(string value, IReadOnlyList<HeightBar> bars)
    {
        if (bars.Count == 0)
            throw new ArgumentException("Pattern must contain at least one bar.", nameof(bars));

        return new HeightBarPattern(value, bars);
    }
}
```

Create `src/IBEBarcode.Core/IHeightVaryingBarcodeEncoder.cs`:

```csharp
namespace IBEBarcode.Core;

public interface IHeightVaryingBarcodeEncoder
{
    BarcodeSymbology Symbology { get; }

    bool TryEncode(string value, out HeightBarPattern? pattern, out string? error);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/HeightBar.cs src/IBEBarcode.Core/HeightBarPattern.cs src/IBEBarcode.Core/IHeightVaryingBarcodeEncoder.cs tests/IBEBarcode.Core.Tests/HeightBarPatternTests.cs
git commit -m "$(cat <<'EOF'
Add height-varying pattern types for Postnet-style barcodes

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Postnet encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/PostnetEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/PostnetEncoderTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `Postnet`)

**Interfaces:**
- Consumes: `HeightBar`, `HeightBarPattern`, `IHeightVaryingBarcodeEncoder` (Task 1).
- Produces: `BarcodeSymbology.Postnet` and `sealed class PostnetEncoder : IHeightVaryingBarcodeEncoder`. Accepts digits-only ZIP/ZIP+4/delivery-point strings (5, 9, or 11 digits); appends a mod-10 check digit; frames the result with a full-height guard bar on each end.

- [ ] **Step 1: Add the enum value**

Edit `src/IBEBarcode.Core/BarcodeSymbology.cs` so it reads:

```csharp
namespace IBEBarcode.Core;

public enum BarcodeSymbology
{
    Code39,
    Codabar,
    Interleaved2Of5,
    MsiPlessey,
    Ean13,
    Ean8,
    UpcA,
    Isbn,
    Code93,
    Code39Extended,
    Code128,
    Postnet,
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/PostnetEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class PostnetEncoderTests
{
    private readonly PostnetEncoder _encoder = new();

    [Fact]
    public void TryEncode_FiveDigitZip_ComputesCheckDigitAndSucceeds()
    {
        // digits 1+2+3+4+5=15, check=(10-15%10)%10=5
        var success = _encoder.TryEncode("12345", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("123455", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Postnet, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_ProducesGuardBarsAtStartAndEnd()
    {
        _encoder.TryEncode("12345", out var pattern, out _);

        Assert.True(pattern!.Bars[0].IsTall);
        Assert.True(pattern.Bars[^1].IsTall);
    }

    [Fact]
    public void TryEncode_ProducesExpectedBarCount()
    {
        _encoder.TryEncode("12345", out var pattern, out _);

        // guard(1) + 6 digits(5 bars each = 30) + guard(1) = 32
        Assert.Equal(32, pattern!.Bars.Count);
    }

    [Fact]
    public void TryEncode_DigitZero_ProducesTwoOfFiveTallPattern()
    {
        var success = _encoder.TryEncode("00000", out var pattern, out _);

        Assert.True(success);
        // digit 0 = 1 1 0 0 0 (weights 7,4,2,1,0)
        var firstDigitBars = pattern!.Bars.Skip(1).Take(5).Select(b => b.IsTall).ToArray();
        Assert.Equal(new[] { true, true, false, false, false }, firstDigitBars);
    }

    [Fact]
    public void TryEncode_EveryDigit_HasExactlyTwoTallBars()
    {
        var success = _encoder.TryEncode("0123456789", out var pattern, out _);

        Assert.True(success);

        for (var digitIndex = 0; digitIndex < 10; digitIndex++)
        {
            var digitBars = pattern!.Bars.Skip(1 + digitIndex * 5).Take(5).Count(b => b.IsTall);
            Assert.Equal(2, digitBars);
        }
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("1234X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `PostnetEncoder` does not exist yet.

- [ ] **Step 4: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/PostnetEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class PostnetEncoder : IHeightVaryingBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Postnet;

    private static readonly bool[][] DigitPatterns =
    {
        new[] { true, true, false, false, false },
        new[] { false, false, false, true, true },
        new[] { false, false, true, false, true },
        new[] { false, false, true, true, false },
        new[] { false, true, false, false, true },
        new[] { false, true, false, true, false },
        new[] { false, true, true, false, false },
        new[] { true, false, false, false, true },
        new[] { true, false, false, true, false },
        new[] { true, false, true, false, false },
    };

    public bool TryEncode(string value, out HeightBarPattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 5 or 9 or 11 })
        {
            error = "Postnet requires 5 (ZIP), 9 (ZIP+4), or 11 (delivery point) digits.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; Postnet encodes digits only.";
                return false;
            }
        }

        var sum = 0;
        foreach (var ch in value)
        {
            sum += ch - '0';
        }

        var checkDigit = (10 - sum % 10) % 10;

        var bars = new List<HeightBar> { new(true) };

        foreach (var ch in value)
        {
            AppendDigit(bars, ch - '0');
        }

        AppendDigit(bars, checkDigit);
        bars.Add(new HeightBar(true));

        var fullValue = value + checkDigit;
        pattern = HeightBarPattern.Create(fullValue, bars);
        error = null;
        return true;
    }

    private static void AppendDigit(List<HeightBar> bars, int digit)
    {
        foreach (var isTall in DigitPatterns[digit])
        {
            bars.Add(new HeightBar(isTall));
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `PostnetEncoderTests` green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/PostnetEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/PostnetEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Postnet encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Code 128 Set A/C + GS1-128, QR Code (2D — needs its own `BarcodeMatrix`
grid abstraction, Reed-Solomon error correction, and mask-pattern
scoring), then Data Matrix/PDF417/Aztec, then `IBEBarcode.Templates`,
`IBEBarcode.Printing`, `IBEBarcode.Desktop`, `IBEBarcode.Web`.
