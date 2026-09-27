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
}
