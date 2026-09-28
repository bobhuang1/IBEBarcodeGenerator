using System.Text;
using IBEBarcode.Core.Encoders;
using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Tests.Encoders;

public class QrRoundTripTests
{
    [Theory]
    [InlineData("HI")]
    [InlineData("HELLO WORLD")]
    [InlineData("https://example.com/")]
    [InlineData("The quick brown fox jumps over lazy dog")]
    public void EncodeThenDecode_RoundTripsExactly(string original)
    {
        var encoder = new QrEncoder('M');
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("01")]
    [InlineData("012")]
    [InlineData("0123456789012345678901234567890")]
    public void EncodeThenDecode_NumericMode_RoundTripsExactly(string digits)
    {
        var encoder = new QrEncoder('M');
        var success = encoder.TryEncode(digits, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(digits, decoded);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABC")]
    [InlineData("HTTP://WWW.EXAMPLE.COM/PATH:123")]
    public void EncodeThenDecode_AlphanumericMode_RoundTripsExactly(string value)
    {
        var encoder = new QrEncoder('M');
        var success = encoder.TryEncode(value, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(value, decoded);
    }

    [Fact]
    public void EncodeThenDecode_HigherVersionMultiBlock_RoundTripsExactly()
    {
        // 300 bytes at level M forces version >= 10 (16-bit count field) with multiple
        // Reed-Solomon blocks and, once past version 6, real version-info codewords.
        var encoder = new QrEncoder('M');
        var value = string.Concat(Enumerable.Range(0, 300).Select(i => (char)('A' + (i % 26))));
        var success = encoder.TryEncode(value, out var matrix, out var error);

        Assert.True(success, error);

        var version = (matrix!.Width - 17) / 4;
        Assert.True(version >= 10, $"expected a higher version, got {version}");

        var decoded = Decode(matrix);

        Assert.Equal(value, decoded);
    }

    private static readonly (int X, int Y)[] FormatInfoCoordinates =
    {
        (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7),
        (8, 8), (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8),
    };

    private static readonly Dictionary<string, char> LevelByFormatString = new()
    {
        ["111011111000100"] = 'L',
        ["101010000010010"] = 'M',
        ["011010101011111"] = 'Q',
        ["001011010001001"] = 'H',
    };

    private static readonly char[] LevelOrder = { 'L', 'M', 'Q', 'H' };

    private static string Decode(BarcodeMatrix matrix)
    {
        var size = matrix.Width;
        var version = (size - 17) / 4;

        var formatBits = new char[15];

        for (var i = 0; i < 15; i++)
        {
            var (x, y) = FormatInfoCoordinates[i];
            formatBits[14 - i] = matrix[x, y] ? '1' : '0';
        }

        var formatString = new string(formatBits);
        Assert.True(LevelByFormatString.TryGetValue(formatString, out var level), $"Unrecognized format string: {formatString}");

        var levelIndex = Array.IndexOf(LevelOrder, level);
        var versionInfo = QrVersionTable.Versions[version - 1];
        var ecBlocks = versionInfo.LevelsLmqh[levelIndex];

        var reserved = new bool[size, size];
        MarkFinderReserved(reserved, 0, 0, size);
        MarkFinderReserved(reserved, size - 7, 0, size);
        MarkFinderReserved(reserved, 0, size - 7, size);
        MarkTimingReserved(reserved, size);
        MarkAlignmentReserved(reserved, versionInfo.AlignmentCenters);
        reserved[8, size - 8] = true;

        foreach (var (x, y) in FormatInfoCoordinates)
        {
            reserved[x, y] = true;
        }

        for (var i = 0; i < 8; i++)
        {
            reserved[size - i - 1, 8] = true;
        }

        for (var i = 8; i < 15; i++)
        {
            reserved[8, size - 7 + (i - 8)] = true;
        }

        if (version > 6)
        {
            for (var row = 0; row < 6; row++)
            {
                for (var col = size - 11; col <= size - 9; col++)
                {
                    reserved[col, row] = true;
                }
            }

            for (var col = 0; col < 6; col++)
            {
                for (var row = size - 11; row <= size - 9; row++)
                {
                    reserved[col, row] = true;
                }
            }
        }

        var bits = new List<bool>();
        var direction = -1;
        var x2 = size - 1;

        while (x2 > 0)
        {
            if (x2 == 6)
            {
                x2--;
            }

            var y = direction == -1 ? size - 1 : 0;

            while (y >= 0 && y < size)
            {
                for (var i = 0; i < 2; i++)
                {
                    var xx = x2 - i;

                    if (!reserved[xx, y])
                    {
                        var bit = matrix[xx, y];

                        if ((xx + y) % 2 == 0)
                        {
                            bit = !bit;
                        }

                        bits.Add(bit);
                    }
                }

                y += direction;
            }

            direction = -direction;
            x2 -= 2;
        }

        // Rebuild the block structure to de-interleave the codeword stream back into its
        // original per-block, then concatenated, order.
        var blockLengths = new List<int>();

        foreach (var group in ecBlocks.Groups)
        {
            for (var i = 0; i < group.Count; i++)
            {
                blockLengths.Add(group.DataCodewords);
            }
        }

        var totalDataCodewords = blockLengths.Sum();
        var maxBlockLength = blockLengths.Max();

        var codewordBits = new bool[totalDataCodewords * 8];
        var bitPos = 0;
        var blockOffsets = new int[blockLengths.Count];
        var running = 0;

        for (var b = 0; b < blockLengths.Count; b++)
        {
            blockOffsets[b] = running;
            running += blockLengths[b];
        }

        for (var i = 0; i < maxBlockLength; i++)
        {
            for (var b = 0; b < blockLengths.Count; b++)
            {
                if (i >= blockLengths[b])
                {
                    continue;
                }

                var destCodewordIndex = blockOffsets[b] + i;

                for (var bit = 0; bit < 8; bit++)
                {
                    codewordBits[(destCodewordIndex * 8) + bit] = bits[bitPos++];
                }
            }
        }

        var mode = ReadBits(codewordBits, 0, 4);
        var range = version <= 9 ? 0 : version <= 26 ? 1 : 2;

        int countBits = mode switch
        {
            0b0001 => new[] { 10, 12, 14 }[range],
            0b0010 => new[] { 9, 11, 13 }[range],
            0b0100 => new[] { 8, 16, 16 }[range],
            _ => throw new ArgumentOutOfRangeException(nameof(mode), $"Unexpected mode {mode}"),
        };

        var count = ReadBits(codewordBits, 4, countBits);
        var pos = 4 + countBits;

        if (mode == 0b0001)
        {
            var chars = new char[count];
            var written = 0;

            while (written < count)
            {
                var remaining = count - written;

                if (remaining >= 3)
                {
                    var v = ReadBits(codewordBits, pos, 10);
                    pos += 10;
                    chars[written] = (char)('0' + (v / 100));
                    chars[written + 1] = (char)('0' + ((v / 10) % 10));
                    chars[written + 2] = (char)('0' + (v % 10));
                    written += 3;
                }
                else if (remaining == 2)
                {
                    var v = ReadBits(codewordBits, pos, 7);
                    pos += 7;
                    chars[written] = (char)('0' + (v / 10));
                    chars[written + 1] = (char)('0' + (v % 10));
                    written += 2;
                }
                else
                {
                    var v = ReadBits(codewordBits, pos, 4);
                    pos += 4;
                    chars[written] = (char)('0' + v);
                    written += 1;
                }
            }

            return new string(chars);
        }

        if (mode == 0b0010)
        {
            // ISO/IEC 18004 Table 5, independently re-typed for this reverse lookup rather
            // than reusing QrAlphanumeric's forward table.
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
            var chars = new char[count];
            var written = 0;

            while (written < count)
            {
                var remaining = count - written;

                if (remaining >= 2)
                {
                    var v = ReadBits(codewordBits, pos, 11);
                    pos += 11;
                    chars[written] = alphabet[v / 45];
                    chars[written + 1] = alphabet[v % 45];
                    written += 2;
                }
                else
                {
                    var v = ReadBits(codewordBits, pos, 6);
                    pos += 6;
                    chars[written] = alphabet[v];
                    written += 1;
                }
            }

            return new string(chars);
        }

        var bytes = new byte[count];

        for (var i = 0; i < count; i++)
        {
            bytes[i] = (byte)ReadBits(codewordBits, pos + (i * 8), 8);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static int ReadBits(bool[] bits, int start, int count)
    {
        var value = 0;

        for (var i = 0; i < count; i++)
        {
            value = (value << 1) | (bits[start + i] ? 1 : 0);
        }

        return value;
    }

    private static void MarkFinderReserved(bool[,] reserved, int xStart, int yStart, int size)
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
            }
        }
    }

    private static void MarkTimingReserved(bool[,] reserved, int size)
    {
        for (var i = 8; i < size - 8; i++)
        {
            reserved[i, 6] = true;
            reserved[6, i] = true;
        }
    }

    private static void MarkAlignmentReserved(bool[,] reserved, int[] centers)
    {
        var max = centers.Length;

        for (var x = 0; x < max; x++)
        {
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

                var startX = centers[y] - 2;
                var startY = centers[x] - 2;

                for (var dy = 0; dy < 5; dy++)
                {
                    for (var dx = 0; dx < 5; dx++)
                    {
                        reserved[startX + dx, startY + dy] = true;
                    }
                }
            }
        }
    }
}
