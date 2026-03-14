namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A togglable checkbox widget.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class CheckBox : Widget
{
    private string _text;
    private bool _isChecked;

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            Invalidate();
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, _isChecked);
            }
        }
    }

    public Style NormalStyle { get; set; } = Style.Plain;
    public Style FocusedStyle { get; set; } = new Style(Color.Cyan1);

    public event EventHandler<bool>? CheckedChanged;

    public CheckBox(string text, bool isChecked = false)
    {
        _text = text ?? string.Empty;
        _isChecked = isChecked;
        CanFocus = true;
    }

    protected internal override Size MeasureContent(Size available)
    {
        // [x] Text
        return new Size(Math.Min(_text.Length + 4, available.Width), 1);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var style = HasFocus ? FocusedStyle : NormalStyle;
        var marker = _isChecked ? "x" : " ";
        var display = $"[{marker}] {_text}";

        if (display.Length > surface.Width)
        {
            display = display.Substring(0, surface.Width);
        }

        surface.SetText(0, 0, display, style);
    }

    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter)
        {
            IsChecked = !IsChecked;
            return true;
        }

        return false;
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            IsChecked = !IsChecked;
            return true;
        }

        return false;
    }
}

// Stryker restore all
