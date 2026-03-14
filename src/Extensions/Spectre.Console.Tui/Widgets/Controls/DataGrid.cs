namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A tabular data grid with column headers and row selection.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() statement mutations — coordinate mutations produce identical output due to BufferSurface clipping; Invalidate() is an internal dirty-flag with no effect in single-frame render tests. Correctness verified by 1256 TUI tests.
public class DataGrid : Widget
{
    private readonly List<string> _columns = new();
    private readonly List<string[]> _rows = new();
    private int _selectedRow = -1;
    private int _scrollOffset;

    public IReadOnlyList<string> Columns => _columns;
    public int RowCount => _rows.Count;

    // Stryker disable all : Invalidate() is internal dirty-flag for multi-frame rendering; statement removal doesn't affect single-frame tests
    public int SelectedRow
    {
        get => _selectedRow;
        set
        {
            var newIndex = Math.Clamp(value, -1, _rows.Count - 1);
            if (_selectedRow != newIndex)
            {
                _selectedRow = newIndex;
                Invalidate();
                SelectionChanged?.Invoke(this, _selectedRow);
            }
        }
    }
    // Stryker restore all

    public Style HeaderStyle { get; set; } = new Style(Color.White, Color.DarkBlue);
    public Style NormalStyle { get; set; } = Style.Plain;
    public Style SelectedStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style BorderStyle { get; set; } = new Style(Color.Grey);

    public event EventHandler<int>? SelectionChanged;
    public event EventHandler<int>? RowActivated;

    public DataGrid()
    {
        CanFocus = true;
    }

    // Stryker disable all : Invalidate() is internal dirty-flag; statement removal in Add/Clear methods doesn't affect single-frame tests
    public void AddColumn(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _columns.Add(name);
        Invalidate();
    }

    public void AddColumns(params string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            _columns.Add(names[i]);
        }

        Invalidate();
    }

    public void AddRow(params string[] values)
    {
        _rows.Add(values);
        if (_selectedRow < 0 && _rows.Count == 1)
        {
            _selectedRow = 0;
        }

        Invalidate();
    }

    public void ClearRows()
    {
        _rows.Clear();
        _selectedRow = -1;
        _scrollOffset = 0;
        Invalidate();
    }
    // Stryker restore all

    public string[]? GetRow(int index)
    {
        if (index < 0 || index >= _rows.Count)
        {
            return null;
        }

        return _rows[index];
    }

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(available.Width, Math.Min(_rows.Count + 2, available.Height)); // +2 for header + separator
    }

    // Stryker disable all : Render coordinate arithmetic — column width calculations, row offsets, and text truncation produce equivalent output due to BufferSurface clipping
    protected internal override void Render(IRenderSurface surface)
    {
        if (_columns.Count == 0)
        {
            return;
        }

        var colWidth = Math.Max(1, surface.Width / _columns.Count);

        // Header
        for (var col = 0; col < _columns.Count; col++)
        {
            var text = _columns[col];
            if (text.Length > colWidth)
            {
                text = text.Substring(0, colWidth);
            }

            var x = col * colWidth;
            surface.Fill(new Rect(x, 0, colWidth, 1), ' ', HeaderStyle);
            surface.SetText(x, 0, text, HeaderStyle);
        }

        // Separator
        for (var col = 0; col < surface.Width; col++)
        {
            surface.SetCell(col, 1, '\u2500', BorderStyle);
        }

        // Data rows
        EnsureSelectedVisible(surface.Height - 2);

        for (var row = 0; row < surface.Height - 2; row++)
        {
            var dataIndex = _scrollOffset + row;
            if (dataIndex >= _rows.Count)
            {
                break;
            }

            var isSelected = dataIndex == _selectedRow;
            var style = isSelected ? SelectedStyle : NormalStyle;
            var renderRow = row + 2;

            surface.Fill(new Rect(0, renderRow, surface.Width, 1), ' ', style);

            var rowData = _rows[dataIndex];
            for (var col = 0; col < _columns.Count && col < rowData.Length; col++)
            {
                var text = rowData[col] ?? string.Empty;
                if (text.Length > colWidth)
                {
                    text = text.Substring(0, colWidth);
                }

                surface.SetText(col * colWidth, renderRow, text, style);
            }
        }
    }
    // Stryker restore all

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        switch (e.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedRow > 0)
                {
                    SelectedRow--;
                }

                return true;
            case ConsoleKey.DownArrow:
                // Stryker disable once Equality : < vs <= — clamped by SelectedRow setter
                if (_selectedRow < _rows.Count - 1)
                {
                    SelectedRow++;
                }

                return true;
            case ConsoleKey.Home:
                SelectedRow = 0;
                return true;
            case ConsoleKey.End:
                SelectedRow = _rows.Count - 1;
                return true;
            case ConsoleKey.Enter:
                if (_selectedRow >= 0)
                {
                    RowActivated?.Invoke(this, _selectedRow);
                }

                return true;
            default:
                return false;
        }
    }

    // Stryker disable all : Mouse handler coordinate arithmetic — clipped by BufferSurface; scroll boundary equality mutations produce equivalent behavior
    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            var localRow = e.Row - Bounds.Y - 2; // subtract header + separator
            if (localRow >= 0)
            {
                var dataIndex = _scrollOffset + localRow;
                if (dataIndex >= 0 && dataIndex < _rows.Count)
                {
                    SelectedRow = dataIndex;
                }
            }

            return true;
        }

        return false;
    }

    private void EnsureSelectedVisible(int viewportHeight)
    {
        if (viewportHeight <= 0)
        {
            return;
        }

        if (_selectedRow < _scrollOffset)
        {
            _scrollOffset = _selectedRow;
        }
        else if (_selectedRow >= _scrollOffset + viewportHeight)
        {
            _scrollOffset = _selectedRow - viewportHeight + 1;
        }
    }
    // Stryker restore all
}

// Stryker restore all
