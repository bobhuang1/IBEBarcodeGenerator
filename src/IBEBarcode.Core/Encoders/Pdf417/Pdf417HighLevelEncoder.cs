namespace IBEBarcode.Core.Encoders.Pdf417;

internal static class Pdf417HighLevelEncoder
{
    private const int LatchToBytePadded = 901;
    private const int LatchToByte = 924;

    public static int[] EncodeBytes(byte[] bytes)
    {
        var codewords = new List<int>
        {
            bytes.Length % 6 == 0 ? LatchToByte : LatchToBytePadded,
        };

        var idx = 0;

        while (bytes.Length - idx >= 6)
        {
            long t = 0;

            for (var i = 0; i < 6; i++)
            {
                t <<= 8;
                t += bytes[idx + i];
            }

            var chunk = new int[5];

            for (var i = 0; i < 5; i++)
            {
                chunk[i] = (int)(t % 900);
                t /= 900;
            }

            for (var i = 4; i >= 0; i--)
            {
                codewords.Add(chunk[i]);
            }

            idx += 6;
        }

        for (var i = idx; i < bytes.Length; i++)
        {
            codewords.Add(bytes[i]);
        }

        return codewords.ToArray();
    }
}
