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
