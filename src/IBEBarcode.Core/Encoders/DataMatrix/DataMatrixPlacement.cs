namespace IBEBarcode.Core.Encoders.DataMatrix;

internal sealed class DataMatrixPlacement
{
    private readonly byte[] _codewords;
    private readonly int _numCols;
    private readonly int _numRows;
    private readonly bool[] _bits;
    private readonly bool[] _hasBit;

    public DataMatrixPlacement(byte[] codewords, int numCols, int numRows)
    {
        _codewords = codewords;
        _numCols = numCols;
        _numRows = numRows;
        _bits = new bool[numCols * numRows];
        _hasBit = new bool[numCols * numRows];
    }

    public bool GetBit(int col, int row) => _bits[row * _numCols + col];

    private void SetBit(int col, int row, bool bit)
    {
        _bits[row * _numCols + col] = bit;
        _hasBit[row * _numCols + col] = true;
    }

    private bool NoBit(int col, int row) => !_hasBit[row * _numCols + col];

    public void Place()
    {
        var pos = 0;
        var row = 4;
        var col = 0;

        do
        {
            if (row == _numRows && col == 0)
            {
                Corner1(pos++);
            }

            if (row == _numRows - 2 && col == 0 && _numCols % 4 != 0)
            {
                Corner2(pos++);
            }

            if (row == _numRows - 2 && col == 0 && _numCols % 8 == 4)
            {
                Corner3(pos++);
            }

            if (row == _numRows + 4 && col == 2 && _numCols % 8 == 0)
            {
                Corner4(pos++);
            }

            do
            {
                if (row < _numRows && col >= 0 && NoBit(col, row))
                {
                    Utah(row, col, pos++);
                }

                row -= 2;
                col += 2;
            } while (row >= 0 && col < _numCols);

            row++;
            col += 3;

            do
            {
                if (row >= 0 && col < _numCols && NoBit(col, row))
                {
                    Utah(row, col, pos++);
                }

                row += 2;
                col -= 2;
            } while (row < _numRows && col >= 0);

            row += 3;
            col++;
        } while (row < _numRows || col < _numCols);

        if (NoBit(_numCols - 1, _numRows - 1))
        {
            SetBit(_numCols - 1, _numRows - 1, true);
            SetBit(_numCols - 2, _numRows - 2, true);
        }
    }

    private void Module(int row, int col, int pos, int bit)
    {
        if (row < 0)
        {
            row += _numRows;
            col += 4 - ((_numRows + 4) % 8);
        }

        if (col < 0)
        {
            col += _numCols;
            row += 4 - ((_numCols + 4) % 8);
        }

        var v = _codewords[pos];
        var bitSet = (v & (1 << (8 - bit))) != 0;
        SetBit(col, row, bitSet);
    }

    private void Utah(int row, int col, int pos)
    {
        Module(row - 2, col - 2, pos, 1);
        Module(row - 2, col - 1, pos, 2);
        Module(row - 1, col - 2, pos, 3);
        Module(row - 1, col - 1, pos, 4);
        Module(row - 1, col, pos, 5);
        Module(row, col - 2, pos, 6);
        Module(row, col - 1, pos, 7);
        Module(row, col, pos, 8);
    }

    private void Corner1(int pos)
    {
        Module(_numRows - 1, 0, pos, 1);
        Module(_numRows - 1, 1, pos, 2);
        Module(_numRows - 1, 2, pos, 3);
        Module(0, _numCols - 2, pos, 4);
        Module(0, _numCols - 1, pos, 5);
        Module(1, _numCols - 1, pos, 6);
        Module(2, _numCols - 1, pos, 7);
        Module(3, _numCols - 1, pos, 8);
    }

    private void Corner2(int pos)
    {
        Module(_numRows - 3, 0, pos, 1);
        Module(_numRows - 2, 0, pos, 2);
        Module(_numRows - 1, 0, pos, 3);
        Module(0, _numCols - 4, pos, 4);
        Module(0, _numCols - 3, pos, 5);
        Module(0, _numCols - 2, pos, 6);
        Module(0, _numCols - 1, pos, 7);
        Module(1, _numCols - 1, pos, 8);
    }

    private void Corner3(int pos)
    {
        Module(_numRows - 3, 0, pos, 1);
        Module(_numRows - 2, 0, pos, 2);
        Module(_numRows - 1, 0, pos, 3);
        Module(0, _numCols - 2, pos, 4);
        Module(0, _numCols - 1, pos, 5);
        Module(1, _numCols - 1, pos, 6);
        Module(2, _numCols - 1, pos, 7);
        Module(3, _numCols - 1, pos, 8);
    }

    private void Corner4(int pos)
    {
        Module(_numRows - 1, 0, pos, 1);
        Module(_numRows - 1, _numCols - 1, pos, 2);
        Module(0, _numCols - 3, pos, 3);
        Module(0, _numCols - 2, pos, 4);
        Module(0, _numCols - 1, pos, 5);
        Module(1, _numCols - 3, pos, 6);
        Module(1, _numCols - 2, pos, 7);
        Module(1, _numCols - 1, pos, 8);
    }
}
