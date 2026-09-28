namespace IBEBarcode.Core.Encoders;

internal static class Code128SymbolStreamBuilder
{
    /// <summary>
    /// Auto-segments <paramref name="text"/> and appends its symbol values to
    /// <paramref name="values"/>, inserting a Code A/B/C switch codeword whenever the
    /// segment's set differs from <paramref name="currentSet"/>. If <paramref name="currentSet"/>
    /// is null (nothing emitted yet), no switch codeword is emitted for the first segment;
    /// instead its set is returned via <paramref name="firstSegmentSet"/> so the caller can
    /// choose the message's Start symbol. <paramref name="currentSet"/> is updated to the set
    /// the stream ends in, so a subsequent call (e.g. after inserting a set-independent FNC1
    /// codeword) continues without a redundant switch.
    /// </summary>
    public static bool TryAppend(string text, ref Code128Set? currentSet, List<int> values, out Code128Set firstSegmentSet, out string? error)
    {
        firstSegmentSet = default;

        if (!Code128AutoSegmenter.TrySegment(text, out var segments, out error))
        {
            return false;
        }

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (currentSet is null && i == 0)
            {
                firstSegmentSet = segment.Set;
            }
            else if (currentSet != segment.Set)
            {
                var switchSymbol = segment.Set switch
                {
                    Code128Set.A => Code128Symbols.CodeA,
                    Code128Set.C => Code128Symbols.CodeC,
                    _ => Code128Symbols.CodeB,
                };
                values.Add(switchSymbol);
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

            values.AddRange(segmentValues);
            currentSet = segment.Set;
        }

        error = null;
        return true;
    }
}
