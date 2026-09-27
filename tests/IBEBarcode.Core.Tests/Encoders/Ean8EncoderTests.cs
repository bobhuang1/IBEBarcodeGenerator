using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Ean8EncoderTests
{
    private readonly Ean8Encoder _encoder = new();

    [Fact]
    public void TryEncode_SevenDigits_ComputesCheckDigitAndSucceeds()
    {
        var success = _encoder.TryEncode("9638507", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("96385074", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Ean8, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_EightDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("96385070", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("check digit", error);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("9638507", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("9638507", out var pattern, out _);

        // start guard(3) + 4 left digits(16) + middle guard(5) + 4 right digits(16) + end guard(3) = 43
        Assert.Equal(43, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
