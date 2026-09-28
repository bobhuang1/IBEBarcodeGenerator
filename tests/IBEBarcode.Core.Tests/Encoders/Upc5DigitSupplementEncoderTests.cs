using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Upc5DigitSupplementEncoderTests
{
    private readonly Upc5DigitSupplementEncoder _encoder = new();

    [Fact]
    public void TryEncode_FiveDigits_Succeeds()
    {
        var success = _encoder.TryEncode("52495", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("52495", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Upc5DigitSupplement, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("52495", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("52495", out var pattern, out _);

        // start(3) + 5 digits(20) + 4 separators(8) = 31
        Assert.Equal(31, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_ChecksumMatchesHandComputedValue()
    {
        // "52495": d0=5,d1=2,d2=4,d3=9,d4=5
        // checksum = (3*(5+4+5) + 9*(2+9)) mod 10 = (3*14 + 9*11) mod 10 = (42+99) mod 10 = 141 mod 10 = 1
        // CHECK_DIGIT_ENCODINGS[1] = 0x14 = 0b10100 -> digit0=G,digit1=L,digit2=G,digit3=L,digit4=L
        var success = _encoder.TryEncode("52495", out var pattern, out _);

        Assert.True(success);

        var digit0Widths = pattern!.Segments.Skip(3).Take(4).Select(s => s.WidthUnits).ToArray();
        // digit '5' L-code {1,2,3,1}; G-code (reversed) {1,3,2,1}
        Assert.Equal(new[] { 1, 3, 2, 1 }, digit0Widths);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("1234", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("1234X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
