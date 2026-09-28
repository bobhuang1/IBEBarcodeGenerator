using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class Gs1ApplicationIdentifiersTests
{
    [Fact]
    public void TryParseElementString_SingleFixedLengthAi_Succeeds()
    {
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(01)00012345678905", out var elements, out var error);

        Assert.True(success, error);
        Assert.Single(elements);
        Assert.Equal("01", elements[0].Ai);
        Assert.Equal("00012345678905", elements[0].Value);
        Assert.True(elements[0].IsFixedLength);
    }

    [Fact]
    public void TryParseElementString_MultipleAisIncludingAlphanumeric_Succeeds()
    {
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(01)00012345678905(10)ABC-123(17)251231", out var elements, out var error);

        Assert.True(success, error);
        Assert.Equal(3, elements.Count);
        Assert.Equal(("01", "00012345678905"), (elements[0].Ai, elements[0].Value));
        Assert.Equal(("10", "ABC-123"), (elements[1].Ai, elements[1].Value));
        Assert.Equal(("17", "251231"), (elements[2].Ai, elements[2].Value));
        Assert.False(elements[1].IsFixedLength);
        Assert.True(elements[2].IsFixedLength);
    }

    [Fact]
    public void TryParseElementString_UnknownAi_ReturnsError()
    {
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(99)12345", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParseElementString_WrongFixedLength_ReturnsError()
    {
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(01)123", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParseElementString_NonNumericForNumericAi_ReturnsError()
    {
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(01)0001234X678905", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParseElementString_VariableLengthTooLong_ReturnsError()
    {
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(10)" + new string('A', 21), out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParseElementString_MissingClosingParen_ReturnsError()
    {
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(01123", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }
}
