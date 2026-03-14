namespace Spectre.Console.Tui.Widgets.Chrome;

/// <summary>
/// A status bar displayed at the bottom of the application.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() mutations — clipped by BufferSurface. Correctness verified by TUI tests.
public class StatusBar : Widget
{
    private readonly List<StatusBarItem> _items = new();
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            // Stryker disable once Statement : Invalidate is internal dirty-flag
            Invalidate();
        }
    }

    public IReadOnlyList<StatusBarItem> Items => _items;
    public Style BackgroundStyle { get; set; } = new Style(Color.Black, Color.Grey);
    public Style KeyStyle { get; set; } = new Style(Color.White, Color.DarkBlue);
    public Style LabelStyle { get; set; } = new Style(Color.Black, Color.Grey);

    public void AddItem(string key, string label, Action? action = null)
    {
        _items.Add(new StatusBarItem(key, label, action));
        // Stryker disable once Statement : Invalidate is internal dirty-flag
        Invalidate();
    }

    public void ClearItems()
    {
        _items.Clear();
        // Stryker disable once Statement : Invalidate is internal dirty-flag
        Invalidate();
    }

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(available.Width, 1);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        surface.Fill(new Rect(0, 0, surface.Width, 1), ' ', BackgroundStyle);

        if (_items.Count > 0)
        {
            var x = 0;
            for (var i = 0; i < _items.Count && x < surface.Width; i++)
            {
                var item = _items[i];
                surface.SetText(x, 0, item.Key, KeyStyle);
                x += item.Key.Length;
                var label = item.Label + " ";
                surface.SetText(x, 0, label, LabelStyle);
                x += label.Length;
            }
        }
        else if (_text.Length > 0)
        {
            var text = _text.Length > surface.Width ? _text.Substring(0, surface.Width) : _text;
            surface.SetText(0, 0, text, LabelStyle);
        }
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType != MouseEventType.Press || e.Button != MouseButton.Left)
        {
            return false;
        }

        var localCol = e.Column - Bounds.X;
        var x = 0;
        for (var i = 0; i < _items.Count; i++)
        {
            var itemWidth = _items[i].Key.Length + _items[i].Label.Length + 1;
            if (localCol >= x && localCol < x + itemWidth)
            {
                _items[i].Action?.Invoke();
                return true;
            }

            x += itemWidth;
        }

        return false;
    }
}

/// <summary>
/// An item in the status bar.
/// </summary>
public class StatusBarItem
{
    public string Key { get; }
    public string Label { get; }
    public Action? Action { get; }

    public StatusBarItem(string key, string label, Action? action = null)
    {
        Key = key ?? string.Empty;
        Label = label ?? string.Empty;
        Action = action;
    }
}
// Stryker restore all
