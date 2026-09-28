using IBEBarcode.Core.Encoders.Pdf417;

namespace IBEBarcode.Core.Encoders;

public sealed class Pdf417Encoder : IMatrixBarcodeEncoder
{
    private const int MinCols = 2;
    private const int MaxCols = 30;
    private const int MinRows = 3;
    private const int MaxRows = 90;
    private const float DefaultModuleWidth = 0.357f;
    private const float Height = 2.0f;
    private const float PreferredRatio = 3.0f;
    private const int StartPattern = 0x1fea8;
    private const int StopPattern = 0x3fa29;

    private readonly int? _errorCorrectionLevel;
    private readonly bool _compact;
    private readonly bool _numericCompaction;

    public Pdf417Encoder(int? errorCorrectionLevel = null, bool compact = false, bool numericCompaction = false)
    {
        _errorCorrectionLevel = errorCorrectionLevel;
        _compact = compact;
        _numericCompaction = numericCompaction;
    }

    public BarcodeSymbology Symbology => BarcodeSymbology.Pdf417;

    public bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error)
    {
        matrix = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        int[] highLevel;

        if (_numericCompaction)
        {
            foreach (var ch in value)
            {
                if (ch is < '0' or > '9')
                {
                    error = $"Character '{ch}' is not a digit; numeric compaction requires an all-digit value.";
                    return false;
                }
            }

            highLevel = Pdf417NumericCompaction.EncodeDigits(value);
        }
        else
        {
            var bytes = new byte[value.Length];

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] > 255)
                {
                    error = $"Character '{value[i]}' is outside the 0-255 byte range this PDF417 encoder supports.";
                    return false;
                }

                bytes[i] = (byte)value[i];
            }

            highLevel = Pdf417HighLevelEncoder.EncodeBytes(bytes);
        }

        var sourceCodeWords = highLevel.Length;

        int level;

        if (_errorCorrectionLevel is int explicitLevel)
        {
            if (explicitLevel is < 0 or > 8)
            {
                error = "Error correction level must be between 0 and 8.";
                return false;
            }

            level = explicitLevel;
        }
        else
        {
            level = RecommendedMinimumLevel(sourceCodeWords);

            if (level < 0)
            {
                error = "Value is too large to encode: this PDF417 encoder supports at most 863 source codewords.";
                return false;
            }
        }

        var errorCorrectionCodeWords = 1 << (level + 1);

        if (!TryDetermineDimensions(sourceCodeWords, errorCorrectionCodeWords, out var cols, out var rows))
        {
            error = "Unable to fit message within the supported column/row range.";
            return false;
        }

        var pad = GetNumberOfPadCodewords(sourceCodeWords, errorCorrectionCodeWords, cols, rows);

        if (sourceCodeWords + errorCorrectionCodeWords + 1 > 929)
        {
            error = "Encoded message contains too many codewords.";
            return false;
        }

        var n = sourceCodeWords + pad + 1;
        var dataCodewords = new int[n];
        dataCodewords[0] = n;
        Array.Copy(highLevel, 0, dataCodewords, 1, sourceCodeWords);

        for (var i = 0; i < pad; i++)
        {
            dataCodewords[1 + sourceCodeWords + i] = 900;
        }

        var ecCodewords = Pdf417ErrorCorrection.Generate(dataCodewords, level);

        var fullCodewords = new int[dataCodewords.Length + ecCodewords.Length];
        Array.Copy(dataCodewords, fullCodewords, dataCodewords.Length);
        Array.Copy(ecCodewords, 0, fullCodewords, dataCodewords.Length, ecCodewords.Length);

        var width = _compact ? 17 * cols + 35 : 17 * cols + 69;
        var modules = new bool[width, rows];

        var idx = 0;

        for (var y = 0; y < rows; y++)
        {
            var cluster = y % 3;
            var col = 0;

            col = WritePattern(modules, col, y, StartPattern, 17);

            int left;

            if (cluster == 0)
            {
                left = (30 * (y / 3)) + ((rows - 1) / 3);
            }
            else if (cluster == 1)
            {
                left = (30 * (y / 3)) + (level * 3) + ((rows - 1) % 3);
            }
            else
            {
                left = (30 * (y / 3)) + (cols - 1);
            }

            col = WritePattern(modules, col, y, Pdf417CodewordTable.Patterns[cluster][left], 17);

            for (var x = 0; x < cols; x++)
            {
                col = WritePattern(modules, col, y, Pdf417CodewordTable.Patterns[cluster][fullCodewords[idx]], 17);
                idx++;
            }

            if (_compact)
            {
                WritePattern(modules, col, y, StopPattern, 1);
            }
            else
            {
                int right;

                if (cluster == 0)
                {
                    right = (30 * (y / 3)) + (cols - 1);
                }
                else if (cluster == 1)
                {
                    right = (30 * (y / 3)) + ((rows - 1) / 3);
                }
                else
                {
                    right = (30 * (y / 3)) + (level * 3) + ((rows - 1) % 3);
                }

                col = WritePattern(modules, col, y, Pdf417CodewordTable.Patterns[cluster][right], 17);
                WritePattern(modules, col, y, StopPattern, 18);
            }
        }

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }

    private static int WritePattern(bool[,] modules, int startCol, int row, int pattern, int bitCount)
    {
        for (var i = 0; i < bitCount; i++)
        {
            modules[startCol + i, row] = ((pattern >> (bitCount - 1 - i)) & 1) != 0;
        }

        return startCol + bitCount;
    }

    private static int RecommendedMinimumLevel(int n)
    {
        if (n <= 40)
        {
            return 2;
        }

        if (n <= 160)
        {
            return 3;
        }

        if (n <= 320)
        {
            return 4;
        }

        if (n <= 863)
        {
            return 5;
        }

        return -1;
    }

    private static int CalculateNumberOfRows(int m, int k, int c)
    {
        var r = ((m + 1 + k) / c) + 1;

        if (c * r >= (m + 1 + k + c))
        {
            r--;
        }

        return r;
    }

    private static int GetNumberOfPadCodewords(int m, int k, int c, int r)
    {
        var n = c * r - k;
        return n > m + 1 ? n - m - 1 : 0;
    }

    private static bool TryDetermineDimensions(int sourceCodeWords, int errorCorrectionCodeWords, out int cols, out int rows)
    {
        var ratio = 0.0f;
        var found = false;
        cols = MinCols;
        rows = MinRows;
        var currentCol = MinCols;

        for (var c = MinCols; c <= MaxCols; c++)
        {
            currentCol = c;
            var r = CalculateNumberOfRows(sourceCodeWords, errorCorrectionCodeWords, c);

            if (r < MinRows)
            {
                break;
            }

            if (r > MaxRows)
            {
                continue;
            }

            var newRatio = ((17 * c + 69) * DefaultModuleWidth) / (r * Height);

            if (found && Math.Abs(newRatio - PreferredRatio) > Math.Abs(ratio - PreferredRatio))
            {
                continue;
            }

            ratio = newRatio;
            cols = c;
            rows = r;
            found = true;
        }

        if (!found)
        {
            var r = CalculateNumberOfRows(sourceCodeWords, errorCorrectionCodeWords, currentCol);

            if (r < MinRows)
            {
                cols = MinCols;
                rows = MinRows;
                found = true;
            }
        }

        return found;
    }
}
