namespace IBEBarcode.Core.Encoders.Qr;

internal sealed class QrBitWriter
{
    private readonly List<bool> _bits = new();

    public int Count => _bits.Count;

    public void AppendBits(int value, int bitCount)
    {
        for (var i = bitCount - 1; i >= 0; i--)
        {
            _bits.Add(((value >> i) & 1) != 0);
        }
    }

    public byte[] ToBytes()
    {
        var byteCount = (_bits.Count + 7) / 8;
        var bytes = new byte[byteCount];

        for (var i = 0; i < _bits.Count; i++)
        {
            if (_bits[i])
            {
                bytes[i / 8] |= (byte)(0x80 >> (i % 8));
            }
        }

        return bytes;
    }
}
