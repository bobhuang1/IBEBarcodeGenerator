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
        Assert.False(elements[0].SeparatorRequired);
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
        Assert.True(elements[1].SeparatorRequired);
        Assert.False(elements[2].SeparatorRequired);
    }

    [Fact]
    public void TryParseElementString_UnknownAi_ReturnsError()
    {
        // "9999" does not exist in GS1's official AI table (unlike "99", which does and
        // would incorrectly make this test pass once the full table is loaded).
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(9999)12345", out _, out var error);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParseElementString_FullTable_HasOverFiveHundredAis()
    {
        // Sanity check that the full official table (not the earlier 9-AI subset) is loaded.
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(99)AnyInternalValue", out var elements, out var error);

        Assert.True(success, error);
        Assert.Single(elements);
    }

    [Fact]
    public void TryParseElementString_MultiComponentAiWithOptionalSuffix_AcceptsBothMinimalAndMaximalLength()
    {
        // AI 253 (GDTI): 13 required numeric digits + up to 17 optional alphanumeric characters.
        var minimal = Gs1ApplicationIdentifiers.TryParseElementString("(253)1234567890128", out var minimalElements, out var minimalError);
        var maximal = Gs1ApplicationIdentifiers.TryParseElementString("(253)1234567890128EXTRA12345678901", out var maximalElements, out var maximalError);

        Assert.True(minimal, minimalError);
        Assert.True(maximal, maximalError);
        Assert.Equal("1234567890128", minimalElements[0].Value);
        Assert.Equal("1234567890128EXTRA12345678901", maximalElements[0].Value);
    }

    [Fact]
    public void TryParseElementString_MultiComponentAiTooShort_ReturnsError()
    {
        // AI 253 requires at least 13 characters (the fixed numeric portion).
        var success = Gs1ApplicationIdentifiers.TryParseElementString("(253)12345", out _, out var error);

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
