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

    [Fact]
    public void EncodeThenDecode_ExplicitHighErrorCorrectionLevel_RoundTripsExactly()
    {
        var encoder = new Pdf417Encoder(8);
        var success = encoder.TryEncode("Hello, level 8!", out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal("Hello, level 8!", decoded);
    }

    [Fact]
    public void EncodeThenDecode_TextCompaction_AllFourSubmodesRoundTripExactly()
    {
        var encoder = new Pdf417Encoder(textCompaction: true);
        // Exercises Alpha (upper+space), Lower (via ll), Alpha-shift-from-lower (via as),
        // Mixed (digits, via ml), Punctuation (via pl from Mixed), and single-char punct
        // shifts (ps) from Alpha.
        var text = "Hello world THERE 123.45,67:89 Wow! (test) #tag";
        var success = encoder.TryEncode(text, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(text, decoded);
    }

    [Fact]
    public void EncodeThenDecode_NumericCompaction_RoundTripsExactly()
    {
        var encoder = new Pdf417Encoder(numericCompaction: true);
        var digits = "0123456789012345678901234567890123456789"; // 40 digits, single chunk
        var success = encoder.TryEncode(digits, out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!);

        Assert.Equal(digits, decoded);
    }

    [Fact]
    public void EncodeThenDecode_Compact_RoundTripsExactly()
    {
        var encoder = new Pdf417Encoder(compact: true);
        var success = encoder.TryEncode("Compact PDF417!", out var matrix, out var error);

        Assert.True(success, error);

        var decoded = Decode(matrix!, compact: true);

        Assert.Equal("Compact PDF417!", decoded);
    }

    private static string Decode(BarcodeMatrix matrix, bool compact = false)
    {
        var cols = (matrix.Width - (compact ? 35 : 69)) / 17;
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

        if (highLevel[0] == 900)
        {
            return DecodeText(highLevel.GetRange(1, highLevel.Count - 1));
        }

        if (highLevel[0] == 902)
        {
            // Numeric compaction, scoped here (like the encoder) to a single <=44-digit
            // chunk: all codewords after the latch form one base-900 big integer whose
            // decimal representation is "1" + the original digits.
            var value = System.Numerics.BigInteger.Zero;

            for (var i = 1; i < highLevel.Count; i++)
            {
                value = (value * 900) + highLevel[i];
            }

            return value.ToString()[1..];
        }

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

    private static string DecodeText(List<int> codewords)
    {
        // Independently re-derived (not reusing Pdf417TextCompaction's internals) raw
        // forward tables, matching PDF417HighLevelEncoder's TEXT_MIXED_RAW/
        // TEXT_PUNCTUATION_RAW byte-for-byte.
        var mixedRaw = new[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '&', '\r', '\t', ',', ':',
            '#', '-', '.', '$', '/', '+', '%', '*', '=', '^', '\0', ' ', '\0', '\0', '\0',
        };

        var punctuationRaw = new[]
        {
            ';', '<', '>', '@', '[', '\\', ']', '_', '`', '~', '!', '\r', '\t', ',', ':',
            '\n', '-', '.', '$', '/', '"', '|', '*', '(', ')', '?', '{', '}', '\'', '\0',
        };

        var tmpValues = new List<int>();

        foreach (var codeword in codewords)
        {
            tmpValues.Add(codeword / 30);
            tmpValues.Add(codeword % 30);
        }

        const int alpha = 0;
        const int lower = 1;
        const int mixed = 2;
        const int punctuation = 3;

        var submode = alpha;
        var output = new List<char>();
        var i = 0;

        while (i < tmpValues.Count)
        {
            var v = tmpValues[i];

            switch (submode)
            {
                case alpha:
                    if (v == 26)
                    {
                        output.Add(' ');
                        i++;
                    }
                    else if (v == 27)
                    {
                        submode = lower;
                        i++;
                    }
                    else if (v == 28)
                    {
                        submode = mixed;
                        i++;
                    }
                    else if (v == 29)
                    {
                        if (i + 1 >= tmpValues.Count)
                        {
                            i = tmpValues.Count;
                            break;
                        }

                        output.Add(punctuationRaw[tmpValues[i + 1]]);
                        i += 2;
                    }
                    else
                    {
                        output.Add((char)('A' + v));
                        i++;
                    }

                    break;

                case lower:
                    if (v == 26)
                    {
                        output.Add(' ');
                        i++;
                    }
                    else if (v == 27)
                    {
                        if (i + 1 >= tmpValues.Count)
                        {
                            i = tmpValues.Count;
                            break;
                        }

                        output.Add((char)('A' + tmpValues[i + 1]));
                        i += 2;
                    }
                    else if (v == 28)
                    {
                        submode = mixed;
                        i++;
                    }
                    else if (v == 29)
                    {
                        if (i + 1 >= tmpValues.Count)
                        {
                            i = tmpValues.Count;
                            break;
                        }

                        output.Add(punctuationRaw[tmpValues[i + 1]]);
                        i += 2;
                    }
                    else
                    {
                        output.Add((char)('a' + v));
                        i++;
                    }

                    break;

                case mixed:
                    if (v == 25)
                    {
                        submode = punctuation;
                        i++;
                    }
                    else if (v == 28)
                    {
                        submode = alpha;
                        i++;
                    }
                    else if (v == 27)
                    {
                        submode = lower;
                        i++;
                    }
                    else if (v == 29)
                    {
                        if (i + 1 >= tmpValues.Count)
                        {
                            i = tmpValues.Count;
                            break;
                        }

                        output.Add(punctuationRaw[tmpValues[i + 1]]);
                        i += 2;
                    }
                    else
                    {
                        output.Add(mixedRaw[v]);
                        i++;
                    }

                    break;

                default: // punctuation
                    if (v == 29)
                    {
                        submode = alpha;
                        i++;
                    }
                    else
                    {
                        output.Add(punctuationRaw[v]);
                        i++;
                    }

                    break;
            }
        }

        return new string(output.ToArray());
    }
}
