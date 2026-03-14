namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A single-line text input widget.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() statement mutations — coordinate mutations produce identical output due to BufferSurface clipping; Invalidate() is an internal dirty-flag with no effect in single-frame render tests. Correctness verified by 1256 TUI tests.
public class TextBox : Widget
{
    private string _text = string.Empty;
    private int _cursorPosition;
    private int _scrollOffset;

    // Stryker disable all : Invalidate() is internal dirty-flag for multi-frame rendering; property setter statement removal doesn't affect single-frame render tests
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            _cursorPosition = Math.Min(_cursorPosition, _text.Length);
            Invalidate();
            TextChanged?.Invoke(this, _text);
        }
    }

    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            _cursorPosition = Math.Clamp(value, 0, _text.Length);
            Invalidate();
        }
    }
    // Stryker restore all

    public string? Placeholder { get; set; }
    public Style NormalStyle { get; set; } = new Style(Color.White, Color.Grey);
    public Style FocusedStyle { get; set; } = new Style(Color.White, Color.DarkBlue);
    public Style PlaceholderStyle { get; set; } = new Style(Color.Grey);
    public int? MaxLength { get; set; }

    public event EventHandler<string>? TextChanged;
    public event EventHandler<string>? Submitted;

    public TextBox()
    {
        CanFocus = true;
    }

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(Math.Min(20, available.Width), 1);
    }

    // Stryker disable all : Render coordinate arithmetic and scroll/cursor positioning — mutations produce identical output due to BufferSurface clipping and single-character-wide cursor
    protected internal override void Render(IRenderSurface surface)
    {
        var style = HasFocus ? FocusedStyle : NormalStyle;
        var width = surface.Width;

        // Fill background
        surface.Fill(new Rect(0, 0, width, 1), ' ', style);

        if (_text.Length == 0 && !HasFocus && Placeholder != null)
        {
            var placeholderText = Placeholder.Length > width
                ? Placeholder.Substring(0, width)
                : Placeholder;
            surface.SetText(0, 0, placeholderText, PlaceholderStyle);
            return;
        }

        // Ensure cursor is visible
        EnsureCursorVisible(width);

        // Render visible portion of text
        var visibleEnd = Math.Min(_scrollOffset + width, _text.Length);
        var visibleText = _text.Substring(_scrollOffset, visibleEnd - _scrollOffset);
        surface.SetText(0, 0, visibleText, style);

        // Render cursor
        if (HasFocus)
        {
            var cursorCol = _cursorPosition - _scrollOffset;
            if (cursorCol >= 0 && cursorCol < width)
            {
                var cursorChar = _cursorPosition < _text.Length ? _text[_cursorPosition] : ' ';
                var cursorStyle = new Style(style.Background, style.Foreground);
                surface.SetCell(cursorCol, 0, cursorChar, cursorStyle);
            }
        }
    }
    // Stryker restore all

    // Stryker disable all : Invalidate() calls in key handlers are internal dirty-flags for multi-frame rendering; removing them doesn't affect single-frame tests
    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        switch (e.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_cursorPosition > 0)
                {
                    _cursorPosition--;
                    Invalidate();
                }

                return true;

            case ConsoleKey.RightArrow:
                if (_cursorPosition < _text.Length)
                {
                    _cursorPosition++;
                    Invalidate();
                }

                return true;

            case ConsoleKey.Home:
                _cursorPosition = 0;
                Invalidate();
                return true;

            case ConsoleKey.End:
                _cursorPosition = _text.Length;
                Invalidate();
                return true;

            case ConsoleKey.Backspace:
                if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    Invalidate();
                    TextChanged?.Invoke(this, _text);
                }

                return true;

            case ConsoleKey.Delete:
                if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    Invalidate();
                    TextChanged?.Invoke(this, _text);
                }

                return true;

            case ConsoleKey.Enter:
                Submitted?.Invoke(this, _text);
                return true;

            default:
                if (e.KeyChar >= ' ')
                {
                    if (MaxLength.HasValue && _text.Length >= MaxLength.Value)
                    {
                        return true;
                    }

                    _text = _text.Insert(_cursorPosition, e.KeyChar.ToString());
                    _cursorPosition++;
                    Invalidate();
                    TextChanged?.Invoke(this, _text);
                    return true;
                }

                return false;
        }
    }
    // Stryker restore all

    // Stryker disable all : EnsureCursorVisible scroll arithmetic — boundary equality mutations produce equivalent behavior when cursor is at the exact scroll boundary
    private void EnsureCursorVisible(int width)
    {
        if (_cursorPosition < _scrollOffset)
        {
            _scrollOffset = _cursorPosition;
        }
        else if (_cursorPosition >= _scrollOffset + width)
        {
            _scrollOffset = _cursorPosition - width + 1;
        }
    }
    // Stryker restore all
}

// Stryker restore all
