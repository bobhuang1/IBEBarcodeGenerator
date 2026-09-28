namespace IBEBarcode.Core.Encoders;

public sealed class Code128Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Code128;

    private readonly Code128Set _codeSet;

    public Code128Encoder(Code128Set codeSet = Code128Set.B)
    {
        _codeSet = codeSet;
    }

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        int startSymbol;
        int[] values;
        bool success;

        switch (_codeSet)
        {
            case Code128Set.A:
                startSymbol = Code128Symbols.StartA;
                success = Code128Symbols.TryEncodeSetA(value, out values, out error);
                break;
            case Code128Set.C:
                startSymbol = Code128Symbols.StartC;
                success = Code128Symbols.TryEncodeSetC(value, out values, out error);
                break;
            case Code128Set.Auto:
                success = TryEncodeAuto(value, out startSymbol, out values, out error);
                break;
            default:
                startSymbol = Code128Symbols.StartB;
                success = Code128Symbols.TryEncodeSetB(value, out values, out error);
                break;
        }

        if (!success)
        {
            return false;
        }

        var checkSum = Code128Symbols.ComputeChecksum(startSymbol, values);

        var segments = new List<BarSegment>();
        Code128Symbols.AppendSymbol(segments, startSymbol);

        foreach (var v in values)
        {
            Code128Symbols.AppendSymbol(segments, v);
        }

        Code128Symbols.AppendSymbol(segments, checkSum);
        Code128Symbols.AppendSymbol(segments, Code128Symbols.Stop);

        pattern = BarcodePattern.Create(value, segments, value);
        error = null;
        return true;
    }

    private static bool TryEncodeAuto(string value, out int startSymbol, out int[] values, out string? error)
    {
        startSymbol = Code128Symbols.StartB;
        values = Array.Empty<int>();

        if (!Code128AutoSegmenter.TrySegment(value, out var segments, out error))
        {
            return false;
        }

        startSymbol = segments[0].Set switch
        {
            Code128Set.A => Code128Symbols.StartA,
            Code128Set.C => Code128Symbols.StartC,
            _ => Code128Symbols.StartB,
        };

        var result = new List<int>();
        Code128Set? previousSet = null;

        foreach (var segment in segments)
        {
            if (previousSet is not null && previousSet != segment.Set)
            {
                var switchSymbol = segment.Set switch
                {
                    Code128Set.A => Code128Symbols.CodeA,
                    Code128Set.C => Code128Symbols.CodeC,
                    _ => Code128Symbols.CodeB,
                };
                result.Add(switchSymbol);
            }

            bool segmentSuccess;
            int[] segmentValues;

            switch (segment.Set)
            {
                case Code128Set.A:
                    segmentSuccess = Code128Symbols.TryEncodeSetA(segment.Text, out segmentValues, out error);
                    break;
                case Code128Set.C:
                    segmentSuccess = Code128Symbols.TryEncodeSetC(segment.Text, out segmentValues, out error);
                    break;
                default:
                    segmentSuccess = Code128Symbols.TryEncodeSetB(segment.Text, out segmentValues, out error);
                    break;
            }

            if (!segmentSuccess)
            {
                return false;
            }

            result.AddRange(segmentValues);
            previousSet = segment.Set;
        }

        values = result.ToArray();
        error = null;
        return true;
    }
}
