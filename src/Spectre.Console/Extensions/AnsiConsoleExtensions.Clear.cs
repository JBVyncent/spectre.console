namespace Spectre.Console;

/// <summary>
/// Contains extension methods for <see cref="IAnsiConsole"/> that perform
/// partial-erase operations on the current line or screen.
/// </summary>
public static partial class AnsiConsoleExtensions
{
    /// <summary>
    /// Erases the entire current line. The cursor position is not changed.
    /// Emits <c>ESC[2K</c>.
    /// </summary>
    /// <param name="console">The console.</param>
    public static void ClearLine(this IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);

        console.WriteAnsi(w => w.EraseInLine(2));
    }

    /// <summary>
    /// Erases from the cursor position to the end of the current line.
    /// The cursor position is not changed.
    /// Emits <c>ESC[0K</c>.
    /// </summary>
    /// <param name="console">The console.</param>
    public static void ClearLineToEnd(this IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);

        console.WriteAnsi(w => w.EraseInLine(0));
    }

    /// <summary>
    /// Erases from the start of the current line to the cursor position.
    /// The cursor position is not changed.
    /// Emits <c>ESC[1K</c>.
    /// </summary>
    /// <param name="console">The console.</param>
    public static void ClearLineToStart(this IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);

        console.WriteAnsi(w => w.EraseInLine(1));
    }

    /// <summary>
    /// Erases from the cursor position to the end of the screen (bottom).
    /// The cursor position is not changed.
    /// Emits <c>ESC[0J</c>.
    /// </summary>
    /// <param name="console">The console.</param>
    public static void ClearToBottom(this IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);

        console.WriteAnsi(w => w.EraseInDisplay(0));
    }

    /// <summary>
    /// Erases from the start of the screen (top) to the cursor position.
    /// The cursor position is not changed.
    /// Emits <c>ESC[1J</c>.
    /// </summary>
    /// <param name="console">The console.</param>
    public static void ClearToTop(this IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);

        console.WriteAnsi(w => w.EraseInDisplay(1));
    }
}
