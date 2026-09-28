namespace IBEBarcode.Core.Encoders.Pdf417;

internal static class Pdf417ErrorCorrectionCoefficients
{
    // Transcribed mechanically from ZXing's encoder/PDF417ErrorCorrection.java
    // EC_COEFFICIENTS (Apache 2.0), itself from ISO/IEC 15438:2001(E) Annex F.
    // Scoped to levels 0-5 (2/4/8/16/32/64 coefficients) -- levels 6-8 exist in the
    // spec as optional extra-strength options but ISO's own recommended-minimum-level
    // table (Annex E) never recommends them for any data size up to the format's
    // 863-data-codeword practical ceiling, so this plan omits their very large
    // (128/256/512-entry) tables rather than include untested-scope data.
    public static readonly int[][] Levels = new int[][]
    {
        new int[] { 27, 917 },
        new int[] { 522, 568, 723, 809 },
        new int[] { 237, 308, 436, 284, 646, 653, 428, 379 },
        new int[] { 274, 562, 232, 755, 599, 524, 801, 132, 295, 116, 442, 428, 295, 42, 176, 65 },
        new int[] { 361, 575, 922, 525, 176, 586, 640, 321, 536, 742, 677, 742, 687, 284, 193, 517, 273, 494, 263, 147, 593, 800, 571, 320, 803, 133, 231, 390, 685, 330, 63, 410 },
        new int[] { 539, 422, 6, 93, 862, 771, 453, 106, 610, 287, 107, 505, 733, 877, 381, 612, 723, 476, 462, 172, 430, 609, 858, 822, 543, 376, 511, 400, 672, 762, 283, 184, 440, 35, 519, 31, 460, 594, 225, 535, 517, 352, 605, 158, 651, 201, 488, 502, 648, 733, 717, 83, 404, 97, 280, 771, 840, 629, 4, 381, 843, 623, 264, 543 },
    };
}
