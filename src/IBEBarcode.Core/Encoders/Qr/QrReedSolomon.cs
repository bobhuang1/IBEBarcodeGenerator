namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrReedSolomon
{
    public static byte[] ComputeEccCodewords(byte[] data, int eccCount)
    {
        var generator = BuildGeneratorPolynomial(eccCount);
        var remainder = new int[data.Length + eccCount];

        for (var i = 0; i < data.Length; i++)
        {
            remainder[i] = data[i];
        }

        for (var i = 0; i < data.Length; i++)
        {
            var coefficient = remainder[i];

            if (coefficient == 0)
            {
                continue;
            }

            for (var j = 0; j < generator.Length; j++)
            {
                remainder[i + j] ^= QrGaloisField.Multiply(generator[j], coefficient);
            }
        }

        var ecc = new byte[eccCount];

        for (var i = 0; i < eccCount; i++)
        {
            ecc[i] = (byte)remainder[data.Length + i];
        }

        return ecc;
    }

    private static int[] BuildGeneratorPolynomial(int degree)
    {
        var generator = new[] { 1 };

        for (var i = 0; i < degree; i++)
        {
            generator = MultiplyPolynomials(generator, new[] { 1, QrGaloisField.Exp(i) });
        }

        return generator;
    }

    private static int[] MultiplyPolynomials(int[] a, int[] b)
    {
        var result = new int[a.Length + b.Length - 1];

        for (var i = 0; i < a.Length; i++)
        {
            for (var j = 0; j < b.Length; j++)
            {
                result[i + j] ^= QrGaloisField.Multiply(a[i], b[j]);
            }
        }

        return result;
    }
}
