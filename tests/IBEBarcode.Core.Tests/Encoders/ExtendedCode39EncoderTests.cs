using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class ExtendedCode39EncoderTests
{
    private readonly ExtendedCode39Encoder _encoder = new();
    private readonly Code39Encoder _code39 = new();

    [Fact]
    public void TryEncode_BaseAlphabetOnly_MatchesPlainCode39()
    {
        var extendedSuccess = _encoder.TryEncode("HELLO123", out var extendedPattern, out var error);
        var plainSuccess = _code39.TryEncode("HELLO123", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Null(error);
        Assert.Equal("HELLO123", extendedPattern!.Value);
        Assert.Equal(plainPattern!.Segments, extendedPattern.Segments);
        Assert.Equal(BarcodeSymbology.Code39Extended, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_Lowercase_ExpandsToPlusShiftSequence()
    {
        var extendedSuccess = _encoder.TryEncode("hello", out var extendedPattern, out _);
        var plainSuccess = _code39.TryEncode("+H+E+L+L+O", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Equal(plainPattern!.Segments, extendedPattern!.Segments);
    }

    [Fact]
    public void TryEncode_PreservesCaseInValue()
    {
        _encoder.TryEncode("MixedCase", out var pattern, out _);

        Assert.Equal("MixedCase", pattern!.Value);
    }

    [Fact]
    public void TryEncode_PunctuationOutsideBaseAlphabet_ExpandsToSlashShiftSequence()
    {
        var extendedSuccess = _encoder.TryEncode("a!", out var extendedPattern, out _);
        var plainSuccess = _code39.TryEncode("+A/A", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Equal(plainPattern!.Segments, extendedPattern!.Segments);
    }

    [Fact]
    public void TryEncode_AtSignAndBacktickAndNul_ExpandToPercentShiftSequence()
    {
        var extendedSuccess = _encoder.TryEncode("@`\0", out var extendedPattern, out _);
        var plainSuccess = _code39.TryEncode("%V%W%U", out var plainPattern, out _);

        Assert.True(extendedSuccess);
        Assert.True(plainSuccess);
        Assert.Equal(plainPattern!.Segments, extendedPattern!.Segments);
    }

    [Fact]
    public void TryEncode_CharacterAboveAscii127_ReturnsError()
    {
        var success = _encoder.TryEncode("café", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_EmptyValue_ReturnsError()
    {
        var success = _encoder.TryEncode("", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
