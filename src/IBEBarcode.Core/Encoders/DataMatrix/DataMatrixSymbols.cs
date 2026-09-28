namespace IBEBarcode.Core.Encoders.DataMatrix;

internal static class DataMatrixSymbols
{
    public sealed record SymbolSize(int DataCapacity, int ErrorCodewords, int InteriorSize, int[] EccPoly);

    public static readonly IReadOnlyList<SymbolSize> Sizes = new[]
    {
        new SymbolSize(3, 5, 8, new[] { 228, 48, 15, 111, 62 }),
        new SymbolSize(8, 10, 12, new[] { 28, 24, 185, 166, 223, 248, 116, 255, 110, 61 }),
        new SymbolSize(18, 14, 16, new[] { 156, 97, 192, 252, 95, 9, 157, 119, 138, 45, 18, 186, 83, 185 }),
        new SymbolSize(30, 20, 20, new[] { 15, 195, 244, 9, 233, 71, 168, 2, 188, 160, 153, 145, 253, 79, 108, 82, 27, 174, 186, 172 }),
        new SymbolSize(44, 28, 24, new[]
        {
            211, 231, 43, 97, 71, 96, 103, 174, 37, 151, 170, 53, 75, 34, 249, 121,
            17, 138, 110, 213, 141, 136, 120, 151, 233, 168, 93, 255,
        }),
    };
}
