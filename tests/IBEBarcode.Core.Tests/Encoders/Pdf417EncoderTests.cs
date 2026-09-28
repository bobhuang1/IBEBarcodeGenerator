using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Pdf417EncoderTests
{
    private readonly Pdf417Encoder _encoder = new();

    [Fact]
    public void TryEncode_ShortValue_ProducesValidSymbol()
    {
        var success = _encoder.TryEncode("Hello", out var matrix, out var error);

        Assert.True(success, error);
        Assert.NotNull(matrix);
        Assert.True(matrix!.Width >= 69);
        Assert.True(matrix.Height >= 3);
        Assert.Equal(BarcodeSymbology.Pdf417, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_WidthMatchesColumnFormula()
    {
        _encoder.TryEncode("Hello, PDF417!", out var matrix, out _);

        var remainder = (matrix!.Width - 69) % 17;
        Assert.Equal(0, remainder);
    }

    [Fact]
    public void TryEncode_StartOfEveryRowMatchesStartPattern()
    {
        _encoder.TryEncode("Hello", out var matrix, out _);

        for (var y = 0; y < matrix!.Height; y++)
        {
            Assert.True(matrix[0, y]);
        }
    }

    [Fact]
    public void TryEncode_CharacterAbove255_ReturnsError()
    {
        var success = _encoder.TryEncode("cafሴ", out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_EmptyValue_ReturnsError()
    {
        var success = _encoder.TryEncode("", out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }
}
