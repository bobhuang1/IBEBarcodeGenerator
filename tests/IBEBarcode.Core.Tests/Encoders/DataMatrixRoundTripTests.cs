using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;
using IBEBarcode.Core.Encoders.DataMatrix;

namespace IBEBarcode.Core.Tests.Encoders;

public class DataMatrixRoundTripTests
{
    [Theory]
    [InlineData("Hi")]
    [InlineData("Data Matrix!")]
    [InlineData("The quick brown fox 123")]
    [InlineData("ABCD")] // forces interior 10 (dataCapacity 5)
    [InlineData("ABCDEFGHIJKL")] // forces interior 14 (dataCapacity 12)
    [InlineData("1234567890123456789012")] // forces interior 18 (dataCapacity 22)
    [InlineData("123456789012345678901234567890123456")] // forces interior 22 (dataCapacity 36)
    [InlineData("AAAAAAAAAAAAAAA")] // 15 bytes: forces rectangular interior 24x10 (dataCapacity 16)
    public void EncodeThenDecode_RoundTripsExactly(string original)
    {
        var encoder = new DataMatrixEncoder();
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(original, decoded);
    }

    [Fact]
    public void EncodeThenDecode_PreferRectangularTiedCapacity_RoundTripsExactly()
    {
        var encoder = new DataMatrixEncoder(preferRectangular: true);
        var success = encoder.TryEncode("ABCD", out var matrix, out var error);

        Assert.True(success, error);
        Assert.NotEqual(matrix!.Width, matrix.Height);

        var decoded = Decode(matrix);

        Assert.Equal("ABCD", decoded);
    }

    [Theory]
    [InlineData(50)]  // dataCapacity 62, interior 14x14, 4 regions (2x2), single-block
    [InlineData(100)] // dataCapacity 114, interior 18x18, 4 regions, single-block
    [InlineData(300)] // dataCapacity beyond 204: multi-block (interior 24x24, 4 regions, 2 blocks)
    [InlineData(700)] // dataCapacity beyond 696: multi-block (interior 24x24, 16 regions, 6 blocks)
    public void EncodeThenDecode_MultiRegionAndMultiBlock_RoundTripsExactly(int length)
    {
        var encoder = new DataMatrixEncoder();
        var value = string.Concat(Enumerable.Range(0, length).Select(i => (char)('A' + (i % 26))));
        var success = encoder.TryEncode(value, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(value, decoded);
    }

    private static DataMatrixSymbols.SymbolSize FindSizeForMatrix(BarcodeMatrix matrix)
    {
        foreach (var size in DataMatrixSymbols.Sizes)
        {
            var totalWidth = (size.RegionsHorizontal * size.InteriorWidth) + (size.RegionsHorizontal * 2);
            var totalHeight = (size.RegionsVertical * size.InteriorHeight) + (size.RegionsVertical * 2);

            if (totalWidth == matrix.Width && totalHeight == matrix.Height)
            {
                return size;
            }
        }

        throw new ArgumentException($"No known Data Matrix size matches {matrix.Width}x{matrix.Height}.");
    }

    [Theory]
    [InlineData("HELLO")]
    [InlineData("ABCDEFGHI")]
    [InlineData("HELLO WORLD 123")]
    public void EncodeThenDecode_C40Mode_RoundTripsExactly(string original)
    {
        var encoder = new DataMatrixEncoder(mode: DataMatrixEncodingMode.C40);
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = DecodeC40OrText(matrix!, textMode: false);

        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("hello world 123")]
    public void EncodeThenDecode_TextMode_RoundTripsExactly(string original)
    {
        var encoder = new DataMatrixEncoder(mode: DataMatrixEncodingMode.Text);
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = DecodeC40OrText(matrix!, textMode: true);

        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData("ABC123")]
    [InlineData("HELLO*WORLD>123")]
    public void EncodeThenDecode_X12Mode_RoundTripsExactly(string original)
    {
        var encoder = new DataMatrixEncoder(mode: DataMatrixEncodingMode.X12);
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = DecodeX12(matrix!);

        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData("Hello, Base256! \u0001\u0002ÿ")]
    [InlineData("The quick brown fox jumps over the lazy dog 0123456789")]
    public void EncodeThenDecode_Base256Mode_RoundTripsExactly(string original)
    {
        var encoder = new DataMatrixEncoder(mode: DataMatrixEncodingMode.Base256);
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = DecodeBase256(matrix!);

        Assert.Equal(original, decoded);
    }

    // Independently reverses DataMatrixHighLevelEncoder's C40/Text triplet packing and
    // shift-state machine (re-derived here, not shared code).
    private static string DecodeC40OrText(BarcodeMatrix matrix, bool textMode)
    {
        var dataBytes = ExtractDataCodewords(matrix);
        const string shift2Chars = "!\"#$%&'()*+,-./:;<=>?@[\\]^_";

        var pos = 1; // skip the latch codeword
        var values = new List<int>();

        while (pos + 1 < dataBytes.Length && dataBytes[pos] != 254 && dataBytes[pos] != 129)
        {
            var codeword16 = (dataBytes[pos] * 256) + dataBytes[pos + 1] - 1;
            values.Add(codeword16 / 1600);
            var remainder = codeword16 % 1600;
            values.Add(remainder / 40);
            values.Add(remainder % 40);
            pos += 2;
        }

        var result = new System.Text.StringBuilder();
        var shiftState = 0;

        foreach (var value in values)
        {
            if (shiftState == 0)
            {
                switch (value)
                {
                    case 0:
                        shiftState = 1;
                        continue;
                    case 1:
                        shiftState = 2;
                        continue;
                    case 2:
                        shiftState = 3;
                        continue;
                    case 3:
                        result.Append(' ');
                        continue;
                }

                if (value is >= 4 and <= 13)
                {
                    result.Append((char)('0' + (value - 4)));
                }
                else
                {
                    var letter = (char)('A' + (value - 14));
                    result.Append(textMode ? char.ToLowerInvariant(letter) : letter);
                }

                continue;
            }

            switch (shiftState)
            {
                case 1:
                    result.Append((char)value);
                    break;
                case 2:
                    result.Append(shift2Chars[value]);
                    break;
                case 3:
                    if (value <= 25)
                    {
                        var letter = (char)((textMode ? 'A' : 'a') + value);
                        result.Append(letter);
                    }
                    else
                    {
                        result.Append(value switch
                        {
                            26 => '{',
                            27 => '|',
                            28 => '}',
                            29 => '~',
                            _ => (char)127,
                        });
                    }

                    break;
            }

            shiftState = 0;
        }

        // Skip the unlatch codeword (254), then decode any trailing plain-ASCII bytes.
        pos++;

        while (pos < dataBytes.Length && dataBytes[pos] != 129)
        {
            result.Append((char)(dataBytes[pos] - 1));
            pos++;
        }

        return result.ToString();
    }

    // Independently reverses DataMatrixHighLevelEncoder's X12 triplet packing (no shift
    // states -- every value maps directly to a character).
    private static string DecodeX12(BarcodeMatrix matrix)
    {
        var dataBytes = ExtractDataCodewords(matrix);
        var pos = 1;
        var result = new System.Text.StringBuilder();

        while (pos + 1 < dataBytes.Length && dataBytes[pos] != 254 && dataBytes[pos] != 129)
        {
            var codeword16 = (dataBytes[pos] * 256) + dataBytes[pos + 1] - 1;
            var triplet = new[] { codeword16 / 1600, (codeword16 % 1600) / 40, codeword16 % 40 };

            foreach (var value in triplet)
            {
                result.Append(value switch
                {
                    0 => '\r',
                    1 => '*',
                    2 => '>',
                    3 => ' ',
                    (>= 4 and <= 13) => (char)('0' + (value - 4)),
                    _ => (char)('A' + (value - 14)),
                });
            }

            pos += 2;
        }

        pos++;

        while (pos < dataBytes.Length && dataBytes[pos] != 129)
        {
            result.Append((char)(dataBytes[pos] - 1));
            pos++;
        }

        return result.ToString();
    }

    // Independently reverses DataMatrixHighLevelEncoder's Base256 length field and
    // per-byte 255-modulus randomization (re-derived here, not shared code).
    private static string DecodeBase256(BarcodeMatrix matrix)
    {
        var dataBytes = ExtractDataCodewords(matrix);

        static byte Unrandomize255(byte randomized, int position)
        {
            var pseudoRandom = ((149 * position) % 255) + 1;
            var raw = randomized - pseudoRandom;
            return (byte)(raw < 0 ? raw + 256 : raw);
        }

        var position = 2;
        var firstLengthByte = Unrandomize255(dataBytes[1], position);
        position++;
        int length;
        int dataStart;

        if (firstLengthByte <= 249)
        {
            length = firstLengthByte;
            dataStart = 2;
        }
        else
        {
            var secondLengthByte = Unrandomize255(dataBytes[2], position);
            position++;
            length = ((firstLengthByte - 249) * 250) + secondLengthByte;
            dataStart = 3;
        }

        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)Unrandomize255(dataBytes[dataStart + i], position);
            position++;
        }

        return new string(chars);
    }

    private static byte[] ExtractDataCodewords(BarcodeMatrix matrix)
    {
        var size = FindSizeForMatrix(matrix);
        var allCodewords = ExtractAllCodewords(matrix, size);
        var dataBytes = new byte[size.DataCapacity];
        Array.Copy(allCodewords, dataBytes, size.DataCapacity);
        return dataBytes;
    }

    private static byte[] ExtractAllCodewords(BarcodeMatrix matrix, DataMatrixSymbols.SymbolSize size)
    {
        var numCols = size.RegionsHorizontal * size.InteriorWidth;
        var numRows = size.RegionsVertical * size.InteriorHeight;

        // Strip the (possibly multi-region) border to recover the combined data-region
        // interior, using the same region-boundary rule as the encoder's border-assembly.
        var interior = new bool[numCols, numRows];
        var matrixY = 0;

        for (var y = 0; y < numRows; y++)
        {
            if (y % size.InteriorHeight == 0)
            {
                matrixY++; // skip the top border row for this region-row
            }

            var matrixX = 0;

            for (var x = 0; x < numCols; x++)
            {
                if (x % size.InteriorWidth == 0)
                {
                    matrixX++; // skip left border column for this region
                }

                interior[x, y] = matrix[matrixX, matrixY];
                matrixX++;

                if (x % size.InteriorWidth == size.InteriorWidth - 1)
                {
                    matrixX++; // skip right border column for this region
                }
            }

            matrixY++;

            if (y % size.InteriorHeight == size.InteriorHeight - 1)
            {
                matrixY++; // skip the bottom border row for this region-row
            }
        }

        var totalCodewordBits = new List<bool>();
        var hasRead = new bool[numCols, numRows];

        void ReadModule(int row, int col)
        {
            if (row < 0)
            {
                row += numRows;
                col += 4 - ((numRows + 4) % 8);
            }

            if (col < 0)
            {
                col += numCols;
                row += 4 - ((numCols + 4) % 8);
            }

            totalCodewordBits.Add(interior[col, row]);
            hasRead[col, row] = true;
        }

        void ReadUtah(int row, int col)
        {
            ReadModule(row - 2, col - 2);
            ReadModule(row - 2, col - 1);
            ReadModule(row - 1, col - 2);
            ReadModule(row - 1, col - 1);
            ReadModule(row - 1, col);
            ReadModule(row, col - 2);
            ReadModule(row, col - 1);
            ReadModule(row, col);
        }

        void ReadCorner1()
        {
            ReadModule(numRows - 1, 0);
            ReadModule(numRows - 1, 1);
            ReadModule(numRows - 1, 2);
            ReadModule(0, numCols - 2);
            ReadModule(0, numCols - 1);
            ReadModule(1, numCols - 1);
            ReadModule(2, numCols - 1);
            ReadModule(3, numCols - 1);
        }

        void ReadCorner2()
        {
            ReadModule(numRows - 3, 0);
            ReadModule(numRows - 2, 0);
            ReadModule(numRows - 1, 0);
            ReadModule(0, numCols - 4);
            ReadModule(0, numCols - 3);
            ReadModule(0, numCols - 2);
            ReadModule(0, numCols - 1);
            ReadModule(1, numCols - 1);
        }

        void ReadCorner3()
        {
            ReadModule(numRows - 3, 0);
            ReadModule(numRows - 2, 0);
            ReadModule(numRows - 1, 0);
            ReadModule(0, numCols - 2);
            ReadModule(0, numCols - 1);
            ReadModule(1, numCols - 1);
            ReadModule(2, numCols - 1);
            ReadModule(3, numCols - 1);
        }

        void ReadCorner4()
        {
            ReadModule(numRows - 1, 0);
            ReadModule(numRows - 1, numCols - 1);
            ReadModule(0, numCols - 3);
            ReadModule(0, numCols - 2);
            ReadModule(0, numCols - 1);
            ReadModule(1, numCols - 3);
            ReadModule(1, numCols - 2);
            ReadModule(1, numCols - 1);
        }

        var row = 4;
        var col = 0;

        do
        {
            if (row == numRows && col == 0)
            {
                ReadCorner1();
            }

            if (row == numRows - 2 && col == 0 && numCols % 4 != 0)
            {
                ReadCorner2();
            }

            if (row == numRows - 2 && col == 0 && numCols % 8 == 4)
            {
                ReadCorner3();
            }

            if (row == numRows + 4 && col == 2 && numCols % 8 == 0)
            {
                ReadCorner4();
            }

            do
            {
                if (row < numRows && col >= 0 && !hasRead[col, row])
                {
                    ReadUtah(row, col);
                }

                row -= 2;
                col += 2;
            } while (row >= 0 && col < numCols);

            row++;
            col += 3;

            do
            {
                if (row >= 0 && col < numCols && !hasRead[col, row])
                {
                    ReadUtah(row, col);
                }

                row += 2;
                col -= 2;
            } while (row < numRows && col >= 0);

            row += 3;
            col++;
        } while (row < numRows || col < numCols);

        var totalCodewords = size.DataCapacity + size.ErrorCodewords;
        var allCodewords = new byte[totalCodewords];

        for (var i = 0; i < totalCodewords; i++)
        {
            var value = 0;

            for (var b = 0; b < 8; b++)
            {
                value = (value << 1) | (totalCodewordBits[(i * 8) + b] ? 1 : 0);
            }

            allCodewords[i] = (byte)value;
        }

        // Data codewords are the first DataCapacity entries, in original (un-striped)
        // order -- only the ECC region is striped by the multi-block interleaving, per
        // DataMatrixErrorCorrection.EncodeEcc200 (matches ZXing's encodeECC200: the input
        // data codewords are appended to the output unchanged before ECC is computed and
        // interleaved).
        return allCodewords;
    }

    private static string Decode(BarcodeMatrix matrix)
    {
        var dataBytes = ExtractDataCodewords(matrix);
        var messageLength = 0;

        for (var i = 0; i < dataBytes.Length; i++)
        {
            if (dataBytes[i] == 129)
            {
                break;
            }

            messageLength++;
        }

        var chars = new char[messageLength];

        for (var i = 0; i < messageLength; i++)
        {
            chars[i] = (char)(dataBytes[i] - 1);
        }

        return new string(chars);
    }
}
