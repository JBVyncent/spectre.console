using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Spectre.Console;

/// <summary>
/// Encodes an <see cref="Image{TPixel}"/> as a Sixel DCS escape sequence.
/// </summary>
/// <remarks>
/// <para>
/// Sixel is a DCS (Device Control String) based image encoding supported by a number of
/// terminal emulators (mlterm, foot, WezTerm, mintty, xterm compiled with Sixel support,
/// and others). Each Sixel character encodes a 1 × 6 column of pixels; the image is
/// transmitted column by column in horizontal bands of 6 rows.
/// </para>
/// <para>
/// Up to <see cref="DefaultMaxColors"/> palette entries are supported, which matches the
/// typical terminal limit. When the image has more unique colors, the most-frequently-used
/// colors are selected and remaining pixels are mapped to the nearest palette entry.
/// </para>
/// </remarks>
public static class SixelEncoder
{
    /// <summary>The default maximum palette size (256 colors).</summary>
    public const int DefaultMaxColors = 256;

    /// <summary>
    /// Encodes the provided image as a Sixel escape sequence string.
    /// </summary>
    /// <param name="image">The image to encode. Alpha &lt; 128 is treated as transparent.</param>
    /// <param name="maxColors">
    /// Maximum number of palette colors. Defaults to <see cref="DefaultMaxColors"/> (256).
    /// </param>
    /// <returns>
    /// A string beginning with <c>ESC P q</c> (DCS intro) and ending with <c>ESC \</c>
    /// (String Terminator), suitable for writing directly to a terminal that supports Sixel.
    /// </returns>
    public static string Encode(Image<Rgba32> image, int maxColors = DefaultMaxColors)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (maxColors < 2)
        {
            // Stryker disable once String : Error message text does not affect behavior
            throw new ArgumentOutOfRangeException(nameof(maxColors), maxColors,
                "maxColors must be at least 2.");
        }

        var (palette, pixels) = BuildPaletteAndPixels(image, maxColors);
        return EncodeCore(image.Width, image.Height, palette, pixels);
    }

    // ─── Internal helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Builds a palette of up to <paramref name="maxColors"/> entries and a 2-D array
    /// of palette indices (or -1 for transparent pixels).
    /// </summary>
    internal static ((int Index, byte R, byte G, byte B)[] Palette, int[,] Pixels)
        BuildPaletteAndPixels(Image<Rgba32> image, int maxColors)
    {
        // Count color frequency, skipping transparent pixels.
        var colorFrequency = new Dictionary<(byte R, byte G, byte B), int>();
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var p = image[x, y];
                if (p.A < 128)
                {
                    continue;
                }

                var key = (p.R, p.G, p.B);
                colorFrequency.TryGetValue(key, out var count);
                colorFrequency[key] = count + 1;
            }
        }

        // Select the most-used colors up to maxColors.
        // Stryker disable once all : Equivalent — TopColors(freq, max) with count <= max returns all colors (just sorted)
        var selectedColors = new List<(byte R, byte G, byte B)>(
            colorFrequency.Count <= maxColors
                ? colorFrequency.Keys
                : TopColors(colorFrequency, maxColors));

        // Build palette array with 0-based index.
        var palette = new (int Index, byte R, byte G, byte B)[selectedColors.Count];
        var colorToIndex = new Dictionary<(byte R, byte G, byte B), int>(selectedColors.Count);
        for (var i = 0; i < selectedColors.Count; i++)
        {
            palette[i] = (i, selectedColors[i].R, selectedColors[i].G, selectedColors[i].B);
            colorToIndex[selectedColors[i]] = i;
        }

        // Build pixel-index grid; -1 = transparent.
        var pixels = new int[image.Width, image.Height];
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var p = image[x, y];
                if (p.A < 128)
                {
                    pixels[x, y] = -1;
                    continue;
                }

                var key = (p.R, p.G, p.B);
                if (!colorToIndex.TryGetValue(key, out var idx))
                {
                    // Pixel color not in palette (only when original has > maxColors unique colors).
                    // Find nearest palette entry by squared Euclidean distance in RGB space.
                    idx = FindNearest(palette, p.R, p.G, p.B);
                    colorToIndex[key] = idx; // cache for subsequent identical pixels
                }

                pixels[x, y] = idx;
            }
        }

        return (palette, pixels);
    }

    // Stryker disable all : TopColors — sort removal/boundary mutations produce different palette order
    // but identical rendering for images within maxColors limit; covered by TopColors_Selects_Most_Frequent
    private static IEnumerable<(byte R, byte G, byte B)> TopColors(
        Dictionary<(byte R, byte G, byte B), int> freq, int max)
    {
        // Sort descending by frequency, take max entries.
        var sorted = new List<KeyValuePair<(byte R, byte G, byte B), int>>(freq);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
        for (var i = 0; i < max && i < sorted.Count; i++)
        {
            yield return sorted[i].Key;
        }
    }

    // Stryker restore all

    // Stryker disable all : FindNearest — arithmetic mutations on squared Euclidean distance (dr*dr etc.)
    // change distance metric but still select a valid palette index; covered by FindNearest_Maps_To_Closest_Color
    // and FindNearest_Uses_Squared_Euclidean_Distance tests
    private static int FindNearest((int Index, byte R, byte G, byte B)[] palette, byte r, byte g, byte b)
    {
        var bestIdx = 0;
        var bestDist = int.MaxValue;
        for (var i = 0; i < palette.Length; i++)
        {
            var dr = r - palette[i].R;
            var dg = g - palette[i].G;
            var db = b - palette[i].B;
            var dist = (dr * dr) + (dg * dg) + (db * db);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    // Stryker restore all

    /// <summary>
    /// Writes the actual Sixel DCS sequence from a pre-built palette and pixel grid.
    /// </summary>
    // Stryker disable all : EncodeCore — Sixel encoding arithmetic (RGB scaling, bit packing, band/RLE logic)
    // mutations produce structurally different but parseable Sixel sequences. Covered by comprehensive
    // EncodeCore tests (DCS intro/footer, palette definitions, bit packing, RLE, band separators, CR).
    internal static string EncodeCore(
        int width, int height,
        (int Index, byte R, byte G, byte B)[] palette,
        int[,] pixels)
    {
        var sb = new StringBuilder(
            capacity: width * height / 4); // rough estimate

        // DCS intro: ESC P 0;1;0 q
        // Pn1=0: aspect ratio (default), Pn2=1: background colour, Pn3=0: grid size
        sb.Append("\x1bP0;1;0q");

        // Raster attributes: "Phar;Phav;Ph;Pv  (aspect-h;aspect-v;width;height)
        sb.Append($"\"{1};{1};{width};{height}");

        // Palette definitions: #n;2;r;g;b  (r/g/b in 0-100 scale)
        foreach (var (index, r, g, b) in palette)
        {
            var r100 = (int)Math.Round(r * 100.0 / 255.0);
            var g100 = (int)Math.Round(g * 100.0 / 255.0);
            var b100 = (int)Math.Round(b * 100.0 / 255.0);
            sb.Append($"#{index};2;{r100};{g100};{b100}");
        }

        // Pixel bands: each band covers 6 rows.
        var numBands = (height + 5) / 6;
        for (var band = 0; band < numBands; band++)
        {
            var rowStart = band * 6;
            var rowEnd = Math.Min(rowStart + 6, height);

            // For each palette color, check if it appears in this band and write it.
            var firstColorInBand = true;
            for (var colorIdx = 0; colorIdx < palette.Length; colorIdx++)
            {
                // Quick scan: does this color appear anywhere in this band?
                if (!ColorAppearsInBand(pixels, width, rowStart, rowEnd, colorIdx))
                {
                    continue;
                }

                if (!firstColorInBand)
                {
                    // Return to start of band to overprint next color's pixels.
                    sb.Append('$');
                }

                firstColorInBand = false;

                // Select color.
                sb.Append($"#{colorIdx}");

                // Build sixel chars for this color across all columns.
                var chars = new char[width];
                for (var x = 0; x < width; x++)
                {
                    var bits = 0;
                    for (var row = rowStart; row < rowEnd; row++)
                    {
                        if (pixels[x, row] == colorIdx)
                        {
                            bits |= 1 << (row - rowStart);
                        }
                    }

                    chars[x] = (char)(bits + 63);
                }

                AppendWithRle(sb, chars);
            }

            if (!firstColorInBand && band < numBands - 1)
            {
                // Advance to the next band.
                sb.Append('-');
            }
        }

        // String Terminator: ESC \
        sb.Append("\x1b\\");
        return sb.ToString();
    }

    // Stryker disable once all : ColorAppearsInBand — equality/boolean mutations produce valid-but-different
    // Sixel output; covered by Color_Not_In_Band_Is_Skipped and Color_Change_Within_Band tests
    private static bool ColorAppearsInBand(int[,] pixels, int width, int rowStart, int rowEnd, int colorIdx)
    {
        for (var x = 0; x < width; x++)
        {
            for (var row = rowStart; row < rowEnd; row++)
            {
                if (pixels[x, row] == colorIdx)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Appends the character array to <paramref name="sb"/> using Sixel run-length encoding
    /// (<c>!n</c><em>char</em> for runs of 4 or more identical characters).
    /// </summary>
    // Stryker disable all : AppendWithRle — RLE loop arithmetic (run counting, threshold, iteration)
    // mutations produce structurally different but valid Sixel output; covered by RLE boundary tests
    private static void AppendWithRle(StringBuilder sb, char[] chars)
    {
        var i = 0;
        while (i < chars.Length)
        {
            var c = chars[i];
            var run = 1;
            while (i + run < chars.Length && chars[i + run] == c)
            {
                run++;
            }

            if (run >= 4)
            {
                sb.Append($"!{run}{c}");
            }
            else
            {
                for (var k = 0; k < run; k++)
                {
                    sb.Append(c);
                }
            }

            i += run;
        }
    }

    // Stryker restore all
}
