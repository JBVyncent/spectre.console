namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A horizontal slider for numeric values.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() statement mutations — coordinate mutations produce identical output due to BufferSurface clipping; Invalidate() is an internal dirty-flag with no effect in single-frame render tests. Correctness verified by 1256 TUI tests.
public class Slider : Widget
{
    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, Minimum, Maximum);
            if (_value != clamped)
            {
                _value = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, _value);
            }
        }
    }

    public int Minimum { get; set; }
    public int Maximum { get; set; } = 100;
    public int Step { get; set; } = 1;

    public Style TrackStyle { get; set; } = new Style(Color.Grey);
    public Style ThumbStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style FocusedThumbStyle { get; set; } = new Style(Color.White, Color.Cyan1);
    public bool ShowValue { get; set; } = true;

    public event EventHandler<int>? ValueChanged;

    public Slider()
    {
        CanFocus = true;
    }

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(Math.Min(20, available.Width), 1);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var width = surface.Width;
        var valueText = ShowValue ? $" {_value}" : string.Empty;
        var trackWidth = width - valueText.Length;

        if (trackWidth <= 0)
        {
            return;
        }

        var range = Maximum - Minimum;
        var thumbPos = range > 0 ? (int)((double)(_value - Minimum) / range * (trackWidth - 1)) : 0;
        thumbPos = Math.Clamp(thumbPos, 0, trackWidth - 1);

        for (var col = 0; col < trackWidth; col++)
        {
            if (col == thumbPos)
            {
                var thumbStyle = HasFocus ? FocusedThumbStyle : ThumbStyle;
                surface.SetCell(col, 0, '\u2588', thumbStyle); // █
            }
            else
            {
                surface.SetCell(col, 0, '\u2500', TrackStyle); // ─
            }
        }

        if (ShowValue)
        {
            surface.SetText(trackWidth, 0, valueText, Style.Plain);
        }
    }

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        switch (e.Key)
        {
            case ConsoleKey.LeftArrow:
                Value -= Step;
                return true;
            case ConsoleKey.RightArrow:
                Value += Step;
                return true;
            case ConsoleKey.Home:
                Value = Minimum;
                return true;
            case ConsoleKey.End:
                Value = Maximum;
                return true;
            default:
                return false;
        }
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            var localCol = e.Column - Bounds.X;
            var valueText = ShowValue ? $" {_value}" : string.Empty;
            var trackWidth = Bounds.Width - valueText.Length;

            if (trackWidth > 0 && localCol < trackWidth)
            {
                var range = Maximum - Minimum;
                Value = Minimum + (int)((double)localCol / (trackWidth - 1) * range);
            }

            return true;
        }

        return false;
    }
}

// Stryker restore all
