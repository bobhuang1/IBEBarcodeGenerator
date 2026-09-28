using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Encoders;

public sealed class AztecEncoder : IMatrixBarcodeEncoder
{
    private const int EcPercent = 33;

    // Indexed by layers-1 (layer 1 at index 0). Layers 1-2 use 6-bit words (GF(64)),
    // 3-8 use 8-bit (GF(256)), 9-22 use 10-bit (GF(1024)), 23-32 use 12-bit (GF(4096)).
    private static readonly int[] WordSizeByLayer =
    {
        6, 6, 8, 8, 8, 8, 8, 8, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
    };

    public BarcodeSymbology Symbology => BarcodeSymbology.Aztec;

    public bool TryEncode(string value, out BarcodeMatrix? matrix, out string? error)
    {
        matrix = null;

        if (string.IsNullOrEmpty(value))
        {
            error = "Value must not be empty.";
            return false;
        }

        var bytes = new byte[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 255)
            {
                error = $"Character '{value[i]}' is outside the 0-255 byte range this Aztec encoder supports.";
                return false;
            }

            bytes[i] = (byte)value[i];
        }

        var bits = AztecHighLevelEncoder.EncodeBinaryShift(bytes);
        var eccBits = bits.Count * EcPercent / 100 + 11;
        var totalSizeBits = bits.Count + eccBits;

        // Mirrors ZXing's own default search order: compact layers 1-4 first (i=0..3),
        // then normal layers 4-32 (i=4..32) -- normal layers 1-3 are never tried since
        // compact 2-4 always has equal-or-smaller symbol size at equal-or-greater
        // capacity, so they would never win anyway.
        for (var i = 0; i <= 32; i++)
        {
            var compact = i <= 3;
            var layers = compact ? i + 1 : i;
            var totalBitsInLayer = ((compact ? 88 : 112) + 16 * layers) * layers;

            if (totalSizeBits > totalBitsInLayer)
            {
                continue;
            }

            var wordSize = WordSizeByLayer[layers - 1];
            var stuffedBits = StuffBits(bits, wordSize);
            var usableBitsInLayer = totalBitsInLayer - (totalBitsInLayer % wordSize);

            if (compact && stuffedBits.Count > wordSize * 64)
            {
                continue;
            }

            if (stuffedBits.Count + eccBits > usableBitsInLayer)
            {
                continue;
            }

            return BuildSymbol(value, stuffedBits, layers, compact, wordSize, out matrix, out error);
        }

        error = "Value is too large to encode: this Aztec encoder supports layers 1-32.";
        return false;
    }

    private static bool BuildSymbol(string value, AztecBitBuffer stuffedBits, int layers, bool compact, int wordSize, out BarcodeMatrix? matrix, out string? error)
    {
        var totalBitsInLayer = ((compact ? 88 : 112) + 16 * layers) * layers;
        var messageBits = GenerateCheckWords(stuffedBits, totalBitsInLayer, wordSize);

        var messageSizeInWords = stuffedBits.Count / wordSize;
        var modeMessage = GenerateModeMessage(compact, layers, messageSizeInWords);

        var baseMatrixSize = (compact ? 11 : 14) + layers * 4;
        var alignmentMap = new int[baseMatrixSize];
        int matrixSize;

        if (compact)
        {
            matrixSize = baseMatrixSize;

            for (var i = 0; i < alignmentMap.Length; i++)
            {
                alignmentMap[i] = i;
            }
        }
        else
        {
            matrixSize = baseMatrixSize + 1 + (2 * (((baseMatrixSize / 2) - 1) / 15));
            var origCenter = baseMatrixSize / 2;
            var center = matrixSize / 2;

            for (var i = 0; i < origCenter; i++)
            {
                var newOffset = i + (i / 15);
                alignmentMap[origCenter - i - 1] = center - newOffset - 1;
                alignmentMap[origCenter + i] = center + newOffset + 1;
            }
        }

        var modules = new bool[matrixSize, matrixSize];

        var rowOffset = 0;

        for (var i = 0; i < layers; i++)
        {
            var rowSize = ((layers - i) * 4) + (compact ? 9 : 12);

            for (var j = 0; j < rowSize; j++)
            {
                var columnOffset = j * 2;

                for (var k = 0; k < 2; k++)
                {
                    if (messageBits.Get(rowOffset + columnOffset + k))
                    {
                        modules[alignmentMap[(i * 2) + k], alignmentMap[(i * 2) + j]] = true;
                    }

                    if (messageBits.Get(rowOffset + (rowSize * 2) + columnOffset + k))
                    {
                        modules[alignmentMap[(i * 2) + j], alignmentMap[baseMatrixSize - 1 - (i * 2) - k]] = true;
                    }

                    if (messageBits.Get(rowOffset + (rowSize * 4) + columnOffset + k))
                    {
                        modules[alignmentMap[baseMatrixSize - 1 - (i * 2) - k], alignmentMap[baseMatrixSize - 1 - (i * 2) - j]] = true;
                    }

                    if (messageBits.Get(rowOffset + (rowSize * 6) + columnOffset + k))
                    {
                        modules[alignmentMap[baseMatrixSize - 1 - (i * 2) - j], alignmentMap[(i * 2) + k]] = true;
                    }
                }
            }

            rowOffset += rowSize * 8;
        }

        DrawModeMessage(modules, compact, matrixSize, modeMessage);

        if (compact)
        {
            DrawBullsEye(modules, matrixSize / 2, 5);
        }
        else
        {
            DrawBullsEye(modules, matrixSize / 2, 7);

            for (int i = 0, j = 0; i < baseMatrixSize / 2; i += 15, j += 16)
            {
                for (var k = (matrixSize / 2) & 1; k < matrixSize; k += 2)
                {
                    modules[(matrixSize / 2) - j, k] = true;
                    modules[(matrixSize / 2) + j, k] = true;
                    modules[k, (matrixSize / 2) - j] = true;
                    modules[k, (matrixSize / 2) + j] = true;
                }
            }
        }

        matrix = BarcodeMatrix.Create(value, modules);
        error = null;
        return true;
    }

    private static void DrawBullsEye(bool[,] modules, int center, int size)
    {
        for (var i = 0; i < size; i += 2)
        {
            for (var j = center - i; j <= center + i; j++)
            {
                modules[j, center - i] = true;
                modules[j, center + i] = true;
                modules[center - i, j] = true;
                modules[center + i, j] = true;
            }
        }

        modules[center - size, center - size] = true;
        modules[center - size + 1, center - size] = true;
        modules[center - size, center - size + 1] = true;
        modules[center + size, center - size] = true;
        modules[center + size, center - size + 1] = true;
        modules[center + size, center + size - 1] = true;
    }

    private static void DrawModeMessage(bool[,] modules, bool compact, int matrixSize, AztecBitBuffer modeMessage)
    {
        var center = matrixSize / 2;

        if (compact)
        {
            for (var i = 0; i < 7; i++)
            {
                var offset = center - 3 + i;

                if (modeMessage.Get(i))
                {
                    modules[offset, center - 5] = true;
                }

                if (modeMessage.Get(i + 7))
                {
                    modules[center + 5, offset] = true;
                }

                if (modeMessage.Get(20 - i))
                {
                    modules[offset, center + 5] = true;
                }

                if (modeMessage.Get(27 - i))
                {
                    modules[center - 5, offset] = true;
                }
            }
        }
        else
        {
            for (var i = 0; i < 10; i++)
            {
                var offset = center - 5 + i + (i / 5);

                if (modeMessage.Get(i))
                {
                    modules[offset, center - 7] = true;
                }

                if (modeMessage.Get(i + 10))
                {
                    modules[center + 7, offset] = true;
                }

                if (modeMessage.Get(29 - i))
                {
                    modules[offset, center + 7] = true;
                }

                if (modeMessage.Get(39 - i))
                {
                    modules[center - 7, offset] = true;
                }
            }
        }
    }

    private static AztecBitBuffer GenerateModeMessage(bool compact, int layers, int messageSizeInWords)
    {
        var modeMessage = new AztecBitBuffer();

        if (compact)
        {
            modeMessage.AppendBits(layers - 1, 2);
            modeMessage.AppendBits(messageSizeInWords - 1, 6);
            return GenerateCheckWords(modeMessage, 28, 4);
        }

        modeMessage.AppendBits(layers - 1, 5);
        modeMessage.AppendBits(messageSizeInWords - 1, 11);
        return GenerateCheckWords(modeMessage, 40, 4);
    }

    private static AztecBitBuffer GenerateCheckWords(AztecBitBuffer bitArray, int totalBits, int wordSize)
    {
        var messageSizeInWords = bitArray.Count / wordSize;
        var field = GetField(wordSize);
        var rs = new AztecReedSolomonEncoder(field);
        var totalWords = totalBits / wordSize;
        var messageWords = BitsToWords(bitArray, wordSize, totalWords);
        rs.Encode(messageWords, totalWords - messageSizeInWords);

        var startPad = totalBits % wordSize;
        var result = new AztecBitBuffer();
        result.AppendBits(0, startPad);

        foreach (var word in messageWords)
        {
            result.AppendBits(word, wordSize);
        }

        return result;
    }

    private static int[] BitsToWords(AztecBitBuffer stuffedBits, int wordSize, int totalWords)
    {
        var message = new int[totalWords];
        var n = stuffedBits.Count / wordSize;

        for (var i = 0; i < n; i++)
        {
            var value = 0;

            for (var j = 0; j < wordSize; j++)
            {
                value |= stuffedBits.Get((i * wordSize) + j) ? 1 << (wordSize - j - 1) : 0;
            }

            message[i] = value;
        }

        return message;
    }

    private static AztecGaloisField GetField(int wordSize) => wordSize switch
    {
        4 => new AztecGaloisField(0x13, 16, 1),
        6 => new AztecGaloisField(0x43, 64, 1),
        8 => new AztecGaloisField(0x12D, 256, 1),
        10 => new AztecGaloisField(0x409, 1024, 1),
        12 => new AztecGaloisField(0x1069, 4096, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(wordSize)),
    };

    private static AztecBitBuffer StuffBits(AztecBitBuffer bits, int wordSize)
    {
        var output = new AztecBitBuffer();
        var n = bits.Count;
        var mask = (1 << wordSize) - 2;

        for (var i = 0; i < n; i += wordSize)
        {
            var word = 0;

            for (var j = 0; j < wordSize; j++)
            {
                if (i + j >= n || bits.Get(i + j))
                {
                    word |= 1 << (wordSize - 1 - j);
                }
            }

            if ((word & mask) == mask)
            {
                output.AppendBits(word & mask, wordSize);
                i--;
            }
            else if ((word & mask) == 0)
            {
                output.AppendBits(word | 1, wordSize);
                i--;
            }
            else
            {
                output.AppendBits(word, wordSize);
            }
        }

        return output;
    }
}
