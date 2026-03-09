namespace Spectre.Console;

// Stryker disable all : NoCoverage — internal async input loop; Stryker cannot trace coverage through
// the async ReadKeyAsync pipeline or the rendering render hook lifecycle
internal sealed class ListPrompt<T>
    where T : notnull
{
    private readonly IAnsiConsole _console;
    private readonly IListPromptStrategy<T> _strategy;

    public ListPrompt(IAnsiConsole console, IListPromptStrategy<T> strategy)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(strategy);
        _console = console;
        _strategy = strategy;
    }

    public async Task<ListPromptState<T>> Show(
        ListPromptTree<T> tree,
        Func<T, string> converter,
        SelectionMode selectionMode,
        bool skipUnselectableItems,
        bool searchEnabled,
        bool filterEnabled,
        int requestedPageSize,
        bool wrapAround,
        int? initialIndex = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tree);

        if (!_console.Profile.Capabilities.Interactive)
        {
            throw new NotSupportedException(
                "Cannot show selection prompt since the current " +
                "terminal isn't interactive.");
        }

        if (!_console.Profile.Capabilities.Ansi)
        {
            throw new NotSupportedException(
                "Cannot show selection prompt since the current " +
                "terminal does not support ANSI escape sequences.");
        }

        var nodes = tree.Traverse().ToList();
        if (nodes.Count == 0)
        {
            throw new InvalidOperationException("Cannot show an empty selection prompt. Please call the AddChoice() method to configure the prompt.");
        }

        var state = new ListPromptState<T>(nodes, converter, _strategy.CalculatePageSize(_console, nodes.Count, requestedPageSize), wrapAround, selectionMode, skipUnselectableItems, searchEnabled, filterEnabled, initialIndex);
        var hook = new ListPromptRenderHook<T>(_console, () => BuildRenderable(state));

        using var scope = new RenderHookScope(_console, hook);
        _console.Cursor.Hide();
        try
        {
            hook.Refresh();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rawKey = await _console.Input.ReadKeyAsync(true, cancellationToken).ConfigureAwait(false);
                if (rawKey == null)
                {
                    continue;
                }

                var key = rawKey.Value;
                var result = _strategy.HandleInput(key, state);
                if (result == ListPromptInputResult.Submit)
                {
                    break;
                }

                if (result == ListPromptInputResult.Abort)
                {
                    state.Cancel();
                    break;
                }

                if (state.Update(key) || result == ListPromptInputResult.Refresh)
                {
                    hook.Refresh();
                }
            }
        }
        finally
        {
            hook.Clear();
            _console.Cursor.Show();
        }

        return state;
    }

    private IRenderable BuildRenderable(ListPromptState<T> state)
        => ComputeRenderable(_strategy, _console, state);

    /// <summary>
    /// Computes a paginated <see cref="IRenderable"/> for <paramref name="state"/> using
    /// <paramref name="strategy"/> to do the actual rendering. Called from
    /// <see cref="BuildRenderable"/> and from <see cref="SelectionPromptRenderable{T}"/>.
    /// </summary>
    internal static IRenderable ComputeRenderable(
        IListPromptStrategy<T> strategy,
        IAnsiConsole console,
        ListPromptState<T> state)
    {
        var pageSize = state.PageSize;
        var middleOfList = pageSize / 2;

        // When filter mode is active and search text is non-empty, render only the
        // matching items. The filtered cursor index is the position of the selected
        // item within that subset. When the filter is inactive (no search text, or
        // filter not enabled), we fall back to the full Items list.
        var displayItems = state.GetDisplayItems();
        var displayCount = displayItems.Count;
        var cursorIndex = state.GetDisplayIndex();

        var skip = 0;
        var take = displayCount;

        var scrollable = displayCount > pageSize;
        if (scrollable)
        {
            skip = Math.Max(0, cursorIndex - middleOfList);
            take = Math.Min(pageSize, displayCount - skip);

            if (take < pageSize)
            {
                // Pointer should be below the middle of the (visual) list
                var diff = pageSize - take;
                skip -= diff;
                take += diff;
                cursorIndex = middleOfList + diff;
            }
            else
            {
                // Take skip into account
                cursorIndex -= skip;
            }
        }

        // Build the renderable
        return strategy.Render(
            console,
            scrollable, cursorIndex,
            displayItems.Skip(skip).Take(take)
                .Select((node, index) => (index, node)),
            state.SkipUnselectableItems,
            state.SearchText);
    }
}
// Stryker restore all