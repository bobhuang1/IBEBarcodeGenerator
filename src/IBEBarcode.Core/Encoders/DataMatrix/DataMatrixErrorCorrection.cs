namespace IBEBarcode.Core.Encoders.DataMatrix;

internal static class DataMatrixErrorCorrection
{
    /// <summary>
    /// Produces the full data+ECC codeword stream for a symbol, handling both single-block
    /// and multi-block (interleaved) Reed-Solomon per ECC200. For multi-block symbols, data
    /// codewords are striped across blocks by index modulo block count (block <c>b</c> gets
    /// codewords at indices b, b+blockCount, b+2*blockCount, ...), not contiguous chunks --
    /// verified against ZXing's ErrorCorrection.encodeECC200, a different rule than QR's
    /// per-block chunking. Each block's own ECC codewords are similarly striped into the
    /// output's ECC region.
    /// </summary>
    public static byte[] EncodeEcc200(byte[] dataCodewords, DataMatrixSymbols.SymbolSize symbolInfo)
    {
        var blockCount = symbolInfo.DataCapacity / symbolInfo.RsBlockData;
        var result = new byte[symbolInfo.DataCapacity + symbolInfo.ErrorCodewords];
        Array.Copy(dataCodewords, result, symbolInfo.DataCapacity);

        var poly = DataMatrixSymbols.EccPolyByCount[symbolInfo.RsBlockError];

        if (blockCount == 1)
        {
            var ecc = ComputeEcc(dataCodewords, symbolInfo.RsBlockError, poly);
            Array.Copy(ecc, 0, result, symbolInfo.DataCapacity, symbolInfo.RsBlockError);
            return result;
        }

        for (var block = 0; block < blockCount; block++)
        {
            var blockData = new byte[symbolInfo.RsBlockData];

            for (int d = block, k = 0; d < symbolInfo.DataCapacity; d += blockCount, k++)
            {
                blockData[k] = dataCodewords[d];
            }

            var blockEcc = ComputeEcc(blockData, symbolInfo.RsBlockError, poly);

            for (var k = 0; k < symbolInfo.RsBlockError; k++)
            {
                result[symbolInfo.DataCapacity + block + (k * blockCount)] = blockEcc[k];
            }
        }

        return result;
    }

    public static byte[] ComputeEcc(byte[] dataCodewords, int eccCount, int[] poly)
    {
        var ecc = new int[eccCount];

        foreach (var codeword in dataCodewords)
        {
            var m = ecc[eccCount - 1] ^ codeword;

            for (var k = eccCount - 1; k > 0; k--)
            {
                ecc[k] = m != 0 && poly[k] != 0
                    ? ecc[k - 1] ^ DataMatrixGaloisField.AlogOf((DataMatrixGaloisField.LogOf(m) + DataMatrixGaloisField.LogOf(poly[k])) % 255)
                    : ecc[k - 1];
            }

            ecc[0] = m != 0 && poly[0] != 0
                ? DataMatrixGaloisField.AlogOf((DataMatrixGaloisField.LogOf(m) + DataMatrixGaloisField.LogOf(poly[0])) % 255)
                : 0;
        }

        var result = new byte[eccCount];

        for (var i = 0; i < eccCount; i++)
        {
            result[i] = (byte)ecc[eccCount - 1 - i];
        }

        return result;
    }
}
