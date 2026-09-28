using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class DataMatrixEncoderTests
{
    private readonly DataMatrixEncoder _encoder = new();

    [Fact]
    public void TryEncode_ShortValue_SelectsSmallestSymbol()
    {
        var success = _encoder.TryEncode("Hi", out var matrix, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(10, matrix!.Width);
        Assert.Equal(10, matrix.Height);
        Assert.Equal(BarcodeSymbology.DataMatrix, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_LongerValue_SelectsLargerSymbol()
    {
        var success = _encoder.TryEncode("Hello, Data Matrix!", out var matrix, out _);

        Assert.True(success);
        Assert.True(matrix!.Width > 10);
    }

    [Fact]
    public void TryEncode_TopRowAlternatesStartingDark()
    {
        _encoder.TryEncode("Hi", out var matrix, out _);

        Assert.True(matrix![0, 0]);
        Assert.False(matrix[1, 0]);
        Assert.True(matrix[2, 0]);
    }

    [Fact]
    public void TryEncode_BottomRowIsSolidDark()
    {
        _encoder.TryEncode("Hi", out var matrix, out _);

        var size = matrix!.Height;

        for (var x = 0; x < matrix.Width; x++)
        {
            Assert.True(matrix[x, size - 1]);
        }
    }

    [Fact]
    public void TryEncode_LeftColumnIsSolidDark()
    {
        _encoder.TryEncode("Hi", out var matrix, out _);

        for (var y = 0; y < matrix!.Height; y++)
        {
            Assert.True(matrix[0, y]);
        }
    }

    [Fact]
    public void TryEncode_ValueTooLargeForSupportedRange_ReturnsError()
    {
        // Exceeds even the largest supported size (dataCapacity 1304, interior 20x20 x 36
        // regions).
        var success = _encoder.TryEncode(new string('A', 2000), out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_LargeMultiRegionValue_ProducesCorrectlySizedSymbol()
    {
        // 300 bytes needs a size beyond dataCapacity 44 -- forces a genuine multi-region
        // symbol (interior 14x14, 2x2 = 4 regions, dataCapacity 62 would already suffice
        // for less, but at 300 bytes this reaches a much larger multi-region, multi-block
        // size).
        var value = new string('A', 300);

        var success = _encoder.TryEncode(value, out var matrix, out var error);

        Assert.True(success, error);
        Assert.NotNull(matrix);
        // Symbol must be square-ish and reasonably large; the precise size depends on the
        // table, so just sanity-check it grew well beyond the single-region 26x26 max.
        Assert.True(matrix!.Width > 26);
    }

    [Fact]
    public void TryEncode_CharacterAboveAscii127_ReturnsError()
    {
        var success = _encoder.TryEncode("café", out var matrix, out var error);

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
    public void TryEncode_DefaultPrefersSquareOverTiedCapacityRectangle()
    {
        // "ABCD" (4 bytes) fits both the square 10x10 (capacity 5) and rectangular 16x6
        // (also capacity 5) sizes -- default behavior should prefer square, matching
        // ZXing's own default (FORCE_NONE) tie-breaking.
        var success = _encoder.TryEncode("ABCD", out var matrix, out var error);

        Assert.True(success, error);
        Assert.Equal(matrix!.Width, matrix.Height);
        Assert.Equal(12, matrix.Width); // interior 10 + 2 border
    }

    [Fact]
    public void TryEncode_PreferRectangular_SelectsTiedCapacityRectangleInstead()
    {
        var rectangularEncoder = new DataMatrixEncoder(preferRectangular: true);

        var success = rectangularEncoder.TryEncode("ABCD", out var matrix, out var error);

        Assert.True(success, error);
        Assert.NotEqual(matrix!.Width, matrix.Height);
        Assert.Equal(18, matrix.Width);  // interior 16 + 2 border
        Assert.Equal(8, matrix.Height);  // interior 6 + 2 border
    }

    [Fact]
    public void TryEncode_ValueRequiringOnlyRectangularCapacityTier_SelectsRectangleRegardlessOfPreference()
    {
        // 13-16 bytes only fits the rectangular 24x10 size (capacity 16) -- no square
        // alternative exists at that exact capacity tier, so it's picked either way.
        var value = new string('A', 15);

        var success = _encoder.TryEncode(value, out var matrix, out var error);

        Assert.True(success, error);
        Assert.NotEqual(matrix!.Width, matrix.Height);
        Assert.Equal(26, matrix.Width);  // interior 24 + 2 border
        Assert.Equal(12, matrix.Height); // interior 10 + 2 border
    }
}
