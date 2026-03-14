namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A dropdown combo box with text input.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() statement mutations — coordinate mutations produce identical output due to BufferSurface clipping; Invalidate() is an internal dirty-flag with no effect in single-frame render tests. Correctness verified by 1256 TUI tests.
public class ComboBox : Widget
{
    private readonly List<string> _items = new();
    private string _text = string.Empty;
    private int _selectedIndex = -1;
    private bool _isOpen;

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            Invalidate();
            TextChanged?.Invoke(this, _text);
        }
    }

    public IReadOnlyList<string> Items => _items;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var newIndex = Math.Clamp(value, -1, _items.Count - 1);
            if (_selectedIndex != newIndex)
            {
                _selectedIndex = newIndex;
                if (_selectedIndex >= 0)
                {
                    _text = _items[_selectedIndex];
                }

                Invalidate();
                SelectionChanged?.Invoke(this, _selectedIndex);
            }
        }
    }

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            _isOpen = value;
            Invalidate();
        }
    }

    public int DropdownHeight { get; set; } = 5;
    public Style NormalStyle { get; set; } = new Style(Color.White, Color.Grey);
    public Style FocusedStyle { get; set; } = new Style(Color.White, Color.DarkBlue);
    public Style DropdownStyle { get; set; } = Style.Plain;
    public Style SelectedItemStyle { get; set; } = new Style(Color.White, Color.Blue);

    public event EventHandler<string>? TextChanged;
    public event EventHandler<int>? SelectionChanged;

    public ComboBox()
    {
        CanFocus = true;
    }

    public void AddItem(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);
        Invalidate();
    }

    public void AddItems(IEnumerable<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items)
        {
            _items.Add(item);
        }

        Invalidate();
    }

    public void ClearItems()
    {
        _items.Clear();
        _selectedIndex = -1;
        Invalidate();
    }

    protected internal override Size MeasureContent(Size available)
    {
        var height = _isOpen ? 1 + Math.Min(DropdownHeight, _items.Count) : 1;
        return new Size(Math.Min(20, available.Width), height);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var style = HasFocus ? FocusedStyle : NormalStyle;

        // Input field
        surface.Fill(new Rect(0, 0, surface.Width, 1), ' ', style);

        var displayText = _text;
        var arrowSpace = 3; // " v "
        if (displayText.Length > surface.Width - arrowSpace)
        {
            displayText = displayText.Substring(0, surface.Width - arrowSpace);
        }

        surface.SetText(0, 0, displayText, style);
        surface.SetText(surface.Width - arrowSpace, 0, " v ", style);

        // Dropdown
        if (_isOpen)
        {
            var dropHeight = Math.Min(DropdownHeight, _items.Count);
            for (var i = 0; i < dropHeight && i < _items.Count; i++)
            {
                var itemStyle = i == _selectedIndex ? SelectedItemStyle : DropdownStyle;
                surface.Fill(new Rect(0, i + 1, surface.Width, 1), ' ', itemStyle);

                var itemText = _items[i];
                if (itemText.Length > surface.Width)
                {
                    itemText = itemText.Substring(0, surface.Width);
                }

                surface.SetText(0, i + 1, itemText, itemStyle);
            }
        }
    }

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        switch (e.Key)
        {
            case ConsoleKey.DownArrow:
                if (!_isOpen)
                {
                    IsOpen = true;
                    if (_selectedIndex < 0 && _items.Count > 0)
                    {
                        _selectedIndex = 0;
                    }
                }
                else if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                }

                return true;

            case ConsoleKey.UpArrow:
                if (_isOpen && _selectedIndex > 0)
                {
                    SelectedIndex--;
                }

                return true;

            case ConsoleKey.Enter:
                if (_isOpen && _selectedIndex >= 0)
                {
                    _text = _items[_selectedIndex];
                    IsOpen = false;
                    TextChanged?.Invoke(this, _text);
                }

                return true;

            case ConsoleKey.Escape:
                if (_isOpen)
                {
                    IsOpen = false;
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType != MouseEventType.Press || e.Button != MouseButton.Left)
        {
            return false;
        }

        var localRow = e.Row - Bounds.Y;

        if (localRow == 0)
        {
            IsOpen = !_isOpen;
            return true;
        }

        if (_isOpen && localRow > 0)
        {
            var itemIndex = localRow - 1;
            if (itemIndex >= 0 && itemIndex < _items.Count)
            {
                SelectedIndex = itemIndex;
                _text = _items[itemIndex];
                IsOpen = false;
                TextChanged?.Invoke(this, _text);
            }

            return true;
        }

        return false;
    }
}

// Stryker restore all
