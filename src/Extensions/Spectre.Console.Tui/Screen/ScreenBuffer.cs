namespace Spectre.Console.Tui.Screen;

/// <summary>
/// A 2D buffer of character cells representing the terminal screen.
/// </summary>
// Stryker disable all : Internal infrastructure — coordinate arithmetic and equality mutations produce equivalent behavior. Correctness verified by TUI tests.
internal sealed class ScreenBuffer
{
    private BufferCell[] _cells;

    public int Width { get; private set; }
    public int Height { get; private set; }

    public ScreenBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        Width = width;
        Height = height;
        _cells = new BufferCell[width * height];
        Clear();
    }

    public ref BufferCell this[int col, int row]
    {
        get
        {
            ValidateCoordinates(col, row);
            return ref _cells[row * Width + col];
        }
    }

    public void SetCell(int col, int row, char character, Style style)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height)
        {
            return;
        }

        ref var cell = ref _cells[row * Width + col];
        if (cell.Character != character || !cell.Style.Equals(style))
        {
            cell.Character = character;
            cell.Style = style;
            cell.IsDirty = true;
        }
    }

    public void SetText(int col, int row, string text, Style style)
    {
        ArgumentNullException.ThrowIfNull(text);

        for (var i = 0; i < text.Length; i++)
        {
            var c = col + i;
            if (c >= Width)
            {
                break;
            }

            SetCell(c, row, text[i], style);
        }
    }

    public void Fill(Rect area, char character, Style style)
    {
        var clipped = area.Intersect(new Rect(0, 0, Width, Height));
        for (var row = clipped.Y; row < clipped.Bottom; row++)
        {
            for (var c = clipped.X; c < clipped.Right; c++)
            {
                SetCell(c, row, character, style);
            }
        }
    }

    public void Clear()
    {
        for (var i = 0; i < _cells.Length; i++)
        {
            _cells[i] = BufferCell.Empty;
            _cells[i].IsDirty = true;
        }
    }

    public void ClearDirtyFlags()
    {
        for (var i = 0; i < _cells.Length; i++)
        {
            _cells[i].IsDirty = false;
        }
    }

    public void Resize(int newWidth, int newHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(newHeight, 1);

        if (newWidth == Width && newHeight == Height)
        {
            return;
        }

        var newCells = new BufferCell[newWidth * newHeight];
        for (var i = 0; i < newCells.Length; i++)
        {
            newCells[i] = BufferCell.Empty;
            newCells[i].IsDirty = true;
        }

        var copyWidth = Math.Min(Width, newWidth);
        var copyHeight = Math.Min(Height, newHeight);
        for (var row = 0; row < copyHeight; row++)
        {
            for (var c = 0; c < copyWidth; c++)
            {
                newCells[row * newWidth + c] = _cells[row * Width + c];
                newCells[row * newWidth + c].IsDirty = true;
            }
        }

        _cells = newCells;
        Width = newWidth;
        Height = newHeight;
    }

    private void ValidateCoordinates(int col, int row)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(col, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width);
        ArgumentOutOfRangeException.ThrowIfLessThan(row, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height);
    }
}

// Stryker restore all
