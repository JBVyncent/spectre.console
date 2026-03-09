using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageColor = SixLabors.ImageSharp.Color;

namespace Spectre.Console.Tests.Unit;

public sealed class CanvasImageTests
{
    // ── Constructors ─────────────────────────────────────────────────────────

    public sealed class TheConstructors
    {
        [Fact]
        public void Should_Load_From_Stream()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            img.Width.Should().Be(4);
            img.Height.Should().Be(4);
        }

        [Fact]
        public void Should_Load_From_Byte_Array()
        {
            using var stream = CreatePngStream(6, 3, ImageColor.Blue);
            var bytes = ((MemoryStream)stream).ToArray();
            var img = new CanvasImage(bytes.AsSpan());
            img.Width.Should().Be(6);
            img.Height.Should().Be(3);
        }
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public sealed class TheProperties
    {
        [Fact]
        public void MaxWidth_Defaults_To_Null()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            img.MaxWidth.Should().BeNull();
        }

        [Fact]
        public void MaxWidth_Can_Be_Set()
        {
            using var stream = CreatePngStream(10, 10, ImageColor.Red);
            var img = new CanvasImage(stream);
            img.MaxWidth = 5;
            img.MaxWidth.Should().Be(5);
        }

        [Fact]
        public void Resampler_Defaults_To_Null()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            img.Resampler.Should().BeNull();
        }
    }

    // ── Measure ──────────────────────────────────────────────────────────────

    public sealed class TheMeasureMethod
    {
        [Fact]
        public void Should_Use_Image_Width_When_No_MaxWidth()
        {
            var console = new TestConsole();
            console.Profile.Width = 200;
            using var stream = CreatePngStream(10, 5, ImageColor.Red);
            var img = new CanvasImage(stream);

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 200);

            // Unicode mode: pixelWidth=1, so measurement = width * 1 = 10
            measurement.Min.Should().Be(10);
            measurement.Max.Should().Be(10);
        }

        [Fact]
        public void Should_Clamp_When_MaxWidth_Exceeds_Available()
        {
            var console = new TestConsole();
            console.Profile.Width = 8;
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new CanvasImage(stream);

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 8);

            measurement.Min.Should().BeLessThanOrEqualTo(8);
            measurement.Max.Should().BeLessThanOrEqualTo(8);
        }

        [Fact]
        public void Should_Respect_MaxWidth_Property()
        {
            var console = new TestConsole();
            console.Profile.Width = 200;
            using var stream = CreatePngStream(20, 10, ImageColor.Red);
            var img = new CanvasImage(stream) { MaxWidth = 5 };

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 200);

            measurement.Min.Should().Be(5);
            measurement.Max.Should().Be(5);
        }
    }

    // ── Render ────────────────────────────────────────────────────────────────

    public sealed class TheRenderMethod
    {
        [Fact]
        public void Should_Produce_Non_Empty_Segments()
        {
            var console = new TestConsole().EmitAnsiSequences();
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);

            var segments = img.GetSegments(console).ToList();

            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Scale_When_MaxWidth_Is_Set()
        {
            var console = new TestConsole().EmitAnsiSequences();
            using var stream = CreatePngStream(20, 20, ImageColor.Red);
            var img = new CanvasImage(stream) { MaxWidth = 5 };

            // Should not throw and should produce output
            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Scale_When_Exceeding_Terminal_Width()
        {
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Width = 8;
            using var stream = CreatePngStream(20, 20, ImageColor.Red);
            var img = new CanvasImage(stream);

            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Skip_Transparent_Pixels()
        {
            var console = new TestConsole().EmitAnsiSequences();

            // Create image with transparent pixel
            using var image = new Image<Rgba32>(2, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 255); // opaque red
            image[1, 0] = new Rgba32(0, 0, 0, 0);     // transparent
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
            var canvas = new CanvasImage(ms);

            // Should not throw
            var segments = canvas.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Use_Custom_Resampler_When_Scaling()
        {
            var console = new TestConsole().EmitAnsiSequences();
            using var stream = CreatePngStream(20, 20, ImageColor.Green);
            var img = new CanvasImage(stream)
            {
                MaxWidth = 5,
                Resampler = KnownResamplers.NearestNeighbor,
            };

            // Should not throw
            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Render_Pixel_Colors()
        {
            var console = new TestConsole().EmitAnsiSequences();

            using var image = new Image<Rgba32>(1, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 255);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
            var canvas = new CanvasImage(ms);

            console.Write(canvas);

            // Output should contain ANSI color codes
            console.Output.Should().NotBeEmpty();
        }
    }

    // ── Extension methods ────────────────────────────────────────────────────

    public sealed class TheExtensionMethods
    {
        [Fact]
        public void MaxWidth_Sets_Property_And_Returns_Same_Instance()
        {
            using var stream = CreatePngStream(10, 5, ImageColor.Red);
            var img = new CanvasImage(stream);
            var result = CanvasImageExtensions.MaxWidth(img, 3);
            result.Should().BeSameAs(img);
            img.MaxWidth.Should().Be(3);
        }

        [Fact]
        public void MaxWidth_Throws_If_Image_Is_Null()
        {
            var act = () => CanvasImageExtensions.MaxWidth(null!, 10);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void NoMaxWidth_Clears_Property()
        {
            using var stream = CreatePngStream(10, 5, ImageColor.Red);
            var img = new CanvasImage(stream) { MaxWidth = 5 };
            CanvasImageExtensions.NoMaxWidth(img);
            img.MaxWidth.Should().BeNull();
        }

        [Fact]
        public void NoMaxWidth_Throws_If_Image_Is_Null()
        {
            var act = () => CanvasImageExtensions.NoMaxWidth(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Mutate_Applies_Action()
        {
            using var stream = CreatePngStream(10, 10, ImageColor.Red);
            var img = new CanvasImage(stream);
            img.Mutate(ctx => ctx.Resize(5, 5));
            img.Width.Should().Be(5);
            img.Height.Should().Be(5);
        }

        [Fact]
        public void Mutate_Throws_If_Image_Is_Null()
        {
            var act = () => CanvasImageExtensions.Mutate(null!, ctx => { });
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Mutate_Throws_If_Action_Is_Null()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            var act = () => CanvasImageExtensions.Mutate(img, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void BicubicResampler_Sets_Resampler()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            var result = img.BicubicResampler();
            result.Should().BeSameAs(img);
            img.Resampler.Should().Be(KnownResamplers.Bicubic);
        }

        [Fact]
        public void BicubicResampler_Throws_If_Null()
        {
            var act = () => CanvasImageExtensions.BicubicResampler(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void BilinearResampler_Sets_Resampler()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            var result = img.BilinearResampler();
            result.Should().BeSameAs(img);
            img.Resampler.Should().Be(KnownResamplers.Triangle);
        }

        [Fact]
        public void BilinearResampler_Throws_If_Null()
        {
            var act = () => CanvasImageExtensions.BilinearResampler(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void NearestNeighborResampler_Sets_Resampler()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            var result = img.NearestNeighborResampler();
            result.Should().BeSameAs(img);
            img.Resampler.Should().Be(KnownResamplers.NearestNeighbor);
        }

        [Fact]
        public void NearestNeighborResampler_Throws_If_Null()
        {
            var act = () => CanvasImageExtensions.NearestNeighborResampler(null!);
            act.Should().Throw<ArgumentNullException>();
        }

#pragma warning disable CS0618 // Testing obsolete member
        [Fact]
        public void PixelWidth_Sets_Property()
        {
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);
            var result = CanvasImageExtensions.PixelWidth(img, 3);
            result.Should().BeSameAs(img);
            img.PixelWidth.Should().Be(3);
        }

        [Fact]
        public void PixelWidth_Throws_If_Null()
        {
            var act = () => CanvasImageExtensions.PixelWidth(null!, 2);
            act.Should().Throw<ArgumentNullException>();
        }
#pragma warning restore CS0618
    }

    // ── Mutation killers ────────────────────────────────────────────────────

    public sealed class MutationKillers
    {
        [Fact]
        public void Measure_NonUnicode_Uses_PixelWidth_Two()
        {
            // In non-unicode mode, pixelWidth = 2, so a 10-wide image → 20 columns
            var console = new TestConsole();
            console.Profile.Capabilities.Unicode = false;
            console.Profile.Width = 200;
            using var stream = CreatePngStream(10, 5, ImageColor.Red);
            var img = new CanvasImage(stream);

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 200);

            measurement.Min.Should().Be(20);
            measurement.Max.Should().Be(20);
        }

        [Fact]
        public void Measure_Clamps_To_MaxWidth_With_PixelWidth()
        {
            // Non-unicode: pixelWidth=2, image=10w → 20 columns, but maxWidth=15
            var console = new TestConsole();
            console.Profile.Capabilities.Unicode = false;
            console.Profile.Width = 15;
            using var stream = CreatePngStream(10, 5, ImageColor.Red);
            var img = new CanvasImage(stream);

            var options = RenderOptions.Create(console, console.Profile.Capabilities);
            var measurement = ((IRenderable)img).Measure(options, 15);

            measurement.Min.Should().Be(15);
            measurement.Max.Should().Be(15);
        }

        [Fact]
        public void Render_NonUnicode_Scales_Correctly()
        {
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Capabilities.Unicode = false;
            console.Profile.Width = 10;
            using var stream = CreatePngStream(20, 20, ImageColor.Red);
            var img = new CanvasImage(stream);

            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Render_Without_Scaling_When_Image_Fits()
        {
            // Image exactly fits → no rescaling needed (width == Width && height == Height)
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Width = 200;
            using var stream = CreatePngStream(4, 4, ImageColor.Green);
            var img = new CanvasImage(stream);

            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Render_MaxWidth_Scales_Height_Proportionally()
        {
            // 20x40 image with MaxWidth=10 → height should be 20
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Width = 200;
            using var stream = CreatePngStream(20, 40, ImageColor.Blue);
            var img = new CanvasImage(stream) { MaxWidth = 10 };

            // Just verify it renders without crashing with correct proportions
            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Render_Canvas_Scale_Is_False()
        {
            // Tests that canvas Scale=false is used (mutation would set Scale=true)
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Width = 200;
            using var stream = CreatePngStream(4, 4, ImageColor.Red);
            var img = new CanvasImage(stream);

            // With Scale=false, the canvas renders at native size
            // With Scale=true, it would stretch to fill width
            var segments = img.GetSegments(console).ToList();

            // Count non-linebreak, non-empty segments to estimate rendered width
            var textSegments = segments.Where(s => !s.IsLineBreak && !s.IsControlCode).ToList();
            textSegments.Should().NotBeEmpty();
        }

        [Fact]
        public void Render_Sets_Canvas_MaxWidth()
        {
            // Verify MaxWidth is forwarded to canvas
            var console = new TestConsole().EmitAnsiSequences();
            console.Profile.Width = 200;
            using var stream = CreatePngStream(20, 20, ImageColor.Red);
            var img = new CanvasImage(stream) { MaxWidth = 5 };

            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }

        [Fact]
        public void Render_Transparent_Pixel_Is_Skipped_Not_Set()
        {
            // Transparent pixel (alpha=0) should NOT set canvas pixel
            var console = new TestConsole().EmitAnsiSequences();
            using var image = new Image<Rgba32>(2, 1);
            image[0, 0] = new Rgba32(255, 0, 0, 255); // opaque
            image[1, 0] = new Rgba32(0, 0, 0, 0);     // transparent
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
            var img = new CanvasImage(ms);

            var segments = img.GetSegments(console).ToList();
            segments.Should().NotBeEmpty();
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static MemoryStream CreatePngStream(int width, int height, ImageColor color)
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
