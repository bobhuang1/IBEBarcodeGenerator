namespace IBEBarcode.Core.Encoders;

public sealed class Upc5DigitSupplementEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Upc5DigitSupplement;

    private static readonly int[] CheckDigitEncodings =
    {
        0x18, 0x14, 0x12, 0x11, 0x0C, 0x06, 0x03, 0x0A, 0x09, 0x05,
    };

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 5 })
        {
            error = "The 5-digit UPC/EAN supplement requires exactly 5 digits.";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; the 5-digit supplement encodes digits only.";
                return false;
            }
        }

        var d = new int[5];
        for (var i = 0; i < 5; i++)
        {
            d[i] = value[i] - '0';
        }

        var checksum = (3 * (d[0] + d[2] + d[4]) + 9 * (d[1] + d[3])) % 10;
        var parityBits = CheckDigitEncodings[checksum];

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 2);

        for (var i = 0; i < 5; i++)
        {
            var bitPosition = 4 - i;
            var useGCode = ((parityBits >> bitPosition) & 1) != 0;
            EanUpcDigitPatterns.AppendLeftDigit(segments, d[i], useGCode);

            if (i < 4)
            {
                EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1);
            }
        }

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }
}
