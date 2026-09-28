namespace IBEBarcode.Core.Encoders;

internal static class Code128AutoSegmenter
{
    public readonly record struct Segment(Code128Set Set, string Text);

    private static int DigitRunLength(string value, int start)
    {
        var run = 0;

        while (start + run < value.Length && value[start + run] is >= '0' and <= '9')
        {
            run++;
        }

        return run;
    }

    public static bool TrySegment(string value, out List<Segment> segments, out string? error)
    {
        segments = new List<Segment>();
        var i = 0;

        while (i < value.Length)
        {
            var digitRun = DigitRunLength(value, i);

            if (digitRun >= 4)
            {
                var cLen = (digitRun / 2) * 2;
                segments.Add(new Segment(Code128Set.C, value.Substring(i, cLen)));
                i += cLen;
                continue;
            }

            var start = i;
            var hasControlChar = false;
            var hasHighAscii = false;

            while (i < value.Length && DigitRunLength(value, i) < 4)
            {
                var ch = value[i];

                if (ch is >= (char)0 and <= (char)31)
                {
                    hasControlChar = true;
                }
                else if (ch > (char)127)
                {
                    hasHighAscii = true;
                }

                i++;
            }

            var segmentText = value[start..i];

            if (hasHighAscii)
            {
                segments = new List<Segment>();
                error = $"Segment \"{segmentText}\" contains a character outside the ASCII 0-127 range this auto Code 128 encoder supports.";
                return false;
            }

            if (hasControlChar)
            {
                foreach (var ch in segmentText)
                {
                    if (ch > (char)95)
                    {
                        segments = new List<Segment>();
                        error = $"Segment \"{segmentText}\" mixes control characters (needing Set A) with characters Set A cannot encode (96-127) -- this auto Code 128 encoder does not support mid-segment shifting.";
                        return false;
                    }
                }

                segments.Add(new Segment(Code128Set.A, segmentText));
            }
            else
            {
                segments.Add(new Segment(Code128Set.B, segmentText));
            }
        }

        error = null;
        return true;
    }
}
