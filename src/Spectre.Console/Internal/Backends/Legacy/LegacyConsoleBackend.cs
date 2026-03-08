namespace Spectre.Console;

// Stryker disable all : NoCoverage — legacy console backend for non-ANSI terminals; not exercised by TestConsole
internal sealed class LegacyConsoleBackend : IAnsiConsoleBackend
{
    private readonly IAnsiConsole _console;
    private Style _lastStyle;

    public IAnsiConsoleCursor Cursor { get; }

    public LegacyConsoleBackend(IAnsiConsole console)
    {
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through legacy console backend
        ArgumentNullException.ThrowIfNull(console);
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through legacy console backend
        _console = console;
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through legacy console backend
        _lastStyle = Style.Plain;

        Cursor = new LegacyConsoleCursor();
    }

    public void Clear(bool home)
    {
        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        var (x, y) = (System.Console.CursorLeft, System.Console.CursorTop);

        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        System.Console.Clear();

        if (!home)
        {
            // Set the cursor position
            // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
            System.Console.SetCursorPosition(x, y);
        }
    }

    public void Write(IRenderable renderable)
    {
        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        foreach (var segment in renderable.GetSegments(_console))
        {
            // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
            if (segment.IsControlCode)
            {
                continue;
            }

            // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
            if (!_lastStyle.Equals(segment.Style))
            {
                SetStyle(segment.Style);
            }

            // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
            _console.Profile.Out.Writer.Write(segment.Text.NormalizeNewLines(native: true));
        }
    }

    public void Write(Action<AnsiWriter> action)
    {
        // Do nothing. The backend is not capable of emitting ANSI/VT escape sequences.
    }

    private void SetStyle(Style style)
    {
        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        _lastStyle = style;

        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        System.Console.ResetColor();

        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        var background = Color.ToConsoleColor(style.Background);
        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        if (_console.Profile.Capabilities.ColorSystem != ColorSystem.NoColors && (int)background != -1)
        {
            // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
            System.Console.BackgroundColor = background;
        }

        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        var foreground = Color.ToConsoleColor(style.Foreground);
        // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
        if (_console.Profile.Capabilities.ColorSystem != ColorSystem.NoColors && (int)foreground != -1)
        {
            // Stryker disable once all : NoCoverage — legacy console method; NoCoverage through legacy console backend
            System.Console.ForegroundColor = foreground;
        }
    }
}
// Stryker restore all