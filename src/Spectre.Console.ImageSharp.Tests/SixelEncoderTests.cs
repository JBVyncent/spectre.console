using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageColor = SixLabors.ImageSharp.Color;

namespace Spectre.Console.Tests.Unit;

public sealed class SixelEncoderTests
{
    // ── Encode() — public API guards ─────────────────────────────────────────

    public sealed class TheEncodeMethod
    {
        [Fact]
        public void Should_Throw_If_Image_Is_Null()
        {
            var result = Record.Exception(() => SixelEncoder.Encode(null!));
            result.ShouldBeOfType<ArgumentNullException>()
                .And(ex => ex.ParamName.ShouldBe("image"));
        }

        [Fact]
        public void Should_Throw_If_MaxColors_Is_Less_Than_Two()
        {
            using var image = CreateSolidImage(1, 1, ImageColor.Red);

            var result = Record.Exception(() => SixelEncoder.Encode(image, 1));
            result.ShouldBeOfType<ArgumentOutOfRangeException>()
                .And(ex => ex.ParamName.ShouldBe("maxColors"));
        }

        [Fact]
        public void Should_Start_With_DCS_Intro()
        {
            using var image = CreateSolidImage(1, 1, ImageColor.Red);

            var sixel = SixelEncoder.Encode(image);

            sixel.ShouldStartWith("\x1bP");
        }

        [Fact]
        public void Should_End_With_String_Terminator()
        {
            using var image = CreateSolidImage(1, 1, ImageColor.Red);

            var sixel = SixelEncoder.Encode(image);

            sixel.ShouldEndWith("\x1b\\");
        }

        [Fact]
        public void Should_Include_Raster_Attributes_With_Image_Dimensions()
        {
            using var image = CreateSolidImage(4, 6, ImageColor.Blue);

            var sixel = SixelEncoder.Encode(image);

            // "1;1;4;6 (aspect-h;aspect-v;width;height)
            sixel.ShouldContain("\"1;1;4;6");
        }

        [Fact]
        public void Should_Include_Palette_Definition_For_Single_Color()
        {
            // A fully-red solid image must define at least one palette entry starting with #0
            using var image = CreateSolidImage(2, 2, new Rgba32(255, 0, 0, 255));

            var sixel = SixelEncoder.Encode(image);

            // Palette entry format: #<idx>;2;<r100>;<g100>;<b100>
            sixel.ShouldContain("#0;2;");
        }

        [Fact]
        public void Should_Encode_Two_Color_Image_Without_Throwing()
        {
            // 1×2 image: top pixel red, bottom pixel blue.
            using var image = new Image<Rgba32>(1, 2);
            image[0, 0] = new Rgba32(255, 0, 0, 255);
            image[0, 1] = new Rgba32(0, 0, 255, 255);

            var result = Record.Exception(() => SixelEncoder.Encode(image));
            result.ShouldBeNull();
        }

        [Fact]
        public void Default_MaxColors_Is_256()
        {
            SixelEncoder.DefaultMaxColors.ShouldBe(256);
        }
    }

    // ── BuildPaletteAndPixels() — internal ───────────────────────────────────

    public sealed class TheBuildPaletteAndPixelsMethod
    {
        [Fact]
        public void Transparent_Pixels_Get_Minus_One_Index()
        {
            using var image = new Image<Rgba32>(1, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 0); // fully transparent

            var (_, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            pixels[0, 0].ShouldBe(-1);
        }

        [Fact]
        public void Semi_Transparent_Pixel_Below_128_Is_Transparent()
        {
            using var image = new Image<Rgba32>(1, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 127); // alpha 127 < 128

            var (_, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            pixels[0, 0].ShouldBe(-1);
        }

        [Fact]
        public void Semi_Transparent_Pixel_At_128_Is_Opaque()
        {
            using var image = new Image<Rgba32>(1, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 128); // alpha 128 >= 128

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            pixels[0, 0].ShouldBeGreaterThanOrEqualTo(0);
            palette.Length.ShouldBe(1);
        }

        [Fact]
        public void Single_Color_Image_Produces_Single_Palette_Entry()
        {
            using var image = CreateSolidImage(3, 3, new Rgba32(100, 150, 200, 255));

            var (palette, _) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            palette.Length.ShouldBe(1);
        }

        [Fact]
        public void Two_Color_Image_Produces_Two_Palette_Entries()
        {
            using var image = new Image<Rgba32>(2, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 255);
            image[1, 0] = new Rgba32(0, 255, 0, 255);

            var (palette, _) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            palette.Length.ShouldBe(2);
        }

        [Fact]
        public void MaxColors_Limits_Palette_Size()
        {
            // 10 unique fully-opaque colors
            using var image = new Image<Rgba32>(10, 1);
            for (var x = 0; x < 10; x++)
            {
                image[x, 0] = new Rgba32((byte)(x * 25), 0, 0, 255);
            }

            var (palette, _) = SixelEncoder.BuildPaletteAndPixels(image, 4);

            palette.Length.ShouldBeLessThanOrEqualTo(4);
        }

        [Fact]
        public void Pixels_Outside_Palette_Are_Mapped_To_Nearest()
        {
            // Build a 3-color image but limit palette to 2.
            // The third color should be mapped to whichever is closer.
            using var image = new Image<Rgba32>(3, 1);
            image[0, 0] = new Rgba32(0, 0, 0, 255);    // black — palette[0]
            image[1, 0] = new Rgba32(255, 255, 255, 255); // white — palette[1]
            image[2, 0] = new Rgba32(10, 10, 10, 255);   // near-black → maps to black

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 2);

            palette.Length.ShouldBe(2);
            // The near-black pixel must be mapped to a valid palette index (0 or 1).
            pixels[2, 0].ShouldBeGreaterThanOrEqualTo(0);
            pixels[2, 0].ShouldBeLessThan(2);
        }

        [Fact]
        public void All_Transparent_Image_Produces_Empty_Palette()
        {
            using var image = new Image<Rgba32>(2, 2);
            // All pixels default to transparent (RGBA 0,0,0,0)

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            palette.Length.ShouldBe(0);
            pixels[0, 0].ShouldBe(-1);
            pixels[1, 1].ShouldBe(-1);
        }
    }

    // ── EncodeCore() — internal ───────────────────────────────────────────────

    public sealed class TheEncodeCoreMethod
    {
        [Fact]
        public void Single_Pixel_Single_Color_Produces_Valid_Sixel()
        {
            var palette = new[] { (Index: 0, R: (byte)255, G: (byte)0, B: (byte)0) };
            var pixels = new int[1, 1];
            pixels[0, 0] = 0;

            var result = SixelEncoder.EncodeCore(1, 1, palette, pixels);

            result.ShouldStartWith("\x1bP");
            result.ShouldEndWith("\x1b\\");
            result.ShouldContain("#0;2;"); // palette definition
        }

        [Fact]
        public void RLE_Applied_For_Run_Of_Four_Or_More()
        {
            // A 8-wide single-color band will produce !8<c> RLE
            var palette = new[] { (Index: 0, R: (byte)200, G: (byte)100, B: (byte)50) };
            var pixels = new int[8, 1];
            for (var x = 0; x < 8; x++)
            {
                pixels[x, 0] = 0;
            }

            var result = SixelEncoder.EncodeCore(8, 1, palette, pixels);

            // Should contain RLE notation !8<char>
            result.ShouldContain("!8");
        }

        [Fact]
        public void RLE_Not_Applied_For_Run_Of_Three_Or_Less()
        {
            // 3-wide single-color band — each pixel literal, no !n prefix
            var palette = new[] { (Index: 0, R: (byte)200, G: (byte)100, B: (byte)50) };
            var pixels = new int[3, 1];
            for (var x = 0; x < 3; x++)
            {
                pixels[x, 0] = 0;
            }

            var result = SixelEncoder.EncodeCore(3, 1, palette, pixels);

            // No RLE notation expected
            result.ShouldNotContain("!3");
        }

        [Fact]
        public void Completely_Transparent_Image_Produces_Header_And_Footer_Only()
        {
            // No opaque pixels → no color bands in body
            var palette = Array.Empty<(int Index, byte R, byte G, byte B)>();
            var pixels = new int[2, 2];
            pixels[0, 0] = -1;
            pixels[1, 0] = -1;
            pixels[0, 1] = -1;
            pixels[1, 1] = -1;

            var result = SixelEncoder.EncodeCore(2, 2, palette, pixels);

            result.ShouldStartWith("\x1bP");
            result.ShouldEndWith("\x1b\\");
        }

        [Fact]
        public void Multi_Band_Image_Contains_Band_Separator()
        {
            // An image taller than 6 rows produces 2 bands separated by '-'
            var palette = new[] { (Index: 0, R: (byte)128, G: (byte)128, B: (byte)128) };
            var pixels = new int[1, 12];
            for (var y = 0; y < 12; y++)
            {
                pixels[0, y] = 0;
            }

            var result = SixelEncoder.EncodeCore(1, 12, palette, pixels);

            // At least one band separator
            result.ShouldContain("-");
        }

        [Fact]
        public void Color_Change_Within_Band_Uses_CR_Separator()
        {
            // 2-pixel wide image, pixel 0 = color 0, pixel 1 = color 1,
            // both in the same row (same band). The second color in the band
            // must be preceded by '$' (CR) to return to the start of the band.
            var palette = new[]
            {
                (Index: 0, R: (byte)255, G: (byte)0, B: (byte)0),
                (Index: 1, R: (byte)0, G: (byte)255, B: (byte)0),
            };
            var pixels = new int[2, 1];
            pixels[0, 0] = 0;
            pixels[1, 0] = 1;

            var result = SixelEncoder.EncodeCore(2, 1, palette, pixels);

            // The second color's row should start with '$'
            result.ShouldContain("$");
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static Image<Rgba32> CreateSolidImage(int width, int height, ImageColor color)
    {
        var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = color.ToPixel<Rgba32>();
            }
        }

        return image;
    }

    private static Image<Rgba32> CreateSolidImage(int width, int height, Rgba32 color)
    {
        var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = color;
            }
        }

        return image;
    }
}
