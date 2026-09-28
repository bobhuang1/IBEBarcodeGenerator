namespace IBEBarcode.Core.Encoders.Aztec;

internal static class AztecHighLevelEncoder
{
    public static AztecBitBuffer EncodeBinaryShift(byte[] bytes)
    {
        var buffer = new AztecBitBuffer();
        var n = bytes.Length;

        if (n > 62)
        {
            buffer.AppendBits(31, 5);
            buffer.AppendBits(n - 31, 16);

            foreach (var b in bytes)
            {
                buffer.AppendBits(b, 8);
            }
        }
        else if (n > 31)
        {
            buffer.AppendBits(31, 5);
            buffer.AppendBits(31, 5);

            for (var i = 0; i < 31; i++)
            {
                buffer.AppendBits(bytes[i], 8);
            }

            buffer.AppendBits(31, 5);
            buffer.AppendBits(n - 31, 5);

            for (var i = 31; i < n; i++)
            {
                buffer.AppendBits(bytes[i], 8);
            }
        }
        else
        {
            buffer.AppendBits(31, 5);
            buffer.AppendBits(n, 5);

            foreach (var b in bytes)
            {
                buffer.AppendBits(b, 8);
            }
        }

        return buffer;
    }
}
