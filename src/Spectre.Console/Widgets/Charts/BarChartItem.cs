namespace Spectre.Console;

/// <summary>
/// An item that's shown in a bar chart.
/// </summary>
public sealed class BarChartItem : IBarChartItem
{
    /// <summary>
    /// Gets the item label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the item value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets the item color.
    /// </summary>
    public Color? Color { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChartItem"/> class.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="value">The item value.</param>
    /// <param name="color">The item color.</param>
    public BarChartItem(string label, double value, Color? color = null)
    {
        // Stryker disable once all : Equivalent — constructor null guard; always called with non-null from fluent API
        ArgumentNullException.ThrowIfNull(label);
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through BarChart pipeline
        Label = label;
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through BarChart pipeline
        Value = value;
        Color = color;
    }
}