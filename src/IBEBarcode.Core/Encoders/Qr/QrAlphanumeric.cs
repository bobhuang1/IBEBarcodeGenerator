namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrAlphanumeric
{
    // ISO/IEC 18004 Table 5. Index = ASCII code, value = alphanumeric code (0-44) or -1.
    private static readonly int[] Table =
    {
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        36, -1, -1, -1, 37, 38, -1, -1, -1, -1, 39, 40, -1, 41, 42, 43,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 44, -1, -1, -1, -1, -1,
        -1, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, -1, -1, -1, -1, -1,
    };

    public static bool TryGetCode(char ch, out int code)
    {
        if (ch < Table.Length && Table[ch] >= 0)
        {
            code = Table[ch];
            return true;
        }

        code = -1;
        return false;
    }
}
