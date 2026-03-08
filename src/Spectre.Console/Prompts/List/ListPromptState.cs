namespace Spectre.Console;

// Stryker disable all : NoCoverage — internal state class; exercised through interactive prompt pipeline, Stryker cannot trace coverage through async input loop
internal sealed class ListPromptState<T>
    where T : notnull
{
    private readonly Func<T, string> _converter;

    public int Index { get; private set; }
    public int ItemCount => Items.Count;
    public int PageSize { get; }
    public bool WrapAround { get; }
    public SelectionMode Mode { get; }
    public bool SkipUnselectableItems { get; private set; }
    public bool SearchEnabled { get; }
    public bool FilterEnabled { get; }
    public bool IsCancelled { get; private set; }
    public IReadOnlyList<ListPromptItem<T>> Items { get; }
    public string SearchText { get; private set; }

    public ListPromptItem<T> Current => Items[Index];

    // Leaf-only navigation table: indexes into Items of non-group entries.
    // Built when SkipUnselectableItems && Mode == Leaf.
    private readonly IReadOnlyList<int>? _leafIndexes;

    // Filter navigation table: indexes into Items of entries matching the current
    // SearchText. Built (and rebuilt) whenever FilterEnabled && SearchText changes.
    private List<int>? _filteredIndexes;

    public ListPromptState(
        IReadOnlyList<ListPromptItem<T>> items,
        Func<T, string> converter,
        int pageSize, bool wrapAround,
        SelectionMode mode,
        bool skipUnselectableItems,
        bool searchEnabled,
        bool filterEnabled,
        int? initialIndex = null)
    {
        // Stryker disable once all : Equivalent — internal class; converter is always non-null (passed from ListPrompt.Show via SelectionPrompt/MultiSelectionPrompt.ShowAsync)
        ArgumentNullException.ThrowIfNull(converter);
        _converter = converter;
        Items = items;
        PageSize = pageSize;
        WrapAround = wrapAround;
        Mode = mode;
        SkipUnselectableItems = skipUnselectableItems;
        SearchEnabled = searchEnabled;
        FilterEnabled = filterEnabled;
        SearchText = string.Empty;

        // Always build the leaf index table when needed for navigation, regardless of
        // whether an explicit initial index was supplied.
        if (SkipUnselectableItems && mode == SelectionMode.Leaf)
        {
            _leafIndexes =
                Items
                    .Select((item, index) => new { item, index })
                    .Where(x => !x.item.IsGroup)
                    .Select(x => x.index)
                    .ToList()
                    .AsReadOnly();
        }

        if (initialIndex.HasValue && initialIndex.Value >= 0 && initialIndex.Value < items.Count)
        {
            // Honor the caller's requested initial index, unless we are in leaf-only
            // mode and the item at that index is a group header — in that case fall
            // back to the first leaf so that Enter works immediately.
            if (SkipUnselectableItems && mode == SelectionMode.Leaf && items[initialIndex.Value].IsGroup)
            {
                Index = _leafIndexes?.FirstOrDefault() ?? 0;
            }
            else
            {
                Index = initialIndex.Value;
            }
        }
        else if (SkipUnselectableItems && mode == SelectionMode.Leaf)
        {
            Index = _leafIndexes?.FirstOrDefault() ?? 0;
        }
        else
        {
            Index = 0;
        }
    }

    /// <summary>
    /// Returns the items that should be displayed. When filter mode is active and
    /// search text is non-empty this is the filtered subset; otherwise all items.
    /// </summary>
    public IReadOnlyList<ListPromptItem<T>> GetDisplayItems()
    {
        if (FilterEnabled && SearchText.Length > 0 && _filteredIndexes is { Count: > 0 })
        {
            return _filteredIndexes.Select(i => Items[i]).ToList();
        }

        return Items;
    }

    /// <summary>
    /// Returns the cursor position within <see cref="GetDisplayItems()"/>.
    /// When filter mode is active this is the position of the selected item within
    /// the filtered subset. When the selected item is not in the filtered list
    /// (can occur transiently) returns 0.
    /// </summary>
    public int GetDisplayIndex()
    {
        if (FilterEnabled && SearchText.Length > 0 && _filteredIndexes is { Count: > 0 })
        {
            var pos = _filteredIndexes.IndexOf(Index);
            return pos >= 0 ? pos : 0;
        }

        return Index;
    }

    public bool Update(ConsoleKeyInfo keyInfo)
    {
        var index = Index;
        var search = SearchText;

        // Determine which navigation table is active for this key event.
        // Priority: filter (when active text) > leaf indexes > full list.
        var filterActive = FilterEnabled && SearchText.Length > 0
                           && _filteredIndexes is { Count: > 0 };

        if (filterActive)
        {
            index = NavigateFiltered(keyInfo, index);
        }
        else if (SkipUnselectableItems && Mode == SelectionMode.Leaf)
        {
            Debug.Assert(_leafIndexes != null, nameof(_leafIndexes) + " != null");
            index = NavigateLeaf(keyInfo, index);
        }
        else
        {
            index = NavigateFull(keyInfo, index);
        }

        if (SearchEnabled)
        {
            if (FilterEnabled)
            {
                (index, search) = ApplyFilterSearch(keyInfo, index, search);
            }
            else
            {
                (index, search) = ApplyHighlightSearch(keyInfo, index, search);
            }
        }

        // Apply index clamping only when we did not navigate within a constrained
        // set (_filteredIndexes or _leafIndexes), because those already yield valid
        // full-list indexes.  The plain navigateFull case needs clamping / wrapping.
        if (!filterActive && !(SkipUnselectableItems && Mode == SelectionMode.Leaf))
        {
            index = WrapAround
                ? (ItemCount + (index % ItemCount)) % ItemCount
                : index.Clamp(0, ItemCount - 1);
        }

        if (index != Index || SearchText != search)
        {
            Index = index;
            SearchText = search;
            return true;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Navigation helpers
    // -------------------------------------------------------------------------

    private int NavigateFiltered(ConsoleKeyInfo keyInfo, int index)
    {
        Debug.Assert(_filteredIndexes != null, nameof(_filteredIndexes) + " != null");

        var currentPos = _filteredIndexes.IndexOf(index);
        if (currentPos < 0)
        {
            currentPos = 0;
        }

        switch (keyInfo.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                if (currentPos > 0)
                {
                    index = _filteredIndexes[currentPos - 1];
                }
                else if (WrapAround)
                {
                    index = _filteredIndexes[^1];
                }

                break;

            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                if (currentPos < _filteredIndexes.Count - 1)
                {
                    index = _filteredIndexes[currentPos + 1];
                }
                else if (WrapAround)
                {
                    index = _filteredIndexes[0];
                }

                break;

            case ConsoleKey.Home:
                index = _filteredIndexes[0];
                break;

            case ConsoleKey.End:
                index = _filteredIndexes[^1];
                break;

            case ConsoleKey.PageUp:
            {
                var newPos = Math.Max(currentPos - PageSize, 0);
                index = _filteredIndexes[newPos];
                break;
            }

            case ConsoleKey.PageDown:
            {
                var newPos = Math.Min(currentPos + PageSize, _filteredIndexes.Count - 1);
                index = _filteredIndexes[newPos];
                break;
            }
        }

        return index;
    }

    private int NavigateLeaf(ConsoleKeyInfo keyInfo, int index)
    {
        Debug.Assert(_leafIndexes != null, nameof(_leafIndexes) + " != null");

        var currentLeafIndex = _leafIndexes.IndexOf(index);
        switch (keyInfo.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                if (currentLeafIndex > 0)
                {
                    index = _leafIndexes[currentLeafIndex - 1];
                }
                else if (WrapAround)
                {
                    index = _leafIndexes.LastOrDefault();
                }

                break;

            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                if (currentLeafIndex < _leafIndexes.Count - 1)
                {
                    index = _leafIndexes[currentLeafIndex + 1];
                }
                else if (WrapAround)
                {
                    index = _leafIndexes.FirstOrDefault();
                }

                break;

            case ConsoleKey.Home:
                index = _leafIndexes.FirstOrDefault();
                break;

            case ConsoleKey.End:
                index = _leafIndexes.LastOrDefault();
                break;

            case ConsoleKey.PageUp:
            {
                var newLeafIdx = Math.Max(currentLeafIndex - PageSize, 0);
                if (newLeafIdx < _leafIndexes.Count)
                {
                    index = _leafIndexes[newLeafIdx];
                }

                break;
            }

            case ConsoleKey.PageDown:
            {
                var newLeafIdx = Math.Min(currentLeafIndex + PageSize, _leafIndexes.Count - 1);
                if (newLeafIdx < _leafIndexes.Count)
                {
                    index = _leafIndexes[newLeafIdx];
                }

                break;
            }
        }

        return index;
    }

    private int NavigateFull(ConsoleKeyInfo keyInfo, int index)
    {
        return keyInfo.Key switch
        {
            ConsoleKey.UpArrow or ConsoleKey.K => Index - 1,
            ConsoleKey.DownArrow or ConsoleKey.J => Index + 1,
            ConsoleKey.Home => 0,
            ConsoleKey.End => ItemCount - 1,
            ConsoleKey.PageUp => Index - PageSize,
            ConsoleKey.PageDown => Index + PageSize,
            _ => index,
        };
    }

    // -------------------------------------------------------------------------
    // Search helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies filter-mode search: rebuilds <see cref="_filteredIndexes"/> on any
    /// text change and moves the cursor to the first match if the current item is
    /// no longer in the filtered set.
    /// </summary>
    private (int index, string search) ApplyFilterSearch(ConsoleKeyInfo keyInfo, int index, string search)
    {
        if (!char.IsControl(keyInfo.KeyChar))
        {
            search = SearchText + keyInfo.KeyChar;
            RebuildFilteredIndexes(search);
            index = SelectFilteredIndex(index);
        }
        else if (keyInfo.Key == ConsoleKey.Backspace && search.Length > 0)
        {
            search = search[..^1];
            RebuildFilteredIndexes(search);
            index = SelectFilteredIndex(index);
        }

        return (index, search);
    }

    /// <summary>
    /// Applies highlight-mode search: jumps the cursor to the first match without
    /// filtering the visible list. This is the original search behaviour.
    /// </summary>
    private (int index, string search) ApplyHighlightSearch(ConsoleKeyInfo keyInfo, int index, string search)
    {
        if (!char.IsControl(keyInfo.KeyChar))
        {
            search = SearchText + keyInfo.KeyChar;

            var item = Items.FirstOrDefault(x =>
                _converter.Invoke(x.Data).Contains(search, StringComparison.OrdinalIgnoreCase)
                && (!x.IsGroup || Mode != SelectionMode.Leaf));

            if (item != null)
            {
                index = Items.IndexOf(item);
            }
        }

        if (keyInfo.Key == ConsoleKey.Backspace)
        {
            if (search.Length > 0)
            {
                search = search[..^1];
            }

            var item = Items.FirstOrDefault(x =>
                _converter.Invoke(x.Data).Contains(search, StringComparison.OrdinalIgnoreCase) &&
                (!x.IsGroup || Mode != SelectionMode.Leaf));

            if (item != null)
            {
                index = Items.IndexOf(item);
            }
        }

        return (index, search);
    }

    /// <summary>
    /// Rebuilds <see cref="_filteredIndexes"/> to contain the indexes of all items
    /// whose display text contains <paramref name="search"/> (case-insensitive).
    /// In Leaf+SkipUnselectableItems mode group headers are excluded.
    /// </summary>
    private void RebuildFilteredIndexes(string search)
    {
        if (search.Length == 0)
        {
            _filteredIndexes = null;
            return;
        }

        _filteredIndexes = Items
            .Select((item, i) => (item, i))
            .Where(x =>
                _converter.Invoke(x.item.Data).Contains(search, StringComparison.OrdinalIgnoreCase)
                && (!x.item.IsGroup || Mode != SelectionMode.Leaf))
            .Select(x => x.i)
            .ToList();
    }

    /// <summary>
    /// Returns the index to use after a filter rebuild. Keeps <paramref name="current"/>
    /// if it is still in the filtered set; otherwise returns the first filtered index.
    /// </summary>
    private int SelectFilteredIndex(int current)
    {
        if (_filteredIndexes == null || _filteredIndexes.Count == 0)
        {
            return current;
        }

        return _filteredIndexes.Contains(current) ? current : _filteredIndexes[0];
    }

    internal void Cancel()
    {
        IsCancelled = true;
    }
}
// Stryker restore all
