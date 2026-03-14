namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Abstraction for terminal I/O operations.
/// </summary>
internal interface ITerminalDriver
{
    int Width { get; }
    int Height { get; }

    void Initialize();
    void Shutdown();

    void EnableMouse();
    void DisableMouse();

    void HideCursor();
    void ShowCursor();

    void Flush(IReadOnlyList<CellChange> changes);
    void Clear();

    InputEvent? ReadEvent(CancellationToken cancellationToken);
}
