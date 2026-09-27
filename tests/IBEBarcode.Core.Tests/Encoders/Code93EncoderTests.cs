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
