using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class AztecEncoderTests
{
    private readonly AztecEncoder _encoder = new();

    [Fact]
    public void TryEncode_ShortValue_ProducesCompactSizedSymbol()
    {
        var success = _encoder.TryEncode("Hi", out var matrix, out var error);

        Assert.True(success, error);
        Assert.Contains(matrix!.Width, new[] { 15, 19, 23, 27 });
        Assert.Equal(matrix.Width, matrix.Height);
        Assert.Equal(BarcodeSymbology.Aztec, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_LongerValue_SelectsLargerSymbol()
    {
        _encoder.TryEncode("Hi", out var small, out _);
        _encoder.TryEncode("This is a somewhat longer test message for Aztec", out var large, out var error);

        Assert.True(large is not null, error);
        Assert.True(large!.Width >= small!.Width);
    }

    [Fact]
    public void TryEncode_CenterModuleIsDark()
    {
        _encoder.TryEncode("Hi", out var matrix, out _);

        var center = matrix!.Width / 2;
        Assert.True(matrix[center, center]);
    }

    [Fact]
    public void TryEncode_ValueTooLargeForCompactRange_ReturnsError()
    {
        var success = _encoder.TryEncode(new string('A', 200), out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
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
