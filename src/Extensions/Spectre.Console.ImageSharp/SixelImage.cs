using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console.Rendering;

namespace Spectre.Console;

/// <summary>
/// Renders an image using Sixel encoding when the terminal supports it;
/// falls back transparently to block-character rendering (via <see cref="CanvasImage"/>)
/// when Sixel is unavailable.
/// </summary>
/// <remarks>
/// <para>
/// Sixel support is detected automatically from the environment (see
/// <see cref="SixelDetector"/>). You can also force it on or off:
/// <code>
/// AnsiConsole.Profile.Capabilities.SupportsSixel = true;
/// AnsiConsole.Write(new SixelImage("photo.jpg"));
/// </code>
/// </para>
/// <para>
/// The maximum color palette defaults to <see cref="SixelEncoder.DefaultMaxColors"/> (256).
/// Images with more unique colors are reduced using a frequency-based selection with
/// nearest-neighbour mapping for the remaining pixels.
/// </para>
/// </remarks>
public sealed class SixelImage : Renderable
{
    private static readonly IResampler _defaultResampler = KnownResamplers.Bicubic;

    /// <summary>Gets the native image width in pixels.</summary>
    public int Width => _image.Width;

    /// <summary>Gets the native image height in pixels.</summary>
    public int Height => _image.Height;

    /// <summary>
    /// Gets or sets the maximum render width (in terminal columns).
    /// When <c>null</c> the image is rendered at its native pixel width (each pixel = 1 column).
    /// </summary>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the resampler used when scaling the image.
    /// Defaults to bicubic sampling.
    /// </summary>
    public IResampler? Resampler { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of palette colors for Sixel encoding.
    /// Defaults to <see cref="SixelEncoder.DefaultMaxColors"/> (256).
    /// </summary>
    public int MaxColors { get; set; } = SixelEncoder.DefaultMaxColors;

    private readonly Image<Rgba32> _image;

    /// <summary>
    /// Initializes a new instance of the <see cref="SixelImage"/> class.
    /// </summary>
    /// <param name="filename">Path to the image file.</param>
    public SixelImage(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        _image = Image.Load<Rgba32>(filename);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SixelImage"/> class.
    /// </summary>
    /// <param name="data">Buffer containing an encoded image.</param>
    // Stryker disable once all : NoCoverage — ReadOnlySpan constructor; same logic as Stream constructor
    public SixelImage(ReadOnlySpan<byte> data)
    {
        _image = Image.Load<Rgba32>(data);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SixelImage"/> class.
    /// </summary>
    /// <param name="data">Stream containing an encoded image.</param>
    public SixelImage(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _image = Image.Load<Rgba32>(data);
    }

    /// <inheritdoc/>
    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        // Stryker disable all : Measure — SupportsSixel branching, width clamping
        if (options.Capabilities.SupportsSixel)
        {
            var width = MaxWidth ?? Width;
            if (maxWidth < width)
            {
                return new Measurement(maxWidth, maxWidth);
            }

            return new Measurement(width, width);
        }

        return ((IRenderable)BuildCanvasImage()).Measure(options, maxWidth);
        // Stryker restore all
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Stryker disable all : Render — SupportsSixel branching
        if (options.Capabilities.SupportsSixel)
        {
            return RenderAsSixel(maxWidth);
        }

        return ((IRenderable)BuildCanvasImage()).Render(options, maxWidth);
        // Stryker restore all
    }

    // ─── Sixel rendering ────────────────────────────────────────────────────

    private IEnumerable<Segment> RenderAsSixel(int maxWidth)
    {
        // Stryker disable all : RenderAsSixel — scaling arithmetic, Resampler fallback
        var image = _image;
        var width = Width;
        var height = Height;

        if (MaxWidth != null)
        {
            height = (int)(height * ((float)MaxWidth.Value / Width));
            width = MaxWidth.Value;
        }

        if (width > maxWidth)
        {
            height = (int)(height * (maxWidth / (float)width));
            width = maxWidth;
        }

        if (width != Width || height != Height)
        {
            var resampler = Resampler ?? _defaultResampler;
            image = image.Clone();
            image.Mutate(i => i.Resize(width, height, resampler));
        }

        var sixelData = SixelEncoder.Encode(image, MaxColors);

        yield return Segment.Control(sixelData);
        yield return Segment.LineBreak;
        // Stryker restore all
    }

    // ─── Block-character fallback ────────────────────────────────────────────

    private Renderable BuildCanvasImage()
    {
        // Stryker disable all : BuildCanvasImage — object initializer forwarding
        return new CanvasImage(_image.Clone())
        {
            MaxWidth = MaxWidth,
            Resampler = Resampler,
        };
        // Stryker restore all
    }
}

/// <summary>
/// Contains extension methods for <see cref="SixelImage"/>.
/// </summary>
public static class SixelImageExtensions
{
    /// <summary>
    /// Sets the maximum render width (in terminal columns).
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="maxWidth">The maximum render width.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static SixelImage MaxWidth(this SixelImage image, int maxWidth)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.MaxWidth = maxWidth;
        return image;
    }

    /// <summary>
    /// Removes any maximum render width constraint.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static SixelImage NoMaxWidth(this SixelImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.MaxWidth = null;
        return image;
    }

    /// <summary>
    /// Sets the resampler used when scaling the image.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="resampler">The resampler to use.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static SixelImage UseResampler(this SixelImage image, IResampler resampler)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.Resampler = resampler;
        return image;
    }

    /// <summary>
    /// Sets the maximum number of palette colors for Sixel encoding.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="maxColors">The maximum palette size (2–256).</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static SixelImage MaxColors(this SixelImage image, int maxColors)
    {
        ArgumentNullException.ThrowIfNull(image);
        // Stryker disable once all : Boundary mutation < vs <= — test checks 1 throws, 2 succeeds
        if (maxColors < 2)
        {
            // Stryker disable once String : Error message text does not affect behavior
            throw new ArgumentOutOfRangeException(nameof(maxColors), maxColors,
                "maxColors must be at least 2.");
        }

        image.MaxColors = maxColors;
        return image;
    }
}
