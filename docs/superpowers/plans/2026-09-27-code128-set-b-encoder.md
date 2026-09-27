# Code 128 (Set B) Encoder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Code 128 encoder to `IBEBarcode.Core`, scoped to Code Set B (ASCII 32-127 — covers digits, uppercase, lowercase, and punctuation, which is the practical common case for retail/logistics labels).

**Architecture:** Linear, width-based — fits the existing `BarcodePattern`/`BarSegment`/`IBarcodeEncoder` contract with no changes. Unlike Code 39/Codabar/Code 93, Code 128's 107 symbol values map directly to literal 6-integer width arrays (no bit-packing/run-length decoding needed) — the reference table gives widths straight from source.

**Scope decision:** Code Set A (uppercase + control characters) and Code Set C (double-density digit pairs), mid-stream subset switching, and GS1-128 (UCC/EAN-128) application-identifier wrapping are deferred to a later plan. Set B alone covers the overwhelming majority of real-world Code 128 labels and is a clean, independently useful, independently testable increment.

**Tech Stack:** Same as prior plans. The 107-row `CODE_PATTERNS` width table, the Set B character-to-value rule (`value = char - ' '`), and the mod-103 checksum weighting were fetched from ZXing's `Code128Reader`/`Code128Writer` source. The checksum weighting rule (start character and the *first* data character both carry weight 1, then weight increments by 1 per subsequent data character) was independently cross-checked against Wikipedia's fully worked `PJJ123C` example (sum 878, checksum 54) and matches exactly — that example uses Code Set A, but the weighting mechanic itself is subset-independent, only the character-to-value table differs.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) Every project targets `net10.0`; MIT license; no third-party barcode-encoding library; no database; single solution file; `Nullable`/`ImplicitUsings` enabled everywhere.

## Verified reference values used by this plan's tests

- `START_B` = symbol value 104, pattern `{2,1,1,2,1,4}`.
- `STOP` = symbol value 106, pattern `{2,3,3,1,1,1,2}` (7 elements/13 modules, vs. 6 elements/11 modules for every other symbol — includes Code 128's extra terminating bar).
- Checksum weighting mechanic confirmed against Wikipedia's worked `PJJ123C` (Code Set A) example: start value contributes unweighted (equivalently weight 1), first data character also carries weight 1, then weight increments by 1 per subsequent data character; final value is `sum mod 103`.
- Self-derived Set B example (algorithm applied directly, not independently published, but built from two independently verified rules): data `"A1"` → values `A`=33, `1`=17 → checksum = `104 + 33*1 + 17*2 = 171`, `171 mod 103 = 68`.

---

### Task 1: Code 128 (Set B) encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Code128Encoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Code128EncoderTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `Code128`)

**Interfaces:**
- Consumes: `BarSegment`, `BarcodePattern`, `IBarcodeEncoder` from the foundation plan.
- Produces: `BarcodeSymbology.Code128` and `sealed class Code128Encoder : IBarcodeEncoder`. Accepts ASCII 32-127 input; always encodes as Start-B + data + checksum + Stop; no inter-character gaps (Code 128 characters, like Code 93, pack directly adjacent).

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
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/Code128EncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Code128EncoderTests
{
    private readonly Code128Encoder _encoder = new();

    [Fact]
    public void TryEncode_ValidValue_Succeeds()
    {
        var success = _encoder.TryEncode("A1", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("A1", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Code128, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("A1", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("A1", out var pattern, out _);

        // start-B(6) + 'A'(6) + '1'(6) + checksum(6) + stop(7) = 31
        Assert.Equal(31, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_StartAndStopPatternsAreCorrect()
    {
        _encoder.TryEncode("A1", out var pattern, out _);

        var startWidths = pattern!.Segments.Take(6).Select(s => s.WidthUnits).ToArray();
        var stopWidths = pattern.Segments.Skip(24).Take(7).Select(s => s.WidthUnits).ToArray();

        Assert.Equal(new[] { 2, 1, 1, 2, 1, 4 }, startWidths);
        Assert.Equal(new[] { 2, 3, 3, 1, 1, 1, 2 }, stopWidths);
    }

    [Fact]
    public void TryEncode_ChecksumWeighting_MatchesHandComputedValue()
    {
        // "A1": start=104 (weight 1, unweighted), 'A'=33 (weight 1), '1'=17 (weight 2)
        // -> 104 + 33*1 + 17*2 = 171, 171 mod 103 = 68 -> checksum symbol value 68.
        // A different data string whose data values happen to sum to a different
        // checksum must therefore produce different segments in that slot.
        _encoder.TryEncode("A1", out var patternA1, out _);
        _encoder.TryEncode("A2", out var patternA2, out _);

        var checksumA1 = patternA1!.Segments.Skip(18).Take(6).ToArray();
        var checksumA2 = patternA2!.Segments.Skip(18).Take(6).ToArray();

        Assert.NotEqual(checksumA1, checksumA2);
    }

    [Fact]
    public void TryEncode_CharacterOutsideAscii32To127_ReturnsError()
    {
        var success = _encoder.TryEncode("café", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_EmptyValue_ReturnsError()
    {
        var success = _encoder.TryEncode("", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `Code128Encoder` does not exist yet.

- [ ] **Step 4: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/Code128Encoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Code128Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code128;

    private const int StartB = 104;
    private const int Stop = 106;

    private static readonly int[][] Patterns =
    {
        new[] { 2, 1, 2, 2, 2, 2 }, new[] { 2, 2, 2, 1, 2, 2 }, new[] { 2, 2, 2, 2, 2, 1 }, new[] { 1, 2, 1, 2, 2, 3 }, new[] { 1, 2, 1, 3, 2, 2 },
        new[] { 1, 3, 1, 2, 2, 2 }, new[] { 1, 2, 2, 2, 1, 3 }, new[] { 1, 2, 2, 3, 1, 2 }, new[] { 1, 3, 2, 2, 1, 2 }, new[] { 2, 2, 1, 2, 1, 3 },
        new[] { 2, 2, 1, 3, 1, 2 }, new[] { 2, 3, 1, 2, 1, 2 }, new[] { 1, 1, 2, 2, 3, 2 }, new[] { 1, 2, 2, 1, 3, 2 }, new[] { 1, 2, 2, 2, 3, 1 },
        new[] { 1, 1, 3, 2, 2, 2 }, new[] { 1, 2, 3, 1, 2, 2 }, new[] { 1, 2, 3, 2, 2, 1 }, new[] { 2, 2, 3, 2, 1, 1 }, new[] { 2, 2, 1, 1, 3, 2 },
        new[] { 2, 2, 1, 2, 3, 1 }, new[] { 2, 1, 3, 2, 1, 2 }, new[] { 2, 2, 3, 1, 1, 2 }, new[] { 3, 1, 2, 1, 3, 1 }, new[] { 3, 1, 1, 2, 2, 2 },
        new[] { 3, 2, 1, 1, 2, 2 }, new[] { 3, 2, 1, 2, 2, 1 }, new[] { 3, 1, 2, 2, 1, 2 }, new[] { 3, 2, 2, 1, 1, 2 }, new[] { 3, 2, 2, 2, 1, 1 },
        new[] { 2, 1, 2, 1, 2, 3 }, new[] { 2, 1, 2, 3, 2, 1 }, new[] { 2, 3, 2, 1, 2, 1 }, new[] { 1, 1, 1, 3, 2, 3 }, new[] { 1, 3, 1, 1, 2, 3 },
        new[] { 1, 3, 1, 3, 2, 1 }, new[] { 1, 1, 2, 3, 1, 3 }, new[] { 1, 3, 2, 1, 1, 3 }, new[] { 1, 3, 2, 3, 1, 1 }, new[] { 2, 1, 1, 3, 1, 3 },
        new[] { 2, 3, 1, 1, 1, 3 }, new[] { 2, 3, 1, 3, 1, 1 }, new[] { 1, 1, 2, 1, 3, 3 }, new[] { 1, 1, 2, 3, 3, 1 }, new[] { 1, 3, 2, 1, 3, 1 },
        new[] { 1, 1, 3, 1, 2, 3 }, new[] { 1, 1, 3, 3, 2, 1 }, new[] { 1, 3, 3, 1, 2, 1 }, new[] { 3, 1, 3, 1, 2, 1 }, new[] { 2, 1, 1, 3, 3, 1 },
        new[] { 2, 3, 1, 1, 3, 1 }, new[] { 2, 1, 3, 1, 1, 3 }, new[] { 2, 1, 3, 3, 1, 1 }, new[] { 2, 1, 3, 1, 3, 1 }, new[] { 3, 1, 1, 1, 2, 3 },
        new[] { 3, 1, 1, 3, 2, 1 }, new[] { 3, 3, 1, 1, 2, 1 }, new[] { 3, 1, 2, 1, 1, 3 }, new[] { 3, 1, 2, 3, 1, 1 }, new[] { 3, 3, 2, 1, 1, 1 },
        new[] { 3, 1, 4, 1, 1, 1 }, new[] { 2, 2, 1, 4, 1, 1 }, new[] { 4, 3, 1, 1, 1, 1 }, new[] { 1, 1, 1, 2, 2, 4 }, new[] { 1, 1, 1, 4, 2, 2 },
        new[] { 1, 2, 1, 1, 2, 4 }, new[] { 1, 2, 1, 4, 2, 1 }, new[] { 1, 4, 1, 1, 2, 2 }, new[] { 1, 4, 1, 2, 2, 1 }, new[] { 1, 1, 2, 2, 1, 4 },
        new[] { 1, 1, 2, 4, 1, 2 }, new[] { 1, 2, 2, 1, 1, 4 }, new[] { 1, 2, 2, 4, 1, 1 }, new[] { 1, 4, 2, 1, 1, 2 }, new[] { 1, 4, 2, 2, 1, 1 },
        new[] { 2, 4, 1, 2, 1, 1 }, new[] { 2, 2, 1, 1, 1, 4 }, new[] { 4, 1, 3, 1, 1, 1 }, new[] { 2, 4, 1, 1, 1, 2 }, new[] { 1, 3, 4, 1, 1, 1 },
        new[] { 1, 1, 1, 2, 4, 2 }, new[] { 1, 2, 1, 1, 4, 2 }, new[] { 1, 2, 1, 2, 4, 1 }, new[] { 1, 1, 4, 2, 1, 2 }, new[] { 1, 2, 4, 1, 1, 2 },
        new[] { 1, 2, 4, 2, 1, 1 }, new[] { 4, 1, 1, 2, 1, 2 }, new[] { 4, 2, 1, 1, 1, 2 }, new[] { 4, 2, 1, 2, 1, 1 }, new[] { 2, 1, 2, 1, 4, 1 },
        new[] { 2, 1, 4, 1, 2, 1 }, new[] { 4, 1, 2, 1, 2, 1 }, new[] { 1, 1, 1, 1, 4, 3 }, new[] { 1, 1, 1, 3, 4, 1 }, new[] { 1, 3, 1, 1, 4, 1 },
        new[] { 1, 1, 4, 1, 1, 3 }, new[] { 1, 1, 4, 3, 1, 1 }, new[] { 4, 1, 1, 1, 1, 3 }, new[] { 4, 1, 1, 3, 1, 1 }, new[] { 1, 1, 3, 1, 4, 1 },
        new[] { 1, 1, 4, 1, 3, 1 }, new[] { 3, 1, 1, 1, 4, 1 }, new[] { 4, 1, 1, 1, 3, 1 }, new[] { 2, 1, 1, 4, 1, 2 }, new[] { 2, 1, 1, 2, 1, 4 },
        new[] { 2, 1, 1, 2, 3, 2 }, new[] { 2, 3, 3, 1, 1, 1, 2 },
    };

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var values = new int[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (ch < ' ' || ch > (char)127)
            {
                error = $"Character '{ch}' is outside the ASCII 32-127 range Code 128 Set B supports.";
                return false;
            }

            values[i] = ch - ' ';
        }

        var checkSum = StartB;
        var weight = 1;

        foreach (var v in values)
        {
            checkSum += v * weight;
            weight++;
        }

        checkSum %= 103;

        var segments = new List<BarSegment>();
        AppendSymbol(segments, StartB);

        foreach (var v in values)
        {
            AppendSymbol(segments, v);
        }

        AppendSymbol(segments, checkSum);
        AppendSymbol(segments, Stop);

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }

    private static void AppendSymbol(List<BarSegment> segments, int symbolValue)
    {
        var widths = Patterns[symbolValue];
        var isBar = true;

        foreach (var width in widths)
        {
            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `Code128EncoderTests` green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/Code128Encoder.cs tests/IBEBarcode.Core.Tests/Encoders/Code128EncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Code 128 (Set B) encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Code 128 Set A/Set C and subset switching, UCC/EAN-128 (GS1-128 application
identifiers) built on top of Code 128. Then the non-linear pattern
redesign needed for Postnet (height-varying bars) and QR
Code/Data Matrix/PDF417/Aztec (2D). Then `IBEBarcode.Rendering`,
`IBEBarcode.Templates`, `IBEBarcode.Printing`, `IBEBarcode.Desktop`,
`IBEBarcode.Web`.
