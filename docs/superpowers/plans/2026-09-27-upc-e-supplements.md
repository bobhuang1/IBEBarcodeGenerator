# UPC-E Encoder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add UPC-E (the zero-suppressed, 6-digit-payload compressed form of UPC-A) — one of the legacy Professional Edition's original 16 formats that hadn't been built yet (unlike Data Matrix/PDF417/Aztec, which were this project's own later addition, not part of the original request).

**Architecture:** `UpcEEncoder` accepts a 6-digit payload (optionally preceded by the number-system digit and/or followed by the check digit, 6-8 characters total). It expands the payload to the equivalent 11-digit UPC-A data string (one of four expansion rules depending on the payload's last digit), computes the check digit by reusing `EanUpcDigitPatterns.ComputeCheckDigit` (the same helper `Ean13Encoder`/`Ean8Encoder` already use), then draws UPC-E's own compact symbol: a start guard, 6 digits using L/G parity selected from a (number-system, check-digit) lookup table, and UPC-E's distinct 6-module end guard (not the same end guard as UPC-A/EAN — UPC-E has no "second half," so a longer guard prevents scanner ambiguity).

**Tech Stack:** Same as prior Core plans. The parity lookup table, the exact 6-module end guard, and the four last-digit expansion rules were fetched from ZXing's `UPCEReader` source. The expansion algorithm was independently cross-checked against a real, publicly documented example (the Kellogg's Corn Flakes UPC: UPC-E `381100` ↔ UPC-A data `03800000110`, check digit `9`) and reproduces it exactly by hand.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.)

## Verified reference values used by this plan's tests

- Real-world example: UPC-E payload `381100` (number system `0`) expands to UPC-A 11-digit data `03800000110`, check digit `9` (hand-verified via the same weighted check-digit algorithm already proven correct for EAN-13/UPC-A/EAN-8/Code93/ISBN in this codebase) → full UPC-E display `03811009`.
- Parity pattern table (`NUMSYS_AND_CHECK_DIGIT_PATTERNS`, indexed `[numberSystem][checkDigit]`, 6-bit values, bit5=first digit's parity down to bit0=sixth digit's parity, 0=L/odd, 1=G/even):
  - System 0: `0x38, 0x34, 0x32, 0x31, 0x2C, 0x26, 0x23, 0x2A, 0x29, 0x25`
  - System 1: `0x07, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A`
- End guard: 6 narrow elements, all width 1 (space, bar, space, bar, space, bar) — distinct from the usual 3-element `{1,1,1}` end guard.
- Expansion rules by payload's 6th digit: `0`/`1`/`2` → `d0 d1 lastDigit 0000 d2 d3 d4`; `3` → `d0 d1 d2 00000 d3 d4`; `4` → `d0 d1 d2 d3 00000 d4`; `5`-`9` → `d0 d1 d2 d3 d4 0000 lastDigit`.

---

### Task 1: UPC-E encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/UpcEEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/UpcEEncoderTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `UpcE`)

**Interfaces:**
- Consumes: `EanUpcDigitPatterns` (from the EAN/UPC family plan).
- Produces: `BarcodeSymbology.UpcE` and `sealed class UpcEEncoder : IBarcodeEncoder`.

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
    QrCode,
    Gs1_128,
    UpcE,
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/UpcEEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class UpcEEncoderTests
{
    private readonly UpcEEncoder _encoder = new();

    [Fact]
    public void TryEncode_SixDigitPayload_MatchesRealWorldExample()
    {
        // Kellogg's Corn Flakes: UPC-E 381100 (system 0) -> UPC-A data 03800000110, check 9
        var success = _encoder.TryEncode("381100", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("03811009", pattern!.Value);
        Assert.Equal(BarcodeSymbology.UpcE, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SevenDigitsWithNumberSystem_SameResult()
    {
        var success = _encoder.TryEncode("0381100", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("03811009", pattern!.Value);
    }

    [Fact]
    public void TryEncode_EightDigitsWithCorrectCheckDigit_Succeeds()
    {
        var success = _encoder.TryEncode("03811009", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("03811009", pattern!.Value);
    }

    [Fact]
    public void TryEncode_EightDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("03811001", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("381100", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("381100", out var pattern, out _);

        // start(3) + 6 digits(24) + end(6) = 33
        Assert.Equal(33, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_EndGuardIsSixNarrowElements()
    {
        _encoder.TryEncode("381100", out var pattern, out _);

        var endGuard = pattern!.Segments.Skip(27).Take(6).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 1 }, endGuard);
    }

    [Fact]
    public void TryEncode_InvalidNumberSystem_ReturnsError()
    {
        var success = _encoder.TryEncode("2381100", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("12345", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("38110X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `UpcEEncoder` does not exist yet.

- [ ] **Step 4: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/UpcEEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class UpcEEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.UpcE;

    private static readonly int[][] NumSysAndCheckDigitPatterns =
    {
        new[] { 0x38, 0x34, 0x32, 0x31, 0x2C, 0x26, 0x23, 0x2A, 0x29, 0x25 },
        new[] { 0x07, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A },
    };

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 6 or 7 or 8 })
        {
            error = "UPC-E requires a 6-digit payload, optionally preceded by the number system digit and/or followed by the check digit (6-8 characters total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; UPC-E encodes digits only.";
                return false;
            }
        }

        char numberSystem;
        string payload;
        char? providedCheckDigit = null;

        if (value.Length == 6)
        {
            numberSystem = '0';
            payload = value;
        }
        else if (value.Length == 7)
        {
            numberSystem = value[0];
            payload = value.Substring(1, 6);
        }
        else
        {
            numberSystem = value[0];
            payload = value.Substring(1, 6);
            providedCheckDigit = value[7];
        }

        if (numberSystem is not ('0' or '1'))
        {
            error = "UPC-E's number system digit must be 0 or 1.";
            return false;
        }

        var expanded = ExpandToUpcAMiddle(payload);
        var elevenDigitData = $"{numberSystem}{expanded}";
        var checkDigit = EanUpcDigitPatterns.ComputeCheckDigit(elevenDigitData);

        if (providedCheckDigit is not null && providedCheckDigit != checkDigit)
        {
            error = $"Invalid UPC-E check digit: expected '{checkDigit}', got '{providedCheckDigit}'.";
            return false;
        }

        var numSysIndex = numberSystem - '0';
        var checkDigitIndex = checkDigit - '0';
        var parityBits = NumSysAndCheckDigitPatterns[numSysIndex][checkDigitIndex];

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        for (var i = 0; i < 6; i++)
        {
            var digit = payload[i] - '0';
            var bitPosition = 5 - i;
            var useGCode = ((parityBits >> bitPosition) & 1) != 0;
            EanUpcDigitPatterns.AppendLeftDigit(segments, digit, useGCode);
        }

        EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1, 1, 1, 1, 1);

        var fullValue = $"{numberSystem}{payload}{checkDigit}";
        pattern = BarcodePattern.Create(fullValue, segments, fullValue);
        error = null;
        return true;
    }

    private static string ExpandToUpcAMiddle(string payload)
    {
        var lastDigit = payload[5];

        return lastDigit switch
        {
            '0' or '1' or '2' => $"{payload[0]}{payload[1]}{lastDigit}0000{payload[2]}{payload[3]}{payload[4]}",
            '3' => $"{payload[0]}{payload[1]}{payload[2]}00000{payload[3]}{payload[4]}",
            '4' => $"{payload[0]}{payload[1]}{payload[2]}{payload[3]}00000{payload[4]}",
            _ => $"{payload[0]}{payload[1]}{payload[2]}{payload[3]}{payload[4]}0000{lastDigit}",
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `UpcEEncoderTests` green (including the real-world Kellogg's example), full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/UpcEEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/UpcEEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add UPC-E encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

UPC 2-digit and 5-digit supplemental codes (the last two items from the
original legacy format list). Wiring `UpcE` into the desktop/web
symbology pickers. Data Matrix, PDF417, Aztec (this project's own later
addition, not part of the original 16-format list). GS1-128 AI parsing.
A `HeightBarPattern` (Postnet) renderer.
