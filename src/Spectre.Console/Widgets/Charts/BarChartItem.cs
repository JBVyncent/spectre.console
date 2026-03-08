namespace Spectre.Console;

/// <summary>
/// An item that's shown in a bar chart.
/// </summary>
public sealed class BarChartItem : IBarChartItem
{
    private string _label;

    /// <summary>
    /// Gets or sets the item label.
    /// </summary>
    public string Label
    {
        get => _label;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _label = value;
        }
    }

    /// <summary>
    /// Gets or sets the item value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the item color.
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChartItem"/> class.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="value">The item value.</param>
    /// <param name="color">The item color.</param>
    public BarChartItem(string label, double value, Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(label);
        _label = label;
        Value = value;
        Color = color;
    }
}
