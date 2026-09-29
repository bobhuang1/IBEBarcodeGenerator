using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Tests.Encoders.Qr;

public class QrKanjiTests
{
    [Fact]
    public void TryGetValue_FirstJisLevel1Kanji_MatchesHandComputedValue()
    {
        // U+4E9C ("Asia"/"second") is Shift-JIS 0x889F, the first JIS X 0208 Level-1
        // kanji and the canonical worked example in ISO/IEC 18004's own Kanji encoding
        // description. adjusted = 0x889F - 0x8140 = 0x075F = 1887; msb=7, lsb=95;
        // value = 7*0xC0 + 95 = 1439.
        var value = QrKanji.TryGetValue('亜');

        Assert.Equal(1439, value);
    }

    [Fact]
    public void TryGetValue_AsciiCharacter_ReturnsNull()
    {
        var value = QrKanji.TryGetValue('A');

        Assert.Null(value);
    }

    [Fact]
    public void FromValue_IsInverseOfTryGetValue()
    {
        var value = QrKanji.TryGetValue('亜');

        Assert.NotNull(value);
        Assert.Equal('亜', QrKanji.FromValue(value!.Value));
    }
}
