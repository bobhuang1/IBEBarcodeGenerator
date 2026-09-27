using System.Text;

namespace IBEBarcode.Core.Encoders;

public sealed class ExtendedCode39Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code39Extended;

    private readonly Code39Encoder _code39 = new();

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var expanded = new StringBuilder();

        foreach (var ch in value)
        {
            if (ch > 127)
            {
                error = $"Character '{ch}' is outside the 0-127 ASCII range Extended Code 39 supports.";
                return false;
            }

            expanded.Append(ExpandChar(ch));
        }

        if (!_code39.TryEncode(expanded.ToString(), out var innerPattern, out var innerError))
        {
            error = innerError;
            return false;
        }

        pattern = BarcodePattern.Create(value, innerPattern!.Segments, innerPattern.HumanReadableText);
        error = null;
        return true;
    }

    private static string ExpandChar(char ch)
    {
        return ch switch
        {
            >= '0' and <= '9' => ch.ToString(),
            >= 'A' and <= 'Z' => ch.ToString(),
            '-' or '.' or ' ' or '$' or '/' or '+' or '%' => ch.ToString(),
            >= 'a' and <= 'z' => "+" + (char)(ch - 32),
            (char)0 => "%U",
            >= (char)1 and <= (char)26 => "$" + (char)(ch + 64),
            (char)27 => "%A",
            (char)28 => "%B",
            (char)29 => "%C",
            (char)30 => "%D",
            (char)31 => "%E",
            '!' => "/A",
            '"' => "/B",
            '#' => "/C",
            '&' => "/F",
            '\'' => "/G",
            '(' => "/H",
            ')' => "/I",
            '*' => "/J",
            ',' => "/L",
            ':' => "/Z",
            ';' => "%F",
            '<' => "%G",
            '=' => "%H",
            '>' => "%I",
            '?' => "%J",
            '@' => "%V",
            '[' => "%K",
            '\\' => "%L",
            ']' => "%M",
            '^' => "%N",
            '_' => "%O",
            '`' => "%W",
            '{' => "%P",
            '|' => "%Q",
            '}' => "%R",
            '~' => "%S",
            (char)127 => "%T",
            _ => throw new ArgumentOutOfRangeException(nameof(ch), $"Unhandled ASCII character '{ch}' ({(int)ch})."),
        };
    }
}
