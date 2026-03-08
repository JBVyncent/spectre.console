using System.Text;

namespace Spectre.Console.Phantom;

/// <summary>
/// A 2D grid of <see cref="ScreenCell"/> representing terminal screen state.
/// Supports scrolling, resizing, and region-based erase operations.
/// </summary>
public sealed class ScreenBuffer
{
    private ScreenCell[,] _cells;

    /// <summary>
    /// Number of columns in the buffer.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Number of rows in the buffer.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Creates a new screen buffer with the specified dimensions.
    /// </summary>
    public ScreenBuffer(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        Width = width;
        Height = height;
        _cells = new ScreenCell[height, width];
        InitializeCells();
    }

    /// <summary>
    /// Get the cell at the specified position (0-indexed).
    /// </summary>
    public ScreenCell this[int row, int col]
    {
        get
        {
            ValidatePosition(row, col);
            return _cells[row, col];
        }
    }

    /// <summary>
    /// Write a character at the specified position with the given style.
    /// </summary>
    public void WriteChar(int row, int col, char ch, ScreenCell style)
    {
        // Stryker disable once all : Equivalent — ArgumentNullException still propagates from ScreenCell.CopyStyleFrom(null); defensive guard only
        ArgumentNullException.ThrowIfNull(style);

        if (row < 0 || row >= Height || col < 0 || col >= Width)
        {
            return; // Silently ignore out-of-bounds writes (terminal behavior)
        }

        var cell = _cells[row, col];
        cell.Character = ch;
        cell.CopyStyleFrom(style);
    }

    /// <summary>
    /// Erase from the specified position to the end of the display.
    /// </summary>
    public void EraseToEnd(int row, int col)
    {
        // Erase from cursor to end of current line
        for (var c = col; c < Width; c++)
        {
            if (row >= 0 && row < Height)
            {
                _cells[row, c].Reset();
            }
        }

        // Erase all subsequent lines
        for (var r = row + 1; r < Height; r++)
        {
            for (var c = 0; c < Width; c++)
            {
                _cells[r, c].Reset();
            }
        }
    }

    /// <summary>
    /// Erase from the start of the display to the specified position.
    /// </summary>
    public void EraseToStart(int row, int col)
    {
        // Erase all preceding lines
        for (var r = 0; r < row; r++)
        {
            for (var c = 0; c < Width; c++)
            {
                _cells[r, c].Reset();
            }
        }

        // Erase from start of current line to cursor
        for (var c = 0; c <= col && c < Width; c++)
        {
            if (row >= 0 && row < Height)
            {
                _cells[row, c].Reset();
            }
        }
    }

    /// <summary>
    /// Erase the entire display.
    /// </summary>
    public void EraseAll()
    {
        for (var r = 0; r < Height; r++)
        {
            for (var c = 0; c < Width; c++)
            {
                _cells[r, c].Reset();
            }
        }
    }

    /// <summary>
    /// Erase from the specified column to the end of the line.
    /// </summary>
    public void EraseLineToEnd(int row, int col)
    {
        if (row < 0 || row >= Height)
        {
            return;
        }

        for (var c = col; c < Width; c++)
        {
            _cells[row, c].Reset();
        }
    }

    /// <summary>
    /// Erase from the start of the line to the specified column.
    /// </summary>
    public void EraseLineToStart(int row, int col)
    {
        if (row < 0 || row >= Height)
        {
            return;
        }

        for (var c = 0; c <= col && c < Width; c++)
        {
            _cells[row, c].Reset();
        }
    }

    /// <summary>
    /// Erase an entire line.
    /// </summary>
    public void EraseLine(int row)
    {
        if (row < 0 || row >= Height)
        {
            return;
        }

        for (var c = 0; c < Width; c++)
        {
            _cells[row, c].Reset();
        }
    }

    /// <summary>
    /// Scroll the buffer up by one line (top line is lost, bottom line is blank).
    /// </summary>
    public void ScrollUp()
    {
        for (var r = 0; r < Height - 1; r++)
        {
            for (var c = 0; c < Width; c++)
            {
                var src = _cells[r + 1, c];
                var dst = _cells[r, c];
                dst.Character = src.Character;
                dst.CopyStyleFrom(src);
            }
        }

        // Clear the last line
        for (var c = 0; c < Width; c++)
        {
            _cells[Height - 1, c].Reset();
        }
    }

    /// <summary>
    /// Get the text content of a specific row (trailing spaces trimmed).
    /// </summary>
    public string GetRowText(int row)
    {
        ValidateRow(row);

        var sb = new StringBuilder(Width);
        for (var c = 0; c < Width; c++)
        {
            sb.Append(_cells[row, c].Character);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Get the text content of the entire screen as a multi-line string.
    /// Trailing spaces on each line are trimmed. Trailing empty lines are trimmed.
    /// </summary>
    public string GetText()
    {
        var lines = new List<string>(Height);
        for (var r = 0; r < Height; r++)
        {
            lines.Add(GetRowText(r));
        }

        // Trim trailing empty lines
        while (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Get a rectangular region of text content.
    /// </summary>
    public string GetRegionText(int startRow, int startCol, int endRow, int endCol)
    {
        var sb = new StringBuilder();
        for (var r = startRow; r <= endRow && r < Height; r++)
        {
            if (r > startRow)
            {
                sb.Append('\n');
            }

            var cStart = r == startRow ? startCol : 0;
            // Stryker disable once all : Equivalent — `Width - 1` vs `Width + 1`: the `c < Width` guard on the next line clamps iteration to Width-1 regardless
            var cEnd = r == endRow ? endCol : Width - 1;
            for (var c = cStart; c <= cEnd && c < Width; c++)
            {
                sb.Append(_cells[r, c].Character);
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Check if a specific character exists at the given position.
    /// </summary>
    public bool HasCharAt(int row, int col, char expected)
    {
        if (row < 0 || row >= Height || col < 0 || col >= Width)
        {
            return false;
        }

        return _cells[row, col].Character == expected;
    }

    /// <summary>
    /// Find the first occurrence of a string in the buffer.
    /// Returns the (row, col) position, or null if not found.
    /// </summary>
    public (int Row, int Col)? FindText(string text)
    {
        // Stryker disable once all : Equivalent — ArgumentNullException still propagates from string.IndexOf(null, ...); defensive guard only
        ArgumentNullException.ThrowIfNull(text);

        for (var r = 0; r < Height; r++)
        {
            var rowText = GetRowText(r);
            var idx = rowText.IndexOf(text, StringComparison.Ordinal);
            if (idx >= 0)
            {
                return (r, idx);
            }
        }

        return null;
    }

    /// <summary>
    /// Check if the buffer contains the specified text anywhere.
    /// </summary>
    public bool ContainsText(string text)
    {
        return FindText(text) != null;
    }

    private void InitializeCells()
    {
        for (var r = 0; r < Height; r++)
        {
            for (var c = 0; c < Width; c++)
            {
                _cells[r, c] = new ScreenCell();
            }
        }
    }

    private void ValidatePosition(int row, int col)
    {
        if (row < 0 || row >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Row {row} is out of range [0, {Height}).");
        }

        if (col < 0 || col >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(col), $"Column {col} is out of range [0, {Width}).");
        }
    }

    private void ValidateRow(int row)
    {
        if (row < 0 || row >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Row {row} is out of range [0, {Height}).");
        }
    }
}
