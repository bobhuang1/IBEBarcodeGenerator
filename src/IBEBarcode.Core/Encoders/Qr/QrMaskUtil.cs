namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrMaskUtil
{
    private const int N1 = 3;
    private const int N2 = 3;
    private const int N3 = 40;
    private const int N4 = 10;

    private const int TypeInfoPoly = 0x537;
    private const int TypeInfoMaskPattern = 0x5412;

    public static bool GetDataMaskBit(int maskPattern, int x, int y)
    {
        int intermediate;
        int temp;

        switch (maskPattern)
        {
            case 0:
                intermediate = (y + x) & 0x1;
                break;
            case 1:
                intermediate = y & 0x1;
                break;
            case 2:
                intermediate = x % 3;
                break;
            case 3:
                intermediate = (y + x) % 3;
                break;
            case 4:
                intermediate = ((y / 2) + (x / 3)) & 0x1;
                break;
            case 5:
                temp = y * x;
                intermediate = (temp & 0x1) + (temp % 3);
                break;
            case 6:
                temp = y * x;
                intermediate = ((temp & 0x1) + (temp % 3)) & 0x1;
                break;
            case 7:
                temp = y * x;
                intermediate = ((temp % 3) + ((y + x) & 0x1)) & 0x1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(maskPattern));
        }

        return intermediate == 0;
    }

    public static int TotalPenalty(bool[,] modules) =>
        ApplyMaskPenaltyRule1(modules)
        + ApplyMaskPenaltyRule2(modules)
        + ApplyMaskPenaltyRule3(modules)
        + ApplyMaskPenaltyRule4(modules);

    public static int ApplyMaskPenaltyRule1(bool[,] modules)
    {
        var width = modules.GetLength(0);
        var height = modules.GetLength(1);
        return ApplyMaskPenaltyRule1Internal(modules, width, height, true)
            + ApplyMaskPenaltyRule1Internal(modules, width, height, false);
    }

    private static int ApplyMaskPenaltyRule1Internal(bool[,] modules, int width, int height, bool isHorizontal)
    {
        var penalty = 0;
        var iLimit = isHorizontal ? height : width;
        var jLimit = isHorizontal ? width : height;

        for (var i = 0; i < iLimit; i++)
        {
            var numSameBitCells = 0;
            var prevBit = -1;

            for (var j = 0; j < jLimit; j++)
            {
                var bit = (isHorizontal ? modules[j, i] : modules[i, j]) ? 1 : 0;

                if (bit == prevBit)
                {
                    numSameBitCells++;
                }
                else
                {
                    if (numSameBitCells >= 5)
                    {
                        penalty += N1 + (numSameBitCells - 5);
                    }

                    numSameBitCells = 1;
                    prevBit = bit;
                }
            }

            if (numSameBitCells >= 5)
            {
                penalty += N1 + (numSameBitCells - 5);
            }
        }

        return penalty;
    }

    public static int ApplyMaskPenaltyRule2(bool[,] modules)
    {
        var width = modules.GetLength(0);
        var height = modules.GetLength(1);
        var penalty = 0;

        for (var y = 0; y < height - 1; y++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                var value = modules[x, y];

                if (value == modules[x + 1, y] && value == modules[x, y + 1] && value == modules[x + 1, y + 1])
                {
                    penalty++;
                }
            }
        }

        return N2 * penalty;
    }

    public static int ApplyMaskPenaltyRule3(bool[,] modules)
    {
        var width = modules.GetLength(0);
        var height = modules.GetLength(1);
        var numPenalties = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (x + 6 < width
                    && modules[x, y] && !modules[x + 1, y] && modules[x + 2, y] && modules[x + 3, y] && modules[x + 4, y] && !modules[x + 5, y] && modules[x + 6, y]
                    && (IsWhiteHorizontal(modules, width, y, x - 4, x) || IsWhiteHorizontal(modules, width, y, x + 7, x + 11)))
                {
                    numPenalties++;
                }

                if (y + 6 < height
                    && modules[x, y] && !modules[x, y + 1] && modules[x, y + 2] && modules[x, y + 3] && modules[x, y + 4] && !modules[x, y + 5] && modules[x, y + 6]
                    && (IsWhiteVertical(modules, height, x, y - 4, y) || IsWhiteVertical(modules, height, x, y + 7, y + 11)))
                {
                    numPenalties++;
                }
            }
        }

        return numPenalties * N3;
    }

    private static bool IsWhiteHorizontal(bool[,] modules, int width, int y, int from, int to)
    {
        if (from < 0 || width < to)
        {
            return false;
        }

        for (var i = from; i < to; i++)
        {
            if (modules[i, y])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWhiteVertical(bool[,] modules, int height, int x, int from, int to)
    {
        if (from < 0 || height < to)
        {
            return false;
        }

        for (var i = from; i < to; i++)
        {
            if (modules[x, i])
            {
                return false;
            }
        }

        return true;
    }

    public static int ApplyMaskPenaltyRule4(bool[,] modules)
    {
        var width = modules.GetLength(0);
        var height = modules.GetLength(1);
        var numDarkCells = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (modules[x, y])
                {
                    numDarkCells++;
                }
            }
        }

        var numTotalCells = width * height;
        var fivePercentVariances = Math.Abs((numDarkCells * 2) - numTotalCells) * 10 / numTotalCells;
        return fivePercentVariances * N4;
    }

    public static string ComputeFormatString(char level, int maskPattern)
    {
        var levelBits = level switch
        {
            'L' => 0x01,
            'M' => 0x00,
            'Q' => 0x03,
            'H' => 0x02,
            _ => throw new ArgumentOutOfRangeException(nameof(level)),
        };

        var typeInfo = (levelBits << 3) | maskPattern;
        var bchCode = CalculateBchCode(typeInfo, TypeInfoPoly);
        var combined = (typeInfo << 10) | bchCode;
        var masked = combined ^ TypeInfoMaskPattern;

        var chars = new char[15];

        for (var i = 0; i < 15; i++)
        {
            chars[i] = ((masked >> (14 - i)) & 1) != 0 ? '1' : '0';
        }

        return new string(chars);
    }

    private static int FindMsbSet(int value)
    {
        var count = 0;

        while (value != 0)
        {
            value >>= 1;
            count++;
        }

        return count;
    }

    private static int CalculateBchCode(int value, int poly)
    {
        var msbSetInPoly = FindMsbSet(poly);
        value <<= msbSetInPoly - 1;

        while (FindMsbSet(value) >= msbSetInPoly)
        {
            value ^= poly << (FindMsbSet(value) - msbSetInPoly);
        }

        return value;
    }
}
