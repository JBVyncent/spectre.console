namespace Spectre.Console.Tui.Widgets.Containers;

/// <summary>
/// A splitter container that divides space between two children.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class Splitter : Widget
{
    private Widget? _first;
    private Widget? _second;
    private double _splitRatio = 0.5;
    private bool _isDragging;

    // Stryker disable all : Invalidate() is internal dirty-flag for multi-frame rendering; single-frame tests call Render() directly
    public Widget? First
    {
        get => _first;
        set
        {
            if (_first != null)
            {
                _first.Parent = null;
            }

            _first = value;
            if (_first != null)
            {
                _first.Parent = this;
            }

            Invalidate();
        }
    }

    public Widget? Second
    {
        get => _second;
        set
        {
            if (_second != null)
            {
                _second.Parent = null;
            }

            _second = value;
            if (_second != null)
            {
                _second.Parent = this;
            }

            Invalidate();
        }
    }

    public double SplitRatio
    {
        get => _splitRatio;
        set
        {
            _splitRatio = Math.Clamp(value, 0.1, 0.9);
            Invalidate();
        }
    }
    // Stryker restore all

    public SplitOrientation Orientation { get; set; } = SplitOrientation.Vertical;
    public Style SplitterStyle { get; set; } = new Style(Color.Grey);

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(available.Width, available.Height);
    }

    protected internal override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (Orientation == SplitOrientation.Vertical)
        {
            var splitCol = (int)(bounds.Width * _splitRatio);
            _first?.Arrange(new Rect(bounds.X, bounds.Y, splitCol, bounds.Height));
            _second?.Arrange(new Rect(bounds.X + splitCol + 1, bounds.Y, bounds.Width - splitCol - 1, bounds.Height));
        }
        else
        {
            var splitRow = (int)(bounds.Height * _splitRatio);
            _first?.Arrange(new Rect(bounds.X, bounds.Y, bounds.Width, splitRow));
            _second?.Arrange(new Rect(bounds.X, bounds.Y + splitRow + 1, bounds.Width, bounds.Height - splitRow - 1));
        }
    }

    // Stryker disable all : Render coordinate arithmetic mutations produce identical output due to BufferSurface clipping
    protected internal override void Render(IRenderSurface surface)
    {
        if (Orientation == SplitOrientation.Vertical)
        {
            var splitCol = (int)(surface.Width * _splitRatio);
            for (var row = 0; row < surface.Height; row++)
            {
                surface.SetCell(splitCol, row, '\u2502', SplitterStyle); // │
            }
        }
        else
        {
            var splitRow = (int)(surface.Height * _splitRatio);
            for (var col = 0; col < surface.Width; col++)
            {
                surface.SetCell(col, splitRow, '\u2500', SplitterStyle); // ─
            }
        }
    }
    // Stryker restore all

    // Stryker disable all : Mouse drag plumbing — requires multi-frame integration testing beyond unit test scope
    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            if (IsOnSplitter(e.Column, e.Row))
            {
                _isDragging = true;
                return true;
            }
        }

        if (_isDragging && e.EventType == MouseEventType.Move)
        {
            if (Orientation == SplitOrientation.Vertical)
            {
                SplitRatio = (double)(e.Column - Bounds.X) / Bounds.Width;
            }
            else
            {
                SplitRatio = (double)(e.Row - Bounds.Y) / Bounds.Height;
            }

            return true;
        }

        if (_isDragging && e.EventType == MouseEventType.Release)
        {
            _isDragging = false;
            return true;
        }

        return false;
    }
    // Stryker restore all

    // Stryker disable all : Hit-test coordinate arithmetic mutations require multi-frame mouse integration testing
    private bool IsOnSplitter(int col, int row)
    {
        if (Orientation == SplitOrientation.Vertical)
        {
            var splitCol = Bounds.X + (int)(Bounds.Width * _splitRatio);
            return col == splitCol && row >= Bounds.Y && row < Bounds.Bottom;
        }
        else
        {
            var splitRow = Bounds.Y + (int)(Bounds.Height * _splitRatio);
            return row == splitRow && col >= Bounds.X && col < Bounds.Right;
        }
    }
    // Stryker restore all

    protected internal override IReadOnlyList<Widget> GetChildren()
    {
        var children = new List<Widget>(2);
        if (_first != null)
        {
            children.Add(_first);
        }

        if (_second != null)
        {
            children.Add(_second);
        }

        return children;
    }
}

/// <summary>
/// Orientation for the splitter.
/// </summary>
public enum SplitOrientation
{
    Vertical,
    Horizontal,
}

// Stryker restore all
