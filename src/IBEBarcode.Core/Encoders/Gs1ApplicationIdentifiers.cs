namespace IBEBarcode.Core.Encoders;

internal static class Gs1ApplicationIdentifiers
{
    public sealed record Definition(bool FixedLength, int MinLength, int MaxLength, bool Numeric, bool SeparatorRequired);

    public sealed record Element(string Ai, string Value, bool SeparatorRequired);

    // Built from the full official GS1 Application Identifier table -- see
    // Gs1ApplicationIdentifierTable.cs for how it was sourced and what its columns mean.
    private static readonly Dictionary<string, Definition> Definitions = Gs1ApplicationIdentifierTable.Raw
        .ToDictionary(r => r.Ai, r => new Definition(r.Fixed, r.Min, r.Max, r.Numeric, r.Separator));

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
                error = $"Application Identifier ({ai}) is not a recognized GS1 Application Identifier.";
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

            if (aiValue.Length < definition.MinLength)
            {
                error = $"Application Identifier ({ai}) requires at least {definition.MinLength} characters; got {aiValue.Length}.";
                elements.Clear();
                return false;
            }

            if (aiValue.Length > definition.MaxLength)
            {
                error = $"Application Identifier ({ai}) allows at most {definition.MaxLength} characters; got {aiValue.Length}.";
                elements.Clear();
                return false;
            }

            elements.Add(new Element(ai, aiValue, definition.SeparatorRequired));
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
