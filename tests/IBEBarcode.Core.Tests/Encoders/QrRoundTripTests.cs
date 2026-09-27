using System.Text;
using IBEBarcode.Core.Encoders;

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

        var (dataCw, _) = CodewordCounts(version, level);

        var reserved = new bool[size, size];
        MarkFinderReserved(reserved, 0, 0, size);
        MarkFinderReserved(reserved, size - 7, 0, size);
        MarkFinderReserved(reserved, 0, size - 7, size);
        MarkTimingReserved(reserved, size);
        MarkAlignmentReserved(reserved, version, size);
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

        var dataBits = bits.Take(dataCw * 8).ToArray();
        var mode = ReadBits(dataBits, 0, 4);
        Assert.Equal(0b0100, mode);

        var length = ReadBits(dataBits, 4, 8);
        var bytes = new byte[length];

        for (var i = 0; i < length; i++)
        {
            bytes[i] = (byte)ReadBits(dataBits, 12 + i * 8, 8);
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

    private static (int DataCw, int EccCw) CodewordCounts(int version, char level) => (version, level) switch
    {
        (1, 'L') => (19, 7),
        (1, 'M') => (16, 10),
        (1, 'Q') => (13, 13),
        (1, 'H') => (9, 17),
        (2, 'L') => (34, 10),
        (2, 'M') => (28, 16),
        (2, 'Q') => (22, 22),
        (2, 'H') => (16, 28),
        (3, 'L') => (55, 15),
        (3, 'M') => (44, 26),
        (4, 'L') => (80, 20),
        (5, 'L') => (108, 26),
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

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

    private static void MarkAlignmentReserved(bool[,] reserved, int version, int size)
    {
        if (version == 1)
        {
            return;
        }

        var center = version switch
        {
            2 => 18,
            3 => 22,
            4 => 26,
            5 => 30,
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

        var start = center - 2;

        for (var dy = 0; dy < 5; dy++)
        {
            for (var dx = 0; dx < 5; dx++)
            {
                reserved[start + dx, start + dy] = true;
            }
        }
    }
}
