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
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.And(ex => ex.ParamName.Should().Be("image"));
        }

        [Fact]
        public void Should_Throw_If_MaxColors_Is_Less_Than_Two()
        {
            using var image = CreateSolidImage(1, 1, ImageColor.Red);

            var result = Record.Exception(() => SixelEncoder.Encode(image, 1));
            result.Should().BeOfType<ArgumentOutOfRangeException>()
                    .Which.And(ex => ex.ParamName.Should().Be("maxColors"));
        }

        [Fact]
        public void Should_Start_With_DCS_Intro()
        {
            using var image = CreateSolidImage(1, 1, ImageColor.Red);

            var sixel = SixelEncoder.Encode(image);

            sixel.Should().StartWith("\x1bP");
        }

        [Fact]
        public void Should_End_With_String_Terminator()
        {
            using var image = CreateSolidImage(1, 1, ImageColor.Red);

            var sixel = SixelEncoder.Encode(image);

            sixel.Should().EndWith("\x1b\\");
        }

        [Fact]
        public void Should_Include_Raster_Attributes_With_Image_Dimensions()
        {
            using var image = CreateSolidImage(4, 6, ImageColor.Blue);

            var sixel = SixelEncoder.Encode(image);

            // "1;1;4;6 (aspect-h;aspect-v;width;height)
            sixel.Should().Contain("\"1;1;4;6");
        }

        [Fact]
        public void Should_Include_Palette_Definition_For_Single_Color()
        {
            // A fully-red solid image must define at least one palette entry starting with #0
            using var image = CreateSolidImage(2, 2, new Rgba32(255, 0, 0, 255));

            var sixel = SixelEncoder.Encode(image);

            // Palette entry format: #<idx>;2;<r100>;<g100>;<b100>
            sixel.Should().Contain("#0;2;");
        }

        [Fact]
        public void Should_Encode_Two_Color_Image_Without_Throwing()
        {
            // 1×2 image: top pixel red, bottom pixel blue.
            using var image = new Image<Rgba32>(1, 2);
            image[0, 0] = new Rgba32(255, 0, 0, 255);
            image[0, 1] = new Rgba32(0, 0, 255, 255);

            var result = Record.Exception(() => SixelEncoder.Encode(image));
            result.Should().BeNull();
        }

        [Fact]
        public void Default_MaxColors_Is_256()
        {
            SixelEncoder.DefaultMaxColors.Should().Be(256);
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

            pixels[0, 0].Should().Be(-1);
        }

        [Fact]
        public void Semi_Transparent_Pixel_Below_128_Is_Transparent()
        {
            using var image = new Image<Rgba32>(1, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 127); // alpha 127 < 128

            var (_, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            pixels[0, 0].Should().Be(-1);
        }

        [Fact]
        public void Semi_Transparent_Pixel_At_128_Is_Opaque()
        {
            using var image = new Image<Rgba32>(1, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 128); // alpha 128 >= 128

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            pixels[0, 0].Should().BeGreaterThanOrEqualTo(0);
            palette.Length.Should().Be(1);
        }

        [Fact]
        public void Single_Color_Image_Produces_Single_Palette_Entry()
        {
            using var image = CreateSolidImage(3, 3, new Rgba32(100, 150, 200, 255));

            var (palette, _) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            palette.Length.Should().Be(1);
        }

        [Fact]
        public void Two_Color_Image_Produces_Two_Palette_Entries()
        {
            using var image = new Image<Rgba32>(2, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 255);
            image[1, 0] = new Rgba32(0, 255, 0, 255);

            var (palette, _) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            palette.Length.Should().Be(2);
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

            palette.Length.Should().BeLessThanOrEqualTo(4);
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

            palette.Length.Should().Be(2);
            // The near-black pixel must be mapped to a valid palette index (0 or 1).
            pixels[2, 0].Should().BeGreaterThanOrEqualTo(0);
            pixels[2, 0].Should().BeLessThan(2);
        }

        [Fact]
        public void All_Transparent_Image_Produces_Empty_Palette()
        {
            using var image = new Image<Rgba32>(2, 2);
            // All pixels default to transparent (RGBA 0,0,0,0)

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            palette.Length.Should().Be(0);
            pixels[0, 0].Should().Be(-1);
            pixels[1, 1].Should().Be(-1);
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

            result.Should().StartWith("\x1bP");
            result.Should().EndWith("\x1b\\");
            result.Should().Contain("#0;2;"); // palette definition
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
            result.Should().Contain("!8");
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
            result.Should().NotContain("!3");
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

            result.Should().StartWith("\x1bP");
            result.Should().EndWith("\x1b\\");
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
            result.Should().Contain("-");
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
            result.Should().Contain("$");
        }
    }

    // ── Mutation killers — SixelEncoder arithmetic/logic ──────────────────────

    public sealed class MutationKillers
    {
        [Fact]
        public void MaxColors_Boundary_Two_Is_Accepted()
        {
            using var image = CreateSolidImage(1, 1, new Rgba32(255, 0, 0, 255));
            var result = Record.Exception(() => SixelEncoder.Encode(image, 2));
            result.Should().BeNull();
        }

        [Fact]
        public void MaxColors_Boundary_One_Is_Rejected()
        {
            using var image = CreateSolidImage(1, 1, new Rgba32(255, 0, 0, 255));
            var act = () => SixelEncoder.Encode(image, 1);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Color_Frequency_Counted_Correctly()
        {
            // 3 pixels of red, 1 pixel of blue — when limited to 1 color, red wins
            using var image = new Image<Rgba32>(4, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 255);
            image[1, 0] = new Rgba32(255, 0, 0, 255);
            image[2, 0] = new Rgba32(255, 0, 0, 255);
            image[3, 0] = new Rgba32(0, 0, 255, 255);

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 2);

            // Both colors should be in palette with 2 colors allowed
            palette.Length.Should().Be(2);

            // Now limit to 1 — red (most frequent) should win
            var (palette1, _) = SixelEncoder.BuildPaletteAndPixels(image, 1);
            palette1.Length.Should().Be(1);
            palette1[0].R.Should().Be(255);
            palette1[0].G.Should().Be(0);
        }

        [Fact]
        public void Palette_Indices_Are_Zero_Based()
        {
            using var image = new Image<Rgba32>(2, 1);
            image[0, 0] = new Rgba32(100, 0, 0, 255);
            image[1, 0] = new Rgba32(0, 100, 0, 255);

            var (palette, _) = SixelEncoder.BuildPaletteAndPixels(image, 256);

            palette[0].Index.Should().Be(0);
            palette[1].Index.Should().Be(1);
        }

        [Fact]
        public void FindNearest_Maps_To_Closest_Color()
        {
            // 2 colors: black and white. Near-black should map to black (index 0),
            // near-white should map to white (index 1).
            using var image = new Image<Rgba32>(4, 1);
            image[0, 0] = new Rgba32(0, 0, 0, 255);       // black
            image[1, 0] = new Rgba32(255, 255, 255, 255);  // white
            image[2, 0] = new Rgba32(10, 10, 10, 255);     // near-black
            image[3, 0] = new Rgba32(250, 250, 250, 255);  // near-white

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 2);

            // Find which index is black vs white
            var blackIdx = palette[0].R == 0 ? 0 : 1;
            var whiteIdx = 1 - blackIdx;

            pixels[2, 0].Should().Be(blackIdx, "near-black should map to black");
            pixels[3, 0].Should().Be(whiteIdx, "near-white should map to white");
        }

        [Fact]
        public void FindNearest_Uses_Squared_Euclidean_Distance()
        {
            // Color (128, 0, 0) is closer to (200, 0, 0) than to (0, 0, 0)
            // dist to (200,0,0) = (128-200)^2 = 5184
            // dist to (0,0,0) = 128^2 = 16384
            using var image = new Image<Rgba32>(3, 1);
            image[0, 0] = new Rgba32(200, 0, 0, 255);
            image[1, 0] = new Rgba32(0, 0, 0, 255);
            image[2, 0] = new Rgba32(128, 0, 0, 255);  // should map to 200

            var (palette, pixels) = SixelEncoder.BuildPaletteAndPixels(image, 2);

            // pixel[2,0] should be same index as pixel[0,0] (the red)
            pixels[2, 0].Should().Be(pixels[0, 0]);
        }

        [Fact]
        public void RGB_Scaling_Produces_Correct_Palette_Values()
        {
            // RGB (255, 0, 128): r100=100, g100=0, b100=50
            var palette = new[] { (Index: 0, R: (byte)255, G: (byte)0, B: (byte)128) };
            var pixels = new int[1, 1];
            pixels[0, 0] = 0;

            var result = SixelEncoder.EncodeCore(1, 1, palette, pixels);

            result.Should().Contain("#0;2;100;0;50");
        }

        [Fact]
        public void RGB_Scaling_Rounds_Correctly()
        {
            // RGB (1, 1, 1): 1*100/255 = 0.392... → rounds to 0
            var palette = new[] { (Index: 0, R: (byte)1, G: (byte)1, B: (byte)1) };
            var pixels = new int[1, 1];
            pixels[0, 0] = 0;

            var result = SixelEncoder.EncodeCore(1, 1, palette, pixels);

            result.Should().Contain("#0;2;0;0;0");
        }

        [Fact]
        public void Sixel_Bit_Packing_Encodes_Row_Bits_Correctly()
        {
            // 1x6 image, single color, all rows set → bits = 0b111111 = 63, char = 63+63 = 126 = '~'
            var palette = new[] { (Index: 0, R: (byte)100, G: (byte)100, B: (byte)100) };
            var pixels = new int[1, 6];
            for (var y = 0; y < 6; y++)
            {
                pixels[0, y] = 0;
            }

            var result = SixelEncoder.EncodeCore(1, 6, palette, pixels);

            // The sixel char for all 6 bits set = (63) + 63 = 126 = '~'
            result.Should().Contain("~");
        }

        [Fact]
        public void Sixel_Bit_Packing_Single_Row_Set()
        {
            // 1x1 image: only row 0 set → bit 0 = 1 → bits=1, char=1+63=64='@'
            var palette = new[] { (Index: 0, R: (byte)100, G: (byte)100, B: (byte)100) };
            var pixels = new int[1, 1];
            pixels[0, 0] = 0;

            var result = SixelEncoder.EncodeCore(1, 1, palette, pixels);

            result.Should().Contain("@");
        }

        [Fact]
        public void Sixel_Bit_Packing_Row_2_Set()
        {
            // 1x3 image, only row 2 set → bit 2 = 4 → char = 4+63 = 67 = 'C'
            var palette = new[] { (Index: 0, R: (byte)100, G: (byte)100, B: (byte)100) };
            var pixels = new int[1, 3];
            pixels[0, 0] = -1; // transparent
            pixels[0, 1] = -1; // transparent
            pixels[0, 2] = 0;  // color 0

            var result = SixelEncoder.EncodeCore(1, 3, palette, pixels);

            // bit 2 = 4, char = 4 + 63 = 67 = 'C'
            result.Should().Contain("C");
        }

        [Fact]
        public void Band_Separator_Between_Bands()
        {
            // 1x7 image (2 bands: 0-5 and 6), single color
            var palette = new[] { (Index: 0, R: (byte)50, G: (byte)50, B: (byte)50) };
            var pixels = new int[1, 7];
            for (var y = 0; y < 7; y++)
            {
                pixels[0, y] = 0;
            }

            var result = SixelEncoder.EncodeCore(1, 7, palette, pixels);

            // Should have '-' between bands but not after last band
            result.Should().Contain("-");
            // The DCS sequence ends with ESC\, not with -ESC\
            result.Should().NotEndWith("-\x1b\\");
        }

        [Fact]
        public void No_Band_Separator_For_Single_Band()
        {
            // 1x6 image (exactly 1 band)
            var palette = new[] { (Index: 0, R: (byte)50, G: (byte)50, B: (byte)50) };
            var pixels = new int[1, 6];
            for (var y = 0; y < 6; y++)
            {
                pixels[0, y] = 0;
            }

            var result = SixelEncoder.EncodeCore(1, 6, palette, pixels);

            // Extract body between raster attributes and ST
            var bodyStart = result.IndexOf("q") + 1;
            var bodyEnd = result.LastIndexOf("\x1b\\");
            var body = result[bodyStart..bodyEnd];

            body.Should().NotContain("-");
        }

        [Fact]
        public void CR_Separator_Between_Colors_In_Same_Band()
        {
            // 1x1 image with 2 colors at different x positions
            var palette = new[]
            {
                (Index: 0, R: (byte)255, G: (byte)0, B: (byte)0),
                (Index: 1, R: (byte)0, G: (byte)255, B: (byte)0),
            };
            var pixels = new int[2, 1];
            pixels[0, 0] = 0; // color 0
            pixels[1, 0] = 1; // color 1

            var result = SixelEncoder.EncodeCore(2, 1, palette, pixels);

            // '$' separates colors within a band
            result.Should().Contain("$");
        }

        [Fact]
        public void Color_Not_In_Band_Is_Skipped()
        {
            // 2 colors, but each only appears in its own band
            var palette = new[]
            {
                (Index: 0, R: (byte)255, G: (byte)0, B: (byte)0),
                (Index: 1, R: (byte)0, G: (byte)255, B: (byte)0),
            };
            var pixels = new int[1, 12]; // 2 bands
            for (var y = 0; y < 6; y++)
            {
                pixels[0, y] = 0;  // band 0: color 0 only
            }

            for (var y = 6; y < 12; y++)
            {
                pixels[0, y] = 1;  // band 1: color 1 only
            }

            var result = SixelEncoder.EncodeCore(1, 12, palette, pixels);

            // Band 0 should have #0 but no '$' (only one color in band)
            // Band 1 should have #1 but no '$'
            // There should be a '-' between bands
            result.Should().Contain("#0");
            result.Should().Contain("#1");
            result.Should().Contain("-");
        }

        [Fact]
        public void RLE_Exact_Boundary_Three_Not_Compressed()
        {
            // Run of exactly 3 → no RLE
            var palette = new[] { (Index: 0, R: (byte)100, G: (byte)100, B: (byte)100) };
            var pixels = new int[3, 1];
            for (var x = 0; x < 3; x++)
            {
                pixels[x, 0] = 0;
            }

            var result = SixelEncoder.EncodeCore(3, 1, palette, pixels);

            result.Should().NotContain("!");
        }

        [Fact]
        public void RLE_Exact_Boundary_Four_Is_Compressed()
        {
            // Run of exactly 4 → RLE
            var palette = new[] { (Index: 0, R: (byte)100, G: (byte)100, B: (byte)100) };
            var pixels = new int[4, 1];
            for (var x = 0; x < 4; x++)
            {
                pixels[x, 0] = 0;
            }

            var result = SixelEncoder.EncodeCore(4, 1, palette, pixels);

            result.Should().Contain("!4");
        }

        [Fact]
        public void RLE_Mixed_Runs()
        {
            // 6 pixels: 4 of color 0, 2 of color 1
            var palette = new[]
            {
                (Index: 0, R: (byte)255, G: (byte)0, B: (byte)0),
                (Index: 1, R: (byte)0, G: (byte)255, B: (byte)0),
            };
            var pixels = new int[6, 1];
            for (var x = 0; x < 4; x++)
            {
                pixels[x, 0] = 0;
            }

            for (var x = 4; x < 6; x++)
            {
                pixels[x, 0] = 1;
            }

            var result = SixelEncoder.EncodeCore(6, 1, palette, pixels);

            // Color 0 has run of 4 → "!4" RLE
            result.Should().Contain("!4");
        }

        [Fact]
        public void Raster_Attributes_Include_Dimensions()
        {
            var palette = new[] { (Index: 0, R: (byte)0, G: (byte)0, B: (byte)0) };
            var pixels = new int[10, 20];
            for (var y = 0; y < 20; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    pixels[x, y] = 0;
                }
            }

            var result = SixelEncoder.EncodeCore(10, 20, palette, pixels);

            result.Should().Contain("\"1;1;10;20");
        }

        [Fact]
        public void NumBands_Calculation_Correct_For_Non_Multiple_Of_Six()
        {
            // 7 rows → 2 bands. Band 0: rows 0-5, Band 1: row 6
            var palette = new[] { (Index: 0, R: (byte)128, G: (byte)128, B: (byte)128) };
            var pixels = new int[1, 7];
            for (var y = 0; y < 7; y++)
            {
                pixels[0, y] = 0;
            }

            var result = SixelEncoder.EncodeCore(1, 7, palette, pixels);

            // Should have exactly 1 band separator
            var separatorCount = result.Count(c => c == '-');
            separatorCount.Should().Be(1);
        }

        [Fact]
        public void Empty_Palette_Produces_No_Color_Data()
        {
            var palette = Array.Empty<(int Index, byte R, byte G, byte B)>();
            var pixels = new int[2, 2];
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    pixels[x, y] = -1;
                }
            }

            var result = SixelEncoder.EncodeCore(2, 2, palette, pixels);

            // Should still have DCS intro and ST but no palette or pixel data
            result.Should().StartWith("\x1bP");
            result.Should().EndWith("\x1b\\");
            result.Should().NotContain("#");
        }

        [Fact]
        public void TopColors_Selects_Most_Frequent()
        {
            // 5 unique colors, limit to 2. The 2 most frequent should be selected.
            using var image = new Image<Rgba32>(10, 1);
            // 4 red pixels (most frequent)
            for (var x = 0; x < 4; x++)
            {
                image[x, 0] = new Rgba32(255, 0, 0, 255);
            }

            // 3 green pixels (second most)
            for (var x = 4; x < 7; x++)
            {
                image[x, 0] = new Rgba32(0, 255, 0, 255);
            }

            // 1 each of blue, cyan, yellow
            image[7, 0] = new Rgba32(0, 0, 255, 255);
            image[8, 0] = new Rgba32(0, 255, 255, 255);
            image[9, 0] = new Rgba32(255, 255, 0, 255);

            var (palette, _) = SixelEncoder.BuildPaletteAndPixels(image, 2);

            palette.Length.Should().Be(2);
            // Both selected colors should be red or green
            var colors = palette.Select(p => (p.R, p.G, p.B)).ToList();
            colors.Should().Contain((255, 0, 0));
            colors.Should().Contain((0, 255, 0));
        }

        [Fact]
        public void Transparent_Pixel_Does_Not_Set_Bits_In_Sixel()
        {
            // 1x6, rows 0-4 are color 0, row 5 is transparent
            var palette = new[] { (Index: 0, R: (byte)200, G: (byte)200, B: (byte)200) };
            var pixels = new int[1, 6];
            for (var y = 0; y < 5; y++)
            {
                pixels[0, y] = 0;
            }

            pixels[0, 5] = -1; // transparent

            var result = SixelEncoder.EncodeCore(1, 6, palette, pixels);

            // bits = 0b011111 = 31, char = 31+63 = 94 = '^'
            result.Should().Contain("^");
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
