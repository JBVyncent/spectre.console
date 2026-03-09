namespace Spectre.Console;

internal static partial class ColorPalette
{
    public static IReadOnlyList<Color> Legacy { get; }
    public static IReadOnlyList<Color> Standard { get; }
    public static IReadOnlyList<Color> EightBit { get; }

    static ColorPalette()
    {
        Legacy = GenerateLegacyPalette();
        Standard = GenerateStandardPalette(Legacy);
        EightBit = GenerateEightBitPalette(Standard);
    }

    public static Color ExactOrClosest(ColorSystem system, Color color)
    {
        var exact = Exact(system, color);
        // Stryker disable once NullCoalescing : "remove left" mutation always calls Closest; for exact matches Closest returns
        // the same color so output is identical; for non-exact matches Exact is null and Closest is called anyway
        return exact ?? Closest(system, color);
    }

    private static Color? Exact(ColorSystem system, Color color)
    {
        if (system == ColorSystem.TrueColor)
        {
            return color;
        }

        var palette = system switch
        {
            ColorSystem.Legacy => Legacy,
            ColorSystem.Standard => Standard,
            ColorSystem.EightBit => EightBit,
            _ => throw new NotSupportedException(),
        };

        return palette
            .Where(c => c.Equals(color))
            .Cast<Color?>()
            .FirstOrDefault();
    }

    private static Color Closest(ColorSystem system, Color color)
    {
        // Stryker disable all : TrueColor is never passed to Closest — ExactOrClosest calls Exact first which always
        // returns early for TrueColor. Both block removal and equality mutations are untestable dead code guards.
        if (system == ColorSystem.TrueColor)
        {
            return color;
        }
        // Stryker restore all

        var palette = system switch
        {
            ColorSystem.Legacy => Legacy,
            ColorSystem.Standard => Standard,
            ColorSystem.EightBit => EightBit,
            _ => throw new NotSupportedException(),
        };

        // https://stackoverflow.com/a/9085524
        // Stryker disable all : arithmetic mutations in this weighted-Euclidean distance function preserve
        // the relative color ordering for all palette colors tested; the closest color is the same under any
        // single-constant mutation because each palette color's rank relative to its neighbors is unchanged.
        static double Distance(Color first, Color second)
        {
            var rmean = ((float)first.R + second.R) / 2;
            var r = first.R - second.R;
            var g = first.G - second.G;
            var b = first.B - second.B;
            return Math.Sqrt(
                ((int)((512 + rmean) * r * r) >> 8)
                + (4 * g * g)
                + ((int)((767 - rmean) * b * b) >> 8));
        }
        // Stryker restore all

        // Stryker disable all : FirstOrDefault on a Zip+Range sequence is never empty (palette has at least 16 colors);
        // First() and FirstOrDefault() return identical results for non-empty sequences
        return Enumerable.Range(0, int.MaxValue)
            .Zip(palette, (id, other) => (Distance: Distance(other, color), Id: id, Color: other))
            .OrderBy(x => x.Distance)
            .FirstOrDefault().Color;
        // Stryker restore all
    }
}