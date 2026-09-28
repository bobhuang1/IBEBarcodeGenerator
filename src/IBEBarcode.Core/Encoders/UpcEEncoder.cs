namespace IBEBarcode.Core.Encoders;

public sealed class UpcEEncoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.UpcE;

    private static readonly int[][] NumSysAndCheckDigitPatterns =
    {
        new[] { 0x38, 0x34, 0x32, 0x31, 0x2C, 0x26, 0x23, 0x2A, 0x29, 0x25 },
        new[] { 0x07, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A },
    };

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (value is not { Length: 6 or 7 or 8 })
        {
            error = "UPC-E requires a 6-digit payload, optionally preceded by the number system digit and/or followed by the check digit (6-8 characters total).";
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is < '0' or > '9')
            {
                error = $"Character '{ch}' is not a digit; UPC-E encodes digits only.";
                return false;
            }
        }

        char numberSystem;
        string payload;
        char? providedCheckDigit = null;

        if (value.Length == 6)
        {
            numberSystem = '0';
            payload = value;
        }
        else if (value.Length == 7)
        {
            numberSystem = value[0];
            payload = value.Substring(1, 6);
        }
        else
        {
            numberSystem = value[0];
            payload = value.Substring(1, 6);
            providedCheckDigit = value[7];
        }

        if (numberSystem is not ('0' or '1'))
        {
            error = "UPC-E's number system digit must be 0 or 1.";
            return false;
        }

        var expanded = ExpandToUpcAMiddle(payload);
        var elevenDigitData = $"{numberSystem}{expanded}";
        var checkDigit = EanUpcDigitPatterns.ComputeCheckDigit(elevenDigitData);

        if (providedCheckDigit is not null && providedCheckDigit != checkDigit)
        {
            error = $"Invalid UPC-E check digit: expected '{checkDigit}', got '{providedCheckDigit}'.";
            return false;
        }

        var numSysIndex = numberSystem - '0';
        var checkDigitIndex = checkDigit - '0';
        var parityBits = NumSysAndCheckDigitPatterns[numSysIndex][checkDigitIndex];

        var segments = new List<BarSegment>();
        EanUpcDigitPatterns.AppendGuard(segments, true, 1, 1, 1);

        for (var i = 0; i < 6; i++)
        {
            var digit = payload[i] - '0';
            var bitPosition = 5 - i;
            var useGCode = ((parityBits >> bitPosition) & 1) != 0;
            EanUpcDigitPatterns.AppendLeftDigit(segments, digit, useGCode);
        }

        EanUpcDigitPatterns.AppendGuard(segments, false, 1, 1, 1, 1, 1, 1);

        var fullValue = $"{numberSystem}{payload}{checkDigit}";
        pattern = BarcodePattern.Create(fullValue, segments, fullValue);
        error = null;
        return true;
    }

    private static string ExpandToUpcAMiddle(string payload)
    {
        var lastDigit = payload[5];

        return lastDigit switch
        {
            '0' or '1' or '2' => $"{payload[0]}{payload[1]}{lastDigit}0000{payload[2]}{payload[3]}{payload[4]}",
            '3' => $"{payload[0]}{payload[1]}{payload[2]}00000{payload[3]}{payload[4]}",
            '4' => $"{payload[0]}{payload[1]}{payload[2]}{payload[3]}00000{payload[4]}",
            _ => $"{payload[0]}{payload[1]}{payload[2]}{payload[3]}{payload[4]}0000{lastDigit}",
        };
    }
}
