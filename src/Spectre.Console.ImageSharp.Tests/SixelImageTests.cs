using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageColor = SixLabors.ImageSharp.Color;

namespace Spectre.Console.Tests.Unit;

public sealed class SixelImageTests
{
    // ── Constructor guards ───────────────────────────────────────────────────

    public sealed class TheConstructor
    {
        [Fact]
        public void Should_Throw_If_Filename_Is_Null()
        {
            var result = Record.Exception(() => new SixelImage((string)null!));
            result.ShouldBeOfType<ArgumentNullException>()
                .And(ex => ex.ParamName.ShouldBe("filename"));
        }

        [Fact]
        public void Should_Throw_If_Stream_Is_Null()
        {
            var result = Record.Exception(() => new SixelImage((Stream)null!));
            result.ShouldBeOfType<ArgumentNullException>()
                .And(ex => ex.ParamName.ShouldBe("data"));
        }

        [Fact]
        public void Can_Construct_From_Stream()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var result = Record.Exception(() => new SixelImage(stream));
            result.ShouldBeNull();
        }

        [Fact]
        public void Width_And_Height_Reflect_Image_Dimensions()
        {
            using var stream = CreatePngStream(8, 12, ImageColor.Blue);
            var img = new SixelImage(stream);
            img.Width.ShouldBe(8);
            img.Height.ShouldBe(12);
        }
    }

    // ── Sixel rendering path ─────────────────────────────────────────────────

    public sealed class WhenSixelIsSupported
    {
        [Fact]
        public void Render_Emits_A_Control_Segment()
        {
            var console = new TestConsole();
            console.Profile.Capabilities.SupportsSixel = true;

            using var stream = CreatePngStream(4, 6, ImageColor.Green);
            var img = new SixelImage(stream);

            var segments = img.GetSegments(console).ToList();

            // The first segment must be a control-code segment (the DCS sequence).
            segments.Any(s => s.IsControlCode).ShouldBeTrue();
        }

        [Fact]
        public void Render_Control_Segment_Starts_With_DCS_Intro()
        {
            var console = new TestConsole();
            console.Profile.Capabilities.SupportsSixel = true;

            using var stream = CreatePngStream(2, 2, ImageColor.Red);
            var img = new SixelImage(stream);

            var segments = img.GetSegments(console).ToList();
            var control = segments.First(s => s.IsControlCode);

            control.Text.ShouldStartWith("\x1bP");
            control.Text.ShouldEndWith("\x1b\\");
        }

        [Fact]
        public void Render_Includes_Line_Break_After_Image()
        {
            var console = new TestConsole();
            console.Profile.Capabilities.SupportsSixel = true;

            using var stream = CreatePngStream(2, 2, ImageColor.Blue);
            var img = new SixelImage(stream);

            var segments = img.GetSegments(console).ToList();

            segments.Any(s => s.IsLineBreak).ShouldBeTrue();
        }

        [Fact]
        public void Measure_Returns_Native_Width_When_No_MaxWidth()
        {
            var console = new TestConsole();
            console.Profile.Capabilities.SupportsSixel = true;
            console.Profile.Width = 200;

            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream);

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 200);

            measurement.Min.ShouldBe(20);
            measurement.Max.ShouldBe(20);
        }

        [Fact]
        public void Measure_Respects_MaxWidth_Constraint()
        {
            var console = new TestConsole();
            console.Profile.Capabilities.SupportsSixel = true;
            console.Profile.Width = 200;

            using var stream = CreatePngStream(40, 20, ImageColor.Red);
            var img = new SixelImage(stream) { MaxWidth = 10 };

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 200);

            measurement.Min.ShouldBe(10);
            measurement.Max.ShouldBe(10);
        }

        [Fact]
        public void Measure_Clamps_To_Terminal_Width()
        {
            var console = new TestConsole();
            console.Profile.Capabilities.SupportsSixel = true;
            console.Profile.Width = 10;

            using var stream = CreatePngStream(80, 40, ImageColor.Red);
            var img = new SixelImage(stream);

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 10);

            measurement.Min.ShouldBeLessThanOrEqualTo(10);
            measurement.Max.ShouldBeLessThanOrEqualTo(10);
        }
    }

    // ── Fallback rendering path ──────────────────────────────────────────────

    public sealed class WhenSixelIsNotSupported
    {
        [Fact]
        public void Render_Does_Not_Emit_Control_Segment()
        {
            // Use a TestConsole with ANSI + Unicode for block character rendering.
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Capabilities.SupportsSixel = false;

            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);

            var segments = img.GetSegments(console).ToList();

            // No DCS control segment.
            segments.Any(s => s.IsControlCode && s.Text.Contains("\x1bP")).ShouldBeFalse();
        }

        [Fact]
        public void Render_Produces_Non_Empty_Segments()
        {
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Capabilities.SupportsSixel = false;

            using var stream = CreatePngStream(4, 4, ImageColor.Blue);
            var img = new SixelImage(stream);

            var segments = img.GetSegments(console).ToList();

            segments.ShouldNotBeEmpty();
        }
    }

    // ── Extension methods ────────────────────────────────────────────────────

    public sealed class TheExtensionMethods
    {
        [Fact]
        public void MaxWidth_Sets_The_Property()
        {
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream).MaxWidth(5);
            img.MaxWidth.ShouldBe(5);
        }

        [Fact]
        public void MaxWidth_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream);
            img.MaxWidth(5).ShouldBeSameAs(img);
        }

        [Fact]
        public void MaxWidth_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() => SixelImageExtensions.MaxWidth(null!, 10));
            result.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void NoMaxWidth_Clears_The_Property()
        {
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream);
            img.MaxWidth = 5;

            img.NoMaxWidth();

            img.MaxWidth.ShouldBeNull();
        }

        [Fact]
        public void NoMaxWidth_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream);
            img.NoMaxWidth().ShouldBeSameAs(img);
        }

        [Fact]
        public void NoMaxWidth_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() => SixelImageExtensions.NoMaxWidth(null!));
            result.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void MaxColors_Sets_The_Property()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream).MaxColors(16);
            img.MaxColors.ShouldBe(16);
        }

        [Fact]
        public void MaxColors_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);
            img.MaxColors(16).ShouldBeSameAs(img);
        }

        [Fact]
        public void MaxColors_Throws_If_Less_Than_Two()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);

            var result = Record.Exception(() => img.MaxColors(1));
            result.ShouldBeOfType<ArgumentOutOfRangeException>()
                .And(ex => ex.ParamName.ShouldBe("maxColors"));
        }

        [Fact]
        public void MaxColors_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() => SixelImageExtensions.MaxColors(null!, 16));
            result.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void UseResampler_Sets_The_Resampler()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);

            img.UseResampler(SixLabors.ImageSharp.Processing.KnownResamplers.Lanczos3);

            img.Resampler.ShouldNotBeNull();
        }

        [Fact]
        public void UseResampler_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);
            img.UseResampler(SixLabors.ImageSharp.Processing.KnownResamplers.Lanczos3).ShouldBeSameAs(img);
        }

        [Fact]
        public void UseResampler_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() =>
                SixelImageExtensions.UseResampler(null!, SixLabors.ImageSharp.Processing.KnownResamplers.Bicubic));
            result.ShouldBeOfType<ArgumentNullException>();
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates an in-memory PNG stream with a solid colour fill.</summary>
    private static Stream CreatePngStream(int width, int height, ImageColor color)
    {
        var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = color.ToPixel<Rgba32>();
            }
        }

        var ms = new MemoryStream();
        image.SaveAsPng(ms);
        ms.Position = 0;
        return ms;
    }
}
