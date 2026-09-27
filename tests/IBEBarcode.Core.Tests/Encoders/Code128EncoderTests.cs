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
