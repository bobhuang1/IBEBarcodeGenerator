using System.Text;
using IBEBarcode.Core.Encoders.Qr;

namespace IBEBarcode.Core.Encoders;

public sealed class QrEncoder : IMatrixBarcodeEncoder
{
    public BarcodeSymbology Symbology => BarcodeSymbology.QrCode;

    private readonly char _level;

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelL =
    {
        (1, 19, 7), (2, 34, 10), (3, 55, 15), (4, 80, 20), (5, 108, 26),
    };

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelM =
    {
        (1, 16, 10), (2, 28, 16), (3, 44, 26),
    };

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelQ =
    {
        (1, 13, 13), (2, 22, 22),
    };

    private static readonly (int Version, int DataCodewords, int EccCodewords)[] LevelH =
    {
        (1, 9, 17), (2, 16, 28),
    };

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

        if (!TrySelectVersion(dataBytes.Length, out var version, out var dataCw, out var eccCw))
        {
            error = $"Value is too large to encode at error correction level {_level} within the supported QR versions (1-5, single-block only).";
            return false;
        }

        var codewords = BuildCodewords(dataBytes, dataCw, eccCw);
        var size = 17 + 4 * version;
        var modules = new bool[size, size];
        var reserved = new bool[size, size];

        PlaceFinderPattern(modules, reserved, 0, 0, size);
        PlaceFinderPattern(modules, reserved, size - 7, 0, size);
        PlaceFinderPattern(modules, reserved, 0, size - 7, size);
        PlaceTimingPatterns(modules, reserved, size);
        PlaceAlignmentPattern(modules, reserved, version, size);
        PlaceDarkModule(modules, reserved, size);
        PlaceFormatInfo(modules, reserved, size);
        PlaceDataBits(modules, reserved, codewords, size);

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }

    private (int Version, int DataCodewords, int EccCodewords)[] SupportedCombos() => _level switch
    {
        'L' => LevelL,
        'M' => LevelM,
        'Q' => LevelQ,
        'H' => LevelH,
        _ => throw new InvalidOperationException(),
    };

    private bool TrySelectVersion(int dataByteCount, out int version, out int dataCw, out int eccCw)
    {
        foreach (var (v, dcw, ecw) in SupportedCombos())
        {
            var neededBits = 4 + 8 + dataByteCount * 8;

            if (neededBits <= dcw * 8)
            {
                version = v;
                dataCw = dcw;
                eccCw = ecw;
                return true;
            }
        }

        version = 0;
        dataCw = 0;
        eccCw = 0;
        return false;
    }

    private static byte[] BuildCodewords(byte[] dataBytes, int dataCw, int eccCw)
    {
        var writer = new QrBitWriter();
        writer.AppendBits(0b0100, 4);
        writer.AppendBits(dataBytes.Length, 8);

        foreach (var b in dataBytes)
        {
            writer.AppendBits(b, 8);
        }

        var capacityBits = dataCw * 8;
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

        while (dataCodewords.Count < dataCw)
        {
            dataCodewords.Add(padBytes[padIndex % 2]);
            padIndex++;
        }

        var dataArray = dataCodewords.ToArray();
        var eccCodewords = QrReedSolomon.ComputeEccCodewords(dataArray, eccCw);

        var allCodewords = new byte[dataCw + eccCw];
        Array.Copy(dataArray, allCodewords, dataCw);
        Array.Copy(eccCodewords, 0, allCodewords, dataCw, eccCw);
        return allCodewords;
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

    private static void PlaceAlignmentPattern(bool[,] modules, bool[,] reserved, int version, int size)
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
                var x = start + dx;
                var y = start + dy;
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
