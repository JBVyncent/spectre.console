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
        if (options.Capabilities.SupportsSixel)
        {
            // In Sixel mode each pixel column = 1 terminal column.
            var width = MaxWidth ?? Width;
            if (maxWidth < width)
            {
                return new Measurement(maxWidth, maxWidth);
            }

            return new Measurement(width, width);
        }

        // Fall back to CanvasImage measurement.
        return ((IRenderable)BuildCanvasImage()).Measure(options, maxWidth);
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (options.Capabilities.SupportsSixel)
        {
            return RenderAsSixel(maxWidth);
        }

        return ((IRenderable)BuildCanvasImage()).Render(options, maxWidth);
    }

    // ─── Sixel rendering ────────────────────────────────────────────────────

    private IEnumerable<Segment> RenderAsSixel(int maxWidth)
    {
        var image = _image;
        var width = Width;
        var height = Height;

        // Apply MaxWidth constraint.
        if (MaxWidth != null)
        {
            height = (int)(height * ((float)MaxWidth.Value / Width));
            width = MaxWidth.Value;
        }

        // Apply terminal-width constraint.
        if (width > maxWidth)
        {
            height = (int)(height * (maxWidth / (float)width));
            width = maxWidth;
        }

        // Scale if needed.
        if (width != Width || height != Height)
        {
            var resampler = Resampler ?? _defaultResampler;
            image = image.Clone();
            image.Mutate(i => i.Resize(width, height, resampler));
        }

        var sixelData = SixelEncoder.Encode(image, MaxColors);

        // Control segment — not counted toward layout width.
        yield return Segment.Control(sixelData);

        // Advance cursor past the image height. Each Sixel band = 6 rows, and
        // the terminal renders them without occupying extra text rows — the DCS
        // sequence moves the cursor to the bottom of the image automatically on
        // most terminals. We emit a single newline to ensure subsequent output
        // starts on a fresh line.
        yield return Segment.LineBreak;
    }

    // ─── Block-character fallback ────────────────────────────────────────────

    private Renderable BuildCanvasImage()
    {
        return new CanvasImage(_image.Clone())
        {
            MaxWidth = MaxWidth,
            Resampler = Resampler,
        };
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
        if (maxColors < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maxColors), maxColors,
                "maxColors must be at least 2.");
        }

        image.MaxColors = maxColors;
        return image;
    }
}
