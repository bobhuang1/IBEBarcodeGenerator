using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class UpcAEncoderTests
{
    private readonly UpcAEncoder _encoder = new();

    [Fact]
    public void TryEncode_ElevenDigits_ComputesCheckDigitAndSucceeds()
    {
        var success = _encoder.TryEncode("03600029145", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("036000291452", pattern!.Value);
        Assert.Equal(BarcodeSymbology.UpcA, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_TwelveDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("036000291450", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_ProducesSameSegmentsAsEquivalentEan13()
    {
        var upcSuccess = _encoder.TryEncode("03600029145", out var upcPattern, out _);
        var ean13Success = new Ean13Encoder().TryEncode("003600029145", out var ean13Pattern, out _);

        Assert.True(upcSuccess);
        Assert.True(ean13Success);
        Assert.Equal(ean13Pattern!.Segments, upcPattern!.Segments);
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
