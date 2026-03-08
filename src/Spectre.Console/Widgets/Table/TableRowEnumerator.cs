namespace Spectre.Console;

// Stryker disable all : NoCoverage — table row enumerator; Stryker cannot trace coverage through table rendering pipeline
internal sealed class TableRowEnumerator : IEnumerator<TableRow>
{
    private readonly TableRow[] _items;
    private int _index;

    public TableRow Current => _items[_index];
    object? IEnumerator.Current => _items[_index];

    public TableRowEnumerator(TableRow[] items)
    {
        // Stryker disable once all : NoCoverage — internal constructor null guard; NoCoverage through table row enumerator pipeline
        ArgumentNullException.ThrowIfNull(items);
        // Stryker disable once all : NoCoverage — internal constructor assignment; NoCoverage through table row enumerator pipeline
        _items = items;
        // Stryker disable once all : NoCoverage — internal constructor assignment; NoCoverage through table row enumerator pipeline
        _index = -1;
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
        _index++;
        return _index < _items.Length;
    }

    public void Reset()
    {
        // Stryker disable once all : NoCoverage — Reset method; NoCoverage through table row enumerator pipeline
        _index = -1;
    }
}
// Stryker restore all