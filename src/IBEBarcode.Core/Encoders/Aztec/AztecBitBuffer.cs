namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecBitBuffer
{
    private readonly List<bool> _bits = new();

    public int Count => _bits.Count;

    public bool Get(int index) => _bits[index];

    public void AppendBits(int value, int numBits)
    {
        for (var i = numBits - 1; i >= 0; i--)
        {
            _bits.Add(((value >> i) & 1) != 0);
        }
    }
}
