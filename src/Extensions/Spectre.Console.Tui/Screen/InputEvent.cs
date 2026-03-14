namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Base class for all TUI input events.
/// </summary>
public abstract class InputEvent
{
}

/// <summary>
/// A keyboard input event.
/// </summary>
public sealed class KeyEvent : InputEvent
{
    public ConsoleKey Key { get; }
    public char KeyChar { get; }
    public bool Shift { get; }
    public bool Alt { get; }
    public bool Control { get; }

    public KeyEvent(ConsoleKey key, char keyChar, bool shift = false, bool alt = false, bool control = false)
    {
        Key = key;
        KeyChar = keyChar;
        Shift = shift;
        Alt = alt;
        Control = control;
    }

    public KeyEvent(ConsoleKeyInfo keyInfo)
    {
        Key = keyInfo.Key;
        KeyChar = keyInfo.KeyChar;
        Shift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
        Alt = (keyInfo.Modifiers & ConsoleModifiers.Alt) != 0;
        Control = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;
    }
}

/// <summary>
/// A mouse input event.
/// </summary>
public sealed class MouseEvent : InputEvent
{
    public MouseButton Button { get; }
    public MouseEventType EventType { get; }
    public int Column { get; }
    public int Row { get; }
    public bool Shift { get; }
    public bool Alt { get; }
    public bool Control { get; }

    public MouseEvent(
        MouseButton button,
        MouseEventType eventType,
        int column,
        int row,
        bool shift = false,
        bool alt = false,
        bool control = false)
    {
        Button = button;
        EventType = eventType;
        Column = column;
        Row = row;
        Shift = shift;
        Alt = alt;
        Control = control;
    }
}

/// <summary>
/// A terminal resize event.
/// </summary>
public sealed class ResizeEvent : InputEvent
{
    public int Width { get; }
    public int Height { get; }

    public ResizeEvent(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

/// <summary>
/// Mouse button identifiers.
/// </summary>
public enum MouseButton
{
    None,
    Left,
    Middle,
    Right,
}

/// <summary>
/// Mouse event types.
/// </summary>
public enum MouseEventType
{
    Press,
    Release,
    Move,
    ScrollUp,
    ScrollDown,
}

