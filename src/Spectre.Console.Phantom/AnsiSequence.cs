namespace Spectre.Console.Phantom;

/// <summary>
/// Represents a parsed ANSI escape sequence or plain text segment.
/// </summary>
public abstract record AnsiSequence
{
    /// <summary>
    /// Plain text content (no escape sequences).
    /// </summary>
    public sealed record Text(string Content) : AnsiSequence;

    /// <summary>
    /// SGR (Select Graphic Rendition) — styling and colors.
    /// CSI {params} m
    /// </summary>
    public sealed record Sgr(int[] Parameters) : AnsiSequence;

    /// <summary>
    /// Cursor movement — up, down, left, right, absolute position.
    /// </summary>
    public sealed record CursorMove(CursorDirection Direction, int Count) : AnsiSequence;

    /// <summary>
    /// Cursor position — move to absolute row/column.
    /// CSI {row};{col} H
    /// </summary>
    public sealed record CursorPosition(int Row, int Column) : AnsiSequence;

    /// <summary>
    /// Cursor horizontal absolute — move to column.
    /// CSI {col} G
    /// </summary>
    public sealed record CursorHorizontalAbsolute(int Column) : AnsiSequence;

    /// <summary>
    /// Save cursor position. CSI s
    /// </summary>
    public sealed record SaveCursor : AnsiSequence;

    /// <summary>
    /// Restore cursor position. CSI u
    /// </summary>
    public sealed record RestoreCursor : AnsiSequence;

    /// <summary>
    /// Show or hide cursor. CSI ? 25 h/l
    /// </summary>
    public sealed record CursorVisibility(bool Visible) : AnsiSequence;

    /// <summary>
    /// Erase in display. CSI {mode} J
    /// </summary>
    public sealed record EraseInDisplay(EraseMode Mode) : AnsiSequence;

    /// <summary>
    /// Erase in line. CSI {mode} K
    /// </summary>
    public sealed record EraseInLine(EraseMode Mode) : AnsiSequence;

    /// <summary>
    /// Enter or exit alternate screen buffer. CSI ? 1049 h/l
    /// </summary>
    public sealed record AlternateScreen(bool Enter) : AnsiSequence;

    /// <summary>
    /// OSC hyperlink. ESC ] 8 ; params ; url ST
    /// </summary>
    public sealed record Hyperlink(string? Id, string Url) : AnsiSequence;

    /// <summary>
    /// Newline character.
    /// </summary>
    public sealed record NewLine : AnsiSequence;

    /// <summary>
    /// Carriage return character.
    /// </summary>
    public sealed record CarriageReturn : AnsiSequence;

    /// <summary>
    /// Backspace character.
    /// </summary>
    public sealed record Backspace : AnsiSequence;
}

/// <summary>
/// Direction for cursor movement sequences.
/// </summary>
public enum CursorDirection
{
    Up,
    Down,
    Right,
    Left,
}

/// <summary>
/// Mode for erase operations.
/// </summary>
public enum EraseMode
{
    /// <summary>Erase from cursor to end.</summary>
    ToEnd = 0,
    /// <summary>Erase from start to cursor.</summary>
    ToStart = 1,
    /// <summary>Erase entire line/display.</summary>
    All = 2,
    /// <summary>Erase scrollback buffer (display only).</summary>
    Scrollback = 3,
}
