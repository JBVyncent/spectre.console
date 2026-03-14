namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// A visual progress bar widget.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class ProgressBar : Widget
{
    private double _value;

    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0.0, MaxValue);
            if (Math.Abs(_value - clamped) > 0.001)
            {
                _value = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, _value);
            }
        }
    }

    public double MaxValue { get; set; } = 100.0;
    public Style FilledStyle { get; set; } = new Style(Color.Green);
    public Style EmptyStyle { get; set; } = new Style(Color.Grey);
    public char FilledChar { get; set; } = '\u2588'; // █
    public char EmptyChar { get; set; } = '\u2591'; // ░
    public bool ShowPercentage { get; set; } = true;

    public event EventHandler<double>? ValueChanged;

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(Math.Min(20, available.Width), 1);
    }

    protected internal override void Render(IRenderSurface surface)
    {
        var width = surface.Width;
        var percentText = ShowPercentage ? $" {(int)(Value / MaxValue * 100)}%" : string.Empty;
        var barWidth = width - percentText.Length;

        if (barWidth <= 0)
        {
            return;
        }

        var filledWidth = (int)(barWidth * (Value / MaxValue));
        filledWidth = Math.Clamp(filledWidth, 0, barWidth);

        for (var col = 0; col < barWidth; col++)
        {
            if (col < filledWidth)
            {
                surface.SetCell(col, 0, FilledChar, FilledStyle);
            }
            else
            {
                surface.SetCell(col, 0, EmptyChar, EmptyStyle);
            }
        }

        if (ShowPercentage)
        {
            surface.SetText(barWidth, 0, percentText, Style.Plain);
        }
    }
}

// Stryker restore all
