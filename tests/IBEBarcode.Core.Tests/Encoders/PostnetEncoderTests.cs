using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class PostnetEncoderTests
{
    private readonly PostnetEncoder _encoder = new();

    [Fact]
    public void TryEncode_FiveDigitZip_ComputesCheckDigitAndSucceeds()
    {
        // digits 1+2+3+4+5=15, check=(10-15%10)%10=5
        var success = _encoder.TryEncode("12345", out var pattern, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("123455", pattern!.Value);
        Assert.Equal(BarcodeSymbology.Postnet, _encoder.Symbology);
    }

    [Fact]
    public void TryEncode_ProducesGuardBarsAtStartAndEnd()
    {
        _encoder.TryEncode("12345", out var pattern, out _);

        Assert.True(pattern!.Bars[0].IsTall);
        Assert.True(pattern.Bars[^1].IsTall);
    }

    [Fact]
    public void TryEncode_ProducesExpectedBarCount()
    {
        _encoder.TryEncode("12345", out var pattern, out _);

        // guard(1) + 6 digits(5 bars each = 30) + guard(1) = 32
        Assert.Equal(32, pattern!.Bars.Count);
    }

    [Fact]
    public void TryEncode_DigitZero_ProducesTwoOfFiveTallPattern()
    {
        var success = _encoder.TryEncode("00000", out var pattern, out _);

        Assert.True(success);
        // digit 0 = 1 1 0 0 0 (weights 7,4,2,1,0)
        var firstDigitBars = pattern!.Bars.Skip(1).Take(5).Select(b => b.IsTall).ToArray();
        Assert.Equal(new[] { true, true, false, false, false }, firstDigitBars);
    }

    [Theory]
    [InlineData('0')]
    [InlineData('1')]
    [InlineData('2')]
    [InlineData('3')]
    [InlineData('4')]
    [InlineData('5')]
    [InlineData('6')]
    [InlineData('7')]
    [InlineData('8')]
    [InlineData('9')]
    public void TryEncode_EveryDigit_HasExactlyTwoTallBars(char digitChar)
    {
        var success = _encoder.TryEncode(new string(digitChar, 5), out var pattern, out _);

        Assert.True(success);

        var firstDigitBars = pattern!.Bars.Skip(1).Take(5).Count(b => b.IsTall);
        Assert.Equal(2, firstDigitBars);
    }

    [Fact]
    public void TryEncode_WrongLength_ReturnsError()
    {
        var success = _encoder.TryEncode("123", out var pattern, out var error);

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
