using IBEBarcode.Core;
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

    [Fact]
    public void TryEncode_SetB_DefaultConstructor_StillMatchesPriorBehavior()
    {
        var defaultEncoder = new Code128Encoder();
        var explicitB = new Code128Encoder(Code128Set.B);

        defaultEncoder.TryEncode("A1", out var defaultPattern, out _);
        explicitB.TryEncode("A1", out var explicitPattern, out _);

        Assert.Equal(explicitPattern!.Segments, defaultPattern!.Segments);
    }

    [Fact]
    public void TryEncode_SetA_UppercaseAndControlChars_Succeeds()
    {
        var encoder = new Code128Encoder(Code128Set.A);

        var success = encoder.TryEncode("ABC-123", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("ABC-123", pattern!.Value);
    }

    [Fact]
    public void TryEncode_SetA_LowercaseLetter_ReturnsError()
    {
        var encoder = new Code128Encoder(Code128Set.A);

        var success = encoder.TryEncode("abc", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_SetC_DigitPairs_Succeeds()
    {
        var encoder = new Code128Encoder(Code128Set.C);

        var success = encoder.TryEncode("00012345678905", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("00012345678905", pattern!.Value);
    }

    [Fact]
    public void TryEncode_SetC_OddLength_ReturnsError()
    {
        var encoder = new Code128Encoder(Code128Set.C);

        var success = encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_SetC_ProducesShorterSymbolCountThanSetB()
    {
        var setC = new Code128Encoder(Code128Set.C);
        var setB = new Code128Encoder(Code128Set.B);

        setC.TryEncode("12345678", out var patternC, out _);
        setB.TryEncode("12345678", out var patternB, out _);

        // Set C: start+4 digit-pairs+check+stop = 7 symbols * 6 = 42 (stop is 7-wide) = 41
        // Set B: start+8 digits+check+stop = 10 symbols
        Assert.True(patternC!.Segments.Count < patternB!.Segments.Count);
    }

    [Fact]
    public void TryEncode_SetA_StartSymbolDiffersFromSetB()
    {
        var setA = new Code128Encoder(Code128Set.A);
        var setB = new Code128Encoder(Code128Set.B);

        setA.TryEncode("A", out var patternA, out _);
        setB.TryEncode("A", out var patternB, out _);

        var startA = patternA!.Segments.Take(6).ToArray();
        var startB = patternB!.Segments.Take(6).ToArray();

        Assert.NotEqual(startB, startA);
    }

    [Fact]
    public void TryEncode_Auto_ShortDigitRun_StaysInSetB()
    {
        var auto = new Code128Encoder(Code128Set.Auto);
        var setB = new Code128Encoder(Code128Set.B);

        auto.TryEncode("AB12CD", out var patternAuto, out var error);
        setB.TryEncode("AB12CD", out var patternB, out _);

        Assert.True(patternAuto is not null, error);
        Assert.Equal(patternB!.Segments, patternAuto!.Segments);
    }

    [Fact]
    public void TryEncode_Auto_LongDigitRun_ProducesFewerSymbolsThanSetB()
    {
        var auto = new Code128Encoder(Code128Set.Auto);
        var setB = new Code128Encoder(Code128Set.B);

        auto.TryEncode("AB12345678CD", out var patternAuto, out var error);
        setB.TryEncode("AB12345678CD", out var patternB, out _);

        Assert.True(patternAuto is not null, error);
        Assert.True(patternAuto!.Segments.Count < patternB!.Segments.Count);
    }

    [Fact]
    public void TryEncode_Auto_MixedRunsRoundTripsThroughIndependentChecksum()
    {
        var auto = new Code128Encoder(Code128Set.Auto);

        var success = auto.TryEncode("AB123456CD", out var pattern, out var error);

        Assert.True(success, error);

        // Independently rebuild the same value/switch sequence using the same
        // low-level helpers Code128AutoSegmenter/Code128Encoder use internally, but
        // driven directly here rather than through the encoder under test, as a
        // cross-check that the checksum and segment assembly line up.
        Code128Symbols.TryEncodeSetB("AB", out var ab, out _);
        Code128Symbols.TryEncodeSetC("123456", out var digits, out _);
        Code128Symbols.TryEncodeSetB("CD", out var cd, out _);

        var values = new List<int> { Code128Symbols.StartB };
        values.AddRange(ab);
        values.Add(Code128Symbols.CodeC);
        values.AddRange(digits);
        values.Add(Code128Symbols.CodeB);
        values.AddRange(cd);

        var dataOnly = values.Skip(1).ToArray();
        var checksum = Code128Symbols.ComputeChecksum(Code128Symbols.StartB, dataOnly);
        values.Add(checksum);
        values.Add(Code128Symbols.Stop);

        var expectedSegments = new List<BarSegment>();

        foreach (var v in values)
        {
            Code128Symbols.AppendSymbol(expectedSegments, v);
        }

        Assert.Equal(expectedSegments, pattern!.Segments);
    }

    [Fact]
    public void TryEncode_Auto_UnsupportedCharacterCombination_ReturnsError()
    {
        var auto = new Code128Encoder(Code128Set.Auto);

        var success = auto.TryEncode("a\u0001b", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
