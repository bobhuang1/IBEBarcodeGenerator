namespace IBEBarcode.Core.Encoders.Pdf417;

internal static class Pdf417TextCompaction
{
    private const int LatchToText = 900;

    private const int SubmodeAlpha = 0;
    private const int SubmodeLower = 1;
    private const int SubmodeMixed = 2;
    private const int SubmodePunctuation = 3;

    private static readonly int[] Mixed = BuildInverse(new byte[]
    {
        48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 38, 13, 9, 44, 58,
        35, 45, 46, 36, 47, 43, 37, 42, 61, 94, 0, 32, 0, 0, 0,
    });

    private static readonly int[] Punctuation = BuildInverse(new byte[]
    {
        59, 60, 62, 64, 91, 92, 93, 95, 96, 126, 33, 13, 9, 44, 58,
        10, 45, 46, 36, 47, 34, 124, 42, 40, 41, 63, 123, 125, 39, 0,
    });

    private static int[] BuildInverse(byte[] raw)
    {
        var table = new int[128];
        Array.Fill(table, -1);

        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] > 0)
            {
                table[raw[i]] = i;
            }
        }

        return table;
    }

    private static bool IsAlphaUpper(char ch) => ch == ' ' || (ch is >= 'A' and <= 'Z');

    private static bool IsAlphaLower(char ch) => ch == ' ' || (ch is >= 'a' and <= 'z');

    private static bool IsMixed(char ch) => ch < 128 && Mixed[ch] != -1;

    private static bool IsPunctuation(char ch) => ch < 128 && Punctuation[ch] != -1;

    public static bool IsSupportedCharacter(char ch) =>
        IsAlphaUpper(ch) || IsAlphaLower(ch) || IsMixed(ch) || IsPunctuation(ch);

    public static bool TryEncodeText(string text, out int[] codewords, out string? error)
    {
        foreach (var ch in text)
        {
            if (!IsSupportedCharacter(ch))
            {
                codewords = Array.Empty<int>();
                error = $"Character '{ch}' is not representable in PDF417 Text Compaction.";
                return false;
            }
        }

        var tmp = new List<int>();
        var submode = SubmodeAlpha;
        var idx = 0;

        while (idx < text.Length)
        {
            var ch = text[idx];

            switch (submode)
            {
                case SubmodeAlpha:
                    if (IsAlphaUpper(ch))
                    {
                        tmp.Add(ch == ' ' ? 26 : ch - 'A');
                    }
                    else if (IsAlphaLower(ch))
                    {
                        submode = SubmodeLower;
                        tmp.Add(27);
                        continue;
                    }
                    else if (IsMixed(ch))
                    {
                        submode = SubmodeMixed;
                        tmp.Add(28);
                        continue;
                    }
                    else
                    {
                        tmp.Add(29);
                        tmp.Add(Punctuation[ch]);
                    }

                    break;

                case SubmodeLower:
                    if (IsAlphaLower(ch))
                    {
                        tmp.Add(ch == ' ' ? 26 : ch - 'a');
                    }
                    else if (IsAlphaUpper(ch))
                    {
                        tmp.Add(27);
                        tmp.Add(ch - 'A');
                    }
                    else if (IsMixed(ch))
                    {
                        submode = SubmodeMixed;
                        tmp.Add(28);
                        continue;
                    }
                    else
                    {
                        tmp.Add(29);
                        tmp.Add(Punctuation[ch]);
                    }

                    break;

                case SubmodeMixed:
                    if (IsMixed(ch))
                    {
                        tmp.Add(Mixed[ch]);
                    }
                    else if (IsAlphaUpper(ch))
                    {
                        submode = SubmodeAlpha;
                        tmp.Add(28);
                        continue;
                    }
                    else if (IsAlphaLower(ch))
                    {
                        submode = SubmodeLower;
                        tmp.Add(27);
                        continue;
                    }
                    else
                    {
                        if (idx + 1 < text.Length && IsPunctuation(text[idx + 1]))
                        {
                            submode = SubmodePunctuation;
                            tmp.Add(25);
                            continue;
                        }

                        tmp.Add(29);
                        tmp.Add(Punctuation[ch]);
                    }

                    break;

                default: // SubmodePunctuation
                    if (IsPunctuation(ch))
                    {
                        tmp.Add(Punctuation[ch]);
                    }
                    else
                    {
                        submode = SubmodeAlpha;
                        tmp.Add(29);
                        continue;
                    }

                    break;
            }

            idx++;
        }

        var result = new List<int> { LatchToText };
        var h = 0;

        for (var i = 0; i < tmp.Count; i++)
        {
            var odd = i % 2 != 0;

            if (odd)
            {
                h = (h * 30) + tmp[i];
                result.Add(h);
            }
            else
            {
                h = tmp[i];
            }
        }

        if (tmp.Count % 2 != 0)
        {
            result.Add((h * 30) + 29);
        }

        codewords = result.ToArray();
        error = null;
        return true;
    }
}
