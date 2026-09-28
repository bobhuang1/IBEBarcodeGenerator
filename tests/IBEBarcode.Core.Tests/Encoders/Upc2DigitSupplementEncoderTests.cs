using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Upc2DigitSupplementEncoderTests
{
    private readonly Upc2DigitSupplementEncoder _encoder = new();

    [Fact]
    public void TryEncode_TwoDigits_Succeeds()
    {
        // 42 % 4 = 2 = 0b10 -> digit0=G, digit1=L
        var success = _encoder.TryEncode("42", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("42", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Upc2DigitSupplement, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_SegmentsAlwaysAlternateBarSpace()
    {
        _encoder.TryEncode("42", out var pattern, out _);

        for (var i = 1; i < pattern!.Segments.Count; i++)
        {
            Assert.NotEqual(pattern.Segments[i - 1].IsBar, pattern.Segments[i].IsBar);
        }
    }

    [Fact]
    public void TryEncode_ProducesExpectedSegmentCount()
    {
        _encoder.TryEncode("42", out var pattern, out _);

        // start(3) + digit0(4) + separator(2) + digit1(4) = 13
        Assert.Equal(13, pattern!.Segments.Count);
    }

    [Fact]
    public void TryEncode_StartGuardMatchesKnownPattern()
    {
        _encoder.TryEncode("42", out var pattern, out _);

        var startGuard = pattern!.Segments.Take(3).Select(s => s.WidthUnits).ToArray();
        Assert.Equal(new[] { 1, 1, 2 }, startGuard);
    }

    [Fact]
    public void TryEncode_ParitySelectsCorrectLOrGCode()
    {
        // 42 % 4 = 2 = 0b10 -> digit '4' uses G-code, digit '2' uses L-code
        var success = _encoder.TryEncode("42", out var pattern, out _);

        Assert.True(success);

        var digit0Widths = pattern!.Segments.Skip(3).Take(4).Select(s => s.WidthUnits).ToArray();
        var digit1Widths = pattern.Segments.Skip(9).Take(4).Select(s => s.WidthUnits).ToArray();

        // digit '4' L-code widths {1,1,3,2}; G-code is the reverse {2,3,1,1}
        Assert.Equal(new[] { 2, 3, 1, 1 }, digit0Widths);
        // digit '2' L-code widths {2,1,2,2} (used directly, since parity bit0=0=L)
        Assert.Equal(new[] { 2, 1, 2, 2 }, digit1Widths);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("4", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryEncode_NonDigitCharacter_ReturnsError()
    {
        var success = _encoder.TryEncode("4X", out var pattern, out var error);

        Assert.False(success);
        Assert.Null(pattern);
        Assert.Contains("X", error);
    }
}
