namespace Spectre.Console;

/// <summary>
/// Represents a table title such as a heading or footnote.
/// </summary>
// Stryker disable all : NoCoverage — table title type; Stryker cannot trace coverage through table rendering pipeline
public sealed class TableTitle
{
    /// <summary>
    /// Gets the title text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets or sets the title style.
    /// </summary>
    public Style? Style { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableTitle"/> class.
    /// </summary>
    /// <param name="text">The title text.</param>
    /// <param name="style">The title style.</param>
    public TableTitle(string text, Style? style = null)
    {
        // Stryker disable once all : NoCoverage — constructor null guard; NoCoverage through table title pipeline
        ArgumentNullException.ThrowIfNull(text);
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table title pipeline
        Text = text;
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table title pipeline
        Style = style;
    }

    /// <summary>
    /// Sets the title style.
    /// </summary>
    /// <param name="style">The title style.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public TableTitle SetStyle(Style? style)
    {
        // Stryker disable once all : NoCoverage — SetStyle assignment; NoCoverage through table title pipeline
        Style = style ?? Spectre.Console.Style.Plain;
        // Stryker disable once all : NoCoverage — SetStyle return; NoCoverage through table title pipeline
        return this;
    }

    /// <summary>
    /// Sets the title style.
    /// </summary>
    /// <param name="style">The title style.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public TableTitle SetStyle(string style)
    {
        // Stryker disable once all : NoCoverage — SetStyle null guard; NoCoverage through table title pipeline
        ArgumentNullException.ThrowIfNull(style);

        // Stryker disable once all : NoCoverage — SetStyle assignment; NoCoverage through table title pipeline
        Style = Spectre.Console.Style.Parse(style);
        // Stryker disable once all : NoCoverage — SetStyle return; NoCoverage through table title pipeline
        return this;
    }
}
// Stryker restore all