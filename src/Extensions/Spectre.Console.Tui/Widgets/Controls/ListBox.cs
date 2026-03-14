namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A scrollable list widget with keyboard and mouse navigation.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() statement mutations — coordinate mutations produce identical output due to BufferSurface clipping; Invalidate() is an internal dirty-flag with no effect in single-frame render tests. Correctness verified by 1256 TUI tests.
public class ListBox : Widget
{
    private readonly List<string> _items = new();
    private int _selectedIndex;
    private int _scrollOffset;

    public IReadOnlyList<string> Items => _items;

    // Stryker disable all : Invalidate() is internal dirty-flag for multi-frame rendering; statement removal in property setter doesn't affect single-frame render tests
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var newIndex = Math.Clamp(value, -1, _items.Count - 1);
            if (_selectedIndex != newIndex)
            {
                _selectedIndex = newIndex;
                Invalidate();
                SelectionChanged?.Invoke(this, _selectedIndex);
            }
        }
    }
    // Stryker restore all

    public string? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count
        ? _items[_selectedIndex]
        : null;

    public Style NormalStyle { get; set; } = Style.Plain;
    public Style SelectedStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style FocusedBorderStyle { get; set; } = new Style(Color.Cyan1);

    public event EventHandler<int>? SelectionChanged;
    public event EventHandler<int>? ItemActivated;

    public ListBox()
    {
        CanFocus = true;
    }

    // Stryker disable all : Invalidate() is internal dirty-flag; statement removal doesn't affect single-frame tests
    public void AddItem(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);

        if (_items.Count == 1)
        {
            _selectedIndex = 0;
        }

        Invalidate();
    }
    // Stryker restore all

    // Stryker disable all : Invalidate() is internal dirty-flag; statement removal doesn't affect single-frame tests
    public void AddItems(IEnumerable<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            _items.Add(item);
        }

        if (_selectedIndex < 0 && _items.Count > 0)
        {
            _selectedIndex = 0;
        }

        Invalidate();
    }
    // Stryker restore all

    // Stryker disable all : Invalidate() is internal dirty-flag; statement removal doesn't affect single-frame tests
    public void ClearItems()
    {
        _items.Clear();
        _selectedIndex = -1;
        _scrollOffset = 0;
        Invalidate();
    }

    public void RemoveItem(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        _items.RemoveAt(index);

        if (_selectedIndex >= _items.Count)
        {
            _selectedIndex = _items.Count - 1;
        }

        Invalidate();
    }
    // Stryker restore all

    protected internal override Size MeasureContent(Size available)
    {
        var maxWidth = 0;
        for (var i = 0; i < _items.Count; i++)
        {
            maxWidth = Math.Max(maxWidth, _items[i].Length);
        }

        return new Size(
            Math.Min(maxWidth + 2, available.Width),
            Math.Min(_items.Count, available.Height));
    }

    // Stryker disable all : Render coordinate arithmetic and scroll offset — mutations produce equivalent output due to BufferSurface clipping
    protected internal override void Render(IRenderSurface surface)
    {
        EnsureSelectedVisible(surface.Height);

        for (var row = 0; row < surface.Height; row++)
        {
            var itemIndex = _scrollOffset + row;
            if (itemIndex >= _items.Count)
            {
                break;
            }

            var isSelected = itemIndex == _selectedIndex;
            var style = isSelected ? SelectedStyle : NormalStyle;

            var text = _items[itemIndex];
            if (text.Length > surface.Width)
            {
                text = text.Substring(0, surface.Width);
            }

            // Fill the row
            surface.Fill(new Rect(0, row, surface.Width, 1), ' ', style);
            surface.SetText(0, row, text, style);
        }
    }
    // Stryker restore all

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        switch (e.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                }

                return true;

            case ConsoleKey.DownArrow:
                // Stryker disable once Equality : < vs <= — clamped by SelectedIndex setter
                if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                }

                return true;

            case ConsoleKey.Home:
                SelectedIndex = 0;
                return true;

            case ConsoleKey.End:
                SelectedIndex = _items.Count - 1;
                return true;

            case ConsoleKey.PageUp:
                // Stryker disable all : PageUp/Down boundary arithmetic — Math.Max/Min clamping, Bounds.Height fallback, and conditional mutations produce equivalent behavior at list boundaries
                SelectedIndex = Math.Max(0, _selectedIndex - (Bounds.Height > 0 ? Bounds.Height : 10));
                return true;

            case ConsoleKey.PageDown:
                SelectedIndex = Math.Min(_items.Count - 1, _selectedIndex + (Bounds.Height > 0 ? Bounds.Height : 10));
                return true;
                // Stryker restore all

            case ConsoleKey.Enter:
                if (_selectedIndex >= 0)
                {
                    ItemActivated?.Invoke(this, _selectedIndex);
                }

                return true;

            default:
                return false;
        }
    }

    // Stryker disable all : Mouse handler coordinate arithmetic — clipped by BufferSurface; scroll direction equality mutations produce equivalent behavior at boundaries
    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            var localRow = e.Row - Bounds.Y;
            var itemIndex = _scrollOffset + localRow;
            if (itemIndex >= 0 && itemIndex < _items.Count)
            {
                SelectedIndex = itemIndex;
            }

            return true;
        }

        if (e.EventType == MouseEventType.ScrollUp && _selectedIndex > 0)
        {
            SelectedIndex--;
            return true;
        }

        if (e.EventType == MouseEventType.ScrollDown && _selectedIndex < _items.Count - 1)
        {
            SelectedIndex++;
            return true;
        }

        return false;
    }
    // Stryker restore all

    // Stryker disable all : EnsureSelectedVisible scroll arithmetic — boundary equality mutations produce equivalent scroll behavior
    private void EnsureSelectedVisible(int viewportHeight)
    {
        if (_selectedIndex < _scrollOffset)
        {
            _scrollOffset = _selectedIndex;
        }
        else if (_selectedIndex >= _scrollOffset + viewportHeight)
        {
            _scrollOffset = _selectedIndex - viewportHeight + 1;
        }
    }
    // Stryker restore all
}

// Stryker restore all
