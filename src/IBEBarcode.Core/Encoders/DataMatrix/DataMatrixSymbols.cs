namespace IBEBarcode.Core.Encoders.DataMatrix;

internal static class DataMatrixSymbols
{
    public sealed record SymbolSize(
        int DataCapacity,
        int ErrorCodewords,
        int InteriorWidth,
        int InteriorHeight,
        int RegionsHorizontal,
        int RegionsVertical,
        int RsBlockData,
        int RsBlockError);

    // Reed-Solomon polynomials indexed by codeword count (shared across every symbol size
    // that uses that count, whether as a whole-symbol ECC count for single-block sizes or
    // a per-block count for multi-block ones -- e.g. ecc=42 is used both by the single-block
    // 16x16x4-region size and as the per-block count for the 24x24x4-region multi-block
    // size). Mechanically extracted and length-verified against ZXing's FACTOR_SETS/FACTORS
    // tables (same method used for the original 9-entry table).
    public static readonly IReadOnlyDictionary<int, int[]> EccPolyByCount = new Dictionary<int, int[]>
    {
        [5] = new[] { 228, 48, 15, 111, 62 },
        [7] = new[] { 23, 68, 144, 134, 240, 92, 254 },
        [10] = new[] { 28, 24, 185, 166, 223, 248, 116, 255, 110, 61 },
        [12] = new[] { 41, 153, 158, 91, 61, 42, 142, 213, 97, 178, 100, 242 },
        [14] = new[] { 156, 97, 192, 252, 95, 9, 157, 119, 138, 45, 18, 186, 83, 185 },
        [18] = new[] { 83, 195, 100, 39, 188, 75, 66, 61, 241, 213, 109, 129, 94, 254, 225, 48, 90, 188 },
        [20] = new[] { 15, 195, 244, 9, 233, 71, 168, 2, 188, 160, 153, 145, 253, 79, 108, 82, 27, 174, 186, 172 },
        [24] = new[] { 52, 190, 88, 205, 109, 39, 176, 21, 155, 197, 251, 223, 155, 21, 5, 172, 254, 124, 12, 181, 184, 96, 50, 193 },
        [28] = new[]
        {
            211, 231, 43, 97, 71, 96, 103, 174, 37, 151, 170, 53, 75, 34, 249, 121,
            17, 138, 110, 213, 141, 136, 120, 151, 233, 168, 93, 255,
        },
        // Levels beyond dataCapacity 44 (multi-region and/or multi-block symbols).
        [36] = new[]
        {
            245, 127, 242, 218, 130, 250, 162, 181, 102, 120, 84, 179, 220, 251, 80, 182,
            229, 18, 2, 4, 68, 33, 101, 137, 95, 119, 115, 44, 175, 184, 59, 25,
            225, 98, 81, 112,
        },
        [42] = new[]
        {
            77, 193, 137, 31, 19, 38, 22, 153, 247, 105, 122, 2, 245, 133, 242, 8,
            175, 95, 100, 9, 167, 105, 214, 111, 57, 121, 21, 1, 253, 57, 54, 101,
            248, 202, 69, 50, 150, 177, 226, 5, 9, 5,
        },
        [48] = new[]
        {
            245, 132, 172, 223, 96, 32, 117, 22, 238, 133, 238, 231, 205, 188, 237, 87,
            191, 106, 16, 147, 118, 23, 37, 90, 170, 205, 131, 88, 120, 100, 66, 138,
            186, 240, 82, 44, 176, 87, 187, 147, 160, 175, 69, 213, 92, 253, 225, 19,
        },
        [56] = new[]
        {
            175, 9, 223, 238, 12, 17, 220, 208, 100, 29, 175, 170, 230, 192, 215, 235,
            150, 159, 36, 223, 38, 200, 132, 54, 228, 146, 218, 234, 117, 203, 29, 232,
            144, 238, 22, 150, 201, 117, 62, 207, 164, 13, 137, 245, 127, 67, 247, 28,
            155, 43, 203, 107, 233, 53, 143, 46,
        },
        [62] = new[]
        {
            242, 93, 169, 50, 144, 210, 39, 118, 202, 188, 201, 189, 143, 108, 196, 37,
            185, 112, 134, 230, 245, 63, 197, 190, 250, 106, 185, 221, 175, 64, 114, 71,
            161, 44, 147, 6, 27, 218, 51, 63, 87, 10, 40, 130, 188, 17, 163, 31,
            176, 170, 4, 107, 232, 7, 94, 166, 224, 124, 86, 47, 11, 204,
        },
        [68] = new[]
        {
            220, 228, 173, 89, 251, 149, 159, 56, 89, 33, 147, 244, 154, 36, 73, 127,
            213, 136, 248, 180, 234, 197, 158, 177, 68, 122, 93, 213, 15, 160, 227, 236,
            66, 139, 153, 185, 202, 167, 179, 25, 220, 232, 96, 210, 231, 136, 223, 239,
            181, 241, 59, 52, 172, 25, 49, 232, 211, 189, 64, 54, 108, 153, 132, 63,
            96, 103, 82, 186,
        },
    };

    // Interior square sizes 10/14/18/22 (ascending dataCapacity order below) each have
    // exactly 2 module positions the placement algorithm never assigns a codeword bit to
    // (verified empirically against this project's own DataMatrixPlacement: for a square
    // interior N, positions (N-1,N-2) and (N-2,N-1) are always left at their default
    // false/light value and are never read as data by a spec-compliant decoder, which
    // derives the same untouched positions from the identical placement traversal). No
    // special-casing is needed in DataMatrixPlacement/DataMatrixEncoder for these sizes
    // beyond the table entries below.
    //
    // The two rectangular sizes (16x6 and 24x10) are single-data-region, exact-bit-fit
    // sizes -- DataMatrixPlacement's ISO placement algorithm already handles non-square
    // interiors natively.
    //
    // Sizes beyond dataCapacity 44 use multiple data regions tiled in a grid (2x2, 4x4, or
    // 6x6), verified directly from ZXing's DataMatrixWriter/SymbolInfo: DefaultPlacement is
    // still given a SINGLE placement spanning the *combined* data area (RegionsHorizontal *
    // InteriorWidth by RegionsVertical * InteriorHeight) -- confirming DataMatrixPlacement,
    // which was never written assuming a single region, needs no changes for this either.
    // Only the border-assembly logic (repeated at each region boundary, not just the outer
    // edge) and, for the largest 6 sizes, genuine multi-block Reed-Solomon (data codewords
    // striped across blocks by index modulo block count, not contiguous chunks -- verified
    // against ErrorCorrection.encodeECC200, a different interleaving rule than QR's) are new.
    // RsBlockData/RsBlockError equal DataCapacity/ErrorCodewords (i.e. a single block) for
    // every size except the last 6, where block count = DataCapacity/RsBlockData > 1.
    //
    // The one further official size (interior 16, 36 regions, labeled DataMatrixSymbolInfo144
    // in ZXing) needs special-cased handling ZXing itself singles out as an exception to the
    // usual pattern -- skipped here, mirroring this project's established practice of scoping
    // around the one genuinely ambiguous/special case rather than guessing.
    //
    // Where a rectangular size ties a square size on dataCapacity (5, here), the square
    // size is listed first so it's preferred by TryEncode's first-fit search, matching
    // ZXing's own default (FORCE_NONE) preference for square over rectangular.
    public static readonly IReadOnlyList<SymbolSize> Sizes = new[]
    {
        new SymbolSize(3, 5, 8, 8, 1, 1, 3, 5),
        new SymbolSize(5, 7, 10, 10, 1, 1, 5, 7),
        new SymbolSize(5, 7, 16, 6, 1, 1, 5, 7),
        new SymbolSize(8, 10, 12, 12, 1, 1, 8, 10),
        new SymbolSize(12, 12, 14, 14, 1, 1, 12, 12),
        new SymbolSize(16, 14, 24, 10, 1, 1, 16, 14),
        new SymbolSize(18, 14, 16, 16, 1, 1, 18, 14),
        new SymbolSize(22, 18, 18, 18, 1, 1, 22, 18),
        new SymbolSize(30, 20, 20, 20, 1, 1, 30, 20),
        new SymbolSize(36, 24, 22, 22, 1, 1, 36, 24),
        new SymbolSize(44, 28, 24, 24, 1, 1, 44, 28),
        new SymbolSize(62, 36, 14, 14, 2, 2, 62, 36),
        new SymbolSize(86, 42, 16, 16, 2, 2, 86, 42),
        new SymbolSize(114, 48, 18, 18, 2, 2, 114, 48),
        new SymbolSize(144, 56, 20, 20, 2, 2, 144, 56),
        new SymbolSize(174, 68, 22, 22, 2, 2, 174, 68),
        new SymbolSize(204, 84, 24, 24, 2, 2, 102, 42),
        new SymbolSize(280, 112, 14, 14, 4, 4, 140, 56),
        new SymbolSize(368, 144, 16, 16, 4, 4, 92, 36),
        new SymbolSize(456, 192, 18, 18, 4, 4, 114, 48),
        new SymbolSize(576, 224, 20, 20, 4, 4, 144, 56),
        new SymbolSize(696, 272, 22, 22, 4, 4, 174, 68),
        new SymbolSize(816, 336, 24, 24, 4, 4, 136, 56),
        new SymbolSize(1050, 408, 18, 18, 6, 6, 175, 68),
        new SymbolSize(1304, 496, 20, 20, 6, 6, 163, 62),
    };
}
