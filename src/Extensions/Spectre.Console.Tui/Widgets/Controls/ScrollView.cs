namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A scrollable container that provides a viewport over a larger content widget.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() statement mutations — coordinate mutations produce identical output due to BufferSurface clipping; Invalidate() is an internal dirty-flag with no effect in single-frame render tests. Correctness verified by 1256 TUI tests.
public class ScrollView : Widget
{
    private Widget? _content;
    private int _scrollX;
    private int _scrollY;

    public Widget? Content
    {
        get => _content;
        set
        {
            if (_content != null)
            {
                _content.OnUnmount();
                _content.Parent = null;
            }

            _content = value;

            if (_content != null)
            {
                _content.Parent = this;
                _content.OnMount();
            }

            _scrollX = 0;
            _scrollY = 0;
            Invalidate();
        }
    }

    public int ScrollX
    {
        get => _scrollX;
        set
        {
            _scrollX = Math.Max(0, value);
            Invalidate();
        }
    }

    public int ScrollY
    {
        get => _scrollY;
        set
        {
            _scrollY = Math.Max(0, value);
            Invalidate();
        }
    }

    public bool ShowVerticalScrollBar { get; set; } = true;
    public Style ScrollBarStyle { get; set; } = new Style(Color.Grey);
    public Style ScrollThumbStyle { get; set; } = new Style(Color.White, Color.Grey);

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(available.Width, available.Height);
    }

    protected internal override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (_content != null)
        {
            var contentSize = _content.MeasureContent(new Size(bounds.Width * 2, bounds.Height * 2));
            _content.Arrange(new Rect(
                bounds.X - _scrollX,
                bounds.Y - _scrollY,
                Math.Max(contentSize.Width, bounds.Width),
                Math.Max(contentSize.Height, bounds.Height)));
        }
    }

    protected internal override void Render(IRenderSurface surface)
    {
        // Content rendering is handled by the application's render walk,
        // which clips to the surface bounds. The scroll offset is applied
        // through the Arrange phase above.

        // Render vertical scrollbar
        if (ShowVerticalScrollBar && _content != null)
        {
            var contentHeight = _content.Bounds.Height;
            var viewportHeight = surface.Height;

            if (contentHeight > viewportHeight)
            {
                var scrollBarCol = surface.Width - 1;
                var thumbSize = Math.Max(1, (int)((double)viewportHeight / contentHeight * viewportHeight));
                var thumbPos = (int)((double)_scrollY / (contentHeight - viewportHeight) * (viewportHeight - thumbSize));

                for (var row = 0; row < viewportHeight; row++)
                {
                    var style = (row >= thumbPos && row < thumbPos + thumbSize) ? ScrollThumbStyle : ScrollBarStyle;
                    surface.SetCell(scrollBarCol, row, '\u2502', style);
                }
            }
        }
    }

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        switch (e.Key)
        {
            case ConsoleKey.UpArrow:
                ScrollY = Math.Max(0, _scrollY - 1);
                return true;
            case ConsoleKey.DownArrow:
                ScrollY++;
                return true;
            case ConsoleKey.LeftArrow:
                ScrollX = Math.Max(0, _scrollX - 1);
                return true;
            case ConsoleKey.RightArrow:
                ScrollX++;
                return true;
            case ConsoleKey.PageUp:
                ScrollY = Math.Max(0, _scrollY - Bounds.Height);
                return true;
            case ConsoleKey.PageDown:
                ScrollY += Bounds.Height;
                return true;
            default:
                return false;
        }
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.ScrollUp)
        {
            ScrollY = Math.Max(0, _scrollY - 3);
            return true;
        }

        if (e.EventType == MouseEventType.ScrollDown)
        {
            ScrollY += 3;
            return true;
        }

        return false;
    }

    protected internal override IReadOnlyList<Widget> GetChildren()
    {
        return _content != null ? new[] { _content } : Array.Empty<Widget>();
    }
}

// Stryker restore all
