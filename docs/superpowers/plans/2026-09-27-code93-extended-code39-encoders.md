# Code 93 and Extended Code 39 Encoders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Code 93 (with its mandatory two-character C/K checksum) and Extended Code 39 (full 0-127 ASCII via shift-character sequences) to `IBEBarcode.Core`.

**Architecture:** Both are linear, width-based symbologies and fit the existing `BarcodePattern`/`BarSegment`/`IBarcodeEncoder` contract from the foundation plan without any changes to it. `Code93Encoder` is a standalone implementation (its character encoding is structurally different from Code 39/Codabar: each character is 6 run-length-encoded elements summing to 9 modules, rather than 9 fixed-position bits). `ExtendedCode39Encoder` is a thin wrapper: it expands any ASCII 0-127 input into a plain Code 39-alphabet string (using the standard `$`/`%`/`/`/`+` shift-character sequences) and delegates to the existing `Code39Encoder`.

**Tech Stack:** Same as prior plans — .NET 10, xUnit, no third-party barcode library. Code 93's character-value table and run-length bit-decoding scheme were fetched from ZXing's `Code93Reader` source; the checksum algorithm and its exact weighting/wraparound were cross-checked against a real, independently published worked example (`CODE 93` → check characters `E`, `0`) and reproduced exactly by hand. Extended Code 39's shift-character mapping was derived by inverting ZXing's published `Code39Reader.decodeExtended` table (the encode direction is the unique inverse of that decode table) and spot-checked against every documented shift range.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) Every project targets `net10.0`; MIT license; no third-party barcode-encoding library; no database; single solution file; `Nullable`/`ImplicitUsings` enabled everywhere.

## Verified reference values used by this plan's tests

- Code 93 digit `0` (character value 0) bar pattern `0x114` decodes (MSB-first run-length, 6 elements, starting with a bar) to widths `[1,3,1,1,1,2]` — hand-derived from the raw bit pattern `100010100`.
- Code 93 worked example (published, independently reproduced by hand in this plan's research): data `CODE 93` → character values `[12,24,13,14,38,9,3]` → C-checksum sum `484` → `484 mod 47 = 14` → C = `E`; K-checksum sum (data + C) `611` → `611 mod 47 = 0` → K = `0`. Full encoded string `*CODE 93E0*`.

---

### Task 1: Code 93 encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Code93Encoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Code93EncoderTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `Code93`)

**Interfaces:**
- Consumes: `BarSegment`, `BarcodePattern`, `IBarcodeEncoder` from the foundation plan.
- Produces: `BarcodeSymbology.Code93` and `sealed class Code93Encoder : IBarcodeEncoder` in `IBEBarcode.Core.Encoders`. Alphabet is the same 43 characters as `Code39Encoder` (digits, uppercase, `-. $/+%`) — the 4 Code 93 "shift" control characters used for full-ASCII Code 93 are out of scope here (not requested by the design spec; Extended Code 39 already covers full-ASCII needs). Every encoded value automatically gets two trailing check characters (C then K) before the stop character; there is no inter-character gap (Code 93 characters always start with a bar-run and end with a space-run, so concatenating them directly preserves strict bar/space alternation).

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
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/Code93EncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Code93EncoderTests
{
    private readonly Code93Encoder _encoder = new();

    [Fact]
    public void TryEncode_PublishedWorkedExample_ProducesExpectedCheckCharacters()
    {
        var success = _encoder.TryEncode("CODE 93", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("CODE 93", pattern!.Value);
        Assert.Equal("*CODE 93E0*", pattern.HumanReadableText);
        Assert.Equal(BarcodeSymbology.Code93, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("CODE 93", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("CODE 93", out var pattern, out _);

        // start(1) + 7 data + C(1) + K(1) + stop(1) = 11 characters, 6 elements each = 66
        Assert.Equal(66, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_DigitZero_ProducesKnownRunLengthPattern()
    {
        var success = _encoder.TryEncode("0", out var pattern, out _);

        Assert.True(success);
        // start(6) + '0'(6) + C(6) + K(6) + stop(6) = 30 segments
        Assert.Equal(30, pattern!.Segments.Count);

        var dataSegments = pattern.Segments.Skip(6).Take(6).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 3, 1, 1, 1, 2 }, dataSegments);
    }

    [Fact]
    public void TryEncode_LowercaseIsNormalizedToUppercase()
    {
        var lower = _encoder.TryEncode("code", out var lowerPattern, out _);
        var upper = _encoder.TryEncode("CODE", out var upperPattern, out _);

        Assert.True(lower);
        Assert.True(upper);
        Assert.Equal(upperPattern!.Segments, lowerPattern!.Segments);
    }

    [Fact]
    public void TryEncode_InvalidCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("HAS@SYMBOL", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("@", error);
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
Expected: FAIL — `Code93Encoder` does not exist yet.

- [ ] **Step 4: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/Code93Encoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Code93Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code93;

    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    private static readonly int[] CharacterPatterns =
    {
        0x114, 0x148, 0x144, 0x142, 0x128, 0x124, 0x122, 0x150, 0x112, 0x10A,
        0x1A8, 0x1A4, 0x1A2, 0x194, 0x192, 0x18A, 0x168, 0x164, 0x162, 0x134,
        0x11A, 0x158, 0x14C, 0x146, 0x12C, 0x116, 0x1B4, 0x1B2, 0x1AC, 0x1A6,
        0x196, 0x19A, 0x16C, 0x166, 0x136, 0x13A,
        0x12E, 0x1D4, 0x1D2, 0x1CA, 0x16E, 0x176, 0x1AE,
    };

    private const int StartStopPattern = 0x15E;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var upper = value.ToUpperInvariant();
        var values = new int[upper.Length];

        for (var i = 0; i < upper.Length; i++)
        {
            var index = Alphabet.IndexOf(upper[i]);
            if (index < 0)
            {
                error = $"Character '{upper[i]}' is not valid in Code 93.";
                return false;
            }

            values[i] = index;
        }

        var cValue = WeightedChecksum(values, 20);
        var withC = values.Append(cValue).ToArray();
        var kValue = WeightedChecksum(withC, 15);

        var segments = new List<BarSegment>();
        AppendCharacter(segments, StartStopPattern);

        foreach (var v in values)
        {
            AppendCharacter(segments, CharacterPatterns[v]);
        }

        AppendCharacter(segments, CharacterPatterns[cValue]);
        AppendCharacter(segments, CharacterPatterns[kValue]);
        AppendCharacter(segments, StartStopPattern);

        var humanReadable = $"*{upper}{Alphabet[cValue]}{Alphabet[kValue]}*";
        pattern = BarcodePattern.Create(upper, segments, humanReadable);
        error = null;
        return true;
    }

    private static int WeightedChecksum(int[] values, int maxWeight)
    {
        var sum = 0;
        var weight = 1;

        for (var i = values.Length - 1; i >= 0; i--)
        {
            sum += values[i] * weight;
            weight = weight == maxWeight ? 1 : weight + 1;
        }

        return sum % 47;
    }

    private static void AppendCharacter(List<BarSegment> segments, int bitPattern)
    {
        var isBar = true;
        var bitIndex = 8;

        while (bitIndex >= 0)
        {
            var expected = isBar ? 1 : 0;
            var width = 0;

            while (bitIndex >= 0 && ((bitPattern >> bitIndex) & 1) == expected)
            {
                width++;
                bitIndex--;
            }

            segments.Add(new BarSegment(isBar, width));
            isBar = !isBar;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `Code93EncoderTests` green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/Code93Encoder.cs tests/IBEBarcode.Core.Tests/Encoders/Code93EncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Code 93 encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Extended Code 39 encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/ExtendedCode39Encoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/ExtendedCode39EncoderTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `Code39Extended`)

**Interfaces:**
- Consumes: `Code39Encoder` (foundation plan).
- Produces: `BarcodeSymbology.Code39Extended` and `sealed class ExtendedCode39Encoder : IBarcodeEncoder`. Accepts any ASCII string (codes 0-127), case-sensitive (unlike plain `Code39Encoder`, which uppercases everything — case is exactly how Extended Code 39 distinguishes e.g. `a` from `A`).

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
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/ExtendedCode39EncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class ExtendedCode39EncoderTests
{
    private readonly ExtendedCode39Encoder _encoder = new();
    private readonly Code39Encoder _code39 = new();

    [Fact]
    public void TryEncode_BaseAlphabetOnly_MatchesPlainCode39()
    {
        var extendedSuccess = _encoder.TryEncode("HELLO123", out var extendedPattern, out var error);
        var plainSuccess = _code39.TryEncode("HELLO123", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Null(error);
        Assert.Equal("HELLO123", extendedPattern!.Value);
        Assert.Equal(plainPattern!.Segments, extendedPattern.Segments);
        Assert.Equal(BarcodeSymbology.Code39Extended, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_Lowercase_ExpandsToPlusShiftSequence()
    {
        var extendedSuccess = _encoder.TryEncode("hello", out var extendedPattern, out _);
        var plainSuccess = _code39.TryEncode("+H+E+L+L+O", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Equal(plainPattern!.Segments, extendedPattern!.Segments);
    }

    [Fact]
    public void TryEncode_PreservesCaseInValue()
    {
        _encoder.TryEncode("MixedCase", out var pattern, out _);

        Assert.Equal("MixedCase", pattern!.Value);
    }

    [Fact]
    public void TryEncode_PunctuationOutsideBaseAlphabet_ExpandsToSlashShiftSequence()
    {
        var extendedSuccess = _encoder.TryEncode("a!", out var extendedPattern, out _);
        var plainSuccess = _code39.TryEncode("+A/A", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Equal(plainPattern!.Segments, extendedPattern!.Segments);
    }

    [Fact]
    public void TryEncode_AtSignAndBacktickAndNul_ExpandToPercentShiftSequence()
    {
        var extendedSuccess = _encoder.TryEncode("@`\0", out var extendedPattern, out _);
        var plainSuccess = _code39.TryEncode("%V%W%U", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Equal(plainPattern!.Segments, extendedPattern!.Segments);
    }

    [Fact]
    public void TryEncode_CharacterAboveAscii127_ReturnsError()
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
Expected: FAIL — `ExtendedCode39Encoder` does not exist yet.

- [ ] **Step 4: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/ExtendedCode39Encoder.cs`:

```csharp
using System.Text;

namespace IBEBarcode.Core.Encoders;

public sealed class ExtendedCode39Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code39Extended;

    private readonly Code39Encoder _code39 = new();

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var expanded = new StringBuilder();

        foreach (var ch in value)
        {
            if (ch > 127)
            {
                error = $"Character '{ch}' is outside the 0-127 ASCII range Extended Code 39 supports.";
                return false;
            }

            expanded.Append(ExpandChar(ch));
        }

        if (!_code39.TryEncode(expanded.ToString(), out var innerPattern, out var innerError))
        {
            error = innerError;
            return false;
        }

        pattern = BarcodePattern.Create(value, innerPattern!.Segments, innerPattern.HumanReadableText);
        error = null;
        return true;
    }

    private static string ExpandChar(char ch)
    {
        return ch switch
        {
            >= '0' and <= '9' => ch.ToString(),
            >= 'A' and <= 'Z' => ch.ToString(),
            '-' or '.' or ' ' or '$' or '/' or '+' or '%' => ch.ToString(),
            >= 'a' and <= 'z' => "+" + (char)(ch - 32),
            (char)0 => "%U",
            >= (char)1 and <= (char)26 => "$" + (char)(ch + 64),
            (char)27 => "%A",
            (char)28 => "%B",
            (char)29 => "%C",
            (char)30 => "%D",
            (char)31 => "%E",
            '!' => "/A",
            '"' => "/B",
            '#' => "/C",
            '&' => "/F",
            '\'' => "/G",
            '(' => "/H",
            ')' => "/I",
            '*' => "/J",
            ',' => "/L",
            ':' => "/Z",
            ';' => "%F",
            '<' => "%G",
            '=' => "%H",
            '>' => "%I",
            '?' => "%J",
            '@' => "%V",
            '[' => "%K",
            '\\' => "%L",
            ']' => "%M",
            '^' => "%N",
            '_' => "%O",
            '`' => "%W",
            '{' => "%P",
            '|' => "%Q",
            '}' => "%R",
            '~' => "%S",
            (char)127 => "%T",
            _ => throw new ArgumentOutOfRangeException(nameof(ch), $"Unhandled ASCII character '{ch}' ({(int)ch})."),
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `ExtendedCode39EncoderTests` green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/ExtendedCode39Encoder.cs tests/IBEBarcode.Core.Tests/Encoders/ExtendedCode39EncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Extended Code 39 encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Code 128 A/B/C and UCC/EAN-128 (GS1-128) are the next linear width-based
symbologies. Postnet (height-varying bars, not width-varying — doesn't fit
the current `BarSegment` model) and QR Code/Data Matrix/PDF417/Aztec
(2D, not linear at all) need a new, separate non-linear pattern
abstraction designed before they can be built — that design work is its
own upcoming plan. After all encoders: `IBEBarcode.Rendering`,
`IBEBarcode.Templates`, `IBEBarcode.Printing`, `IBEBarcode.Desktop`,
`IBEBarcode.Web`.
