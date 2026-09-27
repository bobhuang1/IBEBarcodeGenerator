using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class IsbnEncoderTests
{
    private readonly IsbnEncoder _encoder = new();

    [Fact]
    public void TryEncode_NineCoreDigits_ProducesBooklandEan13()
    {
        var success = _encoder.TryEncode("030640615", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("9780306406157", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Isbn, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_TenDigitIsbnWithHyphens_StripsHyphensAndOwnCheckChar()
    {
        var success = _encoder.TryEncode("0-306-40615-2", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("9780306406157", pattern!.Value);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("12345", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
