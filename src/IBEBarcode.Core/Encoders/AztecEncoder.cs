using IBEBarcode.Core.Encoders.Aztec;

namespace IBEBarcode.Core.Encoders;

public sealed class AztecEncoder : IMatrixBarcodeEncoder
{
    private const int EcPercent = 33;
    private static readonly int[] WordSizeByLayer = { 6, 6, 8, 8 };

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

        for (var layerIndex = 0; layerIndex < 4; layerIndex++)
        {
            var layers = layerIndex + 1;
            var totalBitsInLayer = (88 + 16 * layers) * layers;

            if (totalSizeBits > totalBitsInLayer)
            {
                continue;
            }

            var wordSize = WordSizeByLayer[layerIndex];
            var stuffedBits = StuffBits(bits, wordSize);
            var usableBitsInLayer = totalBitsInLayer - (totalBitsInLayer % wordSize);

            if (stuffedBits.Count > wordSize * 64)
            {
                continue;
            }

            if (stuffedBits.Count + eccBits > usableBitsInLayer)
            {
                continue;
            }

            return BuildSymbol(value, stuffedBits, layers, wordSize, out matrix, out error);
        }

        error = "Value is too large to encode: this Aztec encoder supports compact symbols (layers 1-4) only.";
        return false;
    }

    private static bool BuildSymbol(string value, AztecBitBuffer stuffedBits, int layers, int wordSize, out BarcodeMatrix? matrix, out string? error)
    {
        var totalBitsInLayer = (88 + 16 * layers) * layers;
        var messageBits = GenerateCheckWords(stuffedBits, totalBitsInLayer, wordSize);

        var messageSizeInWords = stuffedBits.Count / wordSize;
        var modeMessage = GenerateModeMessage(layers, messageSizeInWords);

        var baseSize = 11 + layers * 4;
        var modules = new bool[baseSize, baseSize];

        var rowOffset = 0;

        for (var i = 0; i < layers; i++)
        {
            var rowSize = (layers - i) * 4 + 9;

            for (var j = 0; j < rowSize; j++)
            {
                var columnOffset = j * 2;

                for (var k = 0; k < 2; k++)
                {
                    if (messageBits.Get(rowOffset + columnOffset + k))
                    {
                        modules[i * 2 + k, i * 2 + j] = true;
                    }

                    if (messageBits.Get(rowOffset + rowSize * 2 + columnOffset + k))
                    {
                        modules[i * 2 + j, baseSize - 1 - i * 2 - k] = true;
                    }

                    if (messageBits.Get(rowOffset + rowSize * 4 + columnOffset + k))
                    {
                        modules[baseSize - 1 - i * 2 - k, baseSize - 1 - i * 2 - j] = true;
                    }

                    if (messageBits.Get(rowOffset + rowSize * 6 + columnOffset + k))
                    {
                        modules[baseSize - 1 - i * 2 - j, i * 2 + k] = true;
                    }
                }
            }

            rowOffset += rowSize * 8;
        }

        DrawModeMessage(modules, baseSize, modeMessage);
        DrawBullsEye(modules, baseSize / 2, 5);

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

    private static void DrawModeMessage(bool[,] modules, int matrixSize, AztecBitBuffer modeMessage)
    {
        var center = matrixSize / 2;

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

    private static AztecBitBuffer GenerateModeMessage(int layers, int messageSizeInWords)
    {
        var modeMessage = new AztecBitBuffer();
        modeMessage.AppendBits(layers - 1, 2);
        modeMessage.AppendBits(messageSizeInWords - 1, 6);
        return GenerateCheckWords(modeMessage, 28, 4);
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
                value |= stuffedBits.Get(i * wordSize + j) ? 1 << (wordSize - j - 1) : 0;
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
