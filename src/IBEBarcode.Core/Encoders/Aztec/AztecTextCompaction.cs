namespace IBEBarcode.Core.Encoders.Aztec;

// A greedy (not bit-optimal) implementation of Aztec's 5 text submodes (Upper/Lower/
// Digit/Mixed/Punct), following ISO/IEC 24778:2008 Table 3-6. The real encoder
// (AztecHighLevelEncoder.encode in ZXing) runs a dynamic-programming search over every
// mode/shift/latch combination per character to find the bit-optimal encoding; this
// encoder instead always LATCHES (never shifts) to whichever mode contains the next
// character, using the shortest available latch route between modes. This is always
// spec-correct but not always maximally compact -- a documented, deliberate scope
// simplification (mirrors Pdf417TextCompaction's greedy, non-optimal approach).
internal static class AztecTextCompaction
{
    private const int ModeUpper = 0;
    private const int ModeLower = 1;
    private const int ModeDigit = 2;
    private const int ModeMixed = 3;
    private const int ModePunct = 4;

    private static readonly int[][] CharMap = BuildCharMap();

    // (from, to) -> sequence of (mode to latch into, latch codeword value to emit while
    // still in the PREVIOUS mode's bit width). Only the shortest single- or double-hop
    // route is used; derived from ISO 24778's LATCH_TABLE (ZXing AztecHighLevelEncoder).
    private static readonly Dictionary<(int From, int To), (int Mode, int Code)[]> LatchRoutes = new()
    {
        [(ModeUpper, ModeLower)] = new[] { (ModeLower, 28) },
        [(ModeUpper, ModeDigit)] = new[] { (ModeDigit, 30) },
        [(ModeUpper, ModeMixed)] = new[] { (ModeMixed, 29) },
        [(ModeUpper, ModePunct)] = new[] { (ModeMixed, 29), (ModePunct, 30) },

        [(ModeLower, ModeUpper)] = new[] { (ModeDigit, 30), (ModeUpper, 14) },
        [(ModeLower, ModeDigit)] = new[] { (ModeDigit, 30) },
        [(ModeLower, ModeMixed)] = new[] { (ModeMixed, 29) },
        [(ModeLower, ModePunct)] = new[] { (ModeMixed, 29), (ModePunct, 30) },

        [(ModeDigit, ModeUpper)] = new[] { (ModeUpper, 14) },
        [(ModeDigit, ModeLower)] = new[] { (ModeUpper, 14), (ModeLower, 28) },
        [(ModeDigit, ModeMixed)] = new[] { (ModeUpper, 14), (ModeMixed, 29) },
        [(ModeDigit, ModePunct)] = new[] { (ModeUpper, 14), (ModeMixed, 29), (ModePunct, 30) },

        [(ModeMixed, ModeUpper)] = new[] { (ModeUpper, 29) },
        [(ModeMixed, ModeLower)] = new[] { (ModeLower, 28) },
        [(ModeMixed, ModeDigit)] = new[] { (ModeUpper, 29), (ModeDigit, 30) },
        [(ModeMixed, ModePunct)] = new[] { (ModePunct, 30) },

        [(ModePunct, ModeUpper)] = new[] { (ModeUpper, 31) },
        [(ModePunct, ModeLower)] = new[] { (ModeUpper, 31), (ModeLower, 28) },
        [(ModePunct, ModeDigit)] = new[] { (ModeUpper, 31), (ModeDigit, 30) },
        [(ModePunct, ModeMixed)] = new[] { (ModeUpper, 31), (ModeMixed, 29) },
    };

    public static bool TryEncodeText(string text, out AztecBitBuffer bits, out string? error)
    {
        bits = new AztecBitBuffer();
        var mode = ModeUpper;

        foreach (var ch in text)
        {
            if (ch > 255)
            {
                error = $"Character '{ch}' is outside the 0-255 byte range Aztec text compaction supports.";
                bits = new AztecBitBuffer();
                return false;
            }

            var code = CharMap[mode][ch];

            if (code == 0)
            {
                var found = -1;

                for (var m = 0; m <= ModePunct; m++)
                {
                    if (CharMap[m][ch] != 0)
                    {
                        found = m;
                        break;
                    }
                }

                if (found == -1)
                {
                    error = $"Character '{ch}' (0x{(int)ch:X2}) is not representable in Aztec text compaction mode.";
                    bits = new AztecBitBuffer();
                    return false;
                }

                foreach (var (stepMode, stepCode) in LatchRoutes[(mode, found)])
                {
                    bits.AppendBits(stepCode, mode == ModeDigit ? 4 : 5);
                    mode = stepMode;
                }

                code = CharMap[mode][ch];
            }

            bits.AppendBits(code, mode == ModeDigit ? 4 : 5);
        }

        error = null;
        return true;
    }

    private static int[][] BuildCharMap()
    {
        var map = new int[5][];

        for (var m = 0; m < 5; m++)
        {
            map[m] = new int[256];
        }

        map[ModeUpper][' '] = 1;

        for (var c = 'A'; c <= 'Z'; c++)
        {
            map[ModeUpper][c] = c - 'A' + 2;
        }

        map[ModeLower][' '] = 1;

        for (var c = 'a'; c <= 'z'; c++)
        {
            map[ModeLower][c] = c - 'a' + 2;
        }

        map[ModeDigit][' '] = 1;

        for (var c = '0'; c <= '9'; c++)
        {
            map[ModeDigit][c] = c - '0' + 2;
        }

        map[ModeDigit][','] = 12;
        map[ModeDigit]['.'] = 13;

        var mixedTable = new[]
        {
            '\0', ' ', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\b',
            '\t', '\n', '\u000B', '\f', '\r', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
            '@', '\\', '^', '_', '`', '|', '~', '\u007F',
        };

        for (var i = 0; i < mixedTable.Length; i++)
        {
            map[ModeMixed][mixedTable[i]] = i;
        }

        // Position 0 and the '\0' filler positions (2-5) are invalid/unused slots in the
        // spec's table and are explicitly skipped, matching ZXing's own construction
        // guard (`if (punctTable[i] > 0)`) -- without it they would collide by all mapping
        // char '\0' to the same (bogus) entry.
        var punctTable = new[]
        {
            '\0', '\r', '\0', '\0', '\0', '\0', '!', '\'', '#', '$',
            '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.',
            '/', ':', ';', '<', '=', '>', '?', '[', ']', '{', '}',
        };

        for (var i = 0; i < punctTable.Length; i++)
        {
            if (punctTable[i] > 0)
            {
                map[ModePunct][punctTable[i]] = i;
            }
        }

        return map;
    }
}
