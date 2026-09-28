namespace IBEBarcode.Core.Encoders.Pdf417;

internal static class Pdf417ErrorCorrection
{
    public static int[] Generate(int[] dataCodewords, int level)
    {
        var poly = Pdf417ErrorCorrectionCoefficients.Levels[level];
        var k = poly.Length;
        var e = new int[k];

        foreach (var codeword in dataCodewords)
        {
            var t1 = (codeword + e[k - 1]) % 929;

            for (var j = k - 1; j >= 1; j--)
            {
                var t2 = (t1 * poly[j]) % 929;
                var t3 = 929 - t2;
                e[j] = (e[j - 1] + t3) % 929;
            }

            var t2First = (t1 * poly[0]) % 929;
            var t3First = 929 - t2First;
            e[0] = t3First % 929;
        }

        var result = new int[k];

        for (var j = k - 1; j >= 0; j--)
        {
            result[k - 1 - j] = e[j] == 0 ? 0 : 929 - e[j];
        }

        return result;
    }
}
