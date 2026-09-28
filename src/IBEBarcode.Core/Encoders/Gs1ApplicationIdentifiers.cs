namespace IBEBarcode.Core.Encoders;

internal static class Gs1ApplicationIdentifiers
{
    public sealed record Definition(bool FixedLength, int Length, bool Numeric);

    public sealed record Element(string Ai, string Value, bool IsFixedLength);

    // A deliberately small, documented subset of GS1 General Specifications Application
    // Identifiers -- common logistics/retail fields, not the full 100+ AI table.
    private static readonly Dictionary<string, Definition> Definitions = new()
    {
        ["00"] = new Definition(true, 18, true),   // SSCC
        ["01"] = new Definition(true, 14, true),   // GTIN
        ["10"] = new Definition(false, 20, false), // Batch/lot number
        ["11"] = new Definition(true, 6, true),    // Production date (YYMMDD)
        ["15"] = new Definition(true, 6, true),    // Best before date (YYMMDD)
        ["17"] = new Definition(true, 6, true),    // Expiration date (YYMMDD)
        ["20"] = new Definition(true, 2, true),    // Variant
        ["21"] = new Definition(false, 20, false), // Serial number
        ["30"] = new Definition(false, 8, true),   // Variable count
    };

    public static bool TryParseElementString(string value, out List<Element> elements, out string? error)
    {
        elements = new List<Element>();
        var i = 0;

        while (i < value.Length)
        {
            if (value[i] != '(')
            {
                error = $"Expected '(' to start an Application Identifier at position {i}.";
                elements.Clear();
                return false;
            }

            var closeParen = value.IndexOf(')', i);

            if (closeParen < 0)
            {
                error = "Unterminated Application Identifier: missing ')'.";
                elements.Clear();
                return false;
            }

            var ai = value[(i + 1)..closeParen];

            if (!Definitions.TryGetValue(ai, out var definition))
            {
                error = $"Application Identifier ({ai}) is not one of the AIs this encoder supports: {string.Join(", ", Definitions.Keys)}.";
                elements.Clear();
                return false;
            }

            var valueStart = closeParen + 1;
            var nextParen = value.IndexOf('(', valueStart);
            var valueEnd = nextParen < 0 ? value.Length : nextParen;
            var aiValue = value[valueStart..valueEnd];

            if (aiValue.Length == 0)
            {
                error = $"Application Identifier ({ai}) has no data.";
                elements.Clear();
                return false;
            }

            if (definition.Numeric)
            {
                foreach (var ch in aiValue)
                {
                    if (ch is < '0' or > '9')
                    {
                        error = $"Application Identifier ({ai}) requires numeric-only data; got \"{aiValue}\".";
                        elements.Clear();
                        return false;
                    }
                }
            }

            if (definition.FixedLength && aiValue.Length != definition.Length)
            {
                error = $"Application Identifier ({ai}) requires exactly {definition.Length} characters; got {aiValue.Length}.";
                elements.Clear();
                return false;
            }

            if (!definition.FixedLength && aiValue.Length > definition.Length)
            {
                error = $"Application Identifier ({ai}) allows at most {definition.Length} characters; got {aiValue.Length}.";
                elements.Clear();
                return false;
            }

            elements.Add(new Element(ai, aiValue, definition.FixedLength));
            i = valueEnd;
        }

        if (elements.Count == 0)
        {
            error = "No Application Identifiers found.";
            return false;
        }

        error = null;
        return true;
    }
}
