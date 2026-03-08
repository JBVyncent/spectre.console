namespace Spectre.Console.Tests;

public static class ConsoleKeyExtensions
{
    // Keys whose enum values collide with printable ASCII characters (e.g. DownArrow=40='(')
    // but which should never contribute a KeyChar in a real terminal.
    private static readonly HashSet<ConsoleKey> _nonPrintingKeys =
    [
        ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.LeftArrow, ConsoleKey.RightArrow,
        ConsoleKey.Home, ConsoleKey.End, ConsoleKey.PageUp, ConsoleKey.PageDown,
        ConsoleKey.Insert, ConsoleKey.Delete, ConsoleKey.Enter, ConsoleKey.Escape,
        ConsoleKey.Tab, ConsoleKey.Backspace,
        ConsoleKey.F1,  ConsoleKey.F2,  ConsoleKey.F3,  ConsoleKey.F4,
        ConsoleKey.F5,  ConsoleKey.F6,  ConsoleKey.F7,  ConsoleKey.F8,
        ConsoleKey.F9,  ConsoleKey.F10, ConsoleKey.F11, ConsoleKey.F12,
    ];

    public static ConsoleKeyInfo ToConsoleKeyInfo(this ConsoleKey key)
    {
        char ch;
        if (_nonPrintingKeys.Contains(key))
        {
            ch = '\0';
        }
        else
        {
            ch = (char)key;
            if (char.IsControl(ch))
            {
                ch = '\0';
            }
        }

        return new ConsoleKeyInfo(ch, key, false, false, false);
    }

    /// <summary>
    /// Converts a printable character to a <see cref="ConsoleKeyInfo"/> suitable for
    /// driving <c>ListPromptState.Update</c> in unit tests.
    /// </summary>
    public static ConsoleKeyInfo ToConsoleKeyInfo(this char ch)
    {
        // Map the character to the nearest ConsoleKey enum value. For printable ASCII
        // letters/digits the enum value matches the uppercase char code.
        var key = char.ToUpperInvariant(ch) is var upper && Enum.IsDefined(typeof(ConsoleKey), (int)upper)
            ? (ConsoleKey)upper
            : ConsoleKey.Oem1;

        return new ConsoleKeyInfo(ch, key, false, false, false);
    }
}