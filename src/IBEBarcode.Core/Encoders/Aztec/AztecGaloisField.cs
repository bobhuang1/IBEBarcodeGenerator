namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecGaloisField
{
    private readonly int[] _expTable;
    private readonly int[] _logTable;
    private readonly int _size;

    public int GeneratorBase { get; }

    public AztecGaloisField(int primitive, int size, int generatorBase)
    {
        _size = size;
        GeneratorBase = generatorBase;

        _expTable = new int[size];
        _logTable = new int[size];

        var x = 1;

        for (var i = 0; i < size; i++)
        {
            _expTable[i] = x;
            x *= 2;

            if (x >= size)
            {
                x ^= primitive;
                x &= size - 1;
            }
        }

        for (var i = 0; i < size - 1; i++)
        {
            _logTable[_expTable[i]] = i;
        }
    }

    public int Exp(int a) => _expTable[a];

    public int Log(int a) => _logTable[a];

    public int Inverse(int a) => _expTable[_size - _logTable[a] - 1];

    public int Multiply(int a, int b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        return _expTable[(_logTable[a] + _logTable[b]) % (_size - 1)];
    }
}
