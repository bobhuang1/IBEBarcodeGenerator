using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Tests.Encoders.Qr;

public class QrAlphanumericTests
{
    [Theory]
    [InlineData('0', 0)]
    [InlineData('9', 9)]
    [InlineData('A', 10)]
    [InlineData('Z', 35)]
    [InlineData(' ', 36)]
    [InlineData('$', 37)]
    [InlineData('%', 38)]
    [InlineData('*', 39)]
    [InlineData('+', 40)]
    [InlineData('-', 41)]
    [InlineData('.', 42)]
    [InlineData('/', 43)]
    [InlineData(':', 44)]
    public void TryGetCode_KnownCharacters_MatchesIsoTable(char ch, int expected)
    {
        var success = QrAlphanumeric.TryGetCode(ch, out var code);

        Assert.True(success);
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('!')]
    [InlineData('_')]
    public void TryGetCode_UnsupportedCharacters_ReturnsFalse(char ch)
    {
        var success = QrAlphanumeric.TryGetCode(ch, out _);

        Assert.False(success);
    }
}
