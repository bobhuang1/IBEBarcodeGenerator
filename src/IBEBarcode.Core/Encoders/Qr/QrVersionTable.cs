namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrVersionTable
{
    public sealed record EcBlockGroup(int Count, int DataCodewords);
    public sealed record EcBlocks(int EccCodewordsPerBlock, EcBlockGroup[] Groups);
    public sealed record VersionInfo(int Version, int[] AlignmentCenters, EcBlocks[] LevelsLmqh);

    // Mechanically extracted from ZXing's qrcode/decoder/Version.java buildVersions()
    // (Apache 2.0), itself from ISO/IEC 18004:2006 Annex D / Table 9. Index 0 = version 1.
    // LevelsLmqh order is [L, M, Q, H], cross-checked against version 1's already-verified
    // hardcoded values before this table replaced them.
    public static readonly VersionInfo[] Versions =
    {
        new VersionInfo(1, Array.Empty<int>(), new[]
        {
            new EcBlocks(7, new[] { new EcBlockGroup(1, 19) }),
            new EcBlocks(10, new[] { new EcBlockGroup(1, 16) }),
            new EcBlocks(13, new[] { new EcBlockGroup(1, 13) }),
            new EcBlocks(17, new[] { new EcBlockGroup(1, 9) }),
        }),
        new VersionInfo(2, new[] { 6, 18 }, new[]
        {
            new EcBlocks(10, new[] { new EcBlockGroup(1, 34) }),
            new EcBlocks(16, new[] { new EcBlockGroup(1, 28) }),
            new EcBlocks(22, new[] { new EcBlockGroup(1, 22) }),
            new EcBlocks(28, new[] { new EcBlockGroup(1, 16) }),
        }),
        new VersionInfo(3, new[] { 6, 22 }, new[]
        {
            new EcBlocks(15, new[] { new EcBlockGroup(1, 55) }),
            new EcBlocks(26, new[] { new EcBlockGroup(1, 44) }),
            new EcBlocks(18, new[] { new EcBlockGroup(2, 17) }),
            new EcBlocks(22, new[] { new EcBlockGroup(2, 13) }),
        }),
        new VersionInfo(4, new[] { 6, 26 }, new[]
        {
            new EcBlocks(20, new[] { new EcBlockGroup(1, 80) }),
            new EcBlocks(18, new[] { new EcBlockGroup(2, 32) }),
            new EcBlocks(26, new[] { new EcBlockGroup(2, 24) }),
            new EcBlocks(16, new[] { new EcBlockGroup(4, 9) }),
        }),
        new VersionInfo(5, new[] { 6, 30 }, new[]
        {
            new EcBlocks(26, new[] { new EcBlockGroup(1, 108) }),
            new EcBlocks(24, new[] { new EcBlockGroup(2, 43) }),
            new EcBlocks(18, new[] { new EcBlockGroup(2, 15), new EcBlockGroup(2, 16) }),
            new EcBlocks(22, new[] { new EcBlockGroup(2, 11), new EcBlockGroup(2, 12) }),
        }),
        new VersionInfo(6, new[] { 6, 34 }, new[]
        {
            new EcBlocks(18, new[] { new EcBlockGroup(2, 68) }),
            new EcBlocks(16, new[] { new EcBlockGroup(4, 27) }),
            new EcBlocks(24, new[] { new EcBlockGroup(4, 19) }),
            new EcBlocks(28, new[] { new EcBlockGroup(4, 15) }),
        }),
        new VersionInfo(7, new[] { 6, 22, 38 }, new[]
        {
            new EcBlocks(20, new[] { new EcBlockGroup(2, 78) }),
            new EcBlocks(18, new[] { new EcBlockGroup(4, 31) }),
            new EcBlocks(18, new[] { new EcBlockGroup(2, 14), new EcBlockGroup(4, 15) }),
            new EcBlocks(26, new[] { new EcBlockGroup(4, 13), new EcBlockGroup(1, 14) }),
        }),
        new VersionInfo(8, new[] { 6, 24, 42 }, new[]
        {
            new EcBlocks(24, new[] { new EcBlockGroup(2, 97) }),
            new EcBlocks(22, new[] { new EcBlockGroup(2, 38), new EcBlockGroup(2, 39) }),
            new EcBlocks(22, new[] { new EcBlockGroup(4, 18), new EcBlockGroup(2, 19) }),
            new EcBlocks(26, new[] { new EcBlockGroup(4, 14), new EcBlockGroup(2, 15) }),
        }),
        new VersionInfo(9, new[] { 6, 26, 46 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(2, 116) }),
            new EcBlocks(22, new[] { new EcBlockGroup(3, 36), new EcBlockGroup(2, 37) }),
            new EcBlocks(20, new[] { new EcBlockGroup(4, 16), new EcBlockGroup(4, 17) }),
            new EcBlocks(24, new[] { new EcBlockGroup(4, 12), new EcBlockGroup(4, 13) }),
        }),
        new VersionInfo(10, new[] { 6, 28, 50 }, new[]
        {
            new EcBlocks(18, new[] { new EcBlockGroup(2, 68), new EcBlockGroup(2, 69) }),
            new EcBlocks(26, new[] { new EcBlockGroup(4, 43), new EcBlockGroup(1, 44) }),
            new EcBlocks(24, new[] { new EcBlockGroup(6, 19), new EcBlockGroup(2, 20) }),
            new EcBlocks(28, new[] { new EcBlockGroup(6, 15), new EcBlockGroup(2, 16) }),
        }),
        new VersionInfo(11, new[] { 6, 30, 54 }, new[]
        {
            new EcBlocks(20, new[] { new EcBlockGroup(4, 81) }),
            new EcBlocks(30, new[] { new EcBlockGroup(1, 50), new EcBlockGroup(4, 51) }),
            new EcBlocks(28, new[] { new EcBlockGroup(4, 22), new EcBlockGroup(4, 23) }),
            new EcBlocks(24, new[] { new EcBlockGroup(3, 12), new EcBlockGroup(8, 13) }),
        }),
        new VersionInfo(12, new[] { 6, 32, 58 }, new[]
        {
            new EcBlocks(24, new[] { new EcBlockGroup(2, 92), new EcBlockGroup(2, 93) }),
            new EcBlocks(22, new[] { new EcBlockGroup(6, 36), new EcBlockGroup(2, 37) }),
            new EcBlocks(26, new[] { new EcBlockGroup(4, 20), new EcBlockGroup(6, 21) }),
            new EcBlocks(28, new[] { new EcBlockGroup(7, 14), new EcBlockGroup(4, 15) }),
        }),
        new VersionInfo(13, new[] { 6, 34, 62 }, new[]
        {
            new EcBlocks(26, new[] { new EcBlockGroup(4, 107) }),
            new EcBlocks(22, new[] { new EcBlockGroup(8, 37), new EcBlockGroup(1, 38) }),
            new EcBlocks(24, new[] { new EcBlockGroup(8, 20), new EcBlockGroup(4, 21) }),
            new EcBlocks(22, new[] { new EcBlockGroup(12, 11), new EcBlockGroup(4, 12) }),
        }),
        new VersionInfo(14, new[] { 6, 26, 46, 66 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(3, 115), new EcBlockGroup(1, 116) }),
            new EcBlocks(24, new[] { new EcBlockGroup(4, 40), new EcBlockGroup(5, 41) }),
            new EcBlocks(20, new[] { new EcBlockGroup(11, 16), new EcBlockGroup(5, 17) }),
            new EcBlocks(24, new[] { new EcBlockGroup(11, 12), new EcBlockGroup(5, 13) }),
        }),
        new VersionInfo(15, new[] { 6, 26, 48, 70 }, new[]
        {
            new EcBlocks(22, new[] { new EcBlockGroup(5, 87), new EcBlockGroup(1, 88) }),
            new EcBlocks(24, new[] { new EcBlockGroup(5, 41), new EcBlockGroup(5, 42) }),
            new EcBlocks(30, new[] { new EcBlockGroup(5, 24), new EcBlockGroup(7, 25) }),
            new EcBlocks(24, new[] { new EcBlockGroup(11, 12), new EcBlockGroup(7, 13) }),
        }),
        new VersionInfo(16, new[] { 6, 26, 50, 74 }, new[]
        {
            new EcBlocks(24, new[] { new EcBlockGroup(5, 98), new EcBlockGroup(1, 99) }),
            new EcBlocks(28, new[] { new EcBlockGroup(7, 45), new EcBlockGroup(3, 46) }),
            new EcBlocks(24, new[] { new EcBlockGroup(15, 19), new EcBlockGroup(2, 20) }),
            new EcBlocks(30, new[] { new EcBlockGroup(3, 15), new EcBlockGroup(13, 16) }),
        }),
        new VersionInfo(17, new[] { 6, 30, 54, 78 }, new[]
        {
            new EcBlocks(28, new[] { new EcBlockGroup(1, 107), new EcBlockGroup(5, 108) }),
            new EcBlocks(28, new[] { new EcBlockGroup(10, 46), new EcBlockGroup(1, 47) }),
            new EcBlocks(28, new[] { new EcBlockGroup(1, 22), new EcBlockGroup(15, 23) }),
            new EcBlocks(28, new[] { new EcBlockGroup(2, 14), new EcBlockGroup(17, 15) }),
        }),
        new VersionInfo(18, new[] { 6, 30, 56, 82 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(5, 120), new EcBlockGroup(1, 121) }),
            new EcBlocks(26, new[] { new EcBlockGroup(9, 43), new EcBlockGroup(4, 44) }),
            new EcBlocks(28, new[] { new EcBlockGroup(17, 22), new EcBlockGroup(1, 23) }),
            new EcBlocks(28, new[] { new EcBlockGroup(2, 14), new EcBlockGroup(19, 15) }),
        }),
        new VersionInfo(19, new[] { 6, 30, 58, 86 }, new[]
        {
            new EcBlocks(28, new[] { new EcBlockGroup(3, 113), new EcBlockGroup(4, 114) }),
            new EcBlocks(26, new[] { new EcBlockGroup(3, 44), new EcBlockGroup(11, 45) }),
            new EcBlocks(26, new[] { new EcBlockGroup(17, 21), new EcBlockGroup(4, 22) }),
            new EcBlocks(26, new[] { new EcBlockGroup(9, 13), new EcBlockGroup(16, 14) }),
        }),
        new VersionInfo(20, new[] { 6, 34, 62, 90 }, new[]
        {
            new EcBlocks(28, new[] { new EcBlockGroup(3, 107), new EcBlockGroup(5, 108) }),
            new EcBlocks(26, new[] { new EcBlockGroup(3, 41), new EcBlockGroup(13, 42) }),
            new EcBlocks(30, new[] { new EcBlockGroup(15, 24), new EcBlockGroup(5, 25) }),
            new EcBlocks(28, new[] { new EcBlockGroup(15, 15), new EcBlockGroup(10, 16) }),
        }),
        new VersionInfo(21, new[] { 6, 28, 50, 72, 94 }, new[]
        {
            new EcBlocks(28, new[] { new EcBlockGroup(4, 116), new EcBlockGroup(4, 117) }),
            new EcBlocks(26, new[] { new EcBlockGroup(17, 42) }),
            new EcBlocks(28, new[] { new EcBlockGroup(17, 22), new EcBlockGroup(6, 23) }),
            new EcBlocks(30, new[] { new EcBlockGroup(19, 16), new EcBlockGroup(6, 17) }),
        }),
        new VersionInfo(22, new[] { 6, 26, 50, 74, 98 }, new[]
        {
            new EcBlocks(28, new[] { new EcBlockGroup(2, 111), new EcBlockGroup(7, 112) }),
            new EcBlocks(28, new[] { new EcBlockGroup(17, 46) }),
            new EcBlocks(30, new[] { new EcBlockGroup(7, 24), new EcBlockGroup(16, 25) }),
            new EcBlocks(24, new[] { new EcBlockGroup(34, 13) }),
        }),
        new VersionInfo(23, new[] { 6, 30, 54, 78, 102 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(4, 121), new EcBlockGroup(5, 122) }),
            new EcBlocks(28, new[] { new EcBlockGroup(4, 47), new EcBlockGroup(14, 48) }),
            new EcBlocks(30, new[] { new EcBlockGroup(11, 24), new EcBlockGroup(14, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(16, 15), new EcBlockGroup(14, 16) }),
        }),
        new VersionInfo(24, new[] { 6, 28, 54, 80, 106 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(6, 117), new EcBlockGroup(4, 118) }),
            new EcBlocks(28, new[] { new EcBlockGroup(6, 45), new EcBlockGroup(14, 46) }),
            new EcBlocks(30, new[] { new EcBlockGroup(11, 24), new EcBlockGroup(16, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(30, 16), new EcBlockGroup(2, 17) }),
        }),
        new VersionInfo(25, new[] { 6, 32, 58, 84, 110 }, new[]
        {
            new EcBlocks(26, new[] { new EcBlockGroup(8, 106), new EcBlockGroup(4, 107) }),
            new EcBlocks(28, new[] { new EcBlockGroup(8, 47), new EcBlockGroup(13, 48) }),
            new EcBlocks(30, new[] { new EcBlockGroup(7, 24), new EcBlockGroup(22, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(22, 15), new EcBlockGroup(13, 16) }),
        }),
        new VersionInfo(26, new[] { 6, 30, 58, 86, 114 }, new[]
        {
            new EcBlocks(28, new[] { new EcBlockGroup(10, 114), new EcBlockGroup(2, 115) }),
            new EcBlocks(28, new[] { new EcBlockGroup(19, 46), new EcBlockGroup(4, 47) }),
            new EcBlocks(28, new[] { new EcBlockGroup(28, 22), new EcBlockGroup(6, 23) }),
            new EcBlocks(30, new[] { new EcBlockGroup(33, 16), new EcBlockGroup(4, 17) }),
        }),
        new VersionInfo(27, new[] { 6, 34, 62, 90, 118 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(8, 122), new EcBlockGroup(4, 123) }),
            new EcBlocks(28, new[] { new EcBlockGroup(22, 45), new EcBlockGroup(3, 46) }),
            new EcBlocks(30, new[] { new EcBlockGroup(8, 23), new EcBlockGroup(26, 24) }),
            new EcBlocks(30, new[] { new EcBlockGroup(12, 15), new EcBlockGroup(28, 16) }),
        }),
        new VersionInfo(28, new[] { 6, 26, 50, 74, 98, 122 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(3, 117), new EcBlockGroup(10, 118) }),
            new EcBlocks(28, new[] { new EcBlockGroup(3, 45), new EcBlockGroup(23, 46) }),
            new EcBlocks(30, new[] { new EcBlockGroup(4, 24), new EcBlockGroup(31, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(11, 15), new EcBlockGroup(31, 16) }),
        }),
        new VersionInfo(29, new[] { 6, 30, 54, 78, 102, 126 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(7, 116), new EcBlockGroup(7, 117) }),
            new EcBlocks(28, new[] { new EcBlockGroup(21, 45), new EcBlockGroup(7, 46) }),
            new EcBlocks(30, new[] { new EcBlockGroup(1, 23), new EcBlockGroup(37, 24) }),
            new EcBlocks(30, new[] { new EcBlockGroup(19, 15), new EcBlockGroup(26, 16) }),
        }),
        new VersionInfo(30, new[] { 6, 26, 52, 78, 104, 130 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(5, 115), new EcBlockGroup(10, 116) }),
            new EcBlocks(28, new[] { new EcBlockGroup(19, 47), new EcBlockGroup(10, 48) }),
            new EcBlocks(30, new[] { new EcBlockGroup(15, 24), new EcBlockGroup(25, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(23, 15), new EcBlockGroup(25, 16) }),
        }),
        new VersionInfo(31, new[] { 6, 30, 56, 82, 108, 134 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(13, 115), new EcBlockGroup(3, 116) }),
            new EcBlocks(28, new[] { new EcBlockGroup(2, 46), new EcBlockGroup(29, 47) }),
            new EcBlocks(30, new[] { new EcBlockGroup(42, 24), new EcBlockGroup(1, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(23, 15), new EcBlockGroup(28, 16) }),
        }),
        new VersionInfo(32, new[] { 6, 34, 60, 86, 112, 138 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(17, 115) }),
            new EcBlocks(28, new[] { new EcBlockGroup(10, 46), new EcBlockGroup(23, 47) }),
            new EcBlocks(30, new[] { new EcBlockGroup(10, 24), new EcBlockGroup(35, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(19, 15), new EcBlockGroup(35, 16) }),
        }),
        new VersionInfo(33, new[] { 6, 30, 58, 86, 114, 142 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(17, 115), new EcBlockGroup(1, 116) }),
            new EcBlocks(28, new[] { new EcBlockGroup(14, 46), new EcBlockGroup(21, 47) }),
            new EcBlocks(30, new[] { new EcBlockGroup(29, 24), new EcBlockGroup(19, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(11, 15), new EcBlockGroup(46, 16) }),
        }),
        new VersionInfo(34, new[] { 6, 34, 62, 90, 118, 146 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(13, 115), new EcBlockGroup(6, 116) }),
            new EcBlocks(28, new[] { new EcBlockGroup(14, 46), new EcBlockGroup(23, 47) }),
            new EcBlocks(30, new[] { new EcBlockGroup(44, 24), new EcBlockGroup(7, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(59, 16), new EcBlockGroup(1, 17) }),
        }),
        new VersionInfo(35, new[] { 6, 30, 54, 78, 102, 126, 150 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(12, 121), new EcBlockGroup(7, 122) }),
            new EcBlocks(28, new[] { new EcBlockGroup(12, 47), new EcBlockGroup(26, 48) }),
            new EcBlocks(30, new[] { new EcBlockGroup(39, 24), new EcBlockGroup(14, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(22, 15), new EcBlockGroup(41, 16) }),
        }),
        new VersionInfo(36, new[] { 6, 24, 50, 76, 102, 128, 154 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(6, 121), new EcBlockGroup(14, 122) }),
            new EcBlocks(28, new[] { new EcBlockGroup(6, 47), new EcBlockGroup(34, 48) }),
            new EcBlocks(30, new[] { new EcBlockGroup(46, 24), new EcBlockGroup(10, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(2, 15), new EcBlockGroup(64, 16) }),
        }),
        new VersionInfo(37, new[] { 6, 28, 54, 80, 106, 132, 158 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(17, 122), new EcBlockGroup(4, 123) }),
            new EcBlocks(28, new[] { new EcBlockGroup(29, 46), new EcBlockGroup(14, 47) }),
            new EcBlocks(30, new[] { new EcBlockGroup(49, 24), new EcBlockGroup(10, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(24, 15), new EcBlockGroup(46, 16) }),
        }),
        new VersionInfo(38, new[] { 6, 32, 58, 84, 110, 136, 162 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(4, 122), new EcBlockGroup(18, 123) }),
            new EcBlocks(28, new[] { new EcBlockGroup(13, 46), new EcBlockGroup(32, 47) }),
            new EcBlocks(30, new[] { new EcBlockGroup(48, 24), new EcBlockGroup(14, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(42, 15), new EcBlockGroup(32, 16) }),
        }),
        new VersionInfo(39, new[] { 6, 26, 54, 82, 110, 138, 166 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(20, 117), new EcBlockGroup(4, 118) }),
            new EcBlocks(28, new[] { new EcBlockGroup(40, 47), new EcBlockGroup(7, 48) }),
            new EcBlocks(30, new[] { new EcBlockGroup(43, 24), new EcBlockGroup(22, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(10, 15), new EcBlockGroup(67, 16) }),
        }),
        new VersionInfo(40, new[] { 6, 30, 58, 86, 114, 142, 170 }, new[]
        {
            new EcBlocks(30, new[] { new EcBlockGroup(19, 118), new EcBlockGroup(6, 119) }),
            new EcBlocks(28, new[] { new EcBlockGroup(18, 47), new EcBlockGroup(31, 48) }),
            new EcBlocks(30, new[] { new EcBlockGroup(34, 24), new EcBlockGroup(34, 25) }),
            new EcBlocks(30, new[] { new EcBlockGroup(20, 15), new EcBlockGroup(61, 16) }),
        }),
    };

    // Precomputed 18-bit BCH version-info codewords for versions 7-40 (index 0 = version 7).
    public static readonly int[] VersionDecodeInfo =
    {
        0x07C94, 0x085BC, 0x09A99, 0x0A4D3, 0x0BBF6,
        0x0C762, 0x0D847, 0x0E60D, 0x0F928, 0x10B78,
        0x1145D, 0x12A17, 0x13532, 0x149A6, 0x15683,
        0x168C9, 0x177EC, 0x18EC4, 0x191E1, 0x1AFAB,
        0x1B08E, 0x1CC1A, 0x1D33F, 0x1ED75, 0x1F250,
        0x209D5, 0x216F0, 0x228BA, 0x2379F, 0x24B0B,
        0x2542E, 0x26A64, 0x27541, 0x28C69,
    };
}
