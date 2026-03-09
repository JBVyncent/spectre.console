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
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.And(ex => ex.ParamName.Should().Be("filename"));
        }

        [Fact]
        public void Should_Throw_If_Stream_Is_Null()
        {
            var result = Record.Exception(() => new SixelImage((Stream)null!));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.And(ex => ex.ParamName.Should().Be("data"));
        }

        [Fact]
        public void Can_Construct_From_Stream()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var result = Record.Exception(() => new SixelImage(stream));
            result.Should().BeNull();
        }

        [Fact]
        public void Width_And_Height_Reflect_Image_Dimensions()
        {
            using var stream = CreatePngStream(8, 12, ImageColor.Blue);
            var img = new SixelImage(stream);
            img.Width.Should().Be(8);
            img.Height.Should().Be(12);
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
            segments.Any(s => s.IsControlCode).Should().BeTrue();
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

            control.Text.Should().StartWith("\x1bP");
            control.Text.Should().EndWith("\x1b\\");
        }

        [Fact]
        public void Render_Includes_Line_Break_After_Image()
        {
            var console = new TestConsole();
            console.Profile.Capabilities.SupportsSixel = true;

            using var stream = CreatePngStream(2, 2, ImageColor.Blue);
            var img = new SixelImage(stream);

            var segments = img.GetSegments(console).ToList();

            segments.Any(s => s.IsLineBreak).Should().BeTrue();
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

            measurement.Min.Should().Be(20);
            measurement.Max.Should().Be(20);
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

            measurement.Min.Should().Be(10);
            measurement.Max.Should().Be(10);
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

            measurement.Min.Should().BeLessThanOrEqualTo(10);
            measurement.Max.Should().BeLessThanOrEqualTo(10);
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
            segments.Any(s => s.IsControlCode && s.Text.Contains("\x1bP")).Should().BeFalse();
        }

        [Fact]
        public void Render_Produces_Non_Empty_Segments()
        {
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Capabilities.SupportsSixel = false;

            using var stream = CreatePngStream(4, 4, ImageColor.Blue);
            var img = new SixelImage(stream);

            var segments = img.GetSegments(console).ToList();

            segments.Should().NotBeEmpty();
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
            img.MaxWidth.Should().Be(5);
        }

        [Fact]
        public void MaxWidth_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream);
            img.MaxWidth(5).Should().BeSameAs(img);
        }

        [Fact]
        public void MaxWidth_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() => SixelImageExtensions.MaxWidth(null!, 10));
            result.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void NoMaxWidth_Clears_The_Property()
        {
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream);
            img.MaxWidth = 5;

            img.NoMaxWidth();

            img.MaxWidth.Should().BeNull();
        }

        [Fact]
        public void NoMaxWidth_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new SixelImage(stream);
            img.NoMaxWidth().Should().BeSameAs(img);
        }

        [Fact]
        public void NoMaxWidth_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() => SixelImageExtensions.NoMaxWidth(null!));
            result.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void MaxColors_Sets_The_Property()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream).MaxColors(16);
            img.MaxColors.Should().Be(16);
        }

        [Fact]
        public void MaxColors_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);
            img.MaxColors(16).Should().BeSameAs(img);
        }

        [Fact]
        public void MaxColors_Throws_If_Less_Than_Two()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);

            var result = Record.Exception(() => img.MaxColors(1));
            result.Should().BeOfType<ArgumentOutOfRangeException>()
                    .Which.And(ex => ex.ParamName.Should().Be("maxColors"));
        }

        [Fact]
        public void MaxColors_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() => SixelImageExtensions.MaxColors(null!, 16));
            result.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void UseResampler_Sets_The_Resampler()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);

            img.UseResampler(SixLabors.ImageSharp.Processing.KnownResamplers.Lanczos3);

            img.Resampler.Should().NotBeNull();
        }

        [Fact]
        public void UseResampler_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new SixelImage(stream);
            img.UseResampler(SixLabors.ImageSharp.Processing.KnownResamplers.Lanczos3).Should().BeSameAs(img);
        }

        [Fact]
        public void UseResampler_Throws_If_Image_Is_Null()
        {
            var result = Record.Exception(() =>
                SixelImageExtensions.UseResampler(null!, SixLabors.ImageSharp.Processing.KnownResamplers.Bicubic));
            result.Should().BeOfType<ArgumentNullException>();
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
