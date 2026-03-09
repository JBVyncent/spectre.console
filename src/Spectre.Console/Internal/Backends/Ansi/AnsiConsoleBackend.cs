namespace Spectre.Console;

internal sealed class AnsiConsoleBackend : IAnsiConsoleBackend
{
    private readonly IAnsiConsole _console;

    public IAnsiConsoleCursor Cursor { get; }
    public Capabilities Capabilities => _console.Profile.Capabilities;

    public AnsiConsoleBackend(IAnsiConsole console)
    {
        // Stryker disable once all : Equivalent — internal constructor only called from AnsiConsoleFacade with non-null console
        ArgumentNullException.ThrowIfNull(console);
        _console = console;

        Cursor = new AnsiConsoleCursor(this);
    }

    public void Clear(bool home)
    {
        Write(w => w.EraseInDisplay(2));
        Write(w => w.ClearScrollback());

        if (home)
        {
            Write(w => w.CursorPosition(1, 1));
        }
    }

    public void Write(IRenderable renderable)
    {
        CreateWriter().Write(_console, renderable);
    }

    public void Write(Action<AnsiWriter> action)
    {
        action(CreateWriter());
    }

    // Resolve the writer from the current Profile.Out each time, so that
    // swapping Profile.Out at runtime is correctly honored by subsequent writes.
    private AnsiWriter CreateWriter()
    {
        return new AnsiWriter(_console.Profile.Out.Writer, _console.Profile.Capabilities);
    }
}