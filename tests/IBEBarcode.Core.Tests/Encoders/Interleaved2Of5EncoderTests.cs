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
