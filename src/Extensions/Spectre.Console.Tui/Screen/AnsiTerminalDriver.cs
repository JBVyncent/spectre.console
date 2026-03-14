namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Terminal driver that uses IAnsiConsole for VT output.
/// </summary>
// Stryker disable all : AnsiTerminalDriver wraps real terminal I/O (IAnsiConsole) and cannot be unit tested
// without a live terminal. All mutations are unreachable from the test harness.
internal sealed class AnsiTerminalDriver : ITerminalDriver
{
    private readonly IAnsiConsole _console;

    public int Width => _console.Profile.Width;
    public int Height => _console.Profile.Height;

    public AnsiTerminalDriver(IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);
        _console = console;
    }

    public void Initialize()
    {
        if (!_console.Profile.Capabilities.Interactive)
        {
            throw new InvalidOperationException(
                "TUI applications require an interactive terminal. " +
                "Cannot run in a non-interactive (piped/redirected) environment.");
        }

        // Capture Ctrl+C as input instead of killing the process
        System.Console.TreatControlCAsInput = true;

        _console.WriteAnsi(writer => writer.EnterAltScreen());
        HideCursor();
    }

    public void Shutdown()
    {
        DisableMouse();
        ShowCursor();
        _console.WriteAnsi(writer => writer.ExitAltScreen());

        // Restore default Ctrl+C behavior
        System.Console.TreatControlCAsInput = false;
    }

    public void EnableMouse()
    {
        // Enable SGR mouse mode (any-event tracking + SGR extended coordinates)
        _console.WriteAnsi(writer =>
        {
            writer.Write("\x1b[?1003h"); // Any-event tracking
            writer.Write("\x1b[?1006h"); // SGR extended mode
        });
    }

    public void DisableMouse()
    {
        _console.WriteAnsi(writer =>
        {
            writer.Write("\x1b[?1003l");
            writer.Write("\x1b[?1006l");
        });
    }

    public void HideCursor()
    {
        _console.Cursor.Hide();
    }

    public void ShowCursor()
    {
        _console.Cursor.Show();
    }

    public void Flush(IReadOnlyList<CellChange> changes)
    {
        if (changes.Count == 0)
        {
            return;
        }

        _console.WriteAnsi(writer =>
        {
            Style? lastStyle = null;

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];

                // Position cursor (1-indexed)
                writer.CursorPosition(change.Row + 1, change.Column + 1);

                // Write styled character
                writer.Write(change.Character.ToString(), change.Style);
                lastStyle = change.Style;
            }
        });
    }

    public void Clear()
    {
        _console.WriteAnsi(writer =>
        {
            writer.CursorHome();
            writer.EraseInDisplay(2);
        });
    }

    public InputEvent? ReadEvent(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        // Non-blocking check — only read if a key is available
        if (!_console.Input.IsKeyAvailable())
        {
            return null;
        }

        var keyInfo = _console.Input.ReadKey(true);
        if (keyInfo == null)
        {
            return null;
        }

        return new KeyEvent(keyInfo.Value);
    }
}
