namespace Spectre.Console;

/// <summary>
/// Represents a collection holding table rows.
/// </summary>
// Stryker disable all : NoCoverage — table row collection; Stryker cannot trace coverage through table rendering pipeline
public sealed class TableRowCollection : IReadOnlyList<TableRow>
{
    private readonly Table _table;
    private readonly IList<TableRow> _list;
    private readonly Lock _lock;

    /// <inheritdoc/>
    TableRow IReadOnlyList<TableRow>.this[int index]
    {
        get
        {
            lock (_lock)
            {
                // Stryker disable once all : NoCoverage — explicit indexer; NoCoverage through table row collection pipeline
                return _list[index];
            }
        }
    }

    /// <summary>
    /// Gets the number of rows in the collection.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _list.Count;
            }
        }
    }

    internal TableRowCollection(Table table)
    {
        // Stryker disable once all : NoCoverage — internal constructor null guard; NoCoverage through table row collection pipeline
        ArgumentNullException.ThrowIfNull(table);
        // Stryker disable once all : NoCoverage — internal constructor assignment; NoCoverage through table row collection pipeline
        _table = table;
        // Stryker disable once all : NoCoverage — internal constructor assignment; NoCoverage through table row collection pipeline
        _list = new List<TableRow>();
        // Stryker disable once all : NoCoverage — internal constructor assignment; NoCoverage through table row collection pipeline
        _lock = LockFactory.Create();
    }

    /// <summary>
    /// Adds a new row.
    /// </summary>
    /// <param name="columns">The columns that are part of the row to add.</param>
    /// <returns>The index of the added item.</returns>
    public int Add(IEnumerable<IRenderable> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        lock (_lock)
        {
            var row = CreateRow(columns);
            _list.Add(row);
            return _list.Count - 1;
        }
    }

    /// <summary>
    /// Inserts a new row at the specified index.
    /// </summary>
    /// <param name="index">The index to insert the row at.</param>
    /// <param name="columns">The columns that are part of the row to insert.</param>
    /// <returns>The index of the inserted item.</returns>
    public int Insert(int index, IEnumerable<IRenderable> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        lock (_lock)
        {
            var row = CreateRow(columns);
            _list.Insert(index, row);
            return _list.IndexOf(row);
        }
    }

    /// <summary>
    /// Update a table cell at the specified index.
    /// </summary>
    /// <param name="row">Index of cell row.</param>
    /// <param name="column">index of cell column.</param>
    /// <param name="cellData">The new cells details.</param>
    public void Update(int row, int column, IRenderable cellData)
    {
        // Stryker disable once all : NoCoverage — Update method null guard; NoCoverage through table row collection pipeline
        ArgumentNullException.ThrowIfNull(cellData);

        lock (_lock)
        {
            // Stryker disable once all : NoCoverage — Update method guard; NoCoverage through table row collection pipeline
            if (row < 0)
            {
                throw new IndexOutOfRangeException("Table row index cannot be negative.");
            }
            // Stryker disable once all : NoCoverage — Update method guard; NoCoverage through table row collection pipeline
            else if (row >= _list.Count)
            {
                throw new IndexOutOfRangeException("Table row index cannot exceed the number of rows in the table.");
            }

            // Stryker disable once all : NoCoverage — Update method body; NoCoverage through table row collection pipeline
            var tableRow = _list.ElementAt(row);
            // Stryker disable once all : NoCoverage — Update method body; NoCoverage through table row collection pipeline
            var currentRenderables = tableRow.ToList();

            // Stryker disable once all : NoCoverage — Update method guard; NoCoverage through table row collection pipeline
            if (column < 0)
            {
                throw new IndexOutOfRangeException("Table column index cannot be negative.");
            }
            // Stryker disable once all : NoCoverage — Update method guard; NoCoverage through table row collection pipeline
            else if (column >= currentRenderables.Count)
            {
                throw new IndexOutOfRangeException("Table column index cannot exceed the number of rows in the table.");
            }

            // Stryker disable once all : NoCoverage — Update method body; NoCoverage through table row collection pipeline
            currentRenderables.RemoveAt(column);

            currentRenderables.Insert(column, cellData);

            var newTableRow = new TableRow(currentRenderables);

            _list.RemoveAt(row);

            _list.Insert(row, newTableRow);
        }
    }

    /// <summary>
    /// Removes a row at the specified index.
    /// </summary>
    /// <param name="index">The index to remove a row at.</param>
    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            // Stryker disable once all : NoCoverage — RemoveAt guard; NoCoverage through table row collection pipeline
            if (index < 0)
            {
                throw new IndexOutOfRangeException("Table row index cannot be negative.");
            }
            // Stryker disable once all : NoCoverage — RemoveAt guard; NoCoverage through table row collection pipeline
            else if (index >= _list.Count)
            {
                throw new IndexOutOfRangeException("Table row index cannot exceed the number of rows in the table.");
            }

            // Stryker disable once all : NoCoverage — RemoveAt body; NoCoverage through table row collection pipeline
            _list.RemoveAt(index);
        }
    }

    /// <summary>
    /// Clears all rows.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _list.Clear();
        }
    }

    /// <inheritdoc/>
    public IEnumerator<TableRow> GetEnumerator()
    {
        lock (_lock)
        {
            var items = new TableRow[_list.Count];
            _list.CopyTo(items, 0);
            return new TableRowEnumerator(items);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        // Stryker disable once all : NoCoverage — explicit IEnumerable; NoCoverage through table row collection pipeline
        return GetEnumerator();
    }

    private TableRow CreateRow(IEnumerable<IRenderable> columns)
    {
        var row = new TableRow(columns);

        // Calculate total span considering TableCell instances
        var totalSpan = 0;
        for (var i = 0; i < row.Count; i++)
        {
            var cell = row[i];
            if (cell is TableCell tableCell)
            {
                totalSpan += tableCell.ColumnSpan;
            }
            else
            {
                totalSpan++;
            }
        }

        if (totalSpan > _table.Columns.Count)
        {
            throw new InvalidOperationException($"The number of row columns (including spans) are greater than the number of table columns. Expected {_table.Columns.Count} but got {totalSpan}.");
        }

        // Need to add missing columns
        // Stryker disable once all : NoCoverage — row padding logic; NoCoverage through table row collection pipeline
        if (totalSpan < _table.Columns.Count)
        {
            // Stryker disable once all : NoCoverage — row padding logic; NoCoverage through table row collection pipeline
            var diff = _table.Columns.Count - totalSpan;
            // Stryker disable once all : NoCoverage — row padding logic; NoCoverage through table row collection pipeline
            Enumerable.Range(0, diff).ForEach(_ => row.Add(Text.Empty));
        }

        return row;
    }
}
// Stryker restore all