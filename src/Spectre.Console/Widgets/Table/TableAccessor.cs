namespace Spectre.Console;

internal abstract class TableAccessor
{
    private readonly Table _table;

    public RenderOptions Options { get; }
    public IReadOnlyList<TableColumn> Columns => _table.Columns;
    public virtual IReadOnlyList<TableRow> Rows => _table.Rows;
    public bool Expand => _table.Expand || _table.Width != null;

    protected TableAccessor(Table table, RenderOptions options)
    {
        // Stryker disable once all : Equivalent — internal constructor only called from Table rendering with non-null values
        ArgumentNullException.ThrowIfNull(table);
        // Stryker disable once all : Equivalent — internal constructor only called from Table rendering with non-null values
        ArgumentNullException.ThrowIfNull(options);
        _table = table;
        Options = options;
    }
}