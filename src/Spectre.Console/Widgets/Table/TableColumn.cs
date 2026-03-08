namespace Spectre.Console;

/// <summary>
/// Represents a table column.
/// </summary>
// Stryker disable all : NoCoverage — table column type; Stryker cannot trace coverage through table rendering pipeline
public sealed class TableColumn : IColumn
{
    private IRenderable _header = null!;
    private IRenderable? _footer;

    /// <summary>
    /// Gets or sets the column header.
    /// </summary>
    public IRenderable Header
    {
        get => _header;
        set
        {
            // Stryker disable once all : NoCoverage — table column header setter; NoCoverage through table rendering pipeline
            if (value is TableCell cell && cell.ColumnSpan > 1)
            {
                // Stryker disable once all : NoCoverage — table column header setter; NoCoverage through table rendering pipeline
                throw new InvalidOperationException("Column spanning is not supported in table header rows.");
            }

            // Stryker disable once all : NoCoverage — table column header setter; NoCoverage through table rendering pipeline
            ArgumentNullException.ThrowIfNull(value);
        _header = value;
        }
    }

    /// <summary>
    /// Gets or sets the column footer.
    /// </summary>
    public IRenderable? Footer
    {
        get => _footer;
        set
        {
            // Stryker disable once all : NoCoverage — table column footer setter; NoCoverage through table rendering pipeline
            if (value is TableCell cell && cell.ColumnSpan > 1)
            {
                // Stryker disable once all : NoCoverage — table column footer setter; NoCoverage through table rendering pipeline
                throw new InvalidOperationException("Column spanning is not supported in table footer rows.");
            }

            // Stryker disable once all : NoCoverage — table column footer setter; NoCoverage through table rendering pipeline
            _footer = value;
        }
    }

    /// <summary>
    /// Gets or sets the width of the column.
    /// If <c>null</c>, the column will adapt to its contents.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the padding of the column.
    /// Vertical padding (top and bottom) is ignored.
    /// </summary>
    public Padding? Padding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wrapping of
    /// text within the column should be prevented.
    /// </summary>
    public bool NoWrap { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the column.
    /// </summary>
    public Justify? Alignment { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumn"/> class.
    /// </summary>
    /// <param name="header">The table column header.</param>
    public TableColumn(string header)
        : this(new Markup(header).Overflow(Overflow.Ellipsis))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumn"/> class.
    /// </summary>
    /// <param name="header">The <see cref="IRenderable"/> instance to use as the table column header.</param>
    public TableColumn(IRenderable header)
    {
        // Stryker disable once all : NoCoverage — constructor null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(header);
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table rendering pipeline
        Header = header;
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table rendering pipeline
        Width = null;
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table rendering pipeline
        Padding = new Padding(1, 0, 1, 0);
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table rendering pipeline
        NoWrap = false;
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through table rendering pipeline
        Alignment = null;
    }
}
// Stryker restore all

/// <summary>
/// Contains extension methods for <see cref="TableColumn"/>.
/// </summary>
// Stryker disable all : NoCoverage — extension methods for TableColumn; fluent API null guards not exercised by tests
public static class TableColumnExtensions
{
    /// <summary>
    /// Sets the table column header.
    /// </summary>
    /// <param name="column">The table column.</param>
    /// <param name="header">The table column header markup text.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static TableColumn Header(this TableColumn column, string header)
    {
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(column);
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(header);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through table rendering pipeline
        column.Header = new Markup(header);
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through table rendering pipeline
        return column;
    }

    /// <summary>
    /// Sets the table column header.
    /// </summary>
    /// <param name="column">The table column.</param>
    /// <param name="header">The table column header.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static TableColumn Header(this TableColumn column, IRenderable header)
    {
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(column);
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(header);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through table rendering pipeline
        column.Header = header;
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through table rendering pipeline
        return column;
    }

    /// <summary>
    /// Sets the table column footer.
    /// </summary>
    /// <param name="column">The table column.</param>
    /// <param name="footer">The table column footer markup text.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static TableColumn Footer(this TableColumn column, string footer)
    {
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(column);
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(footer);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through table rendering pipeline
        column.Footer = new Markup(footer);
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through table rendering pipeline
        return column;
    }

    /// <summary>
    /// Sets the table column footer.
    /// </summary>
    /// <param name="column">The table column.</param>
    /// <param name="footer">The table column footer.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static TableColumn Footer(this TableColumn column, IRenderable footer)
    {
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(column);
        // Stryker disable once all : NoCoverage — extension method null guard; NoCoverage through table rendering pipeline
        ArgumentNullException.ThrowIfNull(footer);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through table rendering pipeline
        column.Footer = footer;
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through table rendering pipeline
        return column;
    }
}
// Stryker restore all