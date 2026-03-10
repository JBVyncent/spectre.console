namespace Spectre.Console.Phantom.Runner;

/// <summary>
/// Maps <see cref="ConsoleKey"/> values to VT100 escape sequences
/// for sending keystrokes to a ConPTY pseudo-terminal.
/// </summary>
public static class KeyMap
{
    /// <summary>
    /// Convert a <see cref="ConsoleKey"/> (with optional modifiers) to
    /// the VT100 escape sequence that the terminal expects on stdin.
    /// </summary>
    public static string ToVt100(ConsoleKey key, bool shift = false, bool ctrl = false, bool alt = false)
    {
        // Ctrl+letter → ASCII control code (1-26)
        if (ctrl && key >= ConsoleKey.A && key <= ConsoleKey.Z)
        {
            return ((char)(key - ConsoleKey.A + 1)).ToString();
        }

        return key switch
        {
            ConsoleKey.Enter => "\r",
            ConsoleKey.Escape => "\x1b",
            ConsoleKey.Backspace => "\x7f",
            ConsoleKey.Tab => shift ? "\x1b[Z" : "\t",
            ConsoleKey.Spacebar => " ",

            // Arrow keys
            ConsoleKey.UpArrow => "\x1b[A",
            ConsoleKey.DownArrow => "\x1b[B",
            ConsoleKey.RightArrow => "\x1b[C",
            ConsoleKey.LeftArrow => "\x1b[D",

            // Navigation
            ConsoleKey.Home => "\x1b[H",
            ConsoleKey.End => "\x1b[F",
            ConsoleKey.Insert => "\x1b[2~",
            ConsoleKey.Delete => "\x1b[3~",
            ConsoleKey.PageUp => "\x1b[5~",
            ConsoleKey.PageDown => "\x1b[6~",

            // Function keys
            ConsoleKey.F1 => "\x1bOP",
            ConsoleKey.F2 => "\x1bOQ",
            ConsoleKey.F3 => "\x1bOR",
            ConsoleKey.F4 => "\x1bOS",
            ConsoleKey.F5 => "\x1b[15~",
            ConsoleKey.F6 => "\x1b[17~",
            ConsoleKey.F7 => "\x1b[18~",
            ConsoleKey.F8 => "\x1b[19~",
            ConsoleKey.F9 => "\x1b[20~",
            ConsoleKey.F10 => "\x1b[21~",
            ConsoleKey.F11 => "\x1b[23~",
            ConsoleKey.F12 => "\x1b[24~",

            // Letters
            _ when key >= ConsoleKey.A && key <= ConsoleKey.Z =>
                (shift ? (char)key : (char)(key - ConsoleKey.A + 'a')).ToString(),

            // Digits
            _ when key >= ConsoleKey.D0 && key <= ConsoleKey.D9 =>
                ((char)('0' + key - ConsoleKey.D0)).ToString(),

            // Fallback: try to map via the ConsoleKey numeric value
            _ => MapFallback(key),
        };
    }

    /// <summary>
    /// Convert a literal character to its terminal input representation.
    /// </summary>
    public static string ToVt100(char ch)
    {
        return ch.ToString();
    }

    private static string MapFallback(ConsoleKey key)
    {
        // Common punctuation keys
        return key switch
        {
            ConsoleKey.OemPeriod => ".",
            ConsoleKey.OemComma => ",",
            ConsoleKey.OemMinus => "-",
            ConsoleKey.OemPlus => "=",
            ConsoleKey.Oem1 => ";",       // semicolon
            ConsoleKey.Oem2 => "/",       // slash
            ConsoleKey.Oem3 => "`",       // backtick
            ConsoleKey.Oem4 => "[",       // open bracket
            ConsoleKey.Oem5 => "\\",      // backslash
            ConsoleKey.Oem6 => "]",       // close bracket
            ConsoleKey.Oem7 => "'",       // single quote
            _ => string.Empty,            // Unknown key — send nothing
        };
    }
}
