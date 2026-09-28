namespace IBEBarcode.Core.Encoders.DataMatrix;

internal static class DataMatrixErrorCorrection
{
    public static byte[] ComputeEcc(byte[] dataCodewords, int eccCount, int[] poly)
    {
        var ecc = new int[eccCount];

        foreach (var codeword in dataCodewords)
        {
            var m = ecc[eccCount - 1] ^ codeword;

            for (var k = eccCount - 1; k > 0; k--)
            {
                ecc[k] = m != 0 && poly[k] != 0
                    ? ecc[k - 1] ^ DataMatrixGaloisField.AlogOf((DataMatrixGaloisField.LogOf(m) + DataMatrixGaloisField.LogOf(poly[k])) % 255)
                    : ecc[k - 1];
            }

            ecc[0] = m != 0 && poly[0] != 0
                ? DataMatrixGaloisField.AlogOf((DataMatrixGaloisField.LogOf(m) + DataMatrixGaloisField.LogOf(poly[0])) % 255)
                : 0;
        }

        var result = new byte[eccCount];

        for (var i = 0; i < eccCount; i++)
        {
            result[i] = (byte)ecc[eccCount - 1 - i];
        }

        return result;
    }
}
