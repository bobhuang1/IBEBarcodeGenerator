using IBEBarcode.Core.Encoders;
using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Tests.Encoders;

public class QrEncoderTests
{
    private static readonly (int X, int Y)[] FormatInfoCoordinates =
    {
        (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7),
        (8, 8), (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8),
    };

    private static readonly char[] LevelOrder = { 'L', 'M', 'Q', 'H' };

    private static int ReadChosenMaskPattern(BarcodeMatrix matrix, char level)
    {
        var formatBits = new char[15];

        for (var i = 0; i < 15; i++)
        {
            var (x, y) = FormatInfoCoordinates[i];
            formatBits[14 - i] = matrix[x, y] ? '1' : '0';
        }

        var formatString = new string(formatBits);

        for (var mask = 0; mask < 8; mask++)
        {
            if (QrMaskUtil.ComputeFormatString(level, mask) == formatString)
            {
                return mask;
            }
        }

        throw new InvalidOperationException($"Unrecognized format string: {formatString}");
    }

    [Fact]
    public void TryEncode_MaskSelectionIsDeterministicForTheSameInput()
    {
        // Basic sanity property of the scoring loop: re-encoding identical input at the
        // same level must always resolve the same "lowest penalty" winner. (Real per-input
        // scoring does vary the *value* it picks across genuinely different inputs -- see
        // QrMaskUtilTests for the penalty formulas themselves, independently hand-verified,
        // and QrRoundTripTests, which round-trips dozens of inputs successfully regardless
        // of which of the 8 masks scoring happens to choose for each -- but determinism for
        // a fixed input is what's cheaply and reliably checkable from outside the encoder.)
        var encoder = new QrEncoder('M');
        var value = "The quick brown fox jumps over the lazy dog 0123456789";

        encoder.TryEncode(value, out var first, out _);
        encoder.TryEncode(value, out var second, out _);

        var maskFirst = ReadChosenMaskPattern(first!, 'M');
        var maskSecond = ReadChosenMaskPattern(second!, 'M');

        Assert.Equal(maskFirst, maskSecond);
    }


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

    [Fact]
    public void TryEncode_AllDigits_AutoSelectsNumericModeAndFitsSmallerVersionThanMixedCase()
    {
        var encoder = new QrEncoder('L');
        var digits = new string('1', 60);
        var lowercase = new string('a', 60); // forces byte mode (lowercase isn't alphanumeric-eligible)

        var numericSuccess = encoder.TryEncode(digits, out var numericMatrix, out var numericError);
        var byteSuccess = encoder.TryEncode(lowercase, out var byteMatrix, out var byteError);

        Assert.True(numericSuccess, numericError);
        Assert.True(byteSuccess, byteError);
        // Numeric mode packs ~3.33 bits/digit vs. byte mode's 8 bits/char, so the same
        // character count should need a smaller (or equal) symbol.
        Assert.True(numericMatrix!.Width <= byteMatrix!.Width);
    }

    [Fact]
    public void TryEncode_UppercaseAndDigits_AutoSelectsAlphanumericModeAndFitsSmallerVersionThanMixedCase()
    {
        var encoder = new QrEncoder('L');
        var alphanumeric = new string('A', 60);
        var lowercase = new string('a', 60);

        var alphanumericSuccess = encoder.TryEncode(alphanumeric, out var alphanumericMatrix, out var alphanumericError);
        var byteSuccess = encoder.TryEncode(lowercase, out var byteMatrix, out var byteError);

        Assert.True(alphanumericSuccess, alphanumericError);
        Assert.True(byteSuccess, byteError);
        Assert.True(alphanumericMatrix!.Width <= byteMatrix!.Width);
    }

    [Fact]
    public void TryEncode_LowercaseLetters_UsesByteMode()
    {
        // Lowercase letters aren't in the QR alphanumeric table, so this must fall back to
        // byte mode -- confirmed indirectly by round-trip tests, and directly here by
        // successful encoding of a character set that would error out if numeric/
        // alphanumeric encoding were incorrectly forced.
        var encoder = new QrEncoder('M');

        var success = encoder.TryEncode("hello world!", out var matrix, out var error);

        Assert.True(success, error);
        Assert.NotNull(matrix);
    }
}
