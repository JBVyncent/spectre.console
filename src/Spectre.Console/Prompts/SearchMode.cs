namespace Spectre.Console;

/// <summary>
/// Controls how a <see cref="SelectionPrompt{T}"/> behaves when the user types
/// while <see cref="SelectionPromptExtensions.EnableSearch{T}(SelectionPrompt{T})"/>
/// is active.
/// </summary>
public enum SearchMode
{
    /// <summary>
    /// Matching characters in item labels are highlighted and the cursor jumps to
    /// the first matching item. All items remain visible. This is the default.
    /// </summary>
    Highlight = 0,

    /// <summary>
    /// The displayed list is filtered to show only items whose labels contain the
    /// current search text. The cursor moves within the filtered results. Clearing
    /// the search text restores the full list.
    /// </summary>
    Filter = 1,
}
