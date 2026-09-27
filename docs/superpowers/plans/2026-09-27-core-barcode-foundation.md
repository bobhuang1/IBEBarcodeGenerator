# Core Barcode Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the solution/repo scaffolding and a working, tested `IBEBarcode.Core` library that can encode Code 39, Codabar, Interleaved 2 of 5, and MSI Plessey barcodes — the first slice of the shared Core library that both the Avalonia desktop app and Blazor WebAssembly web app will depend on.

**Architecture:** A single .NET 10 solution with a `src/IBEBarcode.Core` class library (pure logic, no graphics dependency) and a matching `tests/IBEBarcode.Core.Tests` xUnit project. Each barcode symbology is encoded into a symbology-agnostic `BarcodePattern` (an ordered, alternating sequence of bar/space `BarSegment`s in narrow/wide module units) via an `IBarcodeEncoder` implementation. This pattern is the shared contract that the future `IBEBarcode.Rendering` library (SkiaSharp) will consume to draw pixels — nothing in this plan touches graphics.

**Tech Stack:** .NET 10 (net10.0), xUnit for tests. No third-party barcode library — every encoder is hand-written. Bar/space tables for Code 39 and Codabar are the verified, standard tables (sourced from the widely-used ZXing reference implementation's published constants, re-derived and hand-verified against multiple characters in this plan); Interleaved 2 of 5 digit patterns and MSI Plessey bit encoding are similarly verified against multiple independent published sources.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

- Every project targets `net10.0`.
- Whole repo is MIT licensed.
- No third-party barcode-encoding library dependency — every symbology is custom-encoded (per spec decision).
- No database dependency anywhere.
- Single solution file (`IBEBarcodeGenerator.sln`) at the repo root holds every project, present and future.
- `Nullable` and `ImplicitUsings` are enabled on every project.

---

### Task 1: Solution and project scaffolding

**Files:**
- Create: `IBEBarcodeGenerator.sln`
- Create: `src/IBEBarcode.Core/IBEBarcode.Core.csproj`
- Create: `tests/IBEBarcode.Core.Tests/IBEBarcode.Core.Tests.csproj`
- Create: `tests/IBEBarcode.Core.Tests/SmokeTests.cs`
- Create: `LICENSE`
- Create: `README.md`
- Create: `.gitignore`

**Interfaces:**
- Consumes: nothing (first task).
- Produces: a buildable, testable .NET 10 solution that Task 2 adds source files into.

- [ ] **Step 1: Create the solution and the two projects**

Run from the repo root (`C:\github\ibegroup\IBEBarcodeGeneratror`):

```bash
dotnet new sln -n IBEBarcodeGenerator
dotnet new classlib -n IBEBarcode.Core -o src/IBEBarcode.Core -f net10.0
dotnet new xunit -n IBEBarcode.Core.Tests -o tests/IBEBarcode.Core.Tests -f net10.0
dotnet sln add src/IBEBarcode.Core/IBEBarcode.Core.csproj
dotnet sln add tests/IBEBarcode.Core.Tests/IBEBarcode.Core.Tests.csproj
dotnet add tests/IBEBarcode.Core.Tests/IBEBarcode.Core.Tests.csproj reference src/IBEBarcode.Core/IBEBarcode.Core.csproj
```

Delete the template-generated stub files so they don't collide with later
tasks: `src/IBEBarcode.Core/Class1.cs` and
`tests/IBEBarcode.Core.Tests/UnitTest1.cs`.

- [ ] **Step 2: Set project properties**

Replace the contents of `src/IBEBarcode.Core/IBEBarcode.Core.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>IBEBarcode.Core</RootNamespace>
  </PropertyGroup>

</Project>
```

Replace the contents of `tests/IBEBarcode.Core.Tests/IBEBarcode.Core.Tests.csproj`
with (keep whatever package versions `dotnet new xunit` restored — only add
`Nullable`/`ImplicitUsings`/`RootNamespace` if they're missing):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>IBEBarcode.Core.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\IBEBarcode.Core\IBEBarcode.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write a smoke test**

Create `tests/IBEBarcode.Core.Tests/SmokeTests.cs`:

```csharp
namespace IBEBarcode.Core.Tests;

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
Expected: build succeeds, 1 test passes (`TestProjectIsWiredUpCorrectly`).

- [ ] **Step 5: Add LICENSE, README.md, .gitignore**

Create `LICENSE`:

```
MIT License

Copyright (c) 2026 IBE Group, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

Create `README.md`:

```markdown
# IBE Barcode Generator

Free, open-source, cross-platform barcode label generator — a from-scratch
recreation of the discontinued "IBE Barcode Studio" (Windows/VB), released
under the MIT license.

## Status

Early development. See `docs/superpowers/specs/` for the architecture design
and `docs/superpowers/plans/` for implementation plans.

## Solution layout

- `src/IBEBarcode.Core` — barcode data models and custom-written encoders
  (no graphics dependency).
- `tests/IBEBarcode.Core.Tests` — xUnit tests for `IBEBarcode.Core`.

More projects (`IBEBarcode.Rendering`, `IBEBarcode.Printing`,
`IBEBarcode.Templates`, `IBEBarcode.Desktop`, `IBEBarcode.Web`) land in
later plans; see the design spec for the full solution layout.

## Build and test

```bash
dotnet build
dotnet test
```

## License

MIT — see `LICENSE`.
```

Create `.gitignore`:

```
## .NET
bin/
obj/
*.user
*.suo

## Visual Studio / Rider
.vs/
.idea/
*.userosscache
*.sln.docstates

## Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/

## Test results
[Tt]est[Rr]esult*/
*.trx

## OS files
.DS_Store
Thumbs.db
```

- [ ] **Step 6: Commit**

```bash
git add IBEBarcodeGenerator.sln src/ tests/ LICENSE README.md .gitignore
git commit -m "$(cat <<'EOF'
Scaffold solution and IBEBarcode.Core project

Sets up the .NET 10 solution, the Core class library, its xUnit test
project, and repo-level LICENSE/README/.gitignore.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Core domain types (`BarSegment`, `BarcodePattern`, `IBarcodeEncoder`, `BarcodeSymbology`)

**Files:**
- Create: `src/IBEBarcode.Core/BarSegment.cs`
- Create: `src/IBEBarcode.Core/BarcodePattern.cs`
- Create: `src/IBEBarcode.Core/IBarcodeEncoder.cs`
- Create: `src/IBEBarcode.Core/BarcodeSymbology.cs`
- Test: `tests/IBEBarcode.Core.Tests/BarcodePatternTests.cs`

**Interfaces:**
- Consumes: nothing beyond Task 1's project scaffolding.
- Produces (used by every later task in this plan and by `IBEBarcode.Rendering` later):
  - `readonly struct BarSegment(bool isBar, int widthUnits)` — throws `ArgumentOutOfRangeException` if `widthUnits <= 0`.
  - `sealed class BarcodePattern` with `string Value`, `IReadOnlyList<BarSegment> Segments`, `string? HumanReadableText`, `int TotalWidthUnits`, and factory `BarcodePattern.Create(string value, IReadOnlyList<BarSegment> segments, string? humanReadableText = null)` — throws `ArgumentException` if `segments` is empty or two consecutive segments have the same `IsBar`.
  - `interface IBarcodeEncoder` with `BarcodeSymbology Symbology { get; }` and `bool TryEncode(string value, out BarcodePattern? pattern, out string? error)`.
  - `enum BarcodeSymbology { Code39, Codabar, Interleaved2Of5, MsiPlessey }`.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/BarcodePatternTests.cs`:

```csharp
namespace IBEBarcode.Core.Tests;

public class BarcodePatternTests
{
    [Fact]
    public void BarSegment_ThrowsOnNonPositiveWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarSegment(true, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarSegment(true, -1));
    }

    [Fact]
    public void Create_WithAlternatingSegments_Succeeds()
    {
        var segments = new[]
        {
            new BarSegment(true, 1),
            new BarSegment(false, 2),
            new BarSegment(true, 1),
        };

        var pattern = BarcodePattern.Create("123", segments);

        Assert.Equal("123", pattern.Value);
        Assert.Equal(segments, pattern.Segments);
        Assert.Equal(4, pattern.TotalWidthUnits);
        Assert.Equal("123", pattern.HumanReadableText);
    }

    [Fact]
    public void Create_WithCustomHumanReadableText_UsesIt()
    {
        var segments = new[] { new BarSegment(true, 1) };

        var pattern = BarcodePattern.Create("123", segments, "*123*");

        Assert.Equal("*123*", pattern.HumanReadableText);
    }

    [Fact]
    public void Create_WithConsecutiveBars_Throws()
    {
        var segments = new[]
        {
            new BarSegment(true, 1),
            new BarSegment(true, 1),
        };

        Assert.Throws<ArgumentException>(() => BarcodePattern.Create("x", segments));
    }

    [Fact]
    public void Create_WithNoSegments_Throws()
    {
        Assert.Throws<ArgumentException>(() => BarcodePattern.Create("x", Array.Empty<BarSegment>()));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — compile errors, `BarSegment`/`BarcodePattern` do not exist yet.

- [ ] **Step 3: Implement the domain types**

Create `src/IBEBarcode.Core/BarSegment.cs`:

```csharp
namespace IBEBarcode.Core;

public readonly struct BarSegment : IEquatable<BarSegment>
{
    public bool IsBar { get; }
    public int WidthUnits { get; }

    public BarSegment(bool isBar, int widthUnits)
    {
        if (widthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthUnits), "Segment width must be positive.");

        IsBar = isBar;
        WidthUnits = widthUnits;
    }

    public bool Equals(BarSegment other) => IsBar == other.IsBar && WidthUnits == other.WidthUnits;

    public override bool Equals(object? obj) => obj is BarSegment other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(IsBar, WidthUnits);
}
```

Create `src/IBEBarcode.Core/BarcodePattern.cs`:

```csharp
namespace IBEBarcode.Core;

public sealed class BarcodePattern
{
    public string Value { get; }
    public IReadOnlyList<BarSegment> Segments { get; }
    public string? HumanReadableText { get; }

    private BarcodePattern(string value, IReadOnlyList<BarSegment> segments, string? humanReadableText)
    {
        Value = value;
        Segments = segments;
        HumanReadableText = humanReadableText;
    }

    public static BarcodePattern Create(string value, IReadOnlyList<BarSegment> segments, string? humanReadableText = null)
    {
        if (segments.Count == 0)
            throw new ArgumentException("Pattern must contain at least one segment.", nameof(segments));

        for (var i = 1; i < segments.Count; i++)
        {
            if (segments[i].IsBar == segments[i - 1].IsBar)
            {
                throw new ArgumentException(
                    $"Segments must alternate bar/space; segments {i - 1} and {i} are both {(segments[i].IsBar ? "bars" : "spaces")}.",
                    nameof(segments));
            }
        }

        return new BarcodePattern(value, segments, humanReadableText ?? value);
    }

    public int TotalWidthUnits => Segments.Sum(s => s.WidthUnits);
}
```

Create `src/IBEBarcode.Core/IBarcodeEncoder.cs`:

```csharp
namespace IBEBarcode.Core;

public interface IBarcodeEncoder
{
    BarcodeSymbology Symbology { get; }

    bool TryEncode(string value, out BarcodePattern? pattern, out string? error);
}
```

Create `src/IBEBarcode.Core/BarcodeSymbology.cs`:

```csharp
namespace IBEBarcode.Core;

public enum BarcodeSymbology
{
    Code39,
    Codabar,
    Interleaved2Of5,
    MsiPlessey,
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `BarcodePatternTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/BarSegment.cs src/IBEBarcode.Core/BarcodePattern.cs src/IBEBarcode.Core/IBarcodeEncoder.cs src/IBEBarcode.Core/BarcodeSymbology.cs tests/IBEBarcode.Core.Tests/BarcodePatternTests.cs
git commit -m "$(cat <<'EOF'
Add core barcode domain types

BarSegment/BarcodePattern/IBarcodeEncoder/BarcodeSymbology form the
symbology-agnostic contract every encoder targets and the future
Rendering library will consume.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Code 39 encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Code39Encoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Code39EncoderTests.cs`

**Interfaces:**
- Consumes: `BarSegment`, `BarcodePattern`, `IBarcodeEncoder`, `BarcodeSymbology` from Task 2.
- Produces: `sealed class Code39Encoder : IBarcodeEncoder` in namespace `IBEBarcode.Core.Encoders`.

The 9-bit-per-character encoding table below (43 data characters, alphabet
`"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%"`, plus the `*` start/stop
pattern `0x094`) is the standard Code 39 table: each integer's bits 8..0
map one-to-one onto the character's 9 elements (bar, space, bar, space,
bar, space, bar, space, bar — 5 bars, 4 spaces), bit 8 is the first
element, bit 0 is the last, and a set bit means that element is wide.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/Code39EncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Code39EncoderTests
{
    private readonly Code39Encoder _encoder = new();

    [Fact]
    public void TryEncode_ValidValue_ReturnsPatternWrappedInStartStop()
    {
        var success = _encoder.TryEncode("CODE39", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(pattern);
        Assert.Equal("*CODE39*", pattern!.HumanReadableText);
        Assert.Equal(BarcodeSymbology.Code39, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("CODE39", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_DigitZero_ProducesKnownNarrowWidePattern()
    {
        var success = _encoder.TryEncode("0", out var pattern, out _);

        Assert.True(success);
        // start '*' (9 elements) + gap + '0' (9 elements) + gap + stop '*' (9 elements) = 29 segments
        Assert.Equal(29, pattern!.Segments.Count);

        var zeroSegments = pattern.Segments.Skip(10).Take(9).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 1, 1, 2, 2, 1, 2, 1, 1 }, zeroSegments);
    }

    [Fact]
    public void TryEncode_LowercaseIsNormalizedToUppercase()
    {
        var lower = _encoder.TryEncode("code39", out var lowerPattern, out _);
        var upper = _encoder.TryEncode("CODE39", out var upperPattern, out _);

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

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `Code39Encoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/Code39Encoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Code39Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code39;

    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    private static readonly int[] CharacterPatterns =
    {
        0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064,
        0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C,
        0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016,
        0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x0A8,
        0x0A2, 0x08A, 0x02A,
    };

    private const int StartStopPattern = 0x094;
    private const int NarrowWidth = 1;
    private const int WideWidth = 2;
    private const int InterCharacterGapWidth = 1;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var upper = value.ToUpperInvariant();
        var segments = new List<BarSegment>();

        AppendCharacter(segments, StartStopPattern);
        segments.Add(new BarSegment(false, InterCharacterGapWidth));

        foreach (var ch in upper)
        {
            var index = Alphabet.IndexOf(ch);
            if (index < 0)
            {
                error = $"Character '{ch}' is not valid in Code 39.";
                return false;
            }

            AppendCharacter(segments, CharacterPatterns[index]);
            segments.Add(new BarSegment(false, InterCharacterGapWidth));
        }

        AppendCharacter(segments, StartStopPattern);

        pattern = BarcodePattern.Create(value, segments, $"*{upper}*");
        error = null;
        return true;
    }

    private static void AppendCharacter(List<BarSegment> segments, int bitPattern)
    {
        for (var bitIndex = 8; bitIndex >= 0; bitIndex--)
        {
            var isWide = (bitPattern & (1 << bitIndex)) != 0;
            var isBar = bitIndex % 2 == 0;
            segments.Add(new BarSegment(isBar, isWide ? WideWidth : NarrowWidth));
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `Code39EncoderTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/Code39Encoder.cs tests/IBEBarcode.Core.Tests/Encoders/Code39EncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Code 39 encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Codabar encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/CodabarEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/CodabarEncoderTests.cs`

**Interfaces:**
- Consumes: `BarSegment`, `BarcodePattern`, `IBarcodeEncoder`, `BarcodeSymbology` from Task 2.
- Produces: `sealed class CodabarEncoder : IBarcodeEncoder` in namespace `IBEBarcode.Core.Encoders`. Always wraps the caller's data with Codabar's `A` start/stop character (the four Codabar start/stop letters A/B/C/D are interchangeable conventions; `A` is used as the fixed default here).

The 7-bit-per-character table below (alphabet `"0123456789-$:/.+ABCD"`) is
the standard Codabar table: bits 6..0 map onto the character's 7 elements
(bar, space, bar, space, bar, space, bar — 4 bars, 3 spaces), bit 6 is the
first element, bit 0 is the last, and a set bit means that element is wide.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/CodabarEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class CodabarEncoderTests
{
    private readonly CodabarEncoder _encoder = new();

    [Fact]
    public void TryEncode_ValidValue_WrapsWithStartStopA()
    {
        var success = _encoder.TryEncode("12345", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("A12345A", pattern!.HumanReadableText);
        Assert.Equal(BarcodeSymbology.Codabar, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("0-$:/.+9", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_DigitZero_ProducesKnownNarrowWidePattern()
    {
        var success = _encoder.TryEncode("0", out var pattern, out _);

        Assert.True(success);
        // start 'A' (7 elements) + gap + '0' (7 elements) + gap + stop 'A' (7 elements) = 23 segments
        Assert.Equal(23, pattern!.Segments.Count);

        var zeroSegments = pattern.Segments.Skip(8).Take(7).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 2, 2 }, zeroSegments);
    }

    [Fact]
    public void TryEncode_InvalidCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("12#34", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("#", error);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `CodabarEncoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/CodabarEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class CodabarEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Codabar;

    private const string Alphabet = "0123456789-$:/.+ABCD";

    private static readonly int[] CharacterPatterns =
    {
        0x003, 0x006, 0x009, 0x060, 0x012, 0x042, 0x021, 0x024, 0x030, 0x048,
        0x00C, 0x018, 0x045, 0x051, 0x054, 0x015, 0x01A, 0x029, 0x00B, 0x00E,
    };

    private const int NarrowWidth = 1;
    private const int WideWidth = 2;
    private const int InterCharacterGapWidth = 1;
    private const char StartStopChar = 'A';

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var upper = value.ToUpperInvariant();
        var segments = new List<BarSegment>();

        AppendCharacter(segments, LookupPattern(StartStopChar)!.Value);
        segments.Add(new BarSegment(false, InterCharacterGapWidth));

        foreach (var ch in upper)
        {
            var bitPattern = LookupPattern(ch);
            if (bitPattern is null)
            {
                error = $"Character '{ch}' is not valid in Codabar.";
                return false;
            }

            AppendCharacter(segments, bitPattern.Value);
            segments.Add(new BarSegment(false, InterCharacterGapWidth));
        }

        AppendCharacter(segments, LookupPattern(StartStopChar)!.Value);

        pattern = BarcodePattern.Create(value, segments, $"{StartStopChar}{upper}{StartStopChar}");
        error = null;
        return true;
    }

    private static int? LookupPattern(char ch)
    {
        var index = Alphabet.IndexOf(ch);
        return index < 0 ? null : CharacterPatterns[index];
    }

    private static void AppendCharacter(List<BarSegment> segments, int bitPattern)
    {
        for (var bitIndex = 6; bitIndex >= 0; bitIndex--)
        {
            var isWide = (bitPattern & (1 << bitIndex)) != 0;
            var isBar = bitIndex % 2 == 0;
            segments.Add(new BarSegment(isBar, isWide ? WideWidth : NarrowWidth));
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `CodabarEncoderTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/CodabarEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/CodabarEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Codabar encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Interleaved 2 of 5 encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Interleaved2Of5Encoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Interleaved2Of5EncoderTests.cs`

**Interfaces:**
- Consumes: `BarSegment`, `BarcodePattern`, `IBarcodeEncoder`, `BarcodeSymbology` from Task 2.
- Produces: `sealed class Interleaved2Of5Encoder : IBarcodeEncoder` in namespace `IBEBarcode.Core.Encoders`. Digits-only; rejects odd-length input rather than silently padding (caller decides padding policy).

Each digit 0-9 has a standard 5-element bar-only pattern (2 of 5 wide);
digits are encoded in pairs, the first digit's pattern drawn as the five
bars and the second digit's pattern drawn interleaved as the five spaces.
Start pattern is 4 narrow elements (bar, space, bar, space); end pattern
is wide bar, narrow space, narrow bar.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/Interleaved2Of5EncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Interleaved2Of5EncoderTests
{
    private readonly Interleaved2Of5Encoder _encoder = new();

    [Fact]
    public void TryEncode_ValidEvenLengthValue_Succeeds()
    {
        var success = _encoder.TryEncode("1234", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("1234", pattern!.HumanReadableText);
        Assert.Equal(BarcodeSymbology.Interleaved2Of5, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_OddLengthValue_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("1234", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_DigitPairZeroOne_ProducesKnownPattern()
    {
        var success = _encoder.TryEncode("01", out var pattern, out _);

        Assert.True(success);
        // start (4) + 5 interleaved bar/space pairs (10) + end (3) = 17 segments
        Assert.Equal(17, pattern!.Segments.Count);

        var widths = pattern.Segments.Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 1, 1, 1 }, widths[..4]);
        Assert.Equal(new[] { 1, 2, 1, 1, 2, 1, 2, 1, 1, 2 }, widths[4..14]);
        Assert.Equal(new[] { 2, 1, 1 }, widths[14..]);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("12A4", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("A", error);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `Interleaved2Of5Encoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/Interleaved2Of5Encoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class Interleaved2Of5Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Interleaved2Of5;

    private static readonly bool[][] DigitPatterns =
    {
        new[] { false, false, true, true, false },
        new[] { true, false, false, false, true },
        new[] { false, true, false, false, true },
        new[] { true, true, false, false, false },
        new[] { false, false, true, false, true },
        new[] { true, false, true, false, false },
        new[] { false, true, true, false, false },
        new[] { false, false, false, true, true },
        new[] { true, false, false, true, false },
        new[] { false, true, false, true, false },
    };

    private const int NarrowWidth = 1;
    private const int WideWidth = 2;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        if (value.Length % 2 != 0)
        {
            error = "Interleaved 2 of 5 requires an even number of digits; pad with a leading zero.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; Interleaved 2 of 5 encodes digits only.";
                return false;
            }
        }

        var segments = new List<BarSegment>
        {
            new(true, NarrowWidth),
            new(false, NarrowWidth),
            new(true, NarrowWidth),
            new(false, NarrowWidth),
        };

        for (var i = 0; i < value.Length; i += 2)
        {
            var barDigit = DigitPatterns[value[i] - '0'];
            var spaceDigit = DigitPatterns[value[i + 1] - '0'];

            for (var element = 0; element < 5; element++)
            {
                segments.Add(new BarSegment(true, barDigit[element] ? WideWidth : NarrowWidth));
                segments.Add(new BarSegment(false, spaceDigit[element] ? WideWidth : NarrowWidth));
            }
        }

        segments.Add(new BarSegment(true, WideWidth));
        segments.Add(new BarSegment(false, NarrowWidth));
        segments.Add(new BarSegment(true, NarrowWidth));

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `Interleaved2Of5EncoderTests` green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/Interleaved2Of5Encoder.cs tests/IBEBarcode.Core.Tests/Encoders/Interleaved2Of5EncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Interleaved 2 of 5 encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: MSI Plessey encoder

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/MsiPlesseyEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/MsiPlesseyEncoderTests.cs`

**Interfaces:**
- Consumes: `BarSegment`, `BarcodePattern`, `IBarcodeEncoder`, `BarcodeSymbology` from Task 2.
- Produces: `sealed class MsiPlesseyEncoder : IBarcodeEncoder` in namespace `IBEBarcode.Core.Encoders`, plus `public static char MsiPlesseyEncoder.ComputeCheckDigit(string digits)` (Luhn/Mod-10, standalone so callers choose whether to append it before encoding).

Each digit 0-9 is its 4-bit BCD value; each bit is drawn as a bar+space
pair — bit `1` is a wide bar + narrow space, bit `0` is a narrow bar +
wide space. Start is a wide bar + narrow space; stop is narrow bar + wide
space + narrow bar. Digits-only.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/MsiPlesseyEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class MsiPlesseyEncoderTests
{
    private readonly MsiPlesseyEncoder _encoder = new();

    [Fact]
    public void TryEncode_ValidDigits_Succeeds()
    {
        var success = _encoder.TryEncode("1234", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("1234", pattern!.HumanReadableText);
        Assert.Equal(BarcodeSymbology.MsiPlessey, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("90210", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_DigitZero_ProducesAllNarrowBarWideSpaceBits()
    {
        var success = _encoder.TryEncode("0", out var pattern, out _);

        Assert.True(success);
        // start (2) + digit 0 = four 0-bits, each bar+space (8) + stop (3) = 13 segments
        Assert.Equal(13, pattern!.Segments.Count);

        var digitSegments = pattern.Segments.Skip(2).Take(8).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 2, 1, 2, 1, 2, 1, 2 }, digitSegments);
    }

    [Fact]
    public void TryEncode_DigitNine_ProducesBcd1001BitPattern()
    {
        var success = _encoder.TryEncode("9", out var pattern, out _);

        Assert.True(success);

        var digitSegments = pattern!.Segments.Skip(2).Take(8).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 2, 1, 1, 2, 1, 2, 2, 1 }, digitSegments);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("12x4", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("x", error);
    }

    [Theory]
    [InlineData("1234567", '4')]
    [InlineData("0", '0')]
    public void ComputeCheckDigit_KnownValues_ReturnsExpectedDigit(string digits, char expected)
    {
        var checkDigit = MsiPlesseyEncoder.ComputeCheckDigit(digits);

        Assert.Equal(expected, checkDigit);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `MsiPlesseyEncoder` does not exist yet.

- [ ] **Step 3: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/MsiPlesseyEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders;

public sealed class MsiPlesseyEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.MsiPlessey;

    private const int NarrowWidth = 1;
    private const int WideWidth = 2;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; MSI Plessey encodes digits only.";
                return false;
            }
        }

        var segments = new List<BarSegment>
        {
            new(true, WideWidth),
            new(false, NarrowWidth),
        };

        foreach (var ch in value)
        {
            var digit = ch - '0';
            for (var bitIndex = 3; bitIndex >= 0; bitIndex--)
            {
                var bit = (digit >> bitIndex) & 1;
                segments.Add(new BarSegment(true, bit == 1 ? WideWidth : NarrowWidth));
                segments.Add(new BarSegment(false, bit == 1 ? NarrowWidth : WideWidth));
            }
        }

        segments.Add(new BarSegment(true, NarrowWidth));
        segments.Add(new BarSegment(false, WideWidth));
        segments.Add(new BarSegment(true, NarrowWidth));

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }

    public static char ComputeCheckDigit(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            throw new ArgumentException("Value must not be empty.", nameof(digits));

        var sum = 0;
        var doubleNext = true;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var ch = digits[i];
            if (ch is < '0' or > '9')
                throw new ArgumentException($"Character '{ch}' is not a digit.", nameof(digits));

            var digit = ch - '0';
            if (doubleNext)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            doubleNext = !doubleNext;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return (char)('0' + checkDigit);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `MsiPlesseyEncoderTests` green, full solution test suite green.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/MsiPlesseyEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/MsiPlesseyEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add MSI Plessey encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Remaining Core encoders (Extended Code 39, Code 93, Code 128 A/B/C,
EAN-13/EAN-8, UPC-A/UPC-E/UPC 2/UPC 5, UCC/EAN-128, Postnet, ISBN, then QR
Code/Data Matrix/PDF417/Aztec), `IBEBarcode.Rendering` (SkiaSharp),
`IBEBarcode.Templates`, `IBEBarcode.Printing` (PdfSharp),
`IBEBarcode.Desktop` (Avalonia), and `IBEBarcode.Web` (Blazor WebAssembly)
are each their own follow-up plan, per the design spec's solution layout.
