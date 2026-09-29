using System.Text;

namespace IBEBarcode.Core.Encoders.Qr;

internal static class QrKanji
{
    private static readonly Encoding ShiftJis = CreateShiftJis();

    private static Encoding CreateShiftJis()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(932);
    }

    // Returns the 13-bit packed QR Kanji value for a single character, or null if it
    // doesn't encode to a 2-byte Shift-JIS sequence within QR's supported Kanji ranges
    // (0x8140-0x9FFC, 0xE040-0xEBBF; ISO/IEC 18004 Table 6).
    public static int? TryGetValue(char c)
    {
        var bytes = ShiftJis.GetBytes(c.ToString());

        if (bytes.Length != 2)
        {
            return null;
        }

        var code = (bytes[0] << 8) | bytes[1];
        int adjusted;

        if (code is >= 0x8140 and <= 0x9FFC)
        {
            adjusted = code - 0x8140;
        }
        else if (code is >= 0xE040 and <= 0xEBBF)
        {
            adjusted = code - 0xC140;
        }
        else
        {
            return null;
        }

        var msb = adjusted >> 8;
        var lsb = adjusted & 0xFF;
        return (msb * 0xC0) + lsb;
    }

    // Inverse of TryGetValue -- used only by round-trip test decoders.
    public static char FromValue(int value)
    {
        var msb = value / 0xC0;
        var lsb = value % 0xC0;
        var adjusted = (msb << 8) | lsb;
        var code = adjusted <= 0x1EFF ? adjusted + 0x8140 : adjusted + 0xC140;
        var bytes = new[] { (byte)(code >> 8), (byte)code };
        return ShiftJis.GetString(bytes)[0];
    }
}
