namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Render surface backed by a <see cref="ScreenBuffer"/> with coordinate offset and clipping.
/// </summary>
internal sealed class BufferSurface : IRenderSurface
{
    private readonly ScreenBuffer _buffer;
    private readonly int _offsetX;
    private readonly int _offsetY;
    private readonly Rect _clip;

    public int Width => _clip.Width;
    public int Height => _clip.Height;

    public BufferSurface(ScreenBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
        _offsetX = 0;
        _offsetY = 0;
        _clip = new Rect(0, 0, buffer.Width, buffer.Height);
    }

    public BufferSurface(ScreenBuffer buffer, Rect bounds)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
        _offsetX = bounds.X;
        _offsetY = bounds.Y;
        _clip = bounds.Intersect(new Rect(0, 0, buffer.Width, buffer.Height));
    }

    public void SetCell(int col, int row, char character, Style style)
    {
        var absCol = col + _offsetX;
        var absRow = row + _offsetY;

        if (_clip.Contains(absCol, absRow))
        {
            _buffer.SetCell(absCol, absRow, character, style);
        }
    }

    public void SetText(int col, int row, string text, Style style)
    {
        ArgumentNullException.ThrowIfNull(text);

        for (var i = 0; i < text.Length; i++)
        {
            SetCell(col + i, row, text[i], style);
        }
    }

    public void Fill(Rect area, char character, Style style)
    {
        for (var row = area.Y; row < area.Bottom; row++)
        {
            for (var c = area.X; c < area.Right; c++)
            {
                SetCell(c, row, character, style);
            }
        }
    }

    public void Clear()
    {
        Fill(new Rect(0, 0, Width, Height), ' ', Style.Plain);
    }

    public BufferSurface CreateSubSurface(Rect localBounds)
    {
        var absoluteBounds = new Rect(
            _offsetX + localBounds.X,
            _offsetY + localBounds.Y,
            localBounds.Width,
            localBounds.Height);

        return new BufferSurface(_buffer, absoluteBounds);
    }
}

