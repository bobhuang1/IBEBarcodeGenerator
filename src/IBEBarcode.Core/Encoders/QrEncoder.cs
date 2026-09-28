using System.Text;
using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Encoders;

public sealed class QrEncoder : IMatrixBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.QrCode;

    private readonly char _level;
    private static readonly char[] LevelOrder = { 'L', 'M', 'Q', 'H' };

    private static readonly Dictionary<char, string> FormatStringsMask0 = new()
    {
        ['L'] = "111011111000100",
        ['M'] = "101010000010010",
        ['Q'] = "011010101011111",
        ['H'] = "001011010001001",
    };

    private static readonly (int X, int Y)[] FormatInfoCoordinates =
    {
        (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7),
        (8, 8), (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8),
    };

    public QrEncoder(char errorCorrectionLevel = 'M')
    {
        if (errorCorrectionLevel is not ('L' or 'M' or 'Q' or 'H'))
        {
            throw new ArgumentOutOfRangeException(nameof(errorCorrectionLevel), "Error correction level must be L, M, Q, or H.");
        }

        _level = errorCorrectionLevel;
    }

    public bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error)
    {
        matrix = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var dataBytes = Encoding.UTF8.GetBytes(value);
        var levelIndex = Array.IndexOf(LevelOrder, _level);

        if (!TrySelectVersion(dataBytes.Length, levelIndex, out var versionInfo))
        {
            error = $"Value is too large to encode at error correction level {_level} within the supported QR versions (1-40).";
            return false;
        }

        var countBits = versionInfo.Version <= 9 ? 8 : 16;
        var codewords = BuildCodewords(dataBytes, versionInfo.LevelsLmqh[levelIndex], countBits);
        var version = versionInfo.Version;
        var size = 17 + 4 * version;
        var modules = new bool[size, size];
        var reserved = new bool[size, size];

        PlaceFinderPattern(modules, reserved, 0, 0, size);
        PlaceFinderPattern(modules, reserved, size - 7, 0, size);
        PlaceFinderPattern(modules, reserved, 0, size - 7, size);
        PlaceTimingPatterns(modules, reserved, size);
        PlaceAlignmentPatterns(modules, reserved, versionInfo.AlignmentCenters);
        PlaceDarkModule(modules, reserved, size);
        PlaceFormatInfo(modules, reserved, size);

        if (version > 6)
        {
            PlaceVersionInfo(modules, reserved, version, size);
        }

        PlaceDataBits(modules, reserved, codewords, size);

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }

    private static bool TrySelectVersion(int dataByteCount, int levelIndex, out QrVersionTable.VersionInfo versionInfo)
    {
        foreach (var candidate in QrVersionTable.Versions)
        {
            var countBits = candidate.Version <= 9 ? 8 : 16;
            var neededBits = 4 + countBits + (dataByteCount * 8);
            var totalDataCodewords = TotalDataCodewords(candidate.LevelsLmqh[levelIndex]);

            if (neededBits <= totalDataCodewords * 8)
            {
                versionInfo = candidate;
                return true;
            }
        }

        versionInfo = null!;
        return false;
    }

    private static int TotalDataCodewords(QrVersionTable.EcBlocks ecBlocks)
    {
        var total = 0;

        foreach (var group in ecBlocks.Groups)
        {
            total += group.Count * group.DataCodewords;
        }

        return total;
    }

    private static byte[] BuildCodewords(byte[] dataBytes, QrVersionTable.EcBlocks ecBlocks, int countBits)
    {
        var totalDataCodewords = TotalDataCodewords(ecBlocks);

        var writer = new QrBitWriter();
        writer.AppendBits(0b0100, 4);
        writer.AppendBits(dataBytes.Length, countBits);

        foreach (var b in dataBytes)
        {
            writer.AppendBits(b, 8);
        }

        var capacityBits = totalDataCodewords * 8;
        var terminatorBits = Math.Min(4, capacityBits - writer.Count);

        if (terminatorBits > 0)
        {
            writer.AppendBits(0, terminatorBits);
        }

        while (writer.Count % 8 != 0)
        {
            writer.AppendBits(0, 1);
        }

        var dataCodewords = writer.ToBytes().ToList();
        var padBytes = new byte[] { 0xEC, 0x11 };
        var padIndex = 0;

        while (dataCodewords.Count < totalDataCodewords)
        {
            dataCodewords.Add(padBytes[padIndex % 2]);
            padIndex++;
        }

        var dataArray = dataCodewords.ToArray();

        // Split into per-block data slices per the version's EC block groups, in order.
        var blocks = new List<(byte[] Data, byte[] Ecc)>();
        var offset = 0;

        foreach (var group in ecBlocks.Groups)
        {
            for (var i = 0; i < group.Count; i++)
            {
                var blockData = new byte[group.DataCodewords];
                Array.Copy(dataArray, offset, blockData, 0, group.DataCodewords);
                offset += group.DataCodewords;

                var blockEcc = QrReedSolomon.ComputeEccCodewords(blockData, ecBlocks.EccCodewordsPerBlock);
                blocks.Add((blockData, blockEcc));
            }
        }

        // Interleave: data codewords round-robin by index (ragged -- later blocks in a
        // version can have one more data codeword than earlier ones), then ECC codewords
        // round-robin (always uniform length across blocks).
        var result = new List<byte>();
        var maxDataLength = blocks.Max(b => b.Data.Length);

        for (var i = 0; i < maxDataLength; i++)
        {
            foreach (var block in blocks)
            {
                if (i < block.Data.Length)
                {
                    result.Add(block.Data[i]);
                }
            }
        }

        for (var i = 0; i < ecBlocks.EccCodewordsPerBlock; i++)
        {
            foreach (var block in blocks)
            {
                result.Add(block.Ecc[i]);
            }
        }

        return result.ToArray();
    }

    private static void PlaceFinderPattern(bool[,] modules, bool[,] reserved, int xStart, int yStart, int size)
    {
        for (var dy = -1; dy <= 7; dy++)
        {
            for (var dx = -1; dx <= 7; dx++)
            {
                var x = xStart + dx;
                var y = yStart + dy;

                if (x < 0 || x >= size || y < 0 || y >= size)
                {
                    continue;
                }

                reserved[x, y] = true;

                if (dx is >= 0 and <= 6 && dy is >= 0 and <= 6)
                {
                    modules[x, y] = FinderPatternValue(dx, dy);
                }
            }
        }
    }

    private static bool FinderPatternValue(int x, int y)
    {
        if (x == 0 || x == 6 || y == 0 || y == 6)
        {
            return true;
        }

        return x is >= 2 and <= 4 && y is >= 2 and <= 4;
    }

    private static void PlaceTimingPatterns(bool[,] modules, bool[,] reserved, int size)
    {
        for (var i = 8; i < size - 8; i++)
        {
            var isDark = (i + 1) % 2 == 1;

            if (!reserved[i, 6])
            {
                modules[i, 6] = isDark;
                reserved[i, 6] = true;
            }

            if (!reserved[6, i])
            {
                modules[6, i] = isDark;
                reserved[6, i] = true;
            }
        }
    }

    private static void PlaceAlignmentPatterns(bool[,] modules, bool[,] reserved, int[] centers)
    {
        var max = centers.Length;

        for (var x = 0; x < max; x++)
        {
            var rowCenter = centers[x];

            for (var y = 0; y < max; y++)
            {
                if (x == 0 && (y == 0 || y == max - 1))
                {
                    continue;
                }

                if (x == max - 1 && y == 0)
                {
                    continue;
                }

                var colCenter = centers[y];
                PlaceOneAlignmentPattern(modules, reserved, colCenter, rowCenter);
            }
        }
    }

    private static void PlaceOneAlignmentPattern(bool[,] modules, bool[,] reserved, int centerX, int centerY)
    {
        var startX = centerX - 2;
        var startY = centerY - 2;

        for (var dy = 0; dy < 5; dy++)
        {
            for (var dx = 0; dx < 5; dx++)
            {
                var x = startX + dx;
                var y = startY + dy;
                reserved[x, y] = true;
                modules[x, y] = AlignmentPatternValue(dx, dy);
            }
        }
    }

    private static bool AlignmentPatternValue(int x, int y)
    {
        if (x == 0 || x == 4 || y == 0 || y == 4)
        {
            return true;
        }

        return x == 2 && y == 2;
    }

    private static void PlaceDarkModule(bool[,] modules, bool[,] reserved, int size)
    {
        var row = size - 8;
        modules[8, row] = true;
        reserved[8, row] = true;
    }

    private void PlaceFormatInfo(bool[,] modules, bool[,] reserved, int size)
    {
        var formatString = FormatStringsMask0[_level];

        for (var i = 0; i < 15; i++)
        {
            var bit = formatString[14 - i] == '1';

            var (x1, y1) = FormatInfoCoordinates[i];
            modules[x1, y1] = bit;
            reserved[x1, y1] = true;

            int x2, y2;

            if (i < 8)
            {
                x2 = size - i - 1;
                y2 = 8;
            }
            else
            {
                x2 = 8;
                y2 = size - 7 + (i - 8);
            }

            modules[x2, y2] = bit;
            reserved[x2, y2] = true;
        }
    }

    private static void PlaceVersionInfo(bool[,] modules, bool[,] reserved, int version, int size)
    {
        var versionBits = QrVersionTable.VersionDecodeInfo[version - 7];

        // Top-right block: 3 wide x 6 tall. Bit order (MSB first): for row=5 downto 0,
        // col=(size-9) downto (size-11).
        var k = 0;

        for (var row = 5; row >= 0; row--)
        {
            for (var col = size - 9; col >= size - 11; col--)
            {
                var bit = ((versionBits >> (17 - k)) & 1) != 0;
                modules[col, row] = bit;
                reserved[col, row] = true;
                k++;
            }
        }

        // Bottom-left block: 6 wide x 3 tall, same 18 bits transposed.
        k = 0;

        for (var col = 5; col >= 0; col--)
        {
            for (var row = size - 9; row >= size - 11; row--)
            {
                var bit = ((versionBits >> (17 - k)) & 1) != 0;
                modules[col, row] = bit;
                reserved[col, row] = true;
                k++;
            }
        }
    }

    private static void PlaceDataBits(bool[,] modules, bool[,] reserved, byte[] codewords, int size)
    {
        var bitIndex = 0;
        var totalBits = codewords.Length * 8;
        var direction = -1;
        var x = size - 1;

        while (x > 0)
        {
            if (x == 6)
            {
                x--;
            }

            var y = direction == -1 ? size - 1 : 0;

            while (y >= 0 && y < size)
            {
                for (var i = 0; i < 2; i++)
                {
                    var xx = x - i;

                    if (!reserved[xx, y])
                    {
                        bool bit;

                        if (bitIndex < totalBits)
                        {
                            var byteIndex = bitIndex / 8;
                            var bitInByte = 7 - (bitIndex % 8);
                            bit = ((codewords[byteIndex] >> bitInByte) & 1) != 0;
                            bitIndex++;
                        }
                        else
                        {
                            bit = false;
                        }

                        if ((xx + y) % 2 == 0)
                        {
                            bit = !bit;
                        }

                        modules[xx, y] = bit;
                        reserved[xx, y] = true;
                    }
                }

                y += direction;
            }

            direction = -direction;
            x -= 2;
        }
    }
}
