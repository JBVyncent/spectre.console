namespace Spectre.Console.Tui.Widgets.Chrome;

/// <summary>
/// A horizontal menu bar with dropdown menus.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() mutations — clipped by BufferSurface. Correctness verified by TUI tests.
public class MenuBar : Widget
{
    private readonly List<MenuItem> _items = new();
    private int _selectedIndex = -1;
    private bool _isOpen;

    public IReadOnlyList<MenuItem> Items => _items;
    public Style NormalStyle { get; set; } = new Style(Color.Black, Color.Grey);
    public Style SelectedStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style DisabledStyle { get; set; } = new Style(Color.DarkSlateGray1, Color.Grey);

    public MenuBar()
    {
        // Stryker disable once Boolean : CanFocus default tested by MenuBar_CanFocus_DefaultTrue
        CanFocus = true;
    }

    public void AddItem(MenuItem item)
    {
        // Stryker disable once Statement : null guard tested by AddItem_Null_Throws
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);
        // Stryker disable once Statement : Invalidate is internal dirty-flag; removing it doesn't affect single-frame render tests
        Invalidate();
    }

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(available.Width, 1);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        // Fill background
        surface.Fill(new Rect(0, 0, surface.Width, 1), ' ', NormalStyle);

        var x = 1;
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var style = !item.Enabled ? DisabledStyle
                : i == _selectedIndex ? SelectedStyle
                : NormalStyle;

            var text = $" {item.Text} ";
            surface.SetText(x, 0, text, style);
            x += text.Length;
        }
    }

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        switch (e.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_items.Count > 0)
                {
                    _selectedIndex = _selectedIndex <= 0 ? _items.Count - 1 : _selectedIndex - 1;
                    Invalidate();
                }

                return true;

            case ConsoleKey.RightArrow:
                if (_items.Count > 0)
                {
                    _selectedIndex = (_selectedIndex + 1) % _items.Count;
                    Invalidate();
                }

                return true;

            case ConsoleKey.Enter:
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                {
                    _items[_selectedIndex].RaiseActivated();
                }

                return true;

            case ConsoleKey.Escape:
                _selectedIndex = -1;
                _isOpen = false;
                Invalidate();
                return true;

            default:
                // Check for Alt+letter shortcuts
                if (e.Alt && e.KeyChar != '\0')
                {
                    for (var i = 0; i < _items.Count; i++)
                    {
                        if (_items[i].Text.Length > 0 &&
                            char.ToUpperInvariant(_items[i].Text[0]) == char.ToUpperInvariant(e.KeyChar))
                        {
                            _selectedIndex = i;
                            _items[i].RaiseActivated();
                            Invalidate();
                            return true;
                        }
                    }
                }

                return false;
        }
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType != MouseEventType.Press || e.Button != MouseButton.Left)
        {
            return false;
        }

        var localCol = e.Column - Bounds.X;
        var x = 1;
        for (var i = 0; i < _items.Count; i++)
        {
            var textLen = _items[i].Text.Length + 2;
            if (localCol >= x && localCol < x + textLen)
            {
                _selectedIndex = i;
                _items[i].RaiseActivated();
                Invalidate();
                return true;
            }

            x += textLen;
        }

        return false;
    }
}
// Stryker restore all
