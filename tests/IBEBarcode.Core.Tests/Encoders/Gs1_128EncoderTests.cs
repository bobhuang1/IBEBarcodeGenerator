using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Gs1_128EncoderTests
{
    private readonly Gs1_128Encoder _encoder = new();

    [Fact]
    public void TryEncode_Gtin14ElementString_Succeeds()
    {
        // AI "01" + 12-digit item reference + check digit = 14 digits, GS1's classic GTIN example
        var success = _encoder.TryEncode("00012345678905", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("00012345678905", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Gs1_128, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("00012345678905", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("00012345678905", out var pattern, out _);

        // start(6) + FNC1(6) + 7 digit-pairs(42) + checksum(6) + stop(7) = 67
        Assert.Equal(67, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_StartsWithSetCStartSymbol()
    {
        _encoder.TryEncode("00012345678905", out var pattern, out _);

        var startWidths = pattern!.Segments.Take(6).Select(s => s.WidthUnits).ToArray();

        // Set C start symbol (value 105) pattern, same table entry Code128Encoder(Set C) would use
        Assert.Equal(new[] { 2, 1, 1, 2, 3, 2 }, startWidths);
    }

    [Fact]
    public void TryEncode_OddLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("0001234X678905", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_ParenthesizedSingleFixedAi_Succeeds()
    {
        var success = _encoder.TryEncode("(01)00012345678905", out var pattern, out var error);

        Assert.True(success, error);
        Assert.NotNull(pattern);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ParenthesizedMixedNumericAndAlphanumeric_MatchesIndependentlyBuiltSequence()
    {
        var success = _encoder.TryEncode("(01)00012345678905(10)ABC-123(17)251231", out var pattern, out var error);

        Assert.True(success, error);

        // Independently rebuild the expected symbol sequence using the same low-level
        // helpers, driven directly here rather than through the code under test:
        // FNC1, then "0100012345678905" (AI 01 is fixed-length: no separator follows),
        // then a switch to Set B for "10ABC-123" (AI 10 is variable-length and not last,
        // so FNC1 separates it from what follows), then a switch back to Set C for
        // "17251231" (AI 17 is fixed-length and last).
        var values = new List<int> { Code128Symbols.Fnc1 };
        Code128Symbols.TryEncodeSetC("0100012345678905", out var ai01, out _);
        values.AddRange(ai01);
        values.Add(Code128Symbols.CodeB);
        Code128Symbols.TryEncodeSetB("10ABC-123", out var ai10, out _);
        values.AddRange(ai10);
        values.Add(Code128Symbols.Fnc1);
        values.Add(Code128Symbols.CodeC);
        Code128Symbols.TryEncodeSetC("17251231", out var ai17, out _);
        values.AddRange(ai17);

        var checksum = Code128Symbols.ComputeChecksum(Code128Symbols.StartC, values.ToArray());

        var expectedSegments = new List<BarSegment>();
        Code128Symbols.AppendSymbol(expectedSegments, Code128Symbols.StartC);

        foreach (var v in values)
        {
            Code128Symbols.AppendSymbol(expectedSegments, v);
        }

        Code128Symbols.AppendSymbol(expectedSegments, checksum);
        Code128Symbols.AppendSymbol(expectedSegments, Code128Symbols.Stop);

        Assert.Equal(expectedSegments, pattern!.Segments);
    }

    [Fact]
    public void TryEncode_ParenthesizedVariableLengthLast_NoTrailingSeparator()
    {
        var success = _encoder.TryEncode("(01)00012345678905(21)SN42", out var pattern, out var error);

        Assert.True(success, error);

        var values = new List<int> { Code128Symbols.Fnc1 };
        Code128Symbols.TryEncodeSetC("0100012345678905", out var ai01, out _);
        values.AddRange(ai01);
        values.Add(Code128Symbols.CodeB);
        Code128Symbols.TryEncodeSetB("21SN42", out var ai21, out _);
        values.AddRange(ai21);

        var checksum = Code128Symbols.ComputeChecksum(Code128Symbols.StartC, values.ToArray());

        var expectedSegments = new List<BarSegment>();
        Code128Symbols.AppendSymbol(expectedSegments, Code128Symbols.StartC);

        foreach (var v in values)
        {
            Code128Symbols.AppendSymbol(expectedSegments, v);
        }

        Code128Symbols.AppendSymbol(expectedSegments, checksum);
        Code128Symbols.AppendSymbol(expectedSegments, Code128Symbols.Stop);

        Assert.Equal(expectedSegments, pattern!.Segments);
    }

    [Fact]
    public void TryEncode_ParenthesizedUnknownAi_ReturnsError()
    {
        // "9999" does not exist in GS1's official AI table (unlike "99", which does).
        var success = _encoder.TryEncode("(9999)12345", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
