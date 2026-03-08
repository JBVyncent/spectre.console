namespace Spectre.Console;

internal sealed class LiveRenderable : Renderable
{
    private readonly object _lock = new();
    private readonly IAnsiConsole _console;
    private IRenderable? _renderable;
    private SegmentShape? _shape;

    public IRenderable? Target => _renderable;
    public bool DidOverflow { get; private set; }

    [MemberNotNullWhen(true, nameof(Target))]
    public bool HasRenderable => _renderable != null;
    public VerticalOverflow Overflow { get; set; }
    public VerticalOverflowCropping OverflowCropping { get; set; }

    public LiveRenderable(IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);
        _console = console;
        Overflow = VerticalOverflow.Ellipsis;
        OverflowCropping = VerticalOverflowCropping.Top;
    }

    public LiveRenderable(IAnsiConsole console, IRenderable renderable)
        : this(console)
    {
        ArgumentNullException.ThrowIfNull(renderable);
        _renderable = renderable;
    }

    public void SetRenderable(IRenderable? renderable)
    {
        lock (_lock)
        {
            _renderable = renderable;
        }
    }

    public IRenderable PositionCursor(RenderOptions options)
    {
        lock (_lock)
        {
            if (_shape == null)
            {
                return ControlCode.Empty;
            }

            // Check if the size have been reduced
            if (_shape.Value.Height > options.ConsoleSize.Height || _shape.Value.Width > options.ConsoleSize.Width)
            {
                // Important reset shape, so the size can shrink
                _shape = null;
                return ControlCode.Create(options.Capabilities, w =>
                {
                    w.EraseInDisplay(2);
                    w.ClearScrollback();
                    w.CursorHome();
                });
            }

            // Restore cursor to saved position and erase old live content
            return ControlCode.Create(options.Capabilities, w =>
            {
                w.RestoreCursor();
                w.EraseInDisplay(0);
            });
        }
    }

    public IRenderable RestoreCursor()
    {
        lock (_lock)
        {
            if (_shape == null)
            {
                return ControlCode.Empty;
            }

            // Restore cursor to saved position and erase old live content
            return ControlCode.Create(_console.Profile.Capabilities, w =>
            {
                w.RestoreCursor();
                w.EraseInDisplay(0);
            });
        }
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        lock (_lock)
        {
            DidOverflow = false;

            if (_renderable != null)
            {
                var segments = _renderable.Render(options, maxWidth);
                var lines = Segment.SplitLines(segments, maxWidth);

                var shape = SegmentShape.Calculate(options, lines);
                if (shape.Height > _console.Profile.Height)
                {
                    if (Overflow == VerticalOverflow.Crop)
                    {
                        if (OverflowCropping == VerticalOverflowCropping.Bottom)
                        {
                            // Remove bottom lines
                            var index = Math.Min(_console.Profile.Height, lines.Count);
                            var count = lines.Count - index;
                            lines.RemoveRange(index, count);
                        }
                        else
                        {
                            // Remove top lines
                            var start = lines.Count - _console.Profile.Height;
                            lines.RemoveRange(0, start);
                        }

                        shape = SegmentShape.Calculate(options, lines);
                    }
                    else if (Overflow == VerticalOverflow.Ellipsis)
                    {
                        var ellipsisText = _console.Profile.Capabilities.Unicode ? "…" : "...";
                        var ellipsis = new SegmentLine(((IRenderable)new Markup($"[yellow]{ellipsisText}[/]")).Render(options, maxWidth));

                        if (OverflowCropping == VerticalOverflowCropping.Bottom)
                        {
                            // Remove bottom lines
                            var index = Math.Min(_console.Profile.Height - 1, lines.Count);
                            var count = lines.Count - index;
                            lines.RemoveRange(index, count);
                            lines.Add(ellipsis);
                        }
                        else
                        {
                            // Remove top lines
                            var start = lines.Count - _console.Profile.Height;
                            lines.RemoveRange(0, start + 1);
                            lines.Insert(0, ellipsis);
                        }

                        shape = SegmentShape.Calculate(options, lines);
                    }

                    DidOverflow = true;
                }

                _shape = _shape?.Inflate(shape) ?? shape;
                _shape.Value.Apply(ref lines);

                foreach (var (_, _, last, line) in lines.Enumerate())
                {
                    foreach (var item in line)
                    {
                        yield return item;
                    }

                    if (!last)
                    {
                        yield return Segment.LineBreak;
                    }
                }

                yield break;
            }

            _shape = null;
        }
    }
}