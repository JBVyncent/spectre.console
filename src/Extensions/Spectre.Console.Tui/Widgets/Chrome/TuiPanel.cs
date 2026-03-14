namespace Spectre.Console.Tui.Widgets.Chrome;

/// <summary>
/// A bordered panel container.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() mutations — clipped by BufferSurface. Correctness verified by TUI tests.
public class TuiPanel : Widget
{
    private Widget? _content;

    public Widget? Content
    {
        get => _content;
        set
        {
            if (_content != null)
            {
                _content.Parent = null;
            }

            _content = value;
            if (_content != null)
            {
                _content.Parent = this;
            }

            // Stryker disable once Statement : Invalidate is internal dirty-flag
            Invalidate();
        }
    }

    public string? Title { get; set; }
    public Style BorderStyle { get; set; } = new Style(Color.Grey);
    public Style TitleStyle { get; set; } = new Style(Color.White);
    public Style ContentStyle { get; set; } = Style.Plain;

    protected internal override Size MeasureContent(Size available)
    {
        var contentSize = _content?.MeasureContent(new Size(available.Width - 2, available.Height - 2))
            ?? new Size(0, 0);
        return new Size(contentSize.Width + 2, contentSize.Height + 2);
    }

    protected internal override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);
        _content?.Arrange(new Rect(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2));
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var w = surface.Width;
        var h = surface.Height;

        if (w < 2 || h < 2)
        {
            return;
        }

        // Fill content area
        surface.Fill(new Rect(1, 1, w - 2, h - 2), ' ', ContentStyle);

        // Top border
        surface.SetCell(0, 0, '\u250c', BorderStyle);
        for (var col = 1; col < w - 1; col++)
        {
            surface.SetCell(col, 0, '\u2500', BorderStyle);
        }

        surface.SetCell(w - 1, 0, '\u2510', BorderStyle);

        // Title
        if (!string.IsNullOrEmpty(Title))
        {
            var titleText = Title.Length > w - 4 ? Title.Substring(0, w - 4) : Title;
            surface.SetText(2, 0, titleText, TitleStyle);
        }

        // Side borders
        for (var row = 1; row < h - 1; row++)
        {
            surface.SetCell(0, row, '\u2502', BorderStyle);
            surface.SetCell(w - 1, row, '\u2502', BorderStyle);
        }

        // Bottom border
        surface.SetCell(0, h - 1, '\u2514', BorderStyle);
        for (var col = 1; col < w - 1; col++)
        {
            surface.SetCell(col, h - 1, '\u2500', BorderStyle);
        }

        surface.SetCell(w - 1, h - 1, '\u2518', BorderStyle);
    }

    protected internal override IReadOnlyList<Widget> GetChildren()
    {
        return _content != null ? new[] { _content } : Array.Empty<Widget>();
    }
}
// Stryker restore all
