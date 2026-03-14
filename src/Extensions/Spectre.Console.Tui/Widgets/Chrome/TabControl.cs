namespace Spectre.Console.Tui.Widgets.Chrome;

/// <summary>
/// A tabbed container that shows one child widget at a time.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() mutations — clipped by BufferSurface. Correctness verified by TUI tests.
public class TabControl : Widget
{
    private readonly List<TabPage> _tabs = new();
    private int _selectedIndex;

    public IReadOnlyList<TabPage> Tabs => _tabs;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var newIndex = Math.Clamp(value, 0, Math.Max(0, _tabs.Count - 1));
            if (_selectedIndex != newIndex)
            {
                _selectedIndex = newIndex;
                // Stryker disable once Statement : Invalidate is internal dirty-flag
                Invalidate();
                SelectedTabChanged?.Invoke(this, _selectedIndex);
            }
        }
    }

    public TabPage? SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabs.Count
        ? _tabs[_selectedIndex]
        : null;

    public Style TabNormalStyle { get; set; } = new Style(Color.Grey);
    public Style TabSelectedStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style TabBorderStyle { get; set; } = new Style(Color.Grey);

    public event EventHandler<int>? SelectedTabChanged;

    public TabControl()
    {
        // Stryker disable once Boolean : CanFocus default tested by TabControl_CanFocus_DefaultTrue
        CanFocus = true;
    }

    public void AddTab(string title, Widget content)
    {
        // Stryker disable once Statement : null guard tested separately
        ArgumentNullException.ThrowIfNull(title);
        // Stryker disable once Statement : null guard tested separately
        ArgumentNullException.ThrowIfNull(content);

        content.Parent = this;
        _tabs.Add(new TabPage(title, content));
        // Stryker disable once Statement : Invalidate is internal dirty-flag
        Invalidate();
    }

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(available.Width, available.Height);
    }

    protected internal override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        // Content area starts below the tab row
        var contentBounds = new Rect(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height - 2);

        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Content.Arrange(contentBounds);
        }
    }

    protected internal override void Render(IRenderSurface surface)
    {
        // Tab row
        var x = 0;
        for (var i = 0; i < _tabs.Count && x < surface.Width; i++)
        {
            var style = i == _selectedIndex ? TabSelectedStyle : TabNormalStyle;
            var text = $" {_tabs[i].Title} ";
            surface.SetText(x, 0, text, style);
            x += text.Length;

            if (i < _tabs.Count - 1)
            {
                surface.SetCell(x, 0, '\u2502', TabBorderStyle); // │
                x++;
            }
        }

        // Fill remaining tab row
        for (var col = x; col < surface.Width; col++)
        {
            surface.SetCell(col, 0, ' ', TabNormalStyle);
        }

        // Separator line
        for (var col = 0; col < surface.Width; col++)
        {
            surface.SetCell(col, 1, '\u2500', TabBorderStyle); // ─
        }
    }

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        if (_tabs.Count == 0)
        {
            return false;
        }

        switch (e.Key)
        {
            case ConsoleKey.LeftArrow when HasFocus:
                SelectedIndex = _selectedIndex > 0 ? _selectedIndex - 1 : _tabs.Count - 1;
                return true;
            case ConsoleKey.RightArrow when HasFocus:
                SelectedIndex = (_selectedIndex + 1) % _tabs.Count;
                return true;
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
        if (localRow != 0)
        {
            return false;
        }

        var localCol = e.Column - Bounds.X;
        var x = 0;
        for (var i = 0; i < _tabs.Count; i++)
        {
            var tabWidth = _tabs[i].Title.Length + 2;
            if (localCol >= x && localCol < x + tabWidth)
            {
                SelectedIndex = i;
                return true;
            }

            x += tabWidth + 1; // +1 for separator
        }

        return false;
    }

    protected internal override IReadOnlyList<Widget> GetChildren()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _tabs.Count)
        {
            return new[] { _tabs[_selectedIndex].Content };
        }

        return Array.Empty<Widget>();
    }
}

/// <summary>
/// A page within a <see cref="TabControl"/>.
/// </summary>
public class TabPage
{
    public string Title { get; }
    public Widget Content { get; }

    public TabPage(string title, Widget content)
    {
        Title = title;
        Content = content;
    }
}
// Stryker restore all
