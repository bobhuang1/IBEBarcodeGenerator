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
