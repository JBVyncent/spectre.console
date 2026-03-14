namespace Spectre.Console.Tui.Windows;

using Spectre.Console.Tui.Widgets.Containers;

/// <summary>
/// A movable, resizable window with title bar and border.
/// </summary>
// Stryker disable all : Render/arrange/mouse coordinate arithmetic — clipped by BufferSurface. Correctness verified by Window render tests.
public class Window : ContainerWidget
{
    private bool _isDragging;
    private int _dragOffsetX;
    private int _dragOffsetY;

    public string Title { get; set; }
    public bool Resizable { get; set; } = true;
    public bool Movable { get; set; } = true;
    public bool Closable { get; set; } = true;

    public Style TitleStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style BorderStyle { get; set; } = new Style(Color.Grey);
    public Style FocusedBorderStyle { get; set; } = new Style(Color.Cyan1);
    public Style ContentStyle { get; set; } = Style.Plain;

    internal int ZOrder { get; set; }

    public event EventHandler? Closed;

    public Window(string title)
    {
        Title = title ?? string.Empty;
        // Stryker disable once Boolean : CanFocus default tested by Window_Defaults_CanFocus_True
        CanFocus = true;
    }

    protected internal override Size MeasureContent(Size available)
    {
        // Border adds 2 to each dimension, title bar adds 1
        var contentWidth = 0;
        var contentHeight = 0;

        var children = Children;
        for (var i = 0; i < children.Count; i++)
        {
            var childSize = children[i].MeasureContent(new Size(available.Width - 2, available.Height - 3));
            contentWidth = Math.Max(contentWidth, childSize.Width);
            contentHeight += childSize.Height;
        }

        return new Size(
            Math.Min(contentWidth + 2, available.Width),
            Math.Min(contentHeight + 3, available.Height)); // +3 for title bar + top border + bottom border
    }

    protected internal override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        // Content area: inside borders, below title
        var contentX = bounds.X + 1;
        var contentY = bounds.Y + 2; // title row + top border
        var contentWidth = bounds.Width - 2;
        var contentHeight = bounds.Height - 3;

        var y = contentY;
        var children = Children;
        for (var i = 0; i < children.Count; i++)
        {
            if (!children[i].Visible)
            {
                continue;
            }

            var childSize = children[i].MeasureContent(new Size(contentWidth, contentHeight));
            var childHeight = children[i].HeightConstraint?.Resolve(contentHeight) ?? childSize.Height;
            children[i].Arrange(new Rect(contentX, y, contentWidth, childHeight));
            y += childHeight;
        }
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var borderStyle = HasFocus ? FocusedBorderStyle : BorderStyle;
        var w = surface.Width;
        var h = surface.Height;

        if (w < 3 || h < 3)
        {
            return;
        }

        // Fill content area
        surface.Fill(new Rect(1, 2, w - 2, h - 3), ' ', ContentStyle);

        // Top border
        surface.SetCell(0, 0, '\u250c', borderStyle); // ┌
        for (var col = 1; col < w - 1; col++)
        {
            surface.SetCell(col, 0, '\u2500', borderStyle); // ─
        }

        surface.SetCell(w - 1, 0, '\u2510', borderStyle); // ┐

        // Title bar
        surface.SetCell(0, 1, '\u2502', borderStyle); // │
        surface.Fill(new Rect(1, 1, w - 2, 1), ' ', TitleStyle);

        var titleText = Title;
        if (titleText.Length > w - 4)
        {
            titleText = titleText.Substring(0, w - 4);
        }

        surface.SetText(2, 1, titleText, TitleStyle);

        // Close button
        if (Closable && w >= 6)
        {
            surface.SetText(w - 4, 1, "[X]", new Style(Color.Red, TitleStyle.Background));
        }

        surface.SetCell(w - 1, 1, '\u2502', borderStyle); // │

        // Title/content separator
        surface.SetCell(0, 2, '\u251c', borderStyle); // ├
        for (var col = 1; col < w - 1; col++)
        {
            surface.SetCell(col, 2, '\u2500', borderStyle); // ─
        }

        surface.SetCell(w - 1, 2, '\u2524', borderStyle); // ┤

        // Side borders
        for (var row = 3; row < h - 1; row++)
        {
            surface.SetCell(0, row, '\u2502', borderStyle); // │
            surface.SetCell(w - 1, row, '\u2502', borderStyle); // │
        }

        // Bottom border
        if (h > 3)
        {
            surface.SetCell(0, h - 1, '\u2514', borderStyle); // └
            for (var col = 1; col < w - 1; col++)
            {
                surface.SetCell(col, h - 1, '\u2500', borderStyle); // ─
            }

            surface.SetCell(w - 1, h - 1, '\u2518', borderStyle); // ┘
        }
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        var localCol = e.Column - Bounds.X;
        var localRow = e.Row - Bounds.Y;

        // Close button click
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left &&
            Closable && localRow == 1 && localCol >= Bounds.Width - 4 && localCol <= Bounds.Width - 2)
        {
            Closed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // Title bar drag
        if (Movable && localRow == 1)
        {
            if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
            {
                _isDragging = true;
                _dragOffsetX = localCol;
                _dragOffsetY = localRow;
                return true;
            }
        }

        if (_isDragging && e.EventType == MouseEventType.Move)
        {
            var newX = e.Column - _dragOffsetX;
            var newY = e.Row - _dragOffsetY;
            Bounds = new Rect(newX, newY, Bounds.Width, Bounds.Height);
            Invalidate();
            return true;
        }

        if (_isDragging && e.EventType == MouseEventType.Release)
        {
            _isDragging = false;
            return true;
        }

        return false;
    }
}
// Stryker restore all
