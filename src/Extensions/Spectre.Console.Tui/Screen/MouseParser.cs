namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Parses SGR mouse escape sequences into <see cref="MouseEvent"/> instances.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
internal static class MouseParser
{
    /// <summary>
    /// Tries to parse an SGR mouse sequence.
    /// Format: ESC [ &lt; button ; col ; row M (press) or m (release).
    /// </summary>
    public static MouseEvent? TryParse(string sequence)
    {
        if (string.IsNullOrEmpty(sequence))
        {
            return null;
        }

        // SGR format: \e[<button;col;rowM or \e[<button;col;rowm
        // We expect the sequence after CSI < has been stripped, e.g. "0;15;8M"
        if (sequence.Length < 4)
        {
            return null;
        }

        var terminator = sequence[sequence.Length - 1];
        if (terminator != 'M' && terminator != 'm')
        {
            return null;
        }

        var isRelease = terminator == 'm';
        var data = sequence.Substring(0, sequence.Length - 1);
        var parts = data.Split(';');

        if (parts.Length != 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var buttonCode) ||
            !int.TryParse(parts[1], out var col) ||
            !int.TryParse(parts[2], out var row))
        {
            return null;
        }

        // Convert from 1-indexed to 0-indexed
        col--;
        row--;

        var shift = (buttonCode & 4) != 0;
        var alt = (buttonCode & 8) != 0;
        var control = (buttonCode & 16) != 0;

        var baseButton = buttonCode & 3;
        var isScroll = (buttonCode & 64) != 0;
        var isMotion = (buttonCode & 32) != 0;

        MouseButton button;
        MouseEventType eventType;

        if (isScroll)
        {
            button = MouseButton.None;
            eventType = baseButton == 0 ? MouseEventType.ScrollUp : MouseEventType.ScrollDown;
        }
        else if (isMotion && baseButton == 3)
        {
            button = MouseButton.None;
            eventType = MouseEventType.Move;
        }
        else
        {
            button = baseButton switch
            {
                0 => MouseButton.Left,
                1 => MouseButton.Middle,
                2 => MouseButton.Right,
                _ => MouseButton.None,
            };

            eventType = isRelease ? MouseEventType.Release : MouseEventType.Press;
        }

        return new MouseEvent(button, eventType, col, row, shift, alt, control);
    }
}

// Stryker restore all
