namespace Spectre.Console;

/// <summary>
/// Contains extension methods for <see cref="IAnsiConsole"/>.
/// </summary>
public static partial class AnsiConsoleExtensions
{
    /// <summary>
    /// Writes a VT/Ansi control code sequence to the console (if supported).
    /// </summary>
    /// <param name="console">The console to write to.</param>
    /// <param name="sequence">The VT/Ansi control code sequence to write.</param>
    public static void WriteAnsi(this IAnsiConsole console, string sequence)
    {
        // Stryker disable once Statement : removing guard causes NullReferenceException on console.Profile — same exception family
        ArgumentNullException.ThrowIfNull(console);

        if (console.Profile.Capabilities.Ansi)
        {
            console.Write(new ControlCode(sequence));
        }
    }

    /// <summary>
    /// Gets the VT/ANSI control code sequence for a <see cref="IRenderable"/>.
    /// </summary>
    /// <param name="console">The console.</param>
    /// <param name="renderable">The renderable to the VT/ANSI control code sequence for.</param>
    /// <returns>The VT/ANSI control code sequence.</returns>
    public static string ToAnsi(this IAnsiConsole console, IRenderable renderable)
    {
        // Stryker disable once Statement : removing guard causes NullReferenceException on console.Profile — same exception family
        ArgumentNullException.ThrowIfNull(console);

        // Use a pre-sized StringBuilder to reduce StringWriter internal resizing.
        // Typical ANSI output for a styled renderable is 64-256 chars.
        var sb = new System.Text.StringBuilder(256);
        var buffer = new StringWriter(sb);
        var ansi = new AnsiWriter(buffer, console.Profile.Capabilities);
        ansi.Write(console, renderable);
        return sb.ToString();
    }
}