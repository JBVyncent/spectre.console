namespace Spectre.Console.Phantom;

/// <summary>
/// Factory for creating <see cref="IAnsiConsole"/> instances backed by a
/// <see cref="PhantomTerminal"/>. Use this as the entry point for Phantom-based tests.
/// </summary>
public static class PhantomConsole
{
    /// <summary>
    /// Create a new <see cref="IAnsiConsole"/> backed by a virtual terminal.
    /// Returns both the console and the terminal for assertions.
    /// </summary>
    /// <param name="width">Terminal width in columns (default 80).</param>
    /// <param name="height">Terminal height in rows (default 24).</param>
    /// <param name="colorSystem">Color system to use (default TrueColor).</param>
    /// <param name="ansiSupport">Whether ANSI is supported (default Yes).</param>
    /// <returns>A tuple of (console, output) where output.Terminal provides the screen state.</returns>
    public static (IAnsiConsole Console, PhantomConsoleOutput Output) Create(
        int width = 80,
        int height = 24,
        ColorSystem colorSystem = ColorSystem.TrueColor,
        AnsiSupport ansiSupport = AnsiSupport.Yes)
    {
        var terminal = new PhantomTerminal(width, height);
        var output = new PhantomConsoleOutput(terminal);

        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = ansiSupport,
            ColorSystem = (ColorSystemSupport)colorSystem,
            Out = output,
            Interactive = InteractionSupport.No,
        });

        return (console, output);
    }

    /// <summary>
    /// Create a new <see cref="IAnsiConsole"/> with interactive support enabled.
    /// Use this for testing prompts that need keyboard input simulation.
    /// </summary>
    public static (IAnsiConsole Console, PhantomConsoleOutput Output) CreateInteractive(
        int width = 80,
        int height = 24,
        ColorSystem colorSystem = ColorSystem.TrueColor)
    {
        var terminal = new PhantomTerminal(width, height);
        var output = new PhantomConsoleOutput(terminal);

        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = (ColorSystemSupport)colorSystem,
            Out = output,
            Interactive = InteractionSupport.Yes,
        });

        return (console, output);
    }
}
