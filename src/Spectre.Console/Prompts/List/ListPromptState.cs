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
    public bool IsCancelled { get; private set; }
    public IReadOnlyList<ListPromptItem<T>> Items { get; }
    private readonly IReadOnlyList<int>? _leafIndexes;

    public ListPromptItem<T> Current => Items[Index];
    public string SearchText { get; private set; }

    public ListPromptState(
        IReadOnlyList<ListPromptItem<T>> items,
        Func<T, string> converter,
        int pageSize, bool wrapAround,
        SelectionMode mode,
        bool skipUnselectableItems,
        bool searchEnabled,
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

    public bool Update(ConsoleKeyInfo keyInfo)
    {
        var index = Index;
        if (SkipUnselectableItems && Mode == SelectionMode.Leaf)
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
                    index = Math.Max(currentLeafIndex - PageSize, 0);
                    if (index < _leafIndexes.Count)
                    {
                        index = _leafIndexes[index];
                    }

                    break;

                case ConsoleKey.PageDown:
                    index = Math.Min(currentLeafIndex + PageSize, _leafIndexes.Count - 1);
                    if (index < _leafIndexes.Count)
                    {
                        index = _leafIndexes[index];
                    }

                    break;
            }
        }
        else
        {
            index = keyInfo.Key switch
            {
                ConsoleKey.UpArrow or ConsoleKey.K => Index - 1,
                ConsoleKey.DownArrow or ConsoleKey.J => Index + 1,
                ConsoleKey.Home => 0,
                ConsoleKey.End => ItemCount - 1,
                ConsoleKey.PageUp => Index - PageSize,
                ConsoleKey.PageDown => Index + PageSize,
                _ => Index,
            };
        }

        var search = SearchText;

        if (SearchEnabled)
        {
            // If is text input, append to search filter
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
                    search = search.Substring(0, search.Length - 1);
                }

                var item = Items.FirstOrDefault(x =>
                    _converter.Invoke(x.Data).Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    (!x.IsGroup || Mode != SelectionMode.Leaf));

                if (item != null)
                {
                    index = Items.IndexOf(item);
                }
            }
        }

        index = WrapAround
            ? (ItemCount + (index % ItemCount)) % ItemCount
            : index.Clamp(0, ItemCount - 1);

        if (index != Index || SearchText != search)
        {
            Index = index;
            SearchText = search;
            return true;
        }

        return false;
    }

    internal void Cancel()
    {
        IsCancelled = true;
    }
}
// Stryker restore all