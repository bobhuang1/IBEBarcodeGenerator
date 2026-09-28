namespace IBEBarcode.Core.Encoders;

public sealed class Gs1_128Encoder : IBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.Gs1_128;

    public bool TryEncode(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        if (value.Contains('('))
        {
            return TryEncodeElementString(value, out pattern, out error);
        }

        if (!Code128Symbols.TryEncodeSetC(value, out var digitPairValues, out error))
        {
            return false;
        }

        var values = new int[digitPairValues.Length + 1];
        values[0] = Code128Symbols.Fnc1;
        Array.Copy(digitPairValues, 0, values, 1, digitPairValues.Length);

        var checkSum = Code128Symbols.ComputeChecksum(Code128Symbols.StartC, values);

        var segments = new List<BarSegment>();
        Code128Symbols.AppendSymbol(segments, Code128Symbols.StartC);

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

    private static bool TryEncodeElementString(string value, out BarcodePattern? pattern, out string? error)
    {
        pattern = null;

        if (!Gs1ApplicationIdentifiers.TryParseElementString(value, out var elements, out error))
        {
            return false;
        }

        var values = new List<int> { Code128Symbols.Fnc1 };
        Code128Set? currentSet = null;
        Code128Set? startSet = null;

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var text = element.Ai + element.Value;

            if (!Code128SymbolStreamBuilder.TryAppend(text, ref currentSet, values, out var firstSegmentSet, out error))
            {
                return false;
            }

            startSet ??= firstSegmentSet;

            if (!element.IsFixedLength && i < elements.Count - 1)
            {
                values.Add(Code128Symbols.Fnc1);
            }
        }

        var startSymbol = startSet switch
        {
            Code128Set.A => Code128Symbols.StartA,
            Code128Set.C => Code128Symbols.StartC,
            _ => Code128Symbols.StartB,
        };

        var checkSum = Code128Symbols.ComputeChecksum(startSymbol, values.ToArray());

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
