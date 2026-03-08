namespace Spectre.Console;

internal sealed class ListPromptRenderHook<T> : IRenderHook
    where T : notnull
{
    private readonly IAnsiConsole _console;
    private readonly Func<IRenderable> _builder;
    private readonly LiveRenderable _live;
    private readonly object _lock;
    private bool _dirty;

    public ListPromptRenderHook(
        IAnsiConsole console,
        Func<IRenderable> builder)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(builder);
        _console = console;
        _builder = builder;
        _live = new LiveRenderable(console);
        _lock = new();
        _dirty = true;
    }

    public void Clear()
    {
        _console.Write(_live.RestoreCursor());
    }

    public void Refresh()
    {
        _dirty = true;
        _console.Write(ControlCode.Empty);
    }

    public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        lock (_lock)
        {
            if (!_live.HasRenderable || _dirty)
            {
                _live.SetRenderable(_builder());
                _dirty = false;
            }

            yield return _live.PositionCursor(options);

            foreach (var renderable in renderables)
            {
                yield return renderable;
            }

            // Save cursor position before rendering live display
            yield return ControlCode.Create(options.Capabilities, w => w.SaveCursor());
            yield return _live;
        }
    }
}