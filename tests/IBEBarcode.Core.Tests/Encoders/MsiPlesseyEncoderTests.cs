using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class MsiPlesseyEncoderTests
{
    private readonly MsiPlesseyEncoder _encoder = new();

    [Fact]
    public void TryEncode_ValidDigits_Succeeds()
    {
        var success = _encoder.TryEncode("1234", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("1234", pattern!.HumanReadableText);
        Assert.Equal(BarcodeSymbology.MsiPlessey, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("90210", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_DigitZero_ProducesAllNarrowBarWideSpaceBits()
    {
        var success = _encoder.TryEncode("0", out var pattern, out _);

        Assert.True(success);
        // start (2) + digit 0 = four 0-bits, each bar+space (8) + stop (3) = 13 segments
        Assert.Equal(13, pattern!.Segments.Count);

        var digitSegments = pattern.Segments.Skip(2).Take(8).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 2, 1, 2, 1, 2, 1, 2 }, digitSegments);
    }

    [Fact]
    public void TryEncode_DigitNine_ProducesBcd1001BitPattern()
    {
        var success = _encoder.TryEncode("9", out var pattern, out _);

        Assert.True(success);

        var digitSegments = pattern!.Segments.Skip(2).Take(8).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 2, 1, 1, 2, 1, 2, 2, 1 }, digitSegments);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("12x4", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("x", error);
    }

    [Theory]
    [InlineData("1234567", '4')]
    [InlineData("0", '0')]
    public void ComputeCheckDigit_KnownValues_ReturnsExpectedDigit(string digits, char expected)
    {
        var checkDigit = MsiPlesseyEncoder.ComputeCheckDigit(digits);

        Assert.Equal(expected, checkDigit);
    }
}
