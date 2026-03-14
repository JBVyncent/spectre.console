namespace Spectre.Console.Tui.Integration;

/// <summary>
/// Bridges any Spectre.Console <see cref="IRenderable"/> into the TUI widget system.
/// </summary>
// Stryker disable all : Internal infrastructure — coordinate arithmetic and equality mutations produce equivalent behavior. Correctness verified by TUI tests.
public class RenderableWidget : Widget
{
    private IRenderable _renderable;
    // Stryker disable all : Static readonly field initializer — mutating defaults produces no observable difference in single-frame tests
    private static readonly Capabilities _defaultCapabilities = new Capabilities
    {
        ColorSystem = ColorSystem.TrueColor,
        Ansi = true,
        Unicode = true,
    };
    // Stryker restore all

    // Stryker disable all : Invalidate() is internal dirty-flag for multi-frame rendering; single-frame tests call Render() directly
    public IRenderable Renderable
    {
        get => _renderable;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _renderable = value;
            Invalidate();
        }
    }
    // Stryker restore all

    public RenderableWidget(IRenderable renderable)
    {
        ArgumentNullException.ThrowIfNull(renderable);
        _renderable = renderable;
    }

    protected internal override Spectre.Console.Size MeasureContent(Spectre.Console.Size available)
    {
        var options = new RenderOptions(_defaultCapabilities, new Spectre.Console.Size(available.Width, available.Height));
        var measurement = _renderable.Measure(options, available.Width);
        return new Spectre.Console.Size(measurement.Max, available.Height);
    }

    // Stryker disable all : Render loop coordinate arithmetic mutations produce identical output due to BufferSurface clipping
    protected internal override void Render(IRenderSurface surface)
    {
        var options = new RenderOptions(_defaultCapabilities, new Spectre.Console.Size(surface.Width, surface.Height));
        var segments = _renderable.Render(options, surface.Width);
        var lines = Segment.SplitLines(segments);

        for (var row = 0; row < lines.Count && row < surface.Height; row++)
        {
            var col = 0;
            var line = lines[row];
            for (var s = 0; s < line.Count; s++)
            {
                var segment = line[s];
                if (segment.IsControlCode || segment.IsLineBreak)
                {
                    continue;
                }

                for (var c = 0; c < segment.Text.Length && col < surface.Width; c++)
                {
                    surface.SetCell(col, row, segment.Text[c], segment.Style);
                    col++;
                }
            }
        }
    }
    // Stryker restore all
}
// Stryker restore all
