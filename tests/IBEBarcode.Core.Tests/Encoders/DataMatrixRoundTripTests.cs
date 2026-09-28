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
    public void EncodeThenDecode_RoundTripsExactly(string original)
    {
        var encoder = new DataMatrixEncoder();
        var success = encoder.TryEncode(original, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(original, decoded);
    }

    private static string Decode(BarcodeMatrix matrix)
    {
        var totalSize = matrix.Width;
        var interiorSize = totalSize - 2;

        var dataCapacity = interiorSize switch
        {
            8 => 3,
            10 => 5,
            12 => 8,
            14 => 12,
            16 => 18,
            18 => 22,
            20 => 30,
            22 => 36,
            24 => 44,
            _ => throw new ArgumentOutOfRangeException(nameof(matrix)),
        };

        var interior = new bool[interiorSize, interiorSize];

        for (var y = 0; y < interiorSize; y++)
        {
            for (var x = 0; x < interiorSize; x++)
            {
                interior[x, y] = matrix[x + 1, y + 1];
            }
        }

        var totalCodewordBits = new List<bool>();
        var hasRead = new bool[interiorSize, interiorSize];

        void ReadModule(int row, int col)
        {
            if (row < 0)
            {
                row += interiorSize;
                col += 4 - ((interiorSize + 4) % 8);
            }

            if (col < 0)
            {
                col += interiorSize;
                row += 4 - ((interiorSize + 4) % 8);
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
            ReadModule(interiorSize - 1, 0);
            ReadModule(interiorSize - 1, 1);
            ReadModule(interiorSize - 1, 2);
            ReadModule(0, interiorSize - 2);
            ReadModule(0, interiorSize - 1);
            ReadModule(1, interiorSize - 1);
            ReadModule(2, interiorSize - 1);
            ReadModule(3, interiorSize - 1);
        }

        void ReadCorner2()
        {
            ReadModule(interiorSize - 3, 0);
            ReadModule(interiorSize - 2, 0);
            ReadModule(interiorSize - 1, 0);
            ReadModule(0, interiorSize - 4);
            ReadModule(0, interiorSize - 3);
            ReadModule(0, interiorSize - 2);
            ReadModule(0, interiorSize - 1);
            ReadModule(1, interiorSize - 1);
        }

        void ReadCorner3()
        {
            ReadModule(interiorSize - 3, 0);
            ReadModule(interiorSize - 2, 0);
            ReadModule(interiorSize - 1, 0);
            ReadModule(0, interiorSize - 2);
            ReadModule(0, interiorSize - 1);
            ReadModule(1, interiorSize - 1);
            ReadModule(2, interiorSize - 1);
            ReadModule(3, interiorSize - 1);
        }

        void ReadCorner4()
        {
            ReadModule(interiorSize - 1, 0);
            ReadModule(interiorSize - 1, interiorSize - 1);
            ReadModule(0, interiorSize - 3);
            ReadModule(0, interiorSize - 2);
            ReadModule(0, interiorSize - 1);
            ReadModule(1, interiorSize - 3);
            ReadModule(1, interiorSize - 2);
            ReadModule(1, interiorSize - 1);
        }

        var row = 4;
        var col = 0;

        do
        {
            if (row == interiorSize && col == 0)
            {
                ReadCorner1();
            }

            if (row == interiorSize - 2 && col == 0 && interiorSize % 4 != 0)
            {
                ReadCorner2();
            }

            if (row == interiorSize - 2 && col == 0 && interiorSize % 8 == 4)
            {
                ReadCorner3();
            }

            if (row == interiorSize + 4 && col == 2 && interiorSize % 8 == 0)
            {
                ReadCorner4();
            }

            do
            {
                if (row < interiorSize && col >= 0 && !hasRead[col, row])
                {
                    ReadUtah(row, col);
                }

                row -= 2;
                col += 2;
            } while (row >= 0 && col < interiorSize);

            row++;
            col += 3;

            do
            {
                if (row >= 0 && col < interiorSize && !hasRead[col, row])
                {
                    ReadUtah(row, col);
                }

                row += 2;
                col -= 2;
            } while (row < interiorSize && col >= 0);

            row += 3;
            col++;
        } while (row < interiorSize || col < interiorSize);

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
