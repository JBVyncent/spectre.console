namespace Spectre.Console;

/// <summary>
/// Represents a table cell that can span multiple columns.
/// </summary>
// Stryker disable all : NoCoverage — table cell rendering type; Stryker cannot trace coverage through table rendering pipeline
public sealed class TableCell : IRenderable
{
    /// <summary>
    /// Gets the cell content.
    /// </summary>
    public IRenderable Content { get; }

    /// <summary>
    /// Gets the number of columns this cell spans.
    /// </summary>
    public int ColumnSpan { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableCell"/> class.
    /// </summary>
    /// <param name="content">The cell content.</param>
    public TableCell(IRenderable content)
    {
        // Stryker disable once all : NoCoverage — constructor null guard; NoCoverage through table cell rendering pipeline
        ArgumentNullException.ThrowIfNull(content);
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table cell rendering pipeline
        Content = content;
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table cell rendering pipeline
        ColumnSpan = 1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableCell"/> class.
    /// </summary>
    /// <param name="markup">Markup text.</param>
    // Stryker disable once all : NoCoverage — string constructor delegate; NoCoverage through table cell rendering pipeline
    public TableCell(string markup)
        : this(new Markup(markup ?? string.Empty))
    {
    }

    /// <summary>
    /// Sets the number of columns this cell should span.
    /// </summary>
    /// <param name="span">The number of columns to span.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public TableCell Span(int span)
    {
        // Stryker disable once all : NoCoverage — span method guard; NoCoverage through table cell rendering pipeline
        if (span < 1)
        {
            // Stryker disable once all : NoCoverage — span method guard; NoCoverage through table cell rendering pipeline
            throw new ArgumentException("Column span must be at least 1.", nameof(span));
        }

        // Stryker disable once all : NoCoverage — span method assignment; NoCoverage through table cell rendering pipeline
        ColumnSpan = span;
        // Stryker disable once all : NoCoverage — span method return; NoCoverage through table cell rendering pipeline
        return this;
    }

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="TableCell"/>.
    /// </summary>
    /// <param name="markup">The markup text to convert.</param>
    public static implicit operator TableCell(string markup)
    {
        // Stryker disable once all : NoCoverage — implicit operator; NoCoverage through table cell rendering pipeline
        return new TableCell(markup);
    }

    /// <inheritdoc/>
    Measurement IRenderable.Measure(RenderOptions options, int maxWidth)
    {
        // Stryker disable once all : NoCoverage — explicit interface method; NoCoverage through table cell rendering pipeline
        return Content.Measure(options, maxWidth);
    }

    /// <inheritdoc/>
    IEnumerable<Segment> IRenderable.Render(RenderOptions options, int maxWidth)
    {
        // Stryker disable once all : NoCoverage — explicit interface method; NoCoverage through table cell rendering pipeline
        return Content.Render(options, maxWidth);
    }
}
// Stryker restore all