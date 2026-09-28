# UPC 2-Digit and 5-Digit Supplemental Encoders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the UPC 2-digit and 5-digit supplemental ("add-on") encoders — the last two items from the legacy Professional Edition's original 16-format list that hadn't been built yet. These small codes are printed alongside a main UPC-A/EAN-13 symbol (2-digit for periodical issue numbers, 5-digit for book/periodical prices) — the legacy product listed them as their own distinct format entries, so this plan implements them as standalone `IBarcodeEncoder`s, the same architectural pattern as every other encoder; a caller renders one alongside a main symbol if it wants that layout.

**Architecture:** `Upc2DigitSupplementEncoder` and `Upc5DigitSupplementEncoder` both use a shared start guard (`bar(1), space(1), bar(2)`, distinct from the main EAN/UPC start guard) and a shared inter-digit separator (`space(1), bar(1)`) — no end guard, since supplements are always followed by nothing (the main symbol's own guards already close the overall label). Digit rendering reuses `EanUpcDigitPatterns.AppendLeftDigit` (the same L/G-code drawing routine already used by `Ean13Encoder`/`Ean8Encoder`/`UpcEEncoder`). Parity (which digits use L vs. G) is checksum-derived rather than looked up by an external check digit: the 2-digit form uses `value mod 4` directly as a 2-bit parity selector; the 5-digit form computes a weighted checksum then looks up one of 10 five-bit parity patterns.

**Tech Stack:** Same as prior Core plans. The start-guard/separator widths and the 5-digit checksum formula were fetched from ZXing's `UPCEANExtensionSupport`/`UPCEANExtension5Support` source and hand-traced line by line (the checksum method's two loops and multiplications were reproduced exactly, not paraphrased, to avoid transcription error in a formula that's easy to get subtly wrong).

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.)

## Verified reference values used by this plan

- Shared start guard: `bar(1), space(1), bar(2)` (`EXTENSION_START_PATTERN = {1,1,2}` in ZXing, confirmed bar-first).
- Inter-digit separator: `space(1), bar(1)` (structurally required — every L/G-code digit pattern ends in a bar, so the next element must be a space to keep alternation).
- 2-digit parity: `parityBits = (10*d0 + d1) % 4`, a 2-bit value, bit 1 = first digit's parity, bit 0 = second digit's parity (0 = L, 1 = G).
- 5-digit checksum, traced exactly from ZXing's `extensionChecksum`: `checksum = (3*(d0+d2+d4) + 9*(d1+d3)) mod 10` (d0..d4 = the 5 digits left to right, 0-indexed).
- 5-digit `CHECK_DIGIT_ENCODINGS` table (index = checksum 0-9, value = 5-bit parity mask, bit 4 = first digit's parity down to bit 0 = fifth digit's parity): `0x18, 0x14, 0x12, 0x11, 0x0C, 0x06, 0x03, 0x0A, 0x09, 0x05`.

---

### Task 1: `Upc2DigitSupplementEncoder` and `Upc5DigitSupplementEncoder`

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Upc2DigitSupplementEncoder.cs`
- Create: `src/IBEBarcode.Core/Encoders/Upc5DigitSupplementEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Upc2DigitSupplementEncoderTests.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Upc5DigitSupplementEncoderTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `Upc2DigitSupplement`, `Upc5DigitSupplement`)

**Interfaces:**
- Consumes: `EanUpcDigitPatterns` (from the EAN/UPC family plan).
- Produces: `BarcodeSymbology.Upc2DigitSupplement`/`Upc5DigitSupplement` and the two encoder classes, both `IBarcodeEncoder`.

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
    Code93,
    Code39Extended,
    Code128,
    Postnet,
    QrCode,
    Gs1_128,
    UpcE,
    Upc2DigitSupplement,
    Upc5DigitSupplement,
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/Upc2DigitSupplementEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Upc2DigitSupplementEncoderTests
{
    private readonly Upc2DigitSupplementEncoder _encoder = new();

    [Fact]
    public void TryEncode_TwoDigits_Succeeds()
    {
        // 42 % 4 = 2 = 0b10 -> digit0=G, digit1=L
        var success = _encoder.TryEncode("42", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("42", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Upc2DigitSupplement, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("42", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("42", out var pattern, out _);

        // start(3) + digit0(4) + separator(2) + digit1(4) = 13
        Assert.Equal(13, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_StartGuardMatchesKnownPattern()
    {
        _encoder.TryEncode("42", out var pattern, out _);

        var startGuard = pattern!.Segments.Take(3).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 1, 2 }, startGuard);
    }

    [Fact]
    public void TryEncode_ParitySelectsCorrectLOrGCode()
    {
        // 42 % 4 = 2 = 0b10 -> digit '4' uses G-code, digit '2' uses L-code
        var success = _encoder.TryEncode("42", out var pattern, out _);

        Assert.True(success);

        var digit0Widths = pattern!.Segments.Skip(3).Take(4).Select(s => s.WidthUnits).ToArray();
        var digit1Widths = pattern.Segments.Skip(9).Take(4).Select(s => s.WidthUnits).ToArray();

        // digit '4' L-code widths {1,1,3,2}; G-code is the reverse {2,3,1,1}
        Assert.Equal(new[] { 2, 3, 1, 1 }, digit0Widths);
        // digit '2' L-code widths {2,1,2,2} (used directly, since parity bit0=0=L)
        Assert.Equal(new[] { 2, 1, 2, 2 }, digit1Widths);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("4", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("4X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
```

Create `tests/IBEBarcode.Core.Tests/Encoders/Upc5DigitSupplementEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Upc5DigitSupplementEncoderTests
{
    private readonly Upc5DigitSupplementEncoder _encoder = new();

    [Fact]
    public void TryEncode_FiveDigits_Succeeds()
    {
        var success = _encoder.TryEncode("52495", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("52495", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Upc5DigitSupplement, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("52495", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("52495", out var pattern, out _);

        // start(3) + 5 digits(20) + 4 separators(8) = 31
        Assert.Equal(31, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_ChecksumMatchesHandComputedValue()
    {
        // "52495": d0=5,d1=2,d2=4,d3=9,d4=5
        // checksum = (3*(5+4+5) + 9*(2+9)) mod 10 = (3*14 + 9*11) mod 10 = (42+99) mod 10 = 141 mod 10 = 1
        // CHECK_DIGIT_ENCODINGS[1] = 0x14 = 0b10100 -> digit0=G,digit1=L,digit2=G,digit3=L,digit4=L
        var success = _encoder.TryEncode("52495", out var pattern, out _);

        Assert.True(success);

        var digit0Widths = pattern!.Segments.Skip(3).Take(4).Select(s => s.WidthUnits).ToArray();
        // digit '5' L-code {1,2,3,1}; G-code (reversed) {1,3,2,1}
        Assert.Equal(new[] { 1, 3, 2, 1 }, digit0Widths);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("1234", out var pattern, out var error);

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
Expected: FAIL — the two encoder classes don't exist yet.

- [ ] **Step 4: Implement the encoders**

Create `src/IBEBarcode.Core/Encoders/Upc2DigitSupplementEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Upc2DigitSupplementEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Upc2DigitSupplement;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 2 })
        {
            error = "The 2-digit UPC/EAN supplement requires exactly 2 digits.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; the 2-digit supplement encodes digits only.";
                return false;
            }
        }

        var numericValue = (value[0] - '0') * 10 + (value[1] - '0');
        var parityBits = numericValue % 4;

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 2);

        for (var i = 0; i < 2; i++)
        {
            var digit = value[i] - '0';
            var bitPosition = 1 - i;
            var useGCode = ((parityBits >> bitPosition) & 1) != 0;
            EanUpcDigitPatterns.AppendLeftDigit(segments, digit, useGCode);

            if (i < 1)
            {
                EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1);
            }
        }

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }
}
```

Create `src/IBEBarcode.Core/Encoders/Upc5DigitSupplementEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Upc5DigitSupplementEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Upc5DigitSupplement;

    private static readonly int[] CheckDigitEncodings =
    {
        0x18, 0x14, 0x12, 0x11, 0x0C, 0x06, 0x03, 0x0A, 0x09, 0x05,
    };

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 5 })
        {
            error = "The 5-digit UPC/EAN supplement requires exactly 5 digits.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; the 5-digit supplement encodes digits only.";
                return false;
            }
        }

        var d = new int[5];
        for (var i = 0; i < 5; i++)
        {
            d[i] = value[i] - '0';
        }

        var checksum = (3 * (d[0] + d[2] + d[4]) + 9 * (d[1] + d[3])) % 10;
        var parityBits = CheckDigitEncodings[checksum];

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 2);

        for (var i = 0; i < 5; i++)
        {
            var bitPosition = 4 - i;
            var useGCode = ((parityBits >> bitPosition) & 1) != 0;
            EanUpcDigitPatterns.AppendLeftDigit(segments, d[i], useGCode);

            if (i < 4)
            {
                EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1);
            }
        }

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all new tests green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/Upc2DigitSupplementEncoder.cs src/IBEBarcode.Core/Encoders/Upc5DigitSupplementEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/Upc2DigitSupplementEncoderTests.cs tests/IBEBarcode.Core.Tests/Encoders/Upc5DigitSupplementEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add UPC 2-digit and 5-digit supplemental encoders

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

This completes every format from the legacy Professional Edition's
original 16-format list. Remaining work is this project's own later
additions: Data Matrix, PDF417, Aztec; GS1-128 AI parsing; mid-message
Code128 subset switching; a `HeightBarPattern` (Postnet) renderer;
wiring the two supplement encoders into the desktop/web UIs (they need
a "render alongside a main symbol" layout concept the current
one-barcode-at-a-time picker doesn't have yet); more paper templates;
Azure deployment; GitHub remote creation.
