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

    [Fact]
    public void TryEncode_ExplicitHighErrorCorrectionLevel_ProducesLargerSymbolThanDefault()
    {
        var level8 = new Pdf417Encoder(8);

        _encoder.TryEncode("Hello", out var defaultMatrix, out _);
        var success = level8.TryEncode("Hello", out var level8Matrix, out var error);

        Assert.True(success, error);
        // Level 8 uses 512 error-correction codewords vs. the auto-selected level's much
        // smaller count, so it needs a larger symbol for the same short input.
        Assert.True(level8Matrix!.Height > defaultMatrix!.Height || level8Matrix.Width > defaultMatrix.Width);
    }

    [Fact]
    public void TryEncode_ExplicitLevelOutOfRange_ReturnsError()
    {
        var invalid = new Pdf417Encoder(9);

        var success = invalid.TryEncode("Hello", out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_Compact_ProducesNarrowerSymbolThanStandard()
    {
        var compact = new Pdf417Encoder(compact: true);

        _encoder.TryEncode("Hello, PDF417!", out var standardMatrix, out _);
        var success = compact.TryEncode("Hello, PDF417!", out var compactMatrix, out var error);

        Assert.True(success, error);
        Assert.Equal(standardMatrix!.Height, compactMatrix!.Height);
        Assert.True(compactMatrix.Width < standardMatrix.Width);
    }

    [Fact]
    public void TryEncode_Compact_WidthMatchesCompactColumnFormula()
    {
        var compact = new Pdf417Encoder(compact: true);

        compact.TryEncode("Hello, PDF417!", out var matrix, out _);

        var remainder = (matrix!.Width - 35) % 17;
        Assert.Equal(0, remainder);
    }
}
