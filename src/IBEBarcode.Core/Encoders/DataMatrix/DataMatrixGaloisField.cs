namespace IBEBarcode.Core.Encoders.DataMatrix;

internal static class DataMatrixGaloisField
{
    private const int PrimitivePolynomial = 0x12D;

    private static readonly int[] Log = new int[256];
    private static readonly int[] Alog = new int[255];

    static DataMatrixGaloisField()
    {
        var p = 1;

        for (var i = 0; i < 255; i++)
        {
            Alog[i] = p;
            Log[p] = i;
            p *= 2;

            if (p >= 256)
            {
                p ^= PrimitivePolynomial;
            }
        }
    }

    public static int LogOf(int value) => Log[value];

    public static int AlogOf(int index) => Alog[index];
}
