namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Terminal driver for testing that captures output to a screen buffer
/// and replays queued input events.
/// </summary>
internal sealed class TestTerminalDriver : ITerminalDriver
{
    private readonly Queue<InputEvent> _inputQueue = new();
    private readonly ScreenBuffer _buffer;

    public int Width { get; }
    public int Height { get; }
    public bool IsInitialized { get; private set; }
    public bool IsShutdown { get; private set; }
    public bool MouseEnabled { get; private set; }
    public bool CursorVisible { get; private set; } = true;
    public ScreenBuffer Buffer => _buffer;

    public TestTerminalDriver(int width = 80, int height = 24)
    {
        Width = width;
        Height = height;
        _buffer = new ScreenBuffer(width, height);
    }

    public void Initialize()
    {
        IsInitialized = true;
        IsShutdown = false;
    }

    public void Shutdown()
    {
        IsShutdown = true;
    }

    public void EnableMouse()
    {
        MouseEnabled = true;
    }

    public void DisableMouse()
    {
        MouseEnabled = false;
    }

    public void HideCursor()
    {
        CursorVisible = false;
    }

    public void ShowCursor()
    {
        CursorVisible = true;
    }

    public void Flush(IReadOnlyList<CellChange> changes)
    {
        for (var i = 0; i < changes.Count; i++)
        {
            var change = changes[i];
            _buffer.SetCell(change.Column, change.Row, change.Character, change.Style);
        }
    }

    public void Clear()
    {
        _buffer.Clear();
    }

    public void EnqueueInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        _inputQueue.Enqueue(inputEvent);
    }

    public void EnqueueKey(ConsoleKey key, char keyChar = '\0', bool shift = false, bool alt = false, bool control = false)
    {
        _inputQueue.Enqueue(new KeyEvent(key, keyChar, shift, alt, control));
    }

    public InputEvent? ReadEvent(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        if (_inputQueue.Count > 0)
        {
            return _inputQueue.Dequeue();
        }

        return null;
    }

    public string GetText(int row)
    {
        var sb = new StringBuilder(Width);
        for (var col = 0; col < Width; col++)
        {
            sb.Append(_buffer[col, row].Character);
        }

        return sb.ToString().TrimEnd();
    }

    public char GetChar(int col, int row)
    {
        return _buffer[col, row].Character;
    }

    public Style GetStyle(int col, int row)
    {
        return _buffer[col, row].Style;
    }
}

