using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class QrEncoderTests
{
    [Fact]
    public void TryEncode_ShortValue_ProducesVersion1Size()
    {
        var encoder = new QrEncoder('M');

        var success = encoder.TryEncode("HI", out var matrix, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(21, matrix!.Width);
        Assert.Equal(21, matrix.Height);
        Assert.Equal(BarcodeSymbology.QrCode, encoder.Symbology);
    }

    [Fact]
    public void TryEncode_LargerValue_SelectsLargerVersion()
    {
        var encoder = new QrEncoder('L');

        // 60 bytes needs more than version 1's 19-byte capacity at level L.
        var success = encoder.TryEncode(new string('A', 60), out var matrix, out _);

        Assert.True(success);
        Assert.True(matrix!.Width > 21);
    }

    [Fact]
    public void TryEncode_TopLeftFinderPattern_MatchesKnownShape()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        // Outer ring dark, inner ring light, center 3x3 dark, per the standard finder pattern.
        Assert.True(matrix![0, 0]);
        Assert.True(matrix[6, 0]);
        Assert.True(matrix[0, 6]);
        Assert.True(matrix[6, 6]);
        Assert.False(matrix[1, 1]);
        Assert.True(matrix[3, 3]);
        Assert.False(matrix[7, 0]);
        Assert.False(matrix[0, 7]);
    }

    [Fact]
    public void TryEncode_AllThreeFinderPatternsPresent()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        var size = matrix!.Width;

        Assert.True(matrix[0, 0]);
        Assert.True(matrix[size - 7, 0]);
        Assert.True(matrix[0, size - 7]);
    }

    [Fact]
    public void TryEncode_TimingPatternAlternates()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        Assert.True(matrix![8, 6]);
        Assert.False(matrix[9, 6]);
        Assert.True(matrix[10, 6]);
    }

    [Fact]
    public void TryEncode_DarkModuleIsAlwaysDark()
    {
        var encoder = new QrEncoder('M');
        encoder.TryEncode("HI", out var matrix, out _);

        Assert.True(matrix![8, matrix.Height - 8]);
    }

    [Fact]
    public void TryEncode_ValueTooLargeForSupportedRange_ReturnsError()
    {
        var encoder = new QrEncoder('H');

        // Exceeds even version 40 (the largest QR version) at level H (the lowest-capacity level).
        var success = encoder.TryEncode(new string('A', 2000), out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_EmptyValue_ReturnsError()
    {
        var encoder = new QrEncoder();

        var success = encoder.TryEncode("", out var matrix, out var error);

        Assert.False(success);
        Assert.Null(matrix);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_InvalidErrorCorrectionLevel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrEncoder('X'));
    }
}
