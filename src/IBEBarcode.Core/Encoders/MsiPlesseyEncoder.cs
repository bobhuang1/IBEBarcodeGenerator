namespace IBEBarcode.Core.Encoders;

public sealed class MsiPlesseyEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.MsiPlessey;

    private const int NarrowWidth = 1;
    private const int WideWidth = 2;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; MSI Plessey encodes digits only.";
                return false;
            }
        }

        var segments = new List<BarSegment>
        {
            new(true, WideWidth),
            new(false, NarrowWidth),
        };

        foreach (var ch in value)
        {
            var digit = ch - '0';
            for (var bitIndex = 3; bitIndex >= 0; bitIndex--)
            {
                var bit = (digit >> bitIndex) & 1;
                segments.Add(new BarSegment(true, bit == 1 ? WideWidth : NarrowWidth));
                segments.Add(new BarSegment(false, bit == 1 ? NarrowWidth : WideWidth));
            }
        }

        segments.Add(new BarSegment(true, NarrowWidth));
        segments.Add(new BarSegment(false, WideWidth));
        segments.Add(new BarSegment(true, NarrowWidth));

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }

    public static char ComputeCheckDigit(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            throw new ArgumentException("Value must not be empty.", nameof(digits));

        var sum = 0;
        var doubleNext = true;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var ch = digits[i];
            if (ch is < '0' or > '9')
                throw new ArgumentException($"Character '{ch}' is not a digit.", nameof(digits));

            var digit = ch - '0';
            if (doubleNext)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            doubleNext = !doubleNext;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return (char)('0' + checkDigit);
    }
}
