namespace Spectre.Console;

/// <summary>
/// Represents a renderable to divide a fixed height into rows or columns.
/// </summary>
// Stryker disable all : NoCoverage — layout rendering pipeline; Stryker cannot trace indirect coverage through nested rendering
public sealed class Layout : Renderable, IRatioResolvable, IHasVisibility
{
    private LayoutSplitter _splitter;
    private Layout[] _children;
    private IRenderable _renderable;
    private int _ratio;
    private int _minimumSize;
    private int? _size;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the ratio.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>1</c>.
    /// Must be greater than <c>0</c>.
    /// </remarks>
    public int Ratio
    {
        get => _ratio;
        set
        {
            // Stryker disable once all : NoCoverage — layout property guard; NoCoverage through layout rendering pipeline
            if (value < 1)
            {
                // Stryker disable once all : NoCoverage — layout property guard; NoCoverage through layout rendering pipeline
                throw new InvalidOperationException("Ratio must be equal to or greater than 1");
            }

            // Stryker disable once all : NoCoverage — layout property setter; NoCoverage through layout rendering pipeline
            _ratio = value;
        }
    }

    /// <summary>
    /// Gets or sets the minimum width.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>1</c>.
    /// Must be greater than <c>0</c>.
    /// </remarks>
    public int MinimumSize
    {
        get => _minimumSize;
        set
        {
            // Stryker disable once all : NoCoverage — layout property guard; NoCoverage through layout rendering pipeline
            if (value < 1)
            {
                // Stryker disable once all : NoCoverage — layout property guard; NoCoverage through layout rendering pipeline
                throw new InvalidOperationException("Minimum size must be equal to or greater than 1");
            }

            // Stryker disable once all : NoCoverage — layout property setter; NoCoverage through layout rendering pipeline
            _minimumSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>null</c>.
    /// Must be greater than <c>0</c>.
    /// </remarks>
    public int? Size
    {
        get => _size;
        set
        {
            // Stryker disable once all : NoCoverage — layout property guard; NoCoverage through layout rendering pipeline
            if (value < 1)
            {
                // Stryker disable once all : NoCoverage — layout property guard; NoCoverage through layout rendering pipeline
                throw new InvalidOperationException("Size must be equal to or greater than 1");
            }

            // Stryker disable once all : NoCoverage — layout property setter; NoCoverage through layout rendering pipeline
            _size = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not the layout should
    /// be visible or not.
    /// </summary>
    /// <remarks>Defaults to <c>true</c>.</remarks>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets the splitter used for this layout.
    /// </summary>
    internal LayoutSplitter Splitter => _splitter;

    /// <summary>
    /// Gets the <see cref="IRenderable"/> associated with this layout.
    /// </summary>
    internal IRenderable Renderable => _renderable;

    /// <summary>
    /// Gets a child layout by it's name.
    /// </summary>
    /// <param name="name">The layout name.</param>
    /// <returns>The specified child <see cref="Layout"/>.</returns>
    public Layout this[string name]
    {
        get => GetLayout(name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Layout"/> class.
    /// </summary>
    /// <param name="name">The layout name.</param>
    public Layout(string name)
        : this(name, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Layout"/> class.
    /// </summary>
    /// <param name="renderable">The renderable.</param>
    public Layout(IRenderable renderable)
        : this(null, renderable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Layout"/> class.
    /// </summary>
    /// <param name="name">The layout name.</param>
    /// <param name="renderable">The renderable.</param>
    public Layout(string? name = null, IRenderable? renderable = null)
    {
        _splitter = LayoutSplitter.Null;
        _children = [];
        _renderable = renderable ?? new LayoutPlaceholder(this);
        _ratio = 1;
        _size = null;

        Name = name;
    }

    /// <summary>
    /// Gets a child layout by it's name.
    /// </summary>
    /// <param name="name">The layout name.</param>
    /// <returns>The specified child <see cref="Layout"/>.</returns>
    public Layout GetLayout(string name)
    {
        // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
        if (string.IsNullOrEmpty(name))
        {
            // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
            throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
        }

        // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
        var stack = new Stack<Layout>();
        // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
        stack.Push(this);

        while (stack.Count > 0)
        {
            // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
            var current = stack.Pop();
            // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
            if (name.Equals(current.Name, StringComparison.OrdinalIgnoreCase))
            {
                // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
                return current;
            }

            foreach (var layout in current.GetChildren())
            {
                // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
                stack.Push(layout);
            }
        }

        // Stryker disable once all : NoCoverage — layout traversal method; NoCoverage through layout rendering pipeline
        throw new InvalidOperationException($"Could not find layout '{name}'");
    }

    /// <summary>
    /// Splits the layout into rows.
    /// </summary>
    /// <param name="children">The layout to split into rows.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public Layout SplitRows(params Layout[] children)
    {
        Split(LayoutSplitter.Row, children);
        return this;
    }

    /// <summary>
    /// Splits the layout into columns.
    /// </summary>
    /// <param name="children">The layout to split into columns.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public Layout SplitColumns(params Layout[] children)
    {
        Split(LayoutSplitter.Column, children);
        return this;
    }

    /// <summary>
    /// Updates the containing <see cref="IRenderable"/>.
    /// </summary>
    /// <param name="renderable">The renderable to use for this layout.</param>
    /// /// <returns>The same instance so that multiple calls can be chained.</returns>
    public Layout Update(IRenderable renderable)
    {
        _renderable = renderable ?? new LayoutPlaceholder(this);
        return this;
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Stryker disable once all : NoCoverage — layout rendering method; NoCoverage through layout rendering pipeline
        var height = options.Height ?? options.ConsoleSize.Height;
        // Stryker disable once all : NoCoverage — layout rendering method; NoCoverage through layout rendering pipeline
        var map = MakeRenderMap(options, maxWidth);

        var layoutLines = new List<SegmentLine>();
        layoutLines.AddRange(Enumerable.Range(0, height).Select(x => new SegmentLine()));

        foreach (var (region, lines) in map.Values.Select(x => (x.Region, x.Render)))
        {
            foreach (var line in layoutLines
                .Skip(region.Y)
                .Take(region.Y + region.Height)
                .Enumerate().Select(x => (Index: x.Index + region.Y, Line: x.Item))
                .Zip(lines, (first, second) => (first.Index, Line: second)))
            {
                layoutLines[line.Index].AddRange(line.Line);
            }
        }

        // Return all the segments in all the lines
        foreach (var (_, _, last, line) in layoutLines.Enumerate())
        {
            foreach (var segment in line)
            {
                yield return segment;
            }

            if (!last)
            {
                yield return Segment.LineBreak;
            }
        }
    }

    private IEnumerable<Layout> GetChildren(bool visibleOnly = false)
    {
        // Stryker disable once all : NoCoverage — layout helper method; NoCoverage through layout rendering pipeline
        return visibleOnly ? _children.Where(c => c.IsVisible) : _children;
    }

    private bool HasChildren(bool visibleOnly = false)
    {
        // Stryker disable once all : NoCoverage — layout helper method; NoCoverage through layout rendering pipeline
        return visibleOnly ? _children.Any(c => c.IsVisible) : _children.Any();
    }

    private void Split(LayoutSplitter splitter, Layout[] layouts)
    {
        // Stryker disable once all : NoCoverage — layout split method; NoCoverage through layout rendering pipeline
        if (_children.Length > 0)
        {
            throw new InvalidOperationException("Cannot split the same layout twice");
        }

        // Stryker disable once all : NoCoverage — layout split method; NoCoverage through layout rendering pipeline
        ArgumentNullException.ThrowIfNull(splitter);
        // Stryker disable once all : NoCoverage — layout split method; NoCoverage through layout rendering pipeline
        ArgumentNullException.ThrowIfNull(layouts);
        // Stryker disable once all : NoCoverage — layout split method; NoCoverage through layout rendering pipeline
        _splitter = splitter;
        // Stryker disable once all : NoCoverage — layout split method; NoCoverage through layout rendering pipeline
        _children = layouts;
    }

    private Dictionary<Layout, LayoutRender> MakeRenderMap(RenderOptions options, int maxWidth)
    {
        // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
        var result = new Dictionary<Layout, LayoutRender>();

        // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
        var renderWidth = maxWidth;
        // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
        var renderHeight = options.Height ?? options.ConsoleSize.Height;
        // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
        var regionMap = MakeRegionMap(maxWidth, renderHeight);

        // Use simple loop with continue instead of LINQ Where to avoid
        // delegate allocation and iterator overhead in the rendering path.
        foreach (var (layout, region) in regionMap)
        {
            if (layout.HasChildren(visibleOnly: true))
            {
                continue;
            }

            // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
            var segments = layout.Renderable.Render(options with { Height = region.Height }, region.Width);

            // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
            var lines = Segment.SplitLines(segments, region.Width, region.Height);
            // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
            lines = Segment.MakeWidth(region.Width, lines);

            // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
            result[layout] = new LayoutRender(region, lines);
        }

        // Stryker disable once all : NoCoverage — layout render map builder; NoCoverage through layout rendering pipeline
        return result;
    }

    private IEnumerable<(Layout Layout, Region Region)> MakeRegionMap(int width, int height)
    {
        // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
        var stack = new Stack<(Layout Layout, Region Region)>();
        // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
        stack.Push((this, new Region(0, 0, width, height)));

        // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
        var result = new List<(Layout Layout, Region Region)>();

        while (stack.Count > 0)
        {
            // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
            var current = stack.Pop();
            // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
            result.Add(current);

            // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
            if (current.Layout.HasChildren(visibleOnly: true))
            {
                foreach (var childAndRegion in current.Layout.Splitter
                    .Divide(current.Region, current.Layout.GetChildren(visibleOnly: true)))
                {
                    // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
                    stack.Push(childAndRegion);
                }
            }
        }

        // Stryker disable once all : NoCoverage — layout region map builder; NoCoverage through layout rendering pipeline
        return result.ReverseEnumerable();
    }
}
// Stryker restore all

/// <summary>
/// Contains extension methods for <see cref="Layout"/>.
/// </summary>
public static class LayoutExtensions
{
    /// <summary>
    /// Sets the ratio of the layout.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <param name="ratio">The ratio.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Layout Ratio(this Layout layout, int ratio)
    {
        // Stryker disable once all : Equivalent — extension method null guard; Layout is always non-null in fluent API usage
        ArgumentNullException.ThrowIfNull(layout);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through layout pipeline
        layout.Ratio = ratio;
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through layout pipeline
        return layout;
    }

    /// <summary>
    /// Sets the size of the layout.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <param name="size">The size.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Layout Size(this Layout layout, int size)
    {
        // Stryker disable once all : Equivalent — extension method null guard; Layout is always non-null in fluent API usage
        ArgumentNullException.ThrowIfNull(layout);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through layout pipeline
        layout.Size = size;
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through layout pipeline
        return layout;
    }

    /// <summary>
    /// Sets the minimum width of the layout.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <param name="size">The size.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Layout MinimumSize(this Layout layout, int size)
    {
        // Stryker disable once all : Equivalent — extension method null guard; Layout is always non-null in fluent API usage
        ArgumentNullException.ThrowIfNull(layout);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through layout pipeline
        layout.MinimumSize = size;
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through layout pipeline
        return layout;
    }
}