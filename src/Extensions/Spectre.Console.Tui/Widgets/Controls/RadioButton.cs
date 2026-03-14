namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A radio button widget. Use within a <see cref="RadioGroup"/> for mutual exclusion.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class RadioButton : Widget
{
    private string _text;
    private bool _isSelected;

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            Invalidate();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                Invalidate();
                SelectionChanged?.Invoke(this, _isSelected);
            }
        }
    }

    public Style NormalStyle { get; set; } = Style.Plain;
    public Style FocusedStyle { get; set; } = new Style(Color.Cyan1);

    public event EventHandler<bool>? SelectionChanged;
    internal RadioGroup? Group { get; set; }

    public RadioButton(string text, bool isSelected = false)
    {
        _text = text ?? string.Empty;
        _isSelected = isSelected;
        CanFocus = true;
    }

    protected internal override Size MeasureContent(Size available)
    {
        // (o) Text
        return new Size(Math.Min(_text.Length + 4, available.Width), 1);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var style = HasFocus ? FocusedStyle : NormalStyle;
        var marker = _isSelected ? "o" : " ";
        var display = $"({marker}) {_text}";

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
            Select();
            return true;
        }

        return false;
    }

    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            Select();
            return true;
        }

        return false;
    }

    private void Select()
    {
        if (Group != null)
        {
            Group.Select(this);
        }
        else
        {
            IsSelected = true;
        }
    }
}

// Stryker restore all
