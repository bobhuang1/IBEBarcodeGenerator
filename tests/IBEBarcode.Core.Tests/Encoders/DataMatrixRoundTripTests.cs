using IBEBarcode.Core;
using IBEBarcode.Core.Encoders;

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

    private static string Decode(BarcodeMatrix matrix)
    {
        var numCols = matrix.Width - 2;
        var numRows = matrix.Height - 2;

        var dataCapacity = (numCols, numRows) switch
        {
            (8, 8) => 3,
            (10, 10) => 5,
            (16, 6) => 5,
            (12, 12) => 8,
            (14, 14) => 12,
            (24, 10) => 16,
            (16, 16) => 18,
            (18, 18) => 22,
            (20, 20) => 30,
            (22, 22) => 36,
            (24, 24) => 44,
            _ => throw new ArgumentOutOfRangeException(nameof(matrix)),
        };

        var interior = new bool[numCols, numRows];

        for (var y = 0; y < numRows; y++)
        {
            for (var x = 0; x < numCols; x++)
            {
                interior[x, y] = matrix[x + 1, y + 1];
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

        var dataBytes = new byte[dataCapacity];

        for (var i = 0; i < dataCapacity; i++)
        {
            var value = 0;

            for (var b = 0; b < 8; b++)
            {
                value = (value << 1) | (totalCodewordBits[i * 8 + b] ? 1 : 0);
            }

            dataBytes[i] = (byte)value;
        }

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
