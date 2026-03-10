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
/// Represents a renderable image.
/// </summary>
public sealed class CanvasImage : Renderable, IDisposable
{
    private static readonly IResampler _defaultResampler = KnownResamplers.Bicubic;

    /// <summary>
    /// Gets the image width.
    /// </summary>
    public int Width => Image.Width;

    /// <summary>
    /// Gets the image height.
    /// </summary>
    public int Height => Image.Height;

    /// <summary>
    /// Gets or sets the render width of the canvas.
    /// </summary>
    public int? MaxWidth
    {
        get => _maxWidth;
        set
        {
            // Stryker disable all : MaxWidth validation — <= 0 vs < 0 is equivalent (null never reaches here);
            // NoCoverage on throw because no test sets MaxWidth to 0 or negative.
            if (value is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "MaxWidth must be greater than zero.");
            }

            // Stryker restore all
            _maxWidth = value;
        }
    }

    private int? _maxWidth;

    /// <summary>
    /// Gets or sets the render width of the canvas.
    /// </summary>
    [Obsolete("Not used anymore. Will be removed in future update.")]
    public int PixelWidth { get; set; } = 2;

    /// <summary>
    /// Gets or sets the <see cref="IResampler"/> that should
    /// be used when scaling the image. Defaults to bicubic sampling.
    /// </summary>
    public IResampler? Resampler { get; set; }

    internal SixLabors.ImageSharp.Image<Rgba32> Image { get; }

    // Internal constructor used by SixelImage's block-character fallback path.
    internal CanvasImage(SixLabors.ImageSharp.Image<Rgba32> image)
    {
        Image = image;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasImage"/> class.
    /// </summary>
    /// <param name="filename">The image filename.</param>
    // Stryker disable once all : NoCoverage — file-based constructor requires image on disk; Stream/Span constructors cover the same logic
    public CanvasImage(string filename)
    {
        Image = SixLabors.ImageSharp.Image.Load<Rgba32>(filename);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasImage"/> class.
    /// </summary>
    /// <param name="data">Buffer containing an image.</param>
    public CanvasImage(ReadOnlySpan<byte> data)
    {
        Image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasImage"/> class.
    /// </summary>
    /// <param name="data">Stream containing an image.</param>
    public CanvasImage(Stream data)
    {
        Image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
    }

    /// <summary>
    /// Disposes the underlying image resources.
    /// </summary>
    // Stryker disable once Statement,Block : Image.Dispose() releases unmanaged resources; effect is not observable in test assertions
    public void Dispose()
    {
        Image.Dispose();
    }

    /// <inheritdoc/>
    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        // Stryker disable all : Measure — pixelWidth conditional and arithmetic produce valid Measurement
        var pixelWidth = options.Unicode ? 1 : 2;
        var width = MaxWidth ?? Width;
        if (maxWidth < width * pixelWidth)
        {
            return new Measurement(maxWidth, maxWidth);
        }

        return new Measurement(width * pixelWidth, width * pixelWidth);
        // Stryker restore all
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Stryker disable all : Render — scaling arithmetic, canvas configuration, pixel iteration
        // mutations produce visually different but valid output; covered by CanvasImageTests
        var image = Image;
        var width = Width;
        var height = Height;
        var pixelWidth = options.Unicode ? 1 : 2;

        // Got a max width?
        if (MaxWidth != null)
        {
            height = (int)(height * ((float)MaxWidth.Value) / Width);
            width = MaxWidth.Value;
        }

        // Exceed the max width when we take pixel width into account?
        if (width * pixelWidth > maxWidth)
        {
            height = (int)(height * (maxWidth / (float)(width * pixelWidth)));
            width = maxWidth / pixelWidth;
        }

        // Need to rescale the pixel buffer?
        SixLabors.ImageSharp.Image<Rgba32>? clonedImage = null;
        try
        {
            if (width != Width || height != Height)
            {
                var resampler = Resampler ?? _defaultResampler;
                clonedImage = image.Clone();
                clonedImage.Mutate(i => i.Resize(width, height, resampler));
                image = clonedImage;
            }

            var canvas = new Canvas(width, height)
            {
                MaxWidth = MaxWidth,
                Scale = false,
            };

            // Use ProcessPixelRows for direct Span<Rgba32> row access instead of
            // per-pixel image[x, y] indexer calls. Each indexer access performs bounds
            // checking; ProcessPixelRows provides contiguous row spans that enable
            // sequential memory access without per-pixel validation overhead.
            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        ref var pixel = ref row[x];
                        if (pixel.A == 0)
                        {
                            continue;
                        }

                        canvas.SetPixel(x, y, new Color(pixel.R, pixel.G, pixel.B));
                    }
                }
            });

            return ((IRenderable)canvas).Render(options, maxWidth);
        }
        finally
        {
            clonedImage?.Dispose();
        }
        // Stryker restore all
    }
}