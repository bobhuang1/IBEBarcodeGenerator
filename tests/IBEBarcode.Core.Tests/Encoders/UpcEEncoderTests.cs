using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class UpcEEncoderTests
{
    private readonly UpcEEncoder _encoder = new();

    [Fact]
    public void TryEncode_SixDigitPayload_MatchesRealWorldExample()
    {
        // Kellogg's Corn Flakes: UPC-E 381100 (system 0) -> UPC-A data 03800000110, check 9
        var success = _encoder.TryEncode("381100", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("03811009", pattern!.Value);
        Assert.Equal(BarcodeSymbology.UpcE, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SevenDigitsWithNumberSystem_SameResult()
    {
        var success = _encoder.TryEncode("0381100", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("03811009", pattern!.Value);
    }

    [Fact]
    public void TryEncode_EightDigitsWithCorrectCheckDigit_Succeeds()
    {
        var success = _encoder.TryEncode("03811009", out var pattern, out var error);

        Assert.True(success);
        Assert.Equal("03811009", pattern!.Value);
    }

    [Fact]
    public void TryEncode_EightDigitsWithWrongCheckDigit_ReturnsError()
    {
        var success = _encoder.TryEncode("03811001", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("381100", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("381100", out var pattern, out _);

        // start(3) + 6 digits(24) + end(6) = 33
        Assert.Equal(33, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_EndGuardIsSixNarrowElements()
    {
        _encoder.TryEncode("381100", out var pattern, out _);

        var endGuard = pattern!.Segments.Skip(27).Take(6).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 1 }, endGuard);
    }

    [Fact]
    public void TryEncode_InvalidNumberSystem_ReturnsError()
    {
        var success = _encoder.TryEncode("2381100", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("12345", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("38110X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
