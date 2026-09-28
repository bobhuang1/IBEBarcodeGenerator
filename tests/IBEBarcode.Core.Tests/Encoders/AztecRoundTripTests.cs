using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;

namespace IBEBarcode.Core.Tests.Encoders;

public class AztecRoundTripTests
{
    [Theory]
    [InlineData("Hi")]
    [InlineData("Aztec test")]
    [InlineData("The quick brown fox 0123456789")]
    public void EncodeThenDecode_RoundTripsExactly(string original)
    {
        var encoder = new AztecEncoder();
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(original, decoded);
    }

    private static string Decode(BarcodeMatrix matrix)
    {
        var size = matrix.Width;
        var layers = (size - 11) / 4;
        var wordSize = layers <= 2 ? 6 : 8;

        var center = size / 2;
        var modeBits = new bool[28];

        for (var i = 0; i < 7; i++)
        {
            var offset = center - 3 + i;
            modeBits[i] = matrix[offset, center - 5];
            modeBits[i + 7] = matrix[center + 5, offset];
            modeBits[20 - i] = matrix[offset, center + 5];
            modeBits[27 - i] = matrix[center - 5, offset];
        }

        var messageSizeInWords = 0;

        for (var i = 2; i < 8; i++)
        {
            messageSizeInWords = (messageSizeInWords << 1) | (modeBits[i] ? 1 : 0);
        }

        messageSizeInWords += 1;

        var baseSize = size;
        var totalDataBits = 0;

        for (var i = 0; i < layers; i++)
        {
            totalDataBits += ((layers - i) * 4 + 9) * 8;
        }

        var messageBits = new bool[totalDataBits];
        var rowOffset = 0;

        for (var i = 0; i < layers; i++)
        {
            var rowSize = (layers - i) * 4 + 9;

            for (var j = 0; j < rowSize; j++)
            {
                var columnOffset = j * 2;

                for (var k = 0; k < 2; k++)
                {
                    messageBits[rowOffset + columnOffset + k] = matrix[i * 2 + k, i * 2 + j];
                    messageBits[rowOffset + rowSize * 2 + columnOffset + k] = matrix[i * 2 + j, baseSize - 1 - i * 2 - k];
                    messageBits[rowOffset + rowSize * 4 + columnOffset + k] = matrix[baseSize - 1 - i * 2 - k, baseSize - 1 - i * 2 - j];
                    messageBits[rowOffset + rowSize * 6 + columnOffset + k] = matrix[baseSize - 1 - i * 2 - j, i * 2 + k];
                }
            }

            rowOffset += rowSize * 8;
        }

        var totalBitsInLayer = (88 + 16 * layers) * layers;
        var startPad = totalBitsInLayer % wordSize;
        var dataBitCount = messageSizeInWords * wordSize;
        var dataBits = new List<bool>();

        for (var i = 0; i < dataBitCount; i++)
        {
            dataBits.Add(messageBits[startPad + i]);
        }

        var realBits = new List<bool>();
        var idx = 0;
        var mask = (1 << wordSize) - 2;

        while (idx + wordSize <= dataBits.Count)
        {
            var word = 0;

            for (var i = 0; i < wordSize; i++)
            {
                word = (word << 1) | (dataBits[idx + i] ? 1 : 0);
            }

            if ((word & mask) == mask || (word & mask) == 0)
            {
                for (var i = 0; i < wordSize - 1; i++)
                {
                    realBits.Add(dataBits[idx + i]);
                }
            }
            else
            {
                for (var i = 0; i < wordSize; i++)
                {
                    realBits.Add(dataBits[idx + i]);
                }
            }

            idx += wordSize;
        }

        var pos = 0;

        int ReadBits(int count)
        {
            var value = 0;

            for (var i = 0; i < count; i++)
            {
                value = (value << 1) | (realBits[pos++] ? 1 : 0);
            }

            return value;
        }

        var bytes = new List<byte>();
        ReadBits(5);

        var firstLength = ReadBits(5);

        if (firstLength == 31)
        {
            var savedPos = pos;
            var extendedRemaining = ReadBits(16);
            var impliedTotal = extendedRemaining + 31;
            var bitsLeftForBytes = realBits.Count - pos;

            if (bitsLeftForBytes == impliedTotal * 8)
            {
                for (var i = 0; i < impliedTotal; i++)
                {
                    bytes.Add((byte)ReadBits(8));
                }
            }
            else
            {
                pos = savedPos;

                for (var i = 0; i < 31; i++)
                {
                    bytes.Add((byte)ReadBits(8));
                }

                ReadBits(5);
                var secondLength = ReadBits(5);

                for (var i = 0; i < secondLength; i++)
                {
                    bytes.Add((byte)ReadBits(8));
                }
            }
        }
        else
        {
            for (var i = 0; i < firstLength; i++)
            {
                bytes.Add((byte)ReadBits(8));
            }
        }

        var chars = new char[bytes.Count];

        for (var i = 0; i < bytes.Count; i++)
        {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }
}
