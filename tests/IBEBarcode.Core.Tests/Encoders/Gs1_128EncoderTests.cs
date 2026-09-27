using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Gs1_128EncoderTests
{
    private readonly Gs1_128Encoder _encoder = new();

    [Fact]
    public void TryEncode_Gtin14ElementString_Succeeds()
    {
        // AI "01" + 12-digit item reference + check digit = 14 digits, GS1's classic GTIN example
        var success = _encoder.TryEncode("00012345678905", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("00012345678905", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Gs1_128, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("00012345678905", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("00012345678905", out var pattern, out _);

        // start(6) + FNC1(6) + 7 digit-pairs(42) + checksum(6) + stop(7) = 67
        Assert.Equal(67, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_StartsWithSetCStartSymbol()
    {
        _encoder.TryEncode("00012345678905", out var pattern, out _);

        var startWidths = pattern!.Segments.Take(6).Select(s => s.WidthUnits).ToArray();

        // Set C start symbol (value 105) pattern, same table entry Code128Encoder(Set C) would use
        Assert.Equal(new[] { 2, 1, 1, 2, 3, 2 }, startWidths);
    }

    [Fact]
    public void TryEncode_OddLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("0001234X678905", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }
}
