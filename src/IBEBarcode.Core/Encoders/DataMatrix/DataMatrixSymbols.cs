namespace IBEBarcode.Core.Encoders.DataMatrix;

internal static class DataMatrixSymbols
{
    public sealed record SymbolSize(int DataCapacity, int ErrorCodewords, int InteriorSize, int[] EccPoly);

    // Interior sizes 10/14/18/22 (ascending dataCapacity order below) each have exactly
    // 2 module positions the placement algorithm never assigns a codeword bit to (verified
    // empirically against this project's own DataMatrixPlacement: for interior N, positions
    // (N-1,N-2) and (N-2,N-1) are always left at their default false/light value and are
    // never read as data by a spec-compliant decoder, which derives the same untouched
    // positions from the identical placement traversal). No special-casing is needed in
    // DataMatrixPlacement/DataMatrixEncoder for these sizes beyond the table entries below.
    public static readonly IReadOnlyList<SymbolSize> Sizes = new[]
    {
        new SymbolSize(3, 5, 8, new[] { 228, 48, 15, 111, 62 }),
        new SymbolSize(5, 7, 10, new[] { 23, 68, 144, 134, 240, 92, 254 }),
        new SymbolSize(8, 10, 12, new[] { 28, 24, 185, 166, 223, 248, 116, 255, 110, 61 }),
        new SymbolSize(12, 12, 14, new[] { 41, 153, 158, 91, 61, 42, 142, 213, 97, 178, 100, 242 }),
        new SymbolSize(18, 14, 16, new[] { 156, 97, 192, 252, 95, 9, 157, 119, 138, 45, 18, 186, 83, 185 }),
        new SymbolSize(22, 18, 18, new[] { 83, 195, 100, 39, 188, 75, 66, 61, 241, 213, 109, 129, 94, 254, 225, 48, 90, 188 }),
        new SymbolSize(30, 20, 20, new[] { 15, 195, 244, 9, 233, 71, 168, 2, 188, 160, 153, 145, 253, 79, 108, 82, 27, 174, 186, 172 }),
        new SymbolSize(36, 24, 22, new[]
        {
            52, 190, 88, 205, 109, 39, 176, 21, 155, 197, 251, 223, 155, 21, 5, 172,
            254, 124, 12, 181, 184, 96, 50, 193,
        }),
        new SymbolSize(44, 28, 24, new[]
        {
            211, 231, 43, 97, 71, 96, 103, 174, 37, 151, 170, 53, 75, 34, 249, 121,
            17, 138, 110, 213, 141, 136, 120, 151, 233, 168, 93, 255,
        }),
    };
}
