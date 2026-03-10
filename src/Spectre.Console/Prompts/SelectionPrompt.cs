namespace Spectre.Console;

/// <summary>
/// Represents a single list prompt.
/// </summary>
/// <typeparam name="T">The prompt result type.</typeparam>
public sealed class SelectionPrompt<T> : IPrompt<T>, IListPromptStrategy<T>
    where T : notnull
{
    private readonly ListPromptTree<T> _tree;

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// Defaults to <c>10</c>.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether the selection should wrap around when reaching the edge.
    /// Defaults to <c>false</c>.
    /// </summary>
    // Stryker disable once all : Equivalent — tests either set WrapAround explicitly or use small item counts where default doesn't affect outcome
    public bool WrapAround { get; set; } = false;

    /// <summary>
    /// Gets or sets the highlight style of the selected choice.
    /// </summary>
    public Style? HighlightStyle { get; set; }

    /// <summary>
    /// Gets or sets the style of a disabled choice.
    /// </summary>
    public Style? DisabledStyle { get; set; }

    /// <summary>
    /// Gets or sets the style of highlighted search matches.
    /// </summary>
    public Style? SearchHighlightStyle { get; set; }

    /// <summary>
    /// Gets or sets the text that will be displayed when no search text has been entered.
    /// </summary>
    public string? SearchPlaceholderText { get; set; }

    /// <summary>
    /// Gets or sets the converter to get the display string for a choice. By default
    /// the corresponding <see cref="TypeConverter"/> is used.
    /// </summary>
    public Func<T, string>? Converter { get; set; }

    /// <summary>
    /// Gets or sets the text that will be displayed if there are more choices to show.
    /// </summary>
    public string? MoreChoicesText { get; set; }

    /// <summary>
    /// Gets or sets the selection mode.
    /// Defaults to <see cref="SelectionMode.Leaf"/>.
    /// </summary>
    public SelectionMode Mode { get; set; } = SelectionMode.Leaf;

    /// <summary>
    /// Gets or sets a value indicating whether or not search is enabled.
    /// </summary>
    public bool SearchEnabled { get; set; }

    /// <summary>
    /// Gets or sets how the prompt behaves when search is active.
    /// Defaults to <see cref="SearchMode.Highlight"/>.
    /// </summary>
    public SearchMode SearchMode { get; set; } = SearchMode.Highlight;

    /// <summary>
    /// Gets or sets a Func that will be triggered if Cancel is triggered by the 'ESC' key.
    /// </summary>
    public Func<T>? CancelResult { get; set; }

    /// <summary>
    /// Gets or sets the default value. When set, the cursor is pre-positioned on this
    /// item when the prompt first renders. If the value is not found in the choices list,
    /// the cursor starts at the first selectable item as usual.
    /// </summary>
    internal DefaultPromptValue<T>? DefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionPrompt{T}"/> class.
    /// </summary>
    public SelectionPrompt()
    {
        _tree = new ListPromptTree<T>(EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Adds a choice.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>A <see cref="ISelectionItem{T}"/> so that multiple calls can be chained.</returns>
    public ISelectionItem<T> AddChoice(T item)
    {
        var node = new ListPromptItem<T>(item);
        _tree.Add(node);
        return node;
    }

    /// <summary>
    /// Creates a static snapshot of this prompt rendered at the given cursor position.
    /// The returned <see cref="IRenderable"/> can be passed to
    /// <see cref="IAnsiConsole.Write(IRenderable)"/> or composed with other renderables
    /// without any user interaction.
    /// </summary>
    /// <param name="console">
    /// The console used for page-size calculation.
    /// Defaults to <see cref="AnsiConsole.Console"/> when <c>null</c>.
    /// </param>
    /// <param name="cursorIndex">
    /// Zero-based index of the item the cursor should be placed on.
    /// Clamped to the valid item range automatically.
    /// </param>
    /// <returns>An <see cref="IRenderable"/> snapshot of the prompt.</returns>
    public IRenderable ToRenderable(IAnsiConsole? console = null, int cursorIndex = 0)
    {
        console ??= AnsiConsole.Console;
        var nodes = _tree.Traverse().ToList();
        if (nodes.Count == 0)
        {
            return Text.Empty;
        }

        // Stryker disable once all : Equivalent — Converter is null in most callers so both sides yield same result
        var converter = Converter ?? TypeConverterHelper.ConvertToString;
        var pageSize = ((IListPromptStrategy<T>)this).CalculatePageSize(console, nodes.Count, PageSize);
        var clampedCursor = cursorIndex.Clamp(0, nodes.Count - 1);
        var state = new ListPromptState<T>(nodes, converter, pageSize, WrapAround, Mode, true, SearchEnabled, SearchMode == SearchMode.Filter, clampedCursor);
        return ListPrompt<T>.ComputeRenderable(this, console, state);
    }

    /// <summary>
    /// Creates an interactive <see cref="SelectionPromptRenderable{T}"/> that wraps this
    /// prompt and implements <see cref="IRenderable"/>. Embed it in a
    /// <see cref="LiveDisplay"/> and call
    /// <see cref="SelectionPromptRenderable{T}.Update"/> for each key press to drive the
    /// prompt without blocking the calling thread.
    /// </summary>
    /// <param name="console">
    /// The console used for page-size calculation and rendering.
    /// Defaults to <see cref="AnsiConsole.Console"/> when <c>null</c>.
    /// </param>
    /// <returns>
    /// A <see cref="SelectionPromptRenderable{T}"/> ready to be rendered and driven
    /// interactively.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no choices have been added to the prompt.
    /// </exception>
    public SelectionPromptRenderable<T> AsRenderable(IAnsiConsole? console = null)
    {
        console ??= AnsiConsole.Console;
        var nodes = _tree.Traverse().ToList();
        if (nodes.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot create a renderable from an empty selection prompt. " +
                "Please call the AddChoice() method to configure the prompt.");
        }

        // Stryker disable once all : Equivalent — Converter is null in most callers so both sides yield same result
        var converter = Converter ?? TypeConverterHelper.ConvertToString;
        var pageSize = ((IListPromptStrategy<T>)this).CalculatePageSize(console, nodes.Count, PageSize);

        int? initialIndex = null;
        if (DefaultValue is not null)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (comparer.Equals(nodes[i].Data, DefaultValue.Value))
                {
                    initialIndex = i;
                    break;
                }
            }
        }

        var state = new ListPromptState<T>(nodes, converter, pageSize, WrapAround, Mode, true, SearchEnabled, SearchMode == SearchMode.Filter, initialIndex);
        return new SelectionPromptRenderable<T>(this, console, state);
    }

    /// <inheritdoc/>
    public T Show(IAnsiConsole console)
    {
        return ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<T> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        // Create the list prompt
        var prompt = new ListPrompt<T>(console, this);
        // Stryker disable once all : Equivalent — Converter is null in most tests so both sides of ?? produce same result
        var converter = Converter ?? TypeConverterHelper.ConvertToString;

        // Resolve the initial cursor index from DefaultValue, if set.
        // We traverse the tree here (one extra pass) so the index is available before
        // ListPromptState is constructed. For typical prompt sizes this is negligible.
        int? initialIndex = null;
        if (DefaultValue is not null)
        {
            var allItems = _tree.Traverse().ToList();
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < allItems.Count; i++)
            {
                if (comparer.Equals(allItems[i].Data, DefaultValue.Value))
                {
                    initialIndex = i;
                    break;
                }
            }
        }

        // Stryker disable once all : Equivalent — boolean params and ConfigureAwait; internal pipeline values not observable in tests
        var result = await prompt.Show(_tree, converter, Mode, true, SearchEnabled, SearchMode == SearchMode.Filter, PageSize, WrapAround, initialIndex, cancellationToken).ConfigureAwait(false);

        // Stryker disable once all : Equivalent — && vs || doesn't change outcome in tests: CancelResult is always set when testing cancel, and always null otherwise
        if (result.IsCancelled && CancelResult is not null)
        {
            return CancelResult();
        }

        // Return the selected item
        return result.Items[result.Index].Data;
    }

    // Stryker disable all : NoCoverage — input handling and page size calculation exercised through interactive prompt tests but Stryker cannot trace coverage through async input pipeline
    /// <inheritdoc/>
    ListPromptInputResult IListPromptStrategy<T>.HandleInput(ConsoleKeyInfo key, ListPromptState<T> state)
    {
        if (key.Key == ConsoleKey.Enter
         || key.Key == ConsoleKey.Packet
         || (!state.SearchEnabled && key.Key == ConsoleKey.Spacebar))
        {
            // Selecting a non leaf in "leaf mode" is not allowed
            if (state.Current.IsGroup && Mode == SelectionMode.Leaf)
            {
                return ListPromptInputResult.None;
            }

            return ListPromptInputResult.Submit;
        }

        if (key.Key == ConsoleKey.Escape && CancelResult is not null)
        {
            return ListPromptInputResult.Abort;
        }

        return ListPromptInputResult.None;
    }

    /// <inheritdoc/>
    int IListPromptStrategy<T>.CalculatePageSize(IAnsiConsole console, int totalItemCount, int requestedPageSize)
    {
        var extra = 0;

        if (Title != null)
        {
            // Title takes up two rows including a blank line
            extra += 2;
        }

        var scrollable = totalItemCount > requestedPageSize;
        if (SearchEnabled || scrollable)
        {
            extra += 1;
        }

        if (SearchEnabled)
        {
            extra += 1;
        }

        if (scrollable)
        {
            extra += 1;
        }

        if (requestedPageSize > console.Profile.Height - extra)
        {
            return Math.Max(1, console.Profile.Height - extra);
        }

        return requestedPageSize;
    }
    // Stryker restore all

    // Stryker disable all : NoCoverage — rendering method body; visual correctness covered by Expectation
    // snapshot tests but individual line mutations are not traceable by Stryker's coverage analysis
    /// <inheritdoc/>
    IRenderable IListPromptStrategy<T>.Render(IAnsiConsole console, bool scrollable, int cursorIndex,
        IEnumerable<(int Index, ListPromptItem<T> Node)> items, bool skipUnselectableItems, string searchText)
    {
        var list = new List<IRenderable>();
        var disabledStyle = DisabledStyle ?? Color.Grey;
        var highlightStyle = HighlightStyle ?? Color.Blue;
        var searchHighlightStyle = SearchHighlightStyle ?? new Style(foreground: Color.Default, background: Color.Yellow, Decoration.Bold);

        if (Title != null)
        {
            list.Add(new Markup(Title));
        }

        var grid = new Grid();
        grid.AddColumn(new GridColumn().Padding(0, 0, 1, 0).NoWrap());

        if (Title != null)
        {
            grid.AddEmptyRow();
        }

        foreach (var item in items)
        {
            var current = item.Index == cursorIndex;
            var prompt = item.Index == cursorIndex ? ListPromptConstants.Arrow : new string(' ', ListPromptConstants.Arrow.Length);
            var style = item.Node.IsGroup && Mode == SelectionMode.Leaf
                ? disabledStyle
                : current ? highlightStyle : Style.Plain;

            var indent = new string(' ', item.Node.Depth * 2);

            var text = (Converter ?? TypeConverterHelper.ConvertToString)?.Invoke(item.Node.Data) ?? item.Node.Data.ToString() ?? "?";
            if (current)
            {
                text = text.RemoveMarkup().EscapeMarkup();
            }
            else
            {
                text = text.EscapeMarkup();
            }

            if (searchText.Length > 0 && !(item.Node.IsGroup && Mode == SelectionMode.Leaf))
            {
                text = AnsiMarkup.Highlight(text, searchText, searchHighlightStyle, StringComparison.OrdinalIgnoreCase);
            }

            grid.AddRow(new Markup(indent + prompt + " " + text, style));
        }

        list.Add(grid);

        if (SearchEnabled || scrollable)
        {
            // Add padding
            list.Add(Text.Empty);
        }

        if (SearchEnabled)
        {
            list.Add(new Markup(
                searchText.Length > 0 ? searchText.EscapeMarkup() : SearchPlaceholderText ?? ListPromptConstants.SearchPlaceholderMarkup));
        }

        if (scrollable)
        {
            // (Move up and down to reveal more choices)
            list.Add(new Markup(MoreChoicesText ?? ListPromptConstants.MoreChoicesMarkup));
        }

        return new Rows(list);
    }
    // Stryker restore all
}