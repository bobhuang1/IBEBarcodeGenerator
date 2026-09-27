using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Ean13EncoderTests
{
    private readonly Ean13Encoder _encoder = new();

    [Fact]
    public void TryEncode_TwelveDigits_ComputesCheckDigitAndSucceeds()
    {
        var success = _encoder.TryEncode("978030640615", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("9780306406157", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Ean13, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_ThirteenDigitsWithCorrectCheckDigit_Succeeds()
    {
        var success = _encoder.TryEncode("9780306406157", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("9780306406157", pattern!.Value);
    }

    [Fact]
    public void TryEncode_ThirteenDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("9780306406150", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("check digit", error);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("978030640615", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("978030640615", out var pattern, out _);

        // start guard(3) + 6 left digits(24) + middle guard(5) + 6 right digits(24) + end guard(3) = 59
        Assert.Equal(59, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("97803064061X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
