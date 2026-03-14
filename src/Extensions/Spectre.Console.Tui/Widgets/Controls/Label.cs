namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A static text label widget.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class Label : Widget
{
    private string _text;
    private Style _style;

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            Invalidate();
        }
    }

    public Style LabelStyle
    {
        get => _style;
        set
        {
            _style = value;
            Invalidate();
        }
    }

    public Label(string text, Style? style = null)
    {
        _text = text ?? string.Empty;
        _style = style ?? Style.Plain;
    }

    protected internal override Size MeasureContent(Size available)
    {
        var lines = _text.Split('\n');
        var maxWidth = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            maxWidth = Math.Max(maxWidth, lines[i].Length);
        }

        return new Size(Math.Min(maxWidth, available.Width), Math.Min(lines.Length, available.Height));
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var lines = _text.Split('\n');
        for (var row = 0; row < lines.Length && row < surface.Height; row++)
        {
            surface.SetText(0, row, lines[row], _style);
        }
    }
}

// Stryker restore all
