using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;
using IBEBarcode.Core.Encoders.Pdf417;

namespace IBEBarcode.Core.Tests.Encoders;

public class Pdf417RoundTripTests
{
    [Theory]
    [InlineData("Hi")]
    [InlineData("PDF417 test string!")]
    [InlineData("The quick brown fox jumps over the lazy dog 0123456789")]
    public void EncodeThenDecode_RoundTripsExactly(string original)
    {
        var encoder = new Pdf417Encoder();
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(original, decoded);
    }

    private static string Decode(BarcodeMatrix matrix)
    {
        var cols = (matrix.Width - 69) / 17;
        var rows = matrix.Height;

        var reverseLookup = new Dictionary<int, int>[3];

        for (var cluster = 0; cluster < 3; cluster++)
        {
            reverseLookup[cluster] = new Dictionary<int, int>();

            for (var codeword = 0; codeword < 929; codeword++)
            {
                reverseLookup[cluster][Pdf417CodewordTable.Patterns[cluster][codeword]] = codeword;
            }
        }

        int ReadPattern(int startCol, int row, int bitCount)
        {
            var pattern = 0;

            for (var i = 0; i < bitCount; i++)
            {
                pattern <<= 1;
                pattern |= matrix[startCol + i, row] ? 1 : 0;
            }

            return pattern;
        }

        var allDataCodewords = new List<int>();

        for (var y = 0; y < rows; y++)
        {
            var cluster = y % 3;
            var col = 17 + 17;

            for (var x = 0; x < cols; x++)
            {
                var pattern = ReadPattern(col, y, 17);
                allDataCodewords.Add(reverseLookup[cluster][pattern]);
                col += 17;
            }
        }

        var totalDataAndDescriptor = allDataCodewords[0];
        var sourceAndPad = allDataCodewords.GetRange(1, totalDataAndDescriptor - 1);

        var lastNonPad = sourceAndPad.Count - 1;

        while (lastNonPad >= 0 && sourceAndPad[lastNonPad] == 900)
        {
            lastNonPad--;
        }

        var highLevel = sourceAndPad.GetRange(0, lastNonPad + 1);

        var codewordsAfterLatch = highLevel.GetRange(1, highLevel.Count - 1);
        var bytes = new List<byte>();
        var idx = 0;

        while (codewordsAfterLatch.Count - idx >= 5)
        {
            long t = 0;

            for (var i = 0; i < 5; i++)
            {
                t = t * 900 + codewordsAfterLatch[idx + i];
            }

            var sixBytes = new byte[6];

            for (var i = 5; i >= 0; i--)
            {
                sixBytes[i] = (byte)(t & 0xff);
                t >>= 8;
            }

            bytes.AddRange(sixBytes);
            idx += 5;
        }

        for (var i = idx; i < codewordsAfterLatch.Count; i++)
        {
            bytes.Add((byte)codewordsAfterLatch[i]);
        }

        var chars = new char[bytes.Count];

        for (var i = 0; i < bytes.Count; i++)
        {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }
}
