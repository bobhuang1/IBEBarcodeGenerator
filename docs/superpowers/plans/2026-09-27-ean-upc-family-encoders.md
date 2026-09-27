# EAN/UPC/ISBN Encoder Family Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add EAN-13, EAN-8, UPC-A, and ISBN (Bookland) barcode encoders to `IBEBarcode.Core`, sharing one verified digit-pattern table since all four are variants of the same underlying symbol structure.

**Architecture:** A new internal `EanUpcDigitPatterns` helper holds the digit width table (verified against the ZXing reference implementation and cross-checked by hand-decoding real, well-known barcode numbers) plus the shared weighted check-digit algorithm. `Ean13Encoder` and `Ean8Encoder` are the two real implementations; `UpcAEncoder` and `IsbnEncoder` are thin wrappers that delegate to `Ean13Encoder` (UPC-A is EAN-13 with an implied leading `0`; Bookland ISBN-13 is EAN-13 with a `978` prefix over the first 9 ISBN-10 digits).

**Tech Stack:** Same as the foundation plan — .NET 10, xUnit, no third-party barcode library. Digit-pattern widths and the first-digit parity table were fetched from ZXing's published `UPCEANReader`/`EAN13Reader` source and independently verified by hand-decoding three well-known real barcodes (UPC-A `036000291452`, ISBN `978-0-306-40615-7`, and by re-deriving the run-length encoding of digit `0`'s L/G/R patterns from their standard bit-strings).

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as the foundation plan — see `docs/superpowers/plans/2026-09-27-core-barcode-foundation.md`.) Every project targets `net10.0`; MIT license; no third-party barcode-encoding library; no database; single solution file; `Nullable`/`ImplicitUsings` enabled everywhere.

## Verified reference values used by this plan's tests

- UPC-A `03600029145` + check digit `2` → full `036000291452` (real Kellogg's Corn Flakes UPC, hand-verified: odd-position data digits ×3 + even-position ×1 = 58, check = (10 − 58 mod 10) mod 10 = 2).
- ISBN core `030640615` (from ISBN-10 `0-306-40615-2`) → EAN-13 `9780306406157` (real, famous Wikipedia EAN-13 example; hand-verified check digit = 7).
- EAN-8 data `9638507` → check digit `4` (self-derived from the same verified weighting algorithm: 7×3+0×1+5×3+8×1+3×3+6×1+9×3 = 86, check = (10 − 86 mod 10) mod 10 = 4).

---

### Task 1: Shared EAN/UPC digit-pattern helper

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/EanUpcDigitPatterns.cs`
- Modify: `src/IBEBarcode.Core/IBEBarcode.Core.csproj` (add `InternalsVisibleTo` so tests can reach the `internal` helper)
- Test: `tests/IBEBarcode.Core.Tests/Encoders/EanUpcDigitPatternsTests.cs`

**Interfaces:**
- Consumes: `BarSegment` from the foundation plan.
- Produces (`internal static class EanUpcDigitPatterns` in `IBEBarcode.Core.Encoders`, consumed by every task below):
  - `int[][] Widths` — 10 entries (digit 0-9), each 4 ints.
  - `string ParityForFirstDigit(int firstDigit)` — returns a 6-character string of `'L'`/`'G'`.
  - `void AppendLeftDigit(List<BarSegment> segments, int digit, bool useGCode)`.
  - `void AppendRightDigit(List<BarSegment> segments, int digit)`.
  - `void AppendGuard(List<BarSegment> segments, bool startsWithBar, params int[] widths)`.
  - `char ComputeCheckDigit(string dataDigits)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/EanUpcDigitPatternsTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class EanUpcDigitPatternsTests
{
    [Fact]
    public void AppendLeftDigit_DigitZero_LCode_ProducesKnownPattern()
    {
        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendLeftDigit(segments, 0, useGCode: false);

        Assert.Equal(new[] { 3, 2, 1, 1 }, segments.Select(s => s.WidthUnits).ToArray());
        Assert.Equal(new[] { false, true, false, true }, segments.Select(s => s.IsBar).ToArray());
    }

    [Fact]
    public void AppendLeftDigit_DigitZero_GCode_ProducesKnownPattern()
    {
        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendLeftDigit(segments, 0, useGCode: true);

        Assert.Equal(new[] { 1, 1, 2, 3 }, segments.Select(s => s.WidthUnits).ToArray());
        Assert.Equal(new[] { false, true, false, true }, segments.Select(s => s.IsBar).ToArray());
    }

    [Fact]
    public void AppendRightDigit_DigitZero_ProducesKnownPattern()
    {
        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendRightDigit(segments, 0);

        Assert.Equal(new[] { 3, 2, 1, 1 }, segments.Select(s => s.WidthUnits).ToArray());
        Assert.Equal(new[] { true, false, true, false }, segments.Select(s => s.IsBar).ToArray());
    }

    [Fact]
    public void ParityForFirstDigit_KnownValues_MatchStandardTable()
    {
        Assert.Equal("LLLLLL", EanUpcDigitPatterns.ParityForFirstDigit(0));
        Assert.Equal("LLGLGG", EanUpcDigitPatterns.ParityForFirstDigit(1));
        Assert.Equal("LGGLGL", EanUpcDigitPatterns.ParityForFirstDigit(9));
    }

    [Theory]
    [InlineData("03600029145", '2')]
    [InlineData("978030640615", '7')]
    [InlineData("9638507", '4')]
    public void ComputeCheckDigit_KnownValues_ReturnsExpectedDigit(string dataDigits, char expected)
    {
        Assert.Equal(expected, EanUpcDigitPatterns.ComputeCheckDigit(dataDigits));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `EanUpcDigitPatterns` does not exist yet.

- [ ] **Step 3: Implement the helper**

Add to `src/IBEBarcode.Core/IBEBarcode.Core.csproj`, inside a new `<ItemGroup>`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="IBEBarcode.Core.Tests" />
</ItemGroup>
```

Create `src/IBEBarcode.Core/Encoders/EanUpcDigitPatterns.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

internal static class EanUpcDigitPatterns
{
    public static readonly int[][] Widths =
    {
        new[] { 3, 2, 1, 1 },
        new[] { 2, 2, 2, 1 },
        new[] { 2, 1, 2, 2 },
        new[] { 1, 4, 1, 1 },
        new[] { 1, 1, 3, 2 },
        new[] { 1, 2, 3, 1 },
        new[] { 1, 1, 1, 4 },
        new[] { 1, 3, 1, 2 },
        new[] { 1, 2, 1, 3 },
        new[] { 3, 1, 1, 2 },
    };

    private static readonly string[] FirstDigitParity =
    {
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL",
    };

    public static string ParityForFirstDigit(int firstDigit) => FirstDigitParity[firstDigit];

    public static void AppendLeftDigit(List<BarSegment> segments, int digit, bool useGCode)
    {
        var widths = Widths[digit];
        var isBar = false;

        for (var i = 0; i < 4; i++)
        {
            var width = useGCode ? widths[3 - i] : widths[i];
            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }

    public static void AppendRightDigit(List<BarSegment> segments, int digit)
    {
        var widths = Widths[digit];
        var isBar = true;

        for (var i = 0; i < 4; i++)
        {
            segments.Add(new BarSegment(isBar, widths[i]));
            isBar = !isBar;
        }
    }

    public static void AppendGuard(List<BarSegment> segments, bool startsWithBar, params int[] widths)
    {
        var isBar = startsWithBar;

        foreach (var width in widths)
        {
            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }

    public static char ComputeCheckDigit(string dataDigits)
    {
        var sum = 0;
        var weight = 3;

        for (var i = dataDigits.Length - 1; i >= 0; i--)
        {
            sum += (dataDigits[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        return (char)('0' + (10 - sum % 10) % 10);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `EanUpcDigitPatternsTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/EanUpcDigitPatterns.cs src/IBEBarcode.Core/IBEBarcode.Core.csproj tests/IBEBarcode.Core.Tests/Encoders/EanUpcDigitPatternsTests.cs
git commit -m "$(cat <<'EOF'
Add shared EAN/UPC digit-pattern helper

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: `BarcodeSymbology` additions

**Files:**
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs`

**Interfaces:**
- Produces: `BarcodeSymbology.Ean13`, `.Ean8`, `.UpcA`, `.Isbn` values, consumed by Tasks 3-6.

- [ ] **Step 1: Add the enum values**

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
}
```

- [ ] **Step 2: Build to verify nothing broke**

Run: `dotnet test`
Expected: PASS — existing 33 tests still green (adding enum values is additive and doesn't change any switch/pattern-match in the codebase).

- [ ] **Step 3: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs
git commit -m "$(cat <<'EOF'
Add EAN/UPC/ISBN symbology enum values

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: EAN-13 encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Ean13Encoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Ean13EncoderTests.cs`

**Interfaces:**
- Consumes: `EanUpcDigitPatterns` (Task 1), `BarcodeSymbology.Ean13` (Task 2).
- Produces: `sealed class Ean13Encoder : IBarcodeEncoder`. Accepts 12 data digits (optionally + the 13th check digit); `pattern.Value` is always the full 13-digit code.

EAN-13 structure: start guard (bar,space,bar — widths 1,1,1) + 6 left-hand digits (digits 2-7 of the 13, L or G code per `ParityForFirstDigit(digit 1)`) + middle guard (space,bar,space,bar,space — widths 1,1,1,1,1) + 6 right-hand digits (digits 8-12 plus the check digit, all R-code) + end guard (bar,space,bar). Digit 1 itself is never drawn as bars — only implied by the parity pattern.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/Ean13EncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Ean13EncoderTests
{
    private readonly Ean13Encoder _encoder = new();

    [Fact]
    public void TryEncode_TwelveDigits_ComputesCheckDigitAndSucceeds()
    {
        var success = _encoder.TryEncode("978030640615", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("9780306406157", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Ean13, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_ThirteenDigitsWithCorrectCheckDigit_Succeeds()
    {
        var success = _encoder.TryEncode("9780306406157", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("9780306406157", pattern!.Value);
    }

    [Fact]
    public void TryEncode_ThirteenDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("9780306406150", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("check digit", error);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("978030640615", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("978030640615", out var pattern, out _);

        // start guard(3) + 6 left digits(24) + middle guard(5) + 6 right digits(24) + end guard(3) = 59
        Assert.Equal(59, pattern!.Segments.Count);
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
        var success = _encoder.TryEncode("97803064061X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `Ean13Encoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/Ean13Encoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Ean13Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Ean13;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value) || (value.Length != 12 && value.Length != 13))
        {
            error = "EAN-13 requires 12 data digits, optionally followed by the check digit (13 total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; EAN-13 encodes digits only.";
                return false;
            }
        }

        var dataDigits = value[..12];
        var checkDigit = EanUpcDigitPatterns.ComputeCheckDigit(dataDigits);

        if (value.Length == 13 && value[12] != checkDigit)
        {
            error = $"Invalid EAN-13 check digit: expected '{checkDigit}', got '{value[12]}'.";
            return false;
        }

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        var firstDigit = dataDigits[0] - '0';
        var parity = EanUpcDigitPatterns.ParityForFirstDigit(firstDigit);

        for (var i = 1; i < 7; i++)
        {
            EanUpcDigitPatterns.AppendLeftDigit(segments, dataDigits[i] - '0', parity[i - 1] == 'G');
        }

        EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1, 1, 1, 1);

        for (var i = 7; i < 12; i++)
        {
            EanUpcDigitPatterns.AppendRightDigit(segments, dataDigits[i] - '0');
        }

        EanUpcDigitPatterns.AppendRightDigit(segments, checkDigit - '0');
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        var fullValue = dataDigits + checkDigit;
        pattern = BarcodePattern.Create(fullValue, segments, fullValue);
        error = null;
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `Ean13EncoderTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/Ean13Encoder.cs tests/IBEBarcode.Core.Tests/Encoders/Ean13EncoderTests.cs
git commit -m "$(cat <<'EOF'
Add EAN-13 encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: EAN-8 encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Ean8Encoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Ean8EncoderTests.cs`

**Interfaces:**
- Consumes: `EanUpcDigitPatterns` (Task 1), `BarcodeSymbology.Ean8` (Task 2).
- Produces: `sealed class Ean8Encoder : IBarcodeEncoder`. Accepts 7 data digits (optionally + check digit); `pattern.Value` is always the full 8-digit code. Unlike EAN-13, all 4 left-hand digits are always L-code — there is no parity table.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/Ean8EncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Ean8EncoderTests
{
    private readonly Ean8Encoder _encoder = new();

    [Fact]
    public void TryEncode_SevenDigits_ComputesCheckDigitAndSucceeds()
    {
        var success = _encoder.TryEncode("9638507", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("96385074", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Ean8, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_EightDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("96385070", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("check digit", error);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("9638507", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("9638507", out var pattern, out _);

        // start guard(3) + 4 left digits(16) + middle guard(5) + 4 right digits(16) + end guard(3) = 43
        Assert.Equal(43, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `Ean8Encoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/Ean8Encoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Ean8Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Ean8;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value) || (value.Length != 7 && value.Length != 8))
        {
            error = "EAN-8 requires 7 data digits, optionally followed by the check digit (8 total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; EAN-8 encodes digits only.";
                return false;
            }
        }

        var dataDigits = value[..7];
        var checkDigit = EanUpcDigitPatterns.ComputeCheckDigit(dataDigits);

        if (value.Length == 8 && value[7] != checkDigit)
        {
            error = $"Invalid EAN-8 check digit: expected '{checkDigit}', got '{value[7]}'.";
            return false;
        }

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        for (var i = 0; i < 4; i++)
        {
            EanUpcDigitPatterns.AppendLeftDigit(segments, dataDigits[i] - '0', useGCode: false);
        }

        EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1, 1, 1, 1);

        for (var i = 4; i < 7; i++)
        {
            EanUpcDigitPatterns.AppendRightDigit(segments, dataDigits[i] - '0');
        }

        EanUpcDigitPatterns.AppendRightDigit(segments, checkDigit - '0');
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        var fullValue = dataDigits + checkDigit;
        pattern = BarcodePattern.Create(fullValue, segments, fullValue);
        error = null;
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `Ean8EncoderTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/Ean8Encoder.cs tests/IBEBarcode.Core.Tests/Encoders/Ean8EncoderTests.cs
git commit -m "$(cat <<'EOF'
Add EAN-8 encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: UPC-A encoder (delegates to EAN-13)

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/UpcAEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/UpcAEncoderTests.cs`

**Interfaces:**
- Consumes: `Ean13Encoder` (Task 3), `BarcodeSymbology.UpcA` (Task 2).
- Produces: `sealed class UpcAEncoder : IBarcodeEncoder`. Accepts 11 data digits (optionally + check digit); `pattern.Value` is the 12-digit UPC-A code (not the 13-digit EAN-13 form).

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/UpcAEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class UpcAEncoderTests
{
    private readonly UpcAEncoder _encoder = new();

    [Fact]
    public void TryEncode_ElevenDigits_ComputesCheckDigitAndSucceeds()
    {
        var success = _encoder.TryEncode("03600029145", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("036000291452", pattern!.Value);
        Assert.Equal(BarcodeSymbology.UpcA, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_TwelveDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("036000291450", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_ProducesSameSegmentsAsEquivalentEan13()
    {
        var upcSuccess = _encoder.TryEncode("03600029145", out var upcPattern, out _);
        var ean13Success = new Ean13Encoder().TryEncode("003600029145", out var ean13Pattern, out _);

        Assert.True(upcSuccess);
        Assert.True(ean13Success);
        Assert.Equal(ean13Pattern!.Segments, upcPattern!.Segments);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `UpcAEncoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/UpcAEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class UpcAEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.UpcA;

    private readonly Ean13Encoder _ean13 = new();

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value) || (value.Length != 11 && value.Length != 12))
        {
            error = "UPC-A requires 11 data digits, optionally followed by the check digit (12 total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; UPC-A encodes digits only.";
                return false;
            }
        }

        if (!_ean13.TryEncode("0" + value, out var ean13Pattern, out var innerError))
        {
            error = innerError?.Replace("EAN-13", "UPC-A");
            return false;
        }

        var upcValue = ean13Pattern!.Value[1..];
        pattern = BarcodePattern.Create(upcValue, ean13Pattern.Segments, upcValue);
        error = null;
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `UpcAEncoderTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/UpcAEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/UpcAEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add UPC-A encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: ISBN (Bookland) encoder (delegates to EAN-13)

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/IsbnEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/IsbnEncoderTests.cs`

**Interfaces:**
- Consumes: `Ean13Encoder` (Task 3), `BarcodeSymbology.Isbn` (Task 2).
- Produces: `sealed class IsbnEncoder : IBarcodeEncoder`. Accepts the first 9 digits of an ISBN-10 (hyphens allowed and stripped; the ISBN-10's own check character, 10th position, is ignored — Bookland barcodes always use a freshly computed EAN-13 check digit). `pattern.Value` is the resulting 13-digit Bookland EAN-13 code.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/IsbnEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class IsbnEncoderTests
{
    private readonly IsbnEncoder _encoder = new();

    [Fact]
    public void TryEncode_NineCoreDigits_ProducesBooklandEan13()
    {
        var success = _encoder.TryEncode("030640615", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("9780306406157", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Isbn, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_TenDigitIsbnWithHyphens_StripsHyphensAndOwnCheckChar()
    {
        var success = _encoder.TryEncode("0-306-40615-2", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("9780306406157", pattern!.Value);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("12345", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `IsbnEncoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/IsbnEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class IsbnEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Isbn;

    private readonly Ean13Encoder _ean13 = new();

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        var digitsOnly = value.Replace("-", "");

        if (digitsOnly.Length != 9 && digitsOnly.Length != 10)
        {
            error = "ISBN requires the first 9 digits of an ISBN-10 (its own check character is ignored and replaced by a freshly computed EAN-13 check digit).";
            return false;
        }

        var isbnCore = digitsOnly[..9];

        foreach (var ch in isbnCore)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; the first 9 ISBN characters must be digits.";
                return false;
            }
        }

        if (!_ean13.TryEncode("978" + isbnCore, out var ean13Pattern, out error))
        {
            return false;
        }

        pattern = ean13Pattern;
        error = null;
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `IsbnEncoderTests` green, full solution test suite green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/IsbnEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/IsbnEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add ISBN (Bookland) encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

UPC-E (zero-suppressed compression + its own parity table), UPC 2-digit and
5-digit supplements, Code 93, Extended Code 39, Code 128 A/B/C, UCC/EAN-128
(GS1-128), Postnet, then QR Code/Data Matrix/PDF417/Aztec, then
`IBEBarcode.Rendering`, `IBEBarcode.Templates`, `IBEBarcode.Printing`,
`IBEBarcode.Desktop`, `IBEBarcode.Web` — each its own follow-up plan.
