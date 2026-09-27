namespace IBEBarcode.Core;

public sealed class BarcodeMatrix
{
    public string Value { get; }
    public int Width { get; }
    public int Height { get; }

    private readonly bool[,] _modules;

    private BarcodeMatrix(string value, bool[,] modules)
    {
        Value = value;
        _modules = modules;
        Width = modules.GetLength(0);
        Height = modules.GetLength(1);
    }

    public static BarcodeMatrix Create(string value, bool[,] modules)
    {
        if (modules.GetLength(0) == 0 || modules.GetLength(1) == 0)
            throw new ArgumentException("Matrix must have at least one row and column.", nameof(modules));

        return new BarcodeMatrix(value, modules);
    }

    public bool this[int x, int y] => _modules[x, y];
}
