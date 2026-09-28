# Aztec Code Encoder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a from-scratch Aztec Code encoder — a 2D format notable for having no quiet-zone requirement and a compact bullseye finder pattern, common on transit tickets and boarding passes.

**Scope decision (deliberate, mirroring every prior 2D-format plan this session):**
- **Compact Aztec only, layers 1-4** (matrix sizes 15×15, 19×19, 23×23, 27×27). Full-size Aztec (5-32 layers) needs a more complex alignment-mark grid (extra reference lines every 16 rows/columns) and orientation-mark corner patterns that compact symbols don't have — skipping it avoids that entirely. Compact layers 1-2 use a 6-bit word size (GF(64)), layers 3-4 use 8-bit (GF(256)) — only two data Galois fields needed, plus a third fixed one (GF(16)) always required for the mode message regardless of scope. Compact mode's own spec-defined cap of 64 data words per symbol limits practical capacity to roughly 40-60 bytes, similar order of magnitude to this session's Data Matrix scope (44 bytes) — a small-but-genuinely-useful symbol size, not a token stub.
- **Binary Shift mode only** — Aztec normally uses a token-based dynamic-programming optimizer that switches between five text modes (Upper/Lower/Mixed/Punct/Digit) plus Binary Shift for maximum density. This plan skips all of that and encodes every message as a single Binary Shift block starting from the symbol's initial state (mode Upper, zero prior tokens) — Binary Shift is a fully spec-legal encoding for any byte sequence and requires no other mode's tables, mirroring the byte-mode-only scoping used for QR, Data Matrix, and PDF417. Input is restricted to bytes 0-255 (Latin-1/raw-byte semantics), consistent with the PDF417 plan's choice.
- **No ECI / ISO_8859_1-only** — matches the Binary-Shift-only scope; no character-set switching codeword is ever emitted.

**Architecture:** Unlike QR/Data Matrix (each with their own bespoke Reed-Solomon implementation) and PDF417 (a prime-field GF(929) scheme), Aztec's error correction is a **generic, textbook "generator polynomial" Reed-Solomon** parameterized by an arbitrary `(primitive polynomial, field size, generator base)` triple — the same generic algorithm works unmodified for GF(16) (mode message), GF(64) (compact layers 1-2), and GF(256) (compact layers 3-4, and — cross-check — the exact same primitive polynomial `0x12D` this session's Data Matrix plan already verified and implemented). This plan implements that generic algorithm once (`AztecGaloisField`, `AztecGaloisFieldPoly`, `AztecReedSolomonEncoder`) and instantiates it three times with different parameters, rather than writing three bespoke implementations. Matrix assembly reuses `BarcodeMatrix`/`IMatrixBarcodeEncoder`/`MatrixRenderer` unchanged, same as every other 2D format this session.

**Tech Stack:** .NET 10, xUnit. Every structural piece — the generic Galois Field/polynomial/Reed-Solomon algorithm, the three fields' exact parameters, the compact-mode bit-stuffing algorithm, the Binary Shift bit-format (including its two-header edge case for 32-62 byte runs), the mode-message format, and the compact spiral data-placement/bullseye/mode-message-drawing logic — was fetched as complete, literal source from ZXing's `aztec.encoder` package (`Encoder.java`, `HighLevelEncoder.java`, `State.java`, `Token.java`, `BinaryShiftToken.java`) and `common.reedsolomon` package (`GenericGF.java`, `GenericGFPoly.java`, `ReedSolomonEncoder.java`) — Apache 2.0, itself from ISO/IEC 24778:2008. Verified via an independently-written round-trip decoder, the same strategy used for every prior 2D format this session.

**Spec:** `docs/superpowers/specs/2026-09-27-ibe-barcode-generator-design.md`

## Global Constraints

(Same as prior plans.)

## Verified reference values used by this plan

- `WORD_SIZE` by layer count (compact layers 1-4 only): `{6, 6, 8, 8}` (index 0 = layer 1).
- Galois Field parameters `(primitive, size, generatorBase)`: mode message → `(0x13, 16, 1)` (`x^4+x+1`); compact layers 1-2 → `(0x43, 64, 1)` (`x^6+x+1`); compact layers 3-4 → `(0x12D, 256, 1)` (`x^8+x^5+x^3+x^2+1` — same field this session's Data Matrix plan already verified, different generator base: `1` here vs. Data Matrix's own LFSR-based approach which didn't need this parameter).
- `totalBitsInLayer(layers) = (88 + 16*layers) * layers` (compact form of the general `(compact?88:112)+16*layers` formula).
- `DEFAULT_EC_PERCENT = 33`; `eccBits = dataBitCount * 33 / 100 + 11`.
- Compact symbol matrix size: `11 + layers*4` (layers 1-4 → 15/19/23/27).
- Bit-stuffing (`stuffBits`, avoids all-0/all-1 words per the ISO anti-false-alignment rule): for each `wordSize`-bit window (padding missing trailing bits with **1**, not 0, per spec), if the word's top `wordSize-1` bits are all 1 → emit `word & mask` (last bit forced 0) and back up one bit; if all 0 → emit `word | 1` (last bit forced 1) and back up one bit; otherwise emit the word as-is and advance normally. `mask = (1 << wordSize) - 2`.
- Generic Reed-Solomon (`ReedSolomonEncoder.encode`): build generator polynomial `g(x) = product_{d=0}^{ecWords-1} (x + alpha^(d+generatorBase))` (cached incrementally); remainder = `(dataPoly * x^ecWords) mod g(x)`; the ECC words are the remainder's coefficients, left-padded with zeros to exactly `ecWords` length.
- Mode message (compact): `appendBits(layers-1, 2)` + `appendBits(messageSizeInWords-1, 6)` = 8 data bits → Reed-Solomon over GF(16), wordSize 4, total 28 bits (7 words, 2 message + 5 check).
- Binary Shift bit format (`BinaryShiftToken.appendTo`, starting fresh — this plan's only encoding path): let `n` = byte count.
  - `n <= 31`: `[31 (5 bits)] [n (5 bits)]` then `n` literal bytes (8 bits each).
  - `32 <= n <= 62`: `[31 (5)] [31 (5)]` + first 31 bytes, then `[31 (5)] [n-31 (5)]` + remaining `n-31` bytes (two separate headers — a real edge case, not a simplification).
  - `n > 62`: `[31 (5)] [n-31 (16 bits)]` then all `n` bytes (single extended-length header).
- Compact data placement (`Encoder.java`'s spiral loop, with `alignmentMap[x]=x` since compact mode has no alignment-mark remapping): for layer `i` from `0` to `layers-1`, `rowSize = (layers-i)*4 + 9`, `rowOffset` starts at 0 and accumulates `rowSize*8` per layer; for `j` in `0..rowSize-1`, `columnOffset=j*2`, for `k` in `0..1`: four message-bit lookups (`rowOffset+columnOffset+k`, `+rowSize*2`, `+rowSize*4`, `+rowSize*6`) map respectively to matrix positions `(i*2+k, i*2+j)`, `(i*2+j, baseSize-1-i*2-k)`, `(baseSize-1-i*2-k, baseSize-1-i*2-j)`, `(baseSize-1-i*2-j, i*2+k)` where `baseSize = 11+layers*4`.
- Compact bullseye: alternating square rings centered at `(size/2, size/2)`, radius parameter `5` (fixed for all compact sizes).
- Compact mode-message drawing: `center = size/2`; for `i` in `0..6`, `offset = center-3+i`; four bit lookups (`i`, `i+7`, `20-i`, `27-i`) map to matrix positions `(offset, center-5)`, `(center+5, offset)`, `(offset, center+5)`, `(center-5, offset)`.

---

### Task 1: `AztecGaloisField`, `AztecGaloisFieldPoly`, `AztecReedSolomonEncoder`, `AztecBitBuffer`

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Aztec/AztecGaloisField.cs`
- Create: `src/IBEBarcode.Core/Encoders/Aztec/AztecGaloisFieldPoly.cs`
- Create: `src/IBEBarcode.Core/Encoders/Aztec/AztecReedSolomonEncoder.cs`
- Create: `src/IBEBarcode.Core/Encoders/Aztec/AztecBitBuffer.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Aztec/AztecReedSolomonEncoderTests.cs`

**Interfaces:**
- Produces: `internal sealed class AztecGaloisField` (ctor `(int primitive, int size, int generatorBase)`, `int Exp(int)`, `int Log(int)`, `int Inverse(int)`, `int Multiply(int, int)`, `int GeneratorBase { get; }`); `internal sealed class AztecGaloisFieldPoly` (ctor `(AztecGaloisField, int[] coefficients)`, `int Degree`, `bool IsZero`, `int GetCoefficient(int degree)`, `int[] Coefficients`, `AztecGaloisFieldPoly Multiply(AztecGaloisFieldPoly)`, `AztecGaloisFieldPoly MultiplyByMonomial(int degree, int coefficient)`, `AztecGaloisFieldPoly AddOrSubtract(AztecGaloisFieldPoly)`, `AztecGaloisFieldPoly[] Divide(AztecGaloisFieldPoly)`); `internal sealed class AztecReedSolomonEncoder` (ctor `(AztecGaloisField)`, `void Encode(int[] toEncode, int ecWords)`); `internal sealed class AztecBitBuffer` (`void AppendBits(int value, int numBits)`, `bool Get(int index)`, `int Count { get; }`).

- [ ] **Step 1: Write the failing test**

```csharp
using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Tests.Encoders.Aztec;

public class AztecReedSolomonEncoderTests
{
    [Fact]
    public void Encode_Gf16_ProducesDeterministicCheckWords()
    {
        var field = new AztecGaloisField(0x13, 16, 1);
        var rs = new AztecReedSolomonEncoder(field);

        var toEncode = new[] { 3, 5, 0, 0, 0, 0, 0 }; // 2 message words, 5 check words
        rs.Encode(toEncode, 5);

        var toEncodeAgain = new[] { 3, 5, 0, 0, 0, 0, 0 };
        rs.Encode(toEncodeAgain, 5);

        Assert.Equal(toEncode, toEncodeAgain);

        foreach (var word in toEncode)
        {
            Assert.InRange(word, 0, 15);
        }
    }

    [Fact]
    public void Encode_Gf256_MatchesDataMatrixFieldPrimitive()
    {
        // AZTEC_DATA_8 in ZXing is literally DATA_MATRIX_FIELD_256 (primitive 0x12D) --
        // the same field this project's DataMatrixGaloisField already implements, just
        // with a different (polynomial-multiplication) Reed-Solomon algorithm on top.
        var field = new AztecGaloisField(0x12D, 256, 1);
        var rs = new AztecReedSolomonEncoder(field);

        var toEncode = new int[10];
        toEncode[0] = 65;
        toEncode[1] = 66;
        rs.Encode(toEncode, 8);

        for (var i = 2; i < toEncode.Length; i++)
        {
            Assert.InRange(toEncode[i], 0, 255);
        }
    }

    [Fact]
    public void BitBuffer_AppendAndGet_RoundTrips()
    {
        var buffer = new AztecBitBuffer();
        buffer.AppendBits(0b101, 3);
        buffer.AppendBits(0b11001, 5);

        Assert.Equal(8, buffer.Count);
        Assert.True(buffer.Get(0));
        Assert.False(buffer.Get(1));
        Assert.True(buffer.Get(2));
        Assert.True(buffer.Get(3));
        Assert.True(buffer.Get(4));
        Assert.False(buffer.Get(5));
        Assert.False(buffer.Get(6));
        Assert.True(buffer.Get(7));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — none of the four classes exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Core/Encoders/Aztec/AztecGaloisField.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecGaloisField
{
    private readonly int[] _expTable;
    private readonly int[] _logTable;
    private readonly int _size;

    public int GeneratorBase { get; }

    public AztecGaloisField(int primitive, int size, int generatorBase)
    {
        _size = size;
        GeneratorBase = generatorBase;

        _expTable = new int[size];
        _logTable = new int[size];

        var x = 1;

        for (var i = 0; i < size; i++)
        {
            _expTable[i] = x;
            x *= 2;

            if (x >= size)
            {
                x ^= primitive;
                x &= size - 1;
            }
        }

        for (var i = 0; i < size - 1; i++)
        {
            _logTable[_expTable[i]] = i;
        }
    }

    public int Exp(int a) => _expTable[a];

    public int Log(int a) => _logTable[a];

    public int Inverse(int a) => _expTable[_size - _logTable[a] - 1];

    public int Multiply(int a, int b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        return _expTable[(_logTable[a] + _logTable[b]) % (_size - 1)];
    }
}
```

Create `src/IBEBarcode.Core/Encoders/Aztec/AztecGaloisFieldPoly.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecGaloisFieldPoly
{
    private readonly AztecGaloisField _field;

    public int[] Coefficients { get; }

    public int Degree => Coefficients.Length - 1;

    public bool IsZero => Coefficients[0] == 0;

    public AztecGaloisFieldPoly(AztecGaloisField field, int[] coefficients)
    {
        _field = field;

        if (coefficients.Length > 1 && coefficients[0] == 0)
        {
            var firstNonZero = 1;

            while (firstNonZero < coefficients.Length && coefficients[firstNonZero] == 0)
            {
                firstNonZero++;
            }

            Coefficients = firstNonZero == coefficients.Length
                ? new[] { 0 }
                : coefficients[firstNonZero..];
        }
        else
        {
            Coefficients = coefficients;
        }
    }

    public int GetCoefficient(int degree) => Coefficients[Coefficients.Length - 1 - degree];

    public AztecGaloisFieldPoly AddOrSubtract(AztecGaloisFieldPoly other)
    {
        if (IsZero)
        {
            return other;
        }

        if (other.IsZero)
        {
            return this;
        }

        var smaller = Coefficients;
        var larger = other.Coefficients;

        if (smaller.Length > larger.Length)
        {
            (smaller, larger) = (larger, smaller);
        }

        var result = new int[larger.Length];
        var lengthDiff = larger.Length - smaller.Length;
        Array.Copy(larger, result, lengthDiff);

        for (var i = lengthDiff; i < larger.Length; i++)
        {
            result[i] = smaller[i - lengthDiff] ^ larger[i];
        }

        return new AztecGaloisFieldPoly(_field, result);
    }

    public AztecGaloisFieldPoly Multiply(AztecGaloisFieldPoly other)
    {
        if (IsZero || other.IsZero)
        {
            return new AztecGaloisFieldPoly(_field, new[] { 0 });
        }

        var a = Coefficients;
        var b = other.Coefficients;
        var product = new int[a.Length + b.Length - 1];

        for (var i = 0; i < a.Length; i++)
        {
            for (var j = 0; j < b.Length; j++)
            {
                product[i + j] ^= _field.Multiply(a[i], b[j]);
            }
        }

        return new AztecGaloisFieldPoly(_field, product);
    }

    public AztecGaloisFieldPoly MultiplyByMonomial(int degree, int coefficient)
    {
        if (coefficient == 0)
        {
            return new AztecGaloisFieldPoly(_field, new[] { 0 });
        }

        var product = new int[Coefficients.Length + degree];

        for (var i = 0; i < Coefficients.Length; i++)
        {
            product[i] = _field.Multiply(Coefficients[i], coefficient);
        }

        return new AztecGaloisFieldPoly(_field, product);
    }

    public AztecGaloisFieldPoly[] Divide(AztecGaloisFieldPoly other)
    {
        var quotient = new AztecGaloisFieldPoly(_field, new[] { 0 });
        var remainder = this;

        var denominatorLeadingTerm = other.GetCoefficient(other.Degree);
        var inverseDenominatorLeadingTerm = _field.Inverse(denominatorLeadingTerm);

        while (remainder.Degree >= other.Degree && !remainder.IsZero)
        {
            var degreeDifference = remainder.Degree - other.Degree;
            var scale = _field.Multiply(remainder.GetCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
            var term = other.MultiplyByMonomial(degreeDifference, scale);
            var iterationQuotient = BuildMonomial(degreeDifference, scale);
            quotient = quotient.AddOrSubtract(iterationQuotient);
            remainder = remainder.AddOrSubtract(term);
        }

        return new[] { quotient, remainder };
    }

    private AztecGaloisFieldPoly BuildMonomial(int degree, int coefficient)
    {
        if (coefficient == 0)
        {
            return new AztecGaloisFieldPoly(_field, new[] { 0 });
        }

        var coefficients = new int[degree + 1];
        coefficients[0] = coefficient;
        return new AztecGaloisFieldPoly(_field, coefficients);
    }
}
```

Create `src/IBEBarcode.Core/Encoders/Aztec/AztecReedSolomonEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecReedSolomonEncoder
{
    private readonly AztecGaloisField _field;
    private readonly List<AztecGaloisFieldPoly> _cachedGenerators;

    public AztecReedSolomonEncoder(AztecGaloisField field)
    {
        _field = field;
        _cachedGenerators = new List<AztecGaloisFieldPoly> { new(field, new[] { 1 }) };
    }

    private AztecGaloisFieldPoly BuildGenerator(int degree)
    {
        if (degree >= _cachedGenerators.Count)
        {
            var lastGenerator = _cachedGenerators[^1];

            for (var d = _cachedGenerators.Count; d <= degree; d++)
            {
                var nextGenerator = lastGenerator.Multiply(
                    new AztecGaloisFieldPoly(_field, new[] { 1, _field.Exp(d - 1 + _field.GeneratorBase) }));
                _cachedGenerators.Add(nextGenerator);
                lastGenerator = nextGenerator;
            }
        }

        return _cachedGenerators[degree];
    }

    public void Encode(int[] toEncode, int ecWords)
    {
        var dataWords = toEncode.Length - ecWords;
        var generator = BuildGenerator(ecWords);

        var infoCoefficients = new int[dataWords];
        Array.Copy(toEncode, infoCoefficients, dataWords);

        var info = new AztecGaloisFieldPoly(_field, infoCoefficients);
        info = info.MultiplyByMonomial(ecWords, 1);

        var remainder = info.Divide(generator)[1];
        var coefficients = remainder.Coefficients;
        var numZeroCoefficients = ecWords - coefficients.Length;

        for (var i = 0; i < numZeroCoefficients; i++)
        {
            toEncode[dataWords + i] = 0;
        }

        Array.Copy(coefficients, 0, toEncode, dataWords + numZeroCoefficients, coefficients.Length);
    }
}
```

Create `src/IBEBarcode.Core/Encoders/Aztec/AztecBitBuffer.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecBitBuffer
{
    private readonly List<bool> _bits = new();

    public int Count => _bits.Count;

    public bool Get(int index) => _bits[index];

    public void AppendBits(int value, int numBits)
    {
        for (var i = numBits - 1; i >= 0; i--)
        {
            _bits.Add(((value >> i) & 1) != 0);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/Aztec tests/IBEBarcode.Core.Tests/Encoders/Aztec/AztecReedSolomonEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Aztec generic Galois Field / Reed-Solomon / bit buffer

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: `AztecHighLevelEncoder` (Binary Shift only)

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/Aztec/AztecHighLevelEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/Aztec/AztecHighLevelEncoderTests.cs`

**Interfaces:**
- Produces: `internal static class AztecHighLevelEncoder` with `AztecBitBuffer EncodeBinaryShift(byte[] bytes)`.

- [ ] **Step 1: Write the failing test**

```csharp
using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Tests.Encoders.Aztec;

public class AztecHighLevelEncoderTests
{
    [Fact]
    public void EncodeBinaryShift_ShortRun_UsesSingleFiveBitHeader()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[] { 65, 66, 67 });

        // header: B/S(5)=31, length(5)=3, then 3 bytes (8 bits each) = 10 + 24 = 34 bits.
        Assert.Equal(34, bits.Count);
    }

    [Fact]
    public void EncodeBinaryShift_ExactlyThirtyOneBytes_UsesSingleHeader()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[31]);

        Assert.Equal(10 + 31 * 8, bits.Count);
    }

    [Fact]
    public void EncodeBinaryShift_ThirtyTwoToSixtyTwoBytes_UsesTwoHeaders()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[40]);

        // Two 10-bit headers (31 bytes then 9 bytes) + 40*8 data bits.
        Assert.Equal(20 + 40 * 8, bits.Count);
    }

    [Fact]
    public void EncodeBinaryShift_MoreThanSixtyTwoBytes_UsesExtendedHeader()
    {
        var bits = AztecHighLevelEncoder.EncodeBinaryShift(new byte[100]);

        // B/S(5) + 16-bit extended length + 100*8 data bits.
        Assert.Equal(21 + 100 * 8, bits.Count);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `AztecHighLevelEncoder` does not exist yet.

- [ ] **Step 3: Implement**

Create `src/IBEBarcode.Core/Encoders/Aztec/AztecHighLevelEncoder.cs`:

```csharp
namespace IBEBarcode.Core.Encoders.Aztec;

internal static class AztecHighLevelEncoder
{
    public static AztecBitBuffer EncodeBinaryShift(byte[] bytes)
    {
        var buffer = new AztecBitBuffer();
        var n = bytes.Length;

        if (n > 62)
        {
            buffer.AppendBits(31, 5);
            buffer.AppendBits(n - 31, 16);

            foreach (var b in bytes)
            {
                buffer.AppendBits(b, 8);
            }
        }
        else if (n > 31)
        {
            buffer.AppendBits(31, 5);
            buffer.AppendBits(31, 5);

            for (var i = 0; i < 31; i++)
            {
                buffer.AppendBits(bytes[i], 8);
            }

            buffer.AppendBits(31, 5);
            buffer.AppendBits(n - 31, 5);

            for (var i = 31; i < n; i++)
            {
                buffer.AppendBits(bytes[i], 8);
            }
        }
        else
        {
            buffer.AppendBits(31, 5);
            buffer.AppendBits(n, 5);

            foreach (var b in bytes)
            {
                buffer.AppendBits(b, 8);
            }
        }

        return buffer;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IBEBarcode.Core/Encoders/Aztec/AztecHighLevelEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/Aztec/AztecHighLevelEncoderTests.cs
git commit -m "$(cat <<'EOF'
Add Aztec Binary Shift high-level encoder

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: `AztecEncoder`

**Files:**
- Create: `src/IBEBarcode.Core/Encoders/AztecEncoder.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/AztecEncoderTests.cs`
- Test: `tests/IBEBarcode.Core.Tests/Encoders/AztecRoundTripTests.cs`
- Modify: `src/IBEBarcode.Core/BarcodeSymbology.cs` (add `Aztec`)

**Interfaces:**
- Consumes: `AztecHighLevelEncoder.EncodeBinaryShift`, `AztecReedSolomonEncoder`, `AztecGaloisField`, `AztecBitBuffer` (Tasks 1-2).
- Produces: `BarcodeSymbology.Aztec` and `sealed class AztecEncoder : IMatrixBarcodeEncoder`.

- [ ] **Step 1: Add the enum value**

Edit `src/IBEBarcode.Core/BarcodeSymbology.cs`, adding `Aztec,` after `Pdf417,`.

- [ ] **Step 2: Write the failing tests**

Create `tests/IBEBarcode.Core.Tests/Encoders/AztecEncoderTests.cs`:

```csharp
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class AztecEncoderTests
{
    private readonly AztecEncoder _encoder = new();

    [Fact]
    public void TryEncode_ShortValue_ProducesCompactSizedSymbol()
    {
        var success = _encoder.TryEncode("Hi", out var matrix, out var error);

        Assert.True(success, error);
        // Compact sizes are always 15/19/23/27.
        Assert.Contains(matrix!.Width, new[] { 15, 19, 23, 27 });
        Assert.Equal(matrix.Width, matrix.Height);
        Assert.Equal(BarcodeSymbology.Aztec, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_LongerValue_SelectsLargerSymbol()
    {
        _encoder.TryEncode("Hi", out var small, out _);
        _encoder.TryEncode("This is a somewhat longer test message for Aztec", out var large, out var error);

        Assert.True(large is not null, error);
        Assert.True(large!.Width >= small!.Width);
    }

    [Fact]
    public void TryEncode_CenterModuleIsDark()
    {
        _encoder.TryEncode("Hi", out var matrix, out _);

        var center = matrix!.Width / 2;
        Assert.True(matrix[center, center]);
    }

    [Fact]
    public void TryEncode_ValueTooLargeForCompactRange_ReturnsError()
    {
        var success = _encoder.TryEncode(new string('A', 200), out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_CharacterAbove255_ReturnsError()
    {
        var success = _encoder.TryEncode("cafሴ", out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_EmptyValue_ReturnsError()
    {
        var success = _encoder.TryEncode("", out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }
}
```

Create `tests/IBEBarcode.Core.Tests/Encoders/AztecRoundTripTests.cs` — an independently-written decoder confirming encode→decode recovers the original bytes:

```csharp
using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class AztecRoundTripTests
{
    [Theory]
    [InlineData("Hi")]
    [InlineData("Aztec test")]
    [InlineData("The quick brown fox 0123456789")]
    public void EncodeThenDecode_RoundTripsExactly(string original)
    {
        var encoder = new AztecEncoder();
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(original, decoded);
    }

    private static string Decode(BarcodeMatrix matrix)
    {
        var size = matrix.Width;
        var layers = (size - 11) / 4;
        var wordSize = layers <= 2 ? 6 : 8;

        // Read the mode message to recover messageSizeInWords (we already know layers
        // from the symbol size, so we only need the word count here).
        var center = size / 2;
        var modeBits = new bool[28];

        for (var i = 0; i < 7; i++)
        {
            var offset = center - 3 + i;
            modeBits[i] = matrix[offset, center - 5];
            modeBits[i + 7] = matrix[center + 5, offset];
            modeBits[20 - i] = matrix[offset, center + 5];
            modeBits[27 - i] = matrix[center - 5, offset];
        }

        // First 8 bits (2 message words of 4 bits) are layers-1 (2 bits) + wordCount-1 (6 bits).
        var messageSizeInWords = 0;

        for (var i = 2; i < 8; i++)
        {
            messageSizeInWords = (messageSizeInWords << 1) | (modeBits[i] ? 1 : 0);
        }

        messageSizeInWords += 1;

        // Re-read the data spiral into a flat bit list, reversing the placement loop.
        var baseSize = size;
        var totalDataBits = 0;

        for (var i = 0; i < layers; i++)
        {
            totalDataBits += ((layers - i) * 4 + 9) * 8;
        }

        var messageBits = new bool[totalDataBits];
        var rowOffset = 0;

        for (var i = 0; i < layers; i++)
        {
            var rowSize = (layers - i) * 4 + 9;

            for (var j = 0; j < rowSize; j++)
            {
                var columnOffset = j * 2;

                for (var k = 0; k < 2; k++)
                {
                    messageBits[rowOffset + columnOffset + k] = matrix[i * 2 + k, i * 2 + j];
                    messageBits[rowOffset + rowSize * 2 + columnOffset + k] = matrix[i * 2 + j, baseSize - 1 - i * 2 - k];
                    messageBits[rowOffset + rowSize * 4 + columnOffset + k] = matrix[baseSize - 1 - i * 2 - k, baseSize - 1 - i * 2 - j];
                    messageBits[rowOffset + rowSize * 6 + columnOffset + k] = matrix[baseSize - 1 - i * 2 - j, i * 2 + k];
                }
            }

            rowOffset += rowSize * 8;
        }

        // Extract the first messageSizeInWords words (the data words; check words follow).
        var dataBitCount = messageSizeInWords * wordSize;
        var dataBits = new List<bool>();

        for (var i = 0; i < dataBitCount; i++)
        {
            dataBits.Add(messageBits[i]);
        }

        // Undo bit-stuffing: every wordSize-bit window whose bottom bit was forced to
        // maintain the anti-alignment rule contributes wordSize-1 real bits, not wordSize.
        // Simpler for this round-trip: read words, and for each word whose value's low bit
        // was stuffed (top bits all-1 with low bit 0, or top bits all-0 with low bit 1),
        // drop only the stuffed bit and continue reading the next real bit immediately after.
        var realBits = new List<bool>();
        var idx = 0;
        var mask = (1 << wordSize) - 2;

        while (idx + wordSize <= dataBits.Count)
        {
            var word = 0;

            for (var i = 0; i < wordSize; i++)
            {
                word = (word << 1) | (dataBits[idx + i] ? 1 : 0);
            }

            if ((word & mask) == mask)
            {
                for (var i = 0; i < wordSize - 1; i++)
                {
                    realBits.Add(dataBits[idx + i]);
                }

                idx += wordSize - 1;
            }
            else if ((word & mask) == 0)
            {
                for (var i = 0; i < wordSize - 1; i++)
                {
                    realBits.Add(dataBits[idx + i]);
                }

                idx += wordSize - 1;
            }
            else
            {
                for (var i = 0; i < wordSize; i++)
                {
                    realBits.Add(dataBits[idx + i]);
                }

                idx += wordSize;
            }
        }

        // realBits now holds: B/S header(s) + raw bytes. Decode per this plan's
        // Binary-Shift-only format.
        var pos = 0;

        int ReadBits(int count)
        {
            var value = 0;

            for (var i = 0; i < count; i++)
            {
                value = (value << 1) | (realBits[pos++] ? 1 : 0);
            }

            return value;
        }

        var bytes = new List<byte>();
        var bsCode = ReadBits(5);
        _ = bsCode; // always 31 in this plan's scope

        var firstLength = ReadBits(5);

        if (firstLength == 31)
        {
            // Could be the 16-bit extended form OR the "first 31 of 32-62" form.
            // Disambiguate by peeking: extended form's next 16 bits are the remaining
            // count directly; the two-header form's next bits are 31 literal bytes
            // (248 bits) followed by another 5+5-bit header. Try extended first: if the
            // implied total byte count matches the data actually available, use it.
            var savedPos = pos;
            var extendedRemaining = ReadBits(16);
            var impliedTotal = extendedRemaining + 31;
            var bitsLeftForBytes = realBits.Count - pos;

            if (bitsLeftForBytes == impliedTotal * 8)
            {
                for (var i = 0; i < impliedTotal; i++)
                {
                    bytes.Add((byte)ReadBits(8));
                }
            }
            else
            {
                pos = savedPos;

                for (var i = 0; i < 31; i++)
                {
                    bytes.Add((byte)ReadBits(8));
                }

                ReadBits(5); // second header's B/S code, always 31
                var secondLength = ReadBits(5);

                for (var i = 0; i < secondLength; i++)
                {
                    bytes.Add((byte)ReadBits(8));
                }
            }
        }
        else
        {
            for (var i = 0; i < firstLength; i++)
            {
                bytes.Add((byte)ReadBits(8));
            }
        }

        var chars = new char[bytes.Count];

        for (var i = 0; i < bytes.Count; i++)
        {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test`
Expected: FAIL — `AztecEncoder` does not exist yet.

- [ ] **Step 4: Implement the encoder**

Create `src/IBEBarcode.Core/Encoders/AztecEncoder.cs`:

```csharp
using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Encoders;

public sealed class AztecEncoder : IMatrixBarcodeEncoder
{
    private const int EcPercent = 33;
    private static readonly int[] WordSizeByLayer = { 6, 6, 8, 8 };

    public BarcodeSymbology Symbology => BarcodeSymbology.Aztec;

    public bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error)
    {
        matrix = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var bytes = new byte[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 255)
            {
                error = $"Character '{value[i]}' is outside the 0-255 byte range this Aztec encoder supports.";
                return false;
            }

            bytes[i] = (byte)value[i];
        }

        var bits = AztecHighLevelEncoder.EncodeBinaryShift(bytes);
        var eccBits = bits.Count * EcPercent / 100 + 11;
        var totalSizeBits = bits.Count + eccBits;

        for (var layerIndex = 0; layerIndex < 4; layerIndex++)
        {
            var layers = layerIndex + 1;
            var totalBitsInLayer = (88 + 16 * layers) * layers;

            if (totalSizeBits > totalBitsInLayer)
            {
                continue;
            }

            var wordSize = WordSizeByLayer[layerIndex];
            var stuffedBits = StuffBits(bits, wordSize);
            var usableBitsInLayer = totalBitsInLayer - (totalBitsInLayer % wordSize);

            if (stuffedBits.Count > wordSize * 64)
            {
                continue;
            }

            if (stuffedBits.Count + eccBits > usableBitsInLayer)
            {
                continue;
            }

            return BuildSymbol(value, stuffedBits, layers, wordSize, out matrix, out error);
        }

        error = "Value is too large to encode: this Aztec encoder supports compact symbols (layers 1-4) only.";
        return false;
    }

    private static bool BuildSymbol(string value, AztecBitBuffer stuffedBits, int layers, int wordSize, out BarcodeMatrix? matrix, out string? error)
    {
        var totalBitsInLayer = (88 + 16 * layers) * layers;
        var messageBits = GenerateCheckWords(stuffedBits, totalBitsInLayer, wordSize);

        var messageSizeInWords = stuffedBits.Count / wordSize;
        var modeMessage = GenerateModeMessage(layers, messageSizeInWords);

        var baseSize = 11 + layers * 4;
        var modules = new bool[baseSize, baseSize];

        var rowOffset = 0;

        for (var i = 0; i < layers; i++)
        {
            var rowSize = (layers - i) * 4 + 9;

            for (var j = 0; j < rowSize; j++)
            {
                var columnOffset = j * 2;

                for (var k = 0; k < 2; k++)
                {
                    if (messageBits.Get(rowOffset + columnOffset + k))
                    {
                        modules[i * 2 + k, i * 2 + j] = true;
                    }

                    if (messageBits.Get(rowOffset + rowSize * 2 + columnOffset + k))
                    {
                        modules[i * 2 + j, baseSize - 1 - i * 2 - k] = true;
                    }

                    if (messageBits.Get(rowOffset + rowSize * 4 + columnOffset + k))
                    {
                        modules[baseSize - 1 - i * 2 - k, baseSize - 1 - i * 2 - j] = true;
                    }

                    if (messageBits.Get(rowOffset + rowSize * 6 + columnOffset + k))
                    {
                        modules[baseSize - 1 - i * 2 - j, i * 2 + k] = true;
                    }
                }
            }

            rowOffset += rowSize * 8;
        }

        DrawModeMessage(modules, baseSize, modeMessage);
        DrawBullsEye(modules, baseSize / 2, 5);

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }

    private static void DrawBullsEye(bool[,] modules, int center, int size)
    {
        for (var i = 0; i < size; i += 2)
        {
            for (var j = center - i; j <= center + i; j++)
            {
                modules[j, center - i] = true;
                modules[j, center + i] = true;
                modules[center - i, j] = true;
                modules[center + i, j] = true;
            }
        }

        modules[center - size, center - size] = true;
        modules[center - size + 1, center - size] = true;
        modules[center - size, center - size + 1] = true;
        modules[center + size, center - size] = true;
        modules[center + size, center - size + 1] = true;
        modules[center + size, center + size - 1] = true;
    }

    private static void DrawModeMessage(bool[,] modules, int matrixSize, AztecBitBuffer modeMessage)
    {
        var center = matrixSize / 2;

        for (var i = 0; i < 7; i++)
        {
            var offset = center - 3 + i;

            if (modeMessage.Get(i))
            {
                modules[offset, center - 5] = true;
            }

            if (modeMessage.Get(i + 7))
            {
                modules[center + 5, offset] = true;
            }

            if (modeMessage.Get(20 - i))
            {
                modules[offset, center + 5] = true;
            }

            if (modeMessage.Get(27 - i))
            {
                modules[center - 5, offset] = true;
            }
        }
    }

    private static AztecBitBuffer GenerateModeMessage(int layers, int messageSizeInWords)
    {
        var modeMessage = new AztecBitBuffer();
        modeMessage.AppendBits(layers - 1, 2);
        modeMessage.AppendBits(messageSizeInWords - 1, 6);
        return GenerateCheckWords(modeMessage, 28, 4);
    }

    private static AztecBitBuffer GenerateCheckWords(AztecBitBuffer bitArray, int totalBits, int wordSize)
    {
        var messageSizeInWords = bitArray.Count / wordSize;
        var field = GetField(wordSize);
        var rs = new AztecReedSolomonEncoder(field);
        var totalWords = totalBits / wordSize;
        var messageWords = BitsToWords(bitArray, wordSize, totalWords);
        rs.Encode(messageWords, totalWords - messageSizeInWords);

        var startPad = totalBits % wordSize;
        var result = new AztecBitBuffer();
        result.AppendBits(0, startPad);

        foreach (var word in messageWords)
        {
            result.AppendBits(word, wordSize);
        }

        return result;
    }

    private static int[] BitsToWords(AztecBitBuffer stuffedBits, int wordSize, int totalWords)
    {
        var message = new int[totalWords];
        var n = stuffedBits.Count / wordSize;

        for (var i = 0; i < n; i++)
        {
            var value = 0;

            for (var j = 0; j < wordSize; j++)
            {
                value |= stuffedBits.Get(i * wordSize + j) ? 1 << (wordSize - j - 1) : 0;
            }

            message[i] = value;
        }

        return message;
    }

    private static AztecGaloisField GetField(int wordSize) => wordSize switch
    {
        4 => new AztecGaloisField(0x13, 16, 1),
        6 => new AztecGaloisField(0x43, 64, 1),
        8 => new AztecGaloisField(0x12D, 256, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(wordSize)),
    };

    private static AztecBitBuffer StuffBits(AztecBitBuffer bits, int wordSize)
    {
        var output = new AztecBitBuffer();
        var n = bits.Count;
        var mask = (1 << wordSize) - 2;

        for (var i = 0; i < n; i += wordSize)
        {
            var word = 0;

            for (var j = 0; j < wordSize; j++)
            {
                if (i + j >= n || bits.Get(i + j))
                {
                    word |= 1 << (wordSize - 1 - j);
                }
            }

            if ((word & mask) == mask)
            {
                output.AppendBits(word & mask, wordSize);
                i--;
            }
            else if ((word & mask) == 0)
            {
                output.AppendBits(word | 1, wordSize);
                i--;
            }
            else
            {
                output.AppendBits(word, wordSize);
            }
        }

        return output;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `AztecEncoderTests` and `AztecRoundTripTests` green, full solution test suite green.

- [ ] **Step 6: Commit**

```bash
git add src/IBEBarcode.Core/BarcodeSymbology.cs src/IBEBarcode.Core/Encoders/AztecEncoder.cs tests/IBEBarcode.Core.Tests/Encoders/AztecEncoderTests.cs tests/IBEBarcode.Core.Tests/Encoders/AztecRoundTripTests.cs
git commit -m "$(cat <<'EOF'
Add Aztec encoder (Binary Shift mode, compact symbols only)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## What's next (not in this plan)

Full-size Aztec (layers 5-32, needs the alignment-mark grid and its
`alignmentMap` remapping). Text/Digit/Punct/Mixed compaction modes
(would produce smaller symbols for text-heavy input). Reader Initial
Symbol / structured append. Wiring `Aztec` into the desktop/web
symbology pickers (should follow the exact same pattern already used
for QR, Data Matrix, and PDF417, since it also implements
`IMatrixBarcodeEncoder`). PDF417 Text/Numeric Compaction. Larger/
rectangular Data Matrix sizes. Mixed-alphanumeric GS1-128 AI parsing.
Mid-message Code128 subset switching.
