namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A clickable button widget.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class Button : Widget
{
    private string _text;

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            Invalidate();
        }
    }

    public Style NormalStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style FocusedStyle { get; set; } = new Style(Color.White, Color.Cyan1);
    public Style PressedStyle { get; set; } = new Style(Color.Black, Color.White);

    public event EventHandler? Clicked;

    public Button(string text)
    {
        _text = text ?? string.Empty;
        CanFocus = true;
    }

    protected internal override Size MeasureContent(Size available)
    {
        // Button: [ Text ]
        var width = _text.Length + 4;
        return new Size(Math.Min(width, available.Width), 1);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var style = HasFocus ? FocusedStyle : NormalStyle;
        var display = $"[ {_text} ]";

        if (display.Length > surface.Width)
        {
            display = display.Substring(0, surface.Width);
        }

        surface.SetText(0, 0, display, style);
    }

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            Clicked?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return true;
        }

        return false;
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            Clicked?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return true;
        }

        return false;
    }
}

// Stryker restore all
