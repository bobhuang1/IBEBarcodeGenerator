namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrGaloisField
{
    private const int PrimitivePolynomial = 0x11D;

    private static readonly int[] ExpTable = new int[512];
    private static readonly int[] LogTable = new int[256];

    static QrGaloisField()
    {
        var x = 1;

        for (var i = 0; i < 255; i++)
        {
            ExpTable[i] = x;
            LogTable[x] = i;
            x <<= 1;

            if ((x & 0x100) != 0)
            {
                x ^= PrimitivePolynomial;
            }
        }

        for (var i = 255; i < 512; i++)
        {
            ExpTable[i] = ExpTable[i - 255];
        }
    }

    public static int Exp(int power) => ExpTable[power];

    public static int Multiply(int a, int b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        return ExpTable[LogTable[a] + LogTable[b]];
    }
}
