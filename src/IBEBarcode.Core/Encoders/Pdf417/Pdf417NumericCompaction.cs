using System.Numerics;

namespace IBEBarcode.Core.Encoders.Pdf417;

internal static class Pdf417NumericCompaction
{
    private const int LatchToNumeric = 902;

    public static int[] EncodeDigits(string digits)
    {
        var codewords = new List<int> { LatchToNumeric };
        var idx = 0;

        while (idx < digits.Length)
        {
            var len = Math.Min(44, digits.Length - idx);
            var part = "1" + digits.Substring(idx, len);
            var value = BigInteger.Parse(part);
            var temp = new List<int>();

            do
            {
                temp.Add((int)(value % 900));
                value /= 900;
            } while (value != 0);

            for (var i = temp.Count - 1; i >= 0; i--)
            {
                codewords.Add(temp[i]);
            }

            idx += len;
        }

        return codewords.ToArray();
    }
}
