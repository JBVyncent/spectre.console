namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Surface that widgets render into, with widget-local coordinates.
/// </summary>
public interface IRenderSurface
{
    int Width { get; }
    int Height { get; }

    void SetCell(int col, int row, char character, Style style);
    void SetText(int col, int row, string text, Style style);
    void Fill(Rect area, char character, Style style);
    void Clear();
}
