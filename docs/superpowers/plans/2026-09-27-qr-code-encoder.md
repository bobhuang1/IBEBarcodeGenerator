# QR Code Encoder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a real, from-scratch QR Code encoder to `IBEBarcode.Core` — the headline new format called out in the design spec ("especially QR codes").

**Architecture:** QR is a 2D grid, not a linear bar/space sequence, so it needs its own pattern type: `BarcodeMatrix` (a `bool[,]` of dark/light modules) and `IMatrixBarcodeEncoder` (parallel to `IBarcodeEncoder`/`IHeightVaryingBarcodeEncoder`). `QrEncoder` builds a QR symbol in the standard stages: byte-mode data bitstream → Reed-Solomon error correction codewords (via a from-scratch GF(256) implementation) → module placement (finder/timing/alignment/dark-module/format-info) → zigzag data placement with a fixed mask.

**Scope decision (deliberate, to keep this achievable and correct):**
- **Byte mode only.** Encodes the UTF-8 bytes of the input directly. Numeric/Alphanumeric mode (denser encoding for digit/uppercase-only content) and Kanji mode are not implemented — byte mode is always valid for any input, just not maximally space-efficient for numeric-heavy content.
- **Versions 1-5, single-block only.** QR versions above 5 (or lower versions at higher error-correction levels) split data across multiple interleaved Reed-Solomon blocks — a real feature, but a second, separate layer of complexity. This plan supports every (version, level) combination where the QR spec itself uses exactly one block, which is: version 1-2 at any level (L/M/Q/H), version 3 at L or M, version 4 at L, version 5 at L. That covers up to 106 usable bytes at level L — enough for a URL or a short message. Larger payloads are out of scope for this plan.
- **Fixed mask pattern 0.** QR requires picking one of 8 mask patterns and correctly declaring which one was used (in the format info); picking the *best* one (lowest penalty score across 4 scan-reliability heuristics) is an optimization, not a correctness requirement. This plan always uses mask 0 and declares it correctly — the result is always a fully valid, standards-compliant, scannable QR code, just not one that's been optimized for scan robustness.
- **No version info block.** That's only required for version ≥ 7, which this plan doesn't reach.

Data too large for the requested error-correction level's supported version range returns a clear `TryEncode` error rather than guessing or truncating.

**Tech Stack:** .NET 10, xUnit, no third-party library — this is exactly the kind of format the spec's "all custom encoders" decision was about. Every structural table (finder/alignment pattern shapes, timing pattern rule, dark module position, the 15-bit format-info strings, the exact bit-to-module coordinate mapping for format info, the data-placement zigzag algorithm, the mask-0 condition) was fetched from ZXing's `MatrixUtil.java`/`MaskUtil.java` QR encoder source and, where possible, cross-checked against a second independent source (Thonky's QR tutorial). The Reed-Solomon GF(256) implementation is verified in this plan's own tests against a real, independently published worked example (`HELLO WORLD` at version 1-M: 16 known data codewords produce 10 known error-correction codewords).

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.) Every project targets `net10.0`; MIT license; no third-party barcode-encoding library; no database; single solution file; `Nullable`/`ImplicitUsings` enabled everywhere.

## Verified reference values used by this plan's tests

- Single-block (version, data-codewords, ecc-codewords) table, versions 1-5: `(1,L,19,7) (1,M,16,10) (1,Q,13,13) (1,H,9,17) (2,L,34,10) (2,M,28,16) (2,Q,22,22) (2,H,16,28) (3,L,55,15) (3,M,44,26) (4,L,80,20) (5,L,108,26)`.
- Reed-Solomon worked example (version 1-M, "HELLO WORLD"): data codewords `32,91,11,120,209,114,220,77,67,64,236,17,236,17,236,17` (16 bytes) produce error-correction codewords `196,35,39,119,235,215,231,226,93,23` (10 bytes).
- Format info strings for mask pattern 0 (the only mask this plan uses): L=`111011111000100`, M=`101010000010010`, Q=`011010101011111`, H=`001011010001001`.
- Format info module coordinates (copy 1, index 0-14, `(x=column, y=row)`): `(8,0) (8,1) (8,2) (8,3) (8,4) (8,5) (8,7) (8,8) (7,8) (5,8) (4,8) (3,8) (2,8) (1,8) (0,8)`. Copy 2: for index `i<8`, `(width-i-1, 8)`; for `i>=8`, `(8, height-7+(i-8))`.
- Alignment pattern center (version 2-5 only; version 1 has none): version 2→`(18,18)`, 3→`(22,22)`, 4→`(26,26)`, 5→`(30,30)`.
- Dark module: always at `(x=8, y=height-8)`, always dark.
- Timing pattern: for `i` from 8 to `size-9`, modules `(i,6)` and `(6,i)` are dark when `(i+1) is odd`, i.e. dark starting at `i=8`.
- Mask 0 condition: flip the bit at `(x,y)` when `(x+y) mod 2 == 0`.

---

### Task 1: `BarcodeMatrix` and `IMatrixBarcodeEncoder`

**Files:**
- Create: `src/IBEBarcode.Core/BarcodeMatrix.cs`
- Create: `src/IBEBarcode.Core/IMatrixBarcodeEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/BarcodeMatrixTests.cs`

**Interfaces:**
- Produces (consumed by Task 3 and, later, `IBEBarcode.Rendering`):
  - `sealed class BarcodeMatrix` with `string Value`, `int Width`, `int Height`, indexer `bool this[int x, int y]`, factory `BarcodeMatrix.Create(string value, bool[,] modules)`.
  - `interface IMatrixBarcodeEncoder` with `BarcodeSymbology Symbology { get; }` and `bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/BarcodeMatrixTests.cs`:

```csharp
namespace IBEBarcode.Core.Tests;

public class BarcodeMatrixTests
{
    [Fact]
    public void Create_WithModules_Succeeds()
    {
        var modules = new bool[3, 2];
        modules[0, 0] = true;
        modules[2, 1] = true;

        var matrix = BarcodeMatrix.Create("test", modules);

        Assert.Equal("test", matrix.Value);
        Assert.Equal(3, matrix.Width);
        Assert.Equal(2, matrix.Height);
        Assert.True(matrix[0, 0]);
        Assert.False(matrix[1, 0]);
        Assert.True(matrix[2, 1]);
    }

    [Fact]
    public void Create_WithEmptyDimension_Throws()
    {
        Assert.Throws<ArgumentException>(() => BarcodeMatrix.Create("x", new bool[0, 0]));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `BarcodeMatrix` does not exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Core/BarcodeMatrix.cs`:

```csharp
namespace IBEBarcode.Core;

public sealed class BarcodeMatrix
{
    public string Value { get; }
    public int Width { get; }
    public int Height { get; }

    private readonly bool[,] _modules;

    private BarcodeMatrix(string value, bool[,] modules)
    {
        Value = value;
        _modules = modules;
        Width = modules.GetLength(0);
        Height = modules.GetLength(1);
    }

    public static BarcodeMatrix Create(string value, bool[,] modules)
    {
        if (modules.GetLength(0) == 0 || modules.GetLength(1) == 0)
            throw new ArgumentException("Matrix must have at least one row and column.", nameof(modules));

        return new BarcodeMatrix(value, modules);
    }

    public bool this[int x, int y] => _modules[x, y];
}
```

Create `src/IBEBarcode.Core/IMatrixBarcodeEncoder.cs`:

```csharp
namespace IBEBarcode.Core;

public interface IMatrixBarcodeEncoder
{
    BarcodeSymbology Symbology { get; }

    bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeMatrix.cs src/IBEBarcode.Core/IMatrixBarcodeEncoder.cs tests/IBEBarcode.Core.Tests/BarcodeMatrixTests.cs
git commit -m "$(cat <<'EOF'
Add BarcodeMatrix and IMatrixBarcodeEncoder for 2D barcodes

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: QR Galois Field and Reed-Solomon error correction

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Qr/QrGaloisField.cs`
- Create: `src/IBEBarcode.Core/Encoders/Qr/QrReedSolomon.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Qr/QrReedSolomonTests.cs`

**Interfaces:**
- Produces (consumed by Task 3):
  - `internal static class QrGaloisField` — `int Exp(int power)`, `int Multiply(int a, int b)`.
  - `internal static class QrReedSolomon` — `byte[] ComputeEccCodewords(byte[] data, int eccCount)`.

- [ ] **Step 1: Write the failing test**

Create `tests/IBEBarcode.Core.Tests/Encoders/Qr/QrReedSolomonTests.cs`:

```csharp
using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Tests.Encoders.Qr;

public class QrReedSolomonTests
{
    [Fact]
    public void ComputeEccCodewords_PublishedHelloWorldExample_MatchesKnownOutput()
    {
        var data = new byte[] { 32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17 };
        var expectedEcc = new byte[] { 196, 35, 39, 119, 235, 215, 231, 226, 93, 23 };

        var ecc = QrReedSolomon.ComputeEccCodewords(data, 10);

        Assert.Equal(expectedEcc, ecc);
    }
}
```

Note: this test references `IBEBarcode.Core.Encoders.Qr` — an `internal` namespace. Add `InternalsVisibleTo` for `IBEBarcode.Core.Tests` if it isn't already present in `IBEBarcode.Core.csproj` (it was added in the EAN/UPC plan; verify it's still there).

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `QrReedSolomon` does not exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Core/Encoders/Qr/QrGaloisField.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrGaloisField
{
    private const int PrimitivePolynomial = 0x11D;

    private static readonly int[] ExpTable = new int[512];
    private static readonly int[] LogTable = new int[256];

    static QrGaloisField()
    {
        var x = 1;

        for (var i = 0; i < 255; i++)
        {
            ExpTable[i] = x;
            LogTable[x] = i;
            x <<= 1;

            if ((x & 0x100) != 0)
            {
                x ^= PrimitivePolynomial;
            }
        }

        for (var i = 255; i < 512; i++)
        {
            ExpTable[i] = ExpTable[i - 255];
        }
    }

    public static int Exp(int power) => ExpTable[power];

    public static int Multiply(int a, int b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        return ExpTable[LogTable[a] + LogTable[b]];
    }
}
```

Create `src/IBEBarcode.Core/Encoders/Qr/QrReedSolomon.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrReedSolomon
{
    public static byte[] ComputeEccCodewords(byte[] data, int eccCount)
    {
        var generator = BuildGeneratorPolynomial(eccCount);
        var remainder = new int[data.Length + eccCount];

        for (var i = 0; i < data.Length; i++)
        {
            remainder[i] = data[i];
        }

        for (var i = 0; i < data.Length; i++)
        {
            var coefficient = remainder[i];

            if (coefficient == 0)
            {
                continue;
            }

            for (var j = 0; j < generator.Length; j++)
            {
                remainder[i + j] ^= QrGaloisField.Multiply(generator[j], coefficient);
            }
        }

        var ecc = new byte[eccCount];

        for (var i = 0; i < eccCount; i++)
        {
            ecc[i] = (byte)remainder[data.Length + i];
        }

        return ecc;
    }

    private static int[] BuildGeneratorPolynomial(int degree)
    {
        var generator = new[] { 1 };

        for (var i = 0; i < degree; i++)
        {
            generator = MultiplyPolynomials(generator, new[] { 1, QrGaloisField.Exp(i) });
        }

        return generator;
    }

    private static int[] MultiplyPolynomials(int[] a, int[] b)
    {
        var result = new int[a.Length + b.Length - 1];

        for (var i = 0; i < a.Length; i++)
        {
            for (var j = 0; j < b.Length; j++)
            {
                result[i + j] ^= QrGaloisField.Multiply(a[i], b[j]);
            }
        }

        return result;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — the Reed-Solomon output matches the published worked example exactly.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/Qr/QrGaloisField.cs src/IBEBarcode.Core/Encoders/Qr/QrReedSolomon.cs tests/IBEBarcode.Core.Tests/Encoders/Qr/QrReedSolomonTests.cs
git commit -m "$(cat <<'EOF'
Add QR Galois Field and Reed-Solomon error correction

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: `QrEncoder`

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Qr/QrBitWriter.cs`
- Create: `src/IBEBarcode.Core/Encoders/QrEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/QrEncoderTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `QrCode`)

**Interfaces:**
- Consumes: `BarcodeMatrix`, `IMatrixBarcodeEncoder` (Task 1), `QrGaloisField`/`QrReedSolomon` (Task 2).
- Produces: `BarcodeSymbology.QrCode` and `sealed class QrEncoder(char errorCorrectionLevel = 'M') : IMatrixBarcodeEncoder`.

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
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/QrEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class QrEncoderTests
{
    [Fact]
    public void TryEncode_ShortValue_ProducesVersion1Size()
    {
        var encoder = new QrEncoder('M');

        var success = encoder.TryEncode("HI", out var matrix, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(21, matrix!.Width);
        Assert.Equal(21, matrix.Height);
        Assert.Equal(BarcodeSymbology.QrCode, encoder.Symbology);
    }

    [Fact]
    public void TryEncode_LargerValue_SelectsLargerVersion()
    {
        var encoder = new QrEncoder('L');

        // 60 bytes needs more than version 1's 19-byte capacity at level L.
        var success = encoder.TryEncode(new string('A', 60), out var matrix, out _);

        Assert.True(success);
        Assert.True(matrix!.Width > 21);
    }

    [Fact]
    public void TryEncode_TopLeftFinderPattern_MatchesKnownShape()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        // Outer ring dark, inner ring light, center 3x3 dark, per the standard finder pattern.
        Assert.True(matrix![0, 0]);
        Assert.True(matrix[6, 0]);
        Assert.True(matrix[0, 6]);
        Assert.True(matrix[6, 6]);
        Assert.False(matrix[1, 1]);
        Assert.True(matrix[3, 3]);
        Assert.False(matrix[7, 0]);
        Assert.False(matrix[0, 7]);
    }

    [Fact]
    public void TryEncode_AllThreeFinderPatternsPresent()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        var size = matrix!.Width;

        Assert.True(matrix[0, 0]);
        Assert.True(matrix[size - 7, 0]);
        Assert.True(matrix[0, size - 7]);
    }

    [Fact]
    public void TryEncode_TimingPatternAlternates()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        Assert.True(matrix![8, 6]);
        Assert.False(matrix[9, 6]);
        Assert.True(matrix[10, 6]);
    }

    [Fact]
    public void TryEncode_DarkModuleIsAlwaysDark()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        Assert.True(matrix![8, matrix.Height - 8]);
    }

    [Fact]
    public void TryEncode_ValueTooLargeForSupportedRange_ReturnsError()
    {
        var encoder = new QrEncoder('H');

        // Level H is only supported through version 2 (16 data codewords) in this implementation.
        var success = encoder.TryEncode(new string('A', 50), out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_EmptyValue_ReturnsError()
    {
        var encoder = new QrEncoder();

        var success = encoder.TryEncode("", out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_InvalidErrorCorrectionLevel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrEncoder('X'));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `QrEncoder` does not exist yet.

- [ ] **Step 4: Implement**

Create `src/IBEBarcode.Core/Encoders/Qr/QrBitWriter.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Qr;

internal sealed class QrBitWriter
{
    private readonly List<bool> _bits = new();

    public int Count => _bits.Count;

    public void AppendBits(int value, int bitCount)
    {
        for (var i = bitCount - 1; i >= 0; i--)
        {
            _bits.Add(((value >> i) & 1) != 0);
        }
    }

    public byte[] ToBytes()
    {
        var byteCount = (_bits.Count + 7) / 8;
        var bytes = new byte[byteCount];

        for (var i = 0; i < _bits.Count; i++)
        {
            if (_bits[i])
            {
                bytes[i / 8] |= (byte)(0x80 >> (i % 8));
            }
        }

        return bytes;
    }
}
```

Create `src/IBEBarcode.Core/Encoders/QrEncoder.cs`:

```csharp
using System.Text;
using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Encoders;

public sealed class QrEncoder : IMatrixBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.QrCode;

    private readonly char _level;

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelL =
    {
        (1, 19, 7), (2, 34, 10), (3, 55, 15), (4, 80, 20), (5, 108, 26),
    };

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelM =
    {
        (1, 16, 10), (2, 28, 16), (3, 44, 26),
    };

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelQ =
    {
        (1, 13, 13), (2, 22, 22),
    };

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelH =
    {
        (1, 9, 17), (2, 16, 28),
    };

    private static readonly Dictionary<char, string> FormatStringsMask0 = new()
    {
        ['L'] = "111011111000100",
        ['M'] = "101010000010010",
        ['Q'] = "011010101011111",
        ['H'] = "001011010001001",
    };

    private static readonly (int X, int Y)[] FormatInfoCoordinates =
    {
        (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7),
        (8, 8), (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8),
    };

    public QrEncoder(char errorCorrectionLevel = 'M')
    {
        if (errorCorrectionLevel is not ('L' or 'M' or 'Q' or 'H'))
        {
            throw new ArgumentOutOfRangeException(nameof(errorCorrectionLevel), "Error correction level must be L, M, Q, or H.");
        }

        _level = errorCorrectionLevel;
    }

    public bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error)
    {
        matrix = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var dataBytes = Encoding.UTF8.GetBytes(value);

        if (!TrySelectVersion(dataBytes.Length, out var version, out var dataCw, out var eccCw))
        {
            error = $"Value is too large to encode at error correction level {_level} within the supported QR versions (1-5, single-block only).";
            return false;
        }

        var codewords = BuildCodewords(dataBytes, dataCw, eccCw);
        var size = 17 + 4 * version;
        var modules = new bool[size, size];
        var reserved = new bool[size, size];

        PlaceFinderPattern(modules, reserved, 0, 0, size);
        PlaceFinderPattern(modules, reserved, size - 7, 0, size);
        PlaceFinderPattern(modules, reserved, 0, size - 7, size);
        PlaceTimingPatterns(modules, reserved, size);
        PlaceAlignmentPattern(modules, reserved, version, size);
        PlaceDarkModule(modules, reserved, size);
        PlaceFormatInfo(modules, reserved, size);
        PlaceDataBits(modules, reserved, codewords, size);

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }

    private (int Version, int DataCodewords, int EccCodewords)[] SupportedCombos() => _level switch
    {
        'L' => LevelL,
        'M' => LevelM,
        'Q' => LevelQ,
        'H' => LevelH,
        _ => throw new InvalidOperationException(),
    };

    private bool TrySelectVersion(int dataByteCount, out int version, out int dataCw, out int eccCw)
    {
        foreach (var (v, dcw, ecw) in SupportedCombos())
        {
            var neededBits = 4 + 8 + dataByteCount * 8;

            if (neededBits <= dcw * 8)
            {
                version = v;
                dataCw = dcw;
                eccCw = ecw;
                return true;
            }
        }

        version = 0;
        dataCw = 0;
        eccCw = 0;
        return false;
    }

    private static byte[] BuildCodewords(byte[] dataBytes, int dataCw, int eccCw)
    {
        var writer = new QrBitWriter();
        writer.AppendBits(0b0100, 4);
        writer.AppendBits(dataBytes.Length, 8);

        foreach (var b in dataBytes)
        {
            writer.AppendBits(b, 8);
        }

        var capacityBits = dataCw * 8;
        var terminatorBits = Math.Min(4, capacityBits - writer.Count);

        if (terminatorBits > 0)
        {
            writer.AppendBits(0, terminatorBits);
        }

        while (writer.Count % 8 != 0)
        {
            writer.AppendBits(0, 1);
        }

        var dataCodewords = writer.ToBytes().ToList();
        var padBytes = new byte[] { 0xEC, 0x11 };
        var padIndex = 0;

        while (dataCodewords.Count < dataCw)
        {
            dataCodewords.Add(padBytes[padIndex % 2]);
            padIndex++;
        }

        var dataArray = dataCodewords.ToArray();
        var eccCodewords = QrReedSolomon.ComputeEccCodewords(dataArray, eccCw);

        var allCodewords = new byte[dataCw + eccCw];
        Array.Copy(dataArray, allCodewords, dataCw);
        Array.Copy(eccCodewords, 0, allCodewords, dataCw, eccCw);
        return allCodewords;
    }

    private static void PlaceFinderPattern(bool[,] modules, bool[,] reserved, int xStart, int yStart, int size)
    {
        for (var dy = -1; dy <= 7; dy++)
        {
            for (var dx = -1; dx <= 7; dx++)
            {
                var x = xStart + dx;
                var y = yStart + dy;

                if (x < 0 || x >= size || y < 0 || y >= size)
                {
                    continue;
                }

                reserved[x, y] = true;

                if (dx is >= 0 and <= 6 && dy is >= 0 and <= 6)
                {
                    modules[x, y] = FinderPatternValue(dx, dy);
                }
            }
        }
    }

    private static bool FinderPatternValue(int x, int y)
    {
        if (x == 0 || x == 6 || y == 0 || y == 6)
        {
            return true;
        }

        return x is >= 2 and <= 4 && y is >= 2 and <= 4;
    }

    private static void PlaceTimingPatterns(bool[,] modules, bool[,] reserved, int size)
    {
        for (var i = 8; i < size - 8; i++)
        {
            var isDark = (i + 1) % 2 == 1;

            if (!reserved[i, 6])
            {
                modules[i, 6] = isDark;
                reserved[i, 6] = true;
            }

            if (!reserved[6, i])
            {
                modules[6, i] = isDark;
                reserved[6, i] = true;
            }
        }
    }

    private static void PlaceAlignmentPattern(bool[,] modules, bool[,] reserved, int version, int size)
    {
        if (version == 1)
        {
            return;
        }

        var center = version switch
        {
            2 => 18,
            3 => 22,
            4 => 26,
            5 => 30,
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

        var start = center - 2;

        for (var dy = 0; dy < 5; dy++)
        {
            for (var dx = 0; dx < 5; dx++)
            {
                var x = start + dx;
                var y = start + dy;
                reserved[x, y] = true;
                modules[x, y] = AlignmentPatternValue(dx, dy);
            }
        }
    }

    private static bool AlignmentPatternValue(int x, int y)
    {
        if (x == 0 || x == 4 || y == 0 || y == 4)
        {
            return true;
        }

        return x == 2 && y == 2;
    }

    private static void PlaceDarkModule(bool[,] modules, bool[,] reserved, int size)
    {
        var row = size - 8;
        modules[8, row] = true;
        reserved[8, row] = true;
    }

    private void PlaceFormatInfo(bool[,] modules, bool[,] reserved, int size)
    {
        var formatString = FormatStringsMask0[_level];

        for (var i = 0; i < 15; i++)
        {
            var bit = formatString[14 - i] == '1';

            var (x1, y1) = FormatInfoCoordinates[i];
            modules[x1, y1] = bit;
            reserved[x1, y1] = true;

            int x2, y2;

            if (i < 8)
            {
                x2 = size - i - 1;
                y2 = 8;
            }
            else
            {
                x2 = 8;
                y2 = size - 7 + (i - 8);
            }

            modules[x2, y2] = bit;
            reserved[x2, y2] = true;
        }
    }

    private static void PlaceDataBits(bool[,] modules, bool[,] reserved, byte[] codewords, int size)
    {
        var bitIndex = 0;
        var totalBits = codewords.Length * 8;
        var direction = -1;
        var x = size - 1;

        while (x > 0)
        {
            if (x == 6)
            {
                x--;
            }

            var y = direction == -1 ? size - 1 : 0;

            while (y >= 0 && y < size)
            {
                for (var i = 0; i < 2; i++)
                {
                    var xx = x - i;

                    if (!reserved[xx, y])
                    {
                        bool bit;

                        if (bitIndex < totalBits)
                        {
                            var byteIndex = bitIndex / 8;
                            var bitInByte = 7 - (bitIndex % 8);
                            bit = ((codewords[byteIndex] >> bitInByte) & 1) != 0;
                            bitIndex++;
                        }
                        else
                        {
                            bit = false;
                        }

                        if ((xx + y) % 2 == 0)
                        {
                            bit = !bit;
                        }

                        modules[xx, y] = bit;
                        reserved[xx, y] = true;
                    }
                }

                y += direction;
            }

            direction = -direction;
            x -= 2;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `QrEncoderTests` green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/Qr/QrBitWriter.cs src/IBEBarcode.Core/Encoders/QrEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/QrEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add QR Code encoder (byte mode, versions 1-5, single-block)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Multi-block QR versions (6+, or lower versions at higher ECC levels),
Numeric/Alphanumeric mode selection for denser encoding, mask-pattern
scoring (currently fixed at mask 0), version info blocks (version ≥ 7).
Then Data Matrix, PDF417, Aztec. Then Code 128 Set A/C + GS1-128. Then
`IBEBarcode.Rendering` needs a second render path for `BarcodeMatrix`
(square modules on a grid, much simpler than the linear bar renderer).
Then `IBEBarcode.Templates`, `IBEBarcode.Printing`, `IBEBarcode.Desktop`,
`IBEBarcode.Web`.

**Recommended before shipping:** scan a rendered QR code from this
encoder with a real phone camera/scanner app to confirm end-to-end
scannability — this plan's tests verify structural correctness against
the spec but cannot substitute for a real-world scan.
