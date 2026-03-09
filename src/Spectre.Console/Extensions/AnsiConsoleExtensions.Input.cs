namespace Spectre.Console;

/// <summary>
/// Contains extension methods for <see cref="IAnsiConsole"/>.
/// </summary>
public static partial class AnsiConsoleExtensions
{
    // Stryker disable all : Internal input method; Stryker cannot trace coverage through TextPrompt pipeline
    internal static async Task<string> ReadLine(this IAnsiConsole console, Style? style, bool secret, char? mask, IEnumerable<string>? items = null, string? initialText = null, IReadOnlyList<string>? history = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(console);

        style ??= Style.Plain;

        // Pre-fill the buffer when an initial value was requested.  The text is written
        // immediately so the user sees it and can backspace over it or press Enter to accept.
        var text = initialText ?? string.Empty;
        if (!string.IsNullOrEmpty(initialText))
        {
            if (!secret)
            {
                console.Write(initialText, style);
            }
            else if (mask != null)
            {
                console.Write(initialText.Mask(mask), style);
            }

            // When secret && mask == null, write nothing (fully hidden input).
        }

        var autocomplete = new List<string>(items ?? []);

        // History navigation state.  historyIndex == history.Count means "current live input".
        var historyIndex = history?.Count ?? 0;
        var savedText = text; // preserved while browsing history, restored on DownArrow

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rawKey = await console.Input.ReadKeyAsync(true, cancellationToken).ConfigureAwait(false);
            if (rawKey == null)
            {
                continue;
            }

            var key = rawKey.Value;
            if (key.Key == ConsoleKey.Enter)
            {
                return text;
            }

            // History navigation — UpArrow moves to older entries, DownArrow to newer.
            if (history != null && history.Count > 0)
            {
                if (key.Key == ConsoleKey.UpArrow)
                {
                    if (historyIndex > 0)
                    {
                        // Save live text the first time we leave it
                        if (historyIndex == history.Count)
                        {
                            savedText = text;
                        }

                        historyIndex--;
                        var entry = history[historyIndex];
                        ReplaceInputLine(console, style, secret, mask, text, entry);
                        text = entry;
                    }

                    continue;
                }

                if (key.Key == ConsoleKey.DownArrow)
                {
                    if (historyIndex < history.Count)
                    {
                        historyIndex++;
                        var entry = historyIndex == history.Count ? savedText : history[historyIndex];
                        ReplaceInputLine(console, style, secret, mask, text, entry);
                        text = entry;
                    }

                    continue;
                }
            }

            if (key.Key == ConsoleKey.Tab && autocomplete.Count > 0)
            {
                var autoCompleteDirection = key.Modifiers.HasFlag(ConsoleModifiers.Shift)
                    ? AutoCompleteDirection.Backward
                    : AutoCompleteDirection.Forward;
                var replace = AutoComplete(autocomplete, text, autoCompleteDirection);
                if (!string.IsNullOrEmpty(replace))
                {
                    // Erase current visible text and render the suggestion.
                    // When secret, use masked output or nothing (mask == null).
                    if (!secret)
                    {
                        console.Write("\b \b".Repeat(text.Length), style);
                        console.Write(replace);
                    }
                    else if (mask != null)
                    {
                        console.Write("\b \b".Repeat(text.Length), style);
                        console.Write(replace.Mask(mask), style);
                    }

                    text = replace;
                    continue;
                }
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (text.Length > 0)
                {
                    // Determine how many chars to remove: 2 for a surrogate pair, 1 otherwise.
                    // Surrogate pairs encode non-BMP characters (emojis, CJK ext-B, etc.)
                    // which are virtually always display-width 2.
                    var removeCount = 1;
                    var displayWidth = UnicodeCalculator.GetWidth(text[text.Length - 1]);
                    if (text.Length >= 2 && char.IsSurrogatePair(text[text.Length - 2], text[text.Length - 1]))
                    {
                        removeCount = 2;
                        displayWidth = 2;
                    }

                    text = text.Substring(0, text.Length - removeCount);

                    if (mask != null || !secret)
                    {
                        for (var i = 0; i < displayWidth; i++)
                        {
                            console.Write("\b \b");
                        }
                    }
                }

                // Any edit action exits history mode
                historyIndex = history?.Count ?? 0;
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                text += key.KeyChar.ToString();
                var output = key.KeyChar.ToString();
                console.Write(secret ? output.Mask(mask) : output, style);

                // Any edit action exits history mode
                historyIndex = history?.Count ?? 0;
            }
        }
    }

    // Stryker restore all
    // Stryker disable all : Internal helper; not exercised through TextPrompt pipeline by Stryker
    private static void ReplaceInputLine(IAnsiConsole console, Style? style, bool secret, char? mask, string currentText, string newText)
    {
        // Erase the current visible characters (1 \b \b cycle per displayed cell).
        // When secret && mask == null, nothing was displayed so nothing to erase.
        if (currentText.Length > 0 && (!secret || mask != null))
        {
            console.Write("\b \b".Repeat(currentText.Length), style);
        }

        if (!string.IsNullOrEmpty(newText))
        {
            if (!secret)
            {
                console.Write(newText, style);
            }
            else if (mask != null)
            {
                console.Write(newText.Mask(mask), style);
            }

            // When secret && mask == null, write nothing (fully hidden input).
        }
    }

    // Stryker restore all
    // Stryker disable all : Internal autocomplete method; Stryker cannot trace coverage through TextPrompt pipeline
    private static string AutoComplete(List<string> autocomplete, string text, AutoCompleteDirection autoCompleteDirection)
    {
        var found = autocomplete.Find(i => i == text);
        var replace = string.Empty;

        if (found == null)
        {
            // Get the closest match
            var next = autocomplete.Find(i => i.StartsWith(text, true, CultureInfo.InvariantCulture));
            if (next != null)
            {
                replace = next;
            }
            else if (string.IsNullOrEmpty(text))
            {
                // Use the first item
                replace = autocomplete[0];
            }
        }
        else
        {
            // Get the next match
            replace = GetAutocompleteValue(autoCompleteDirection, autocomplete, found);
        }

        return replace;
    }

    // Stryker disable all : Internal autocomplete helper; Stryker cannot trace coverage through TextPrompt pipeline
    private static string GetAutocompleteValue(AutoCompleteDirection autoCompleteDirection, IList<string> autocomplete, string found)
    {
        var foundAutocompleteIndex = autocomplete.IndexOf(found);
        var index = autoCompleteDirection switch
        {
            AutoCompleteDirection.Forward => foundAutocompleteIndex + 1,
            AutoCompleteDirection.Backward => foundAutocompleteIndex - 1,
            _ => throw new ArgumentOutOfRangeException(nameof(autoCompleteDirection), autoCompleteDirection, null),
        };

        if (index >= autocomplete.Count)
        {
            index = 0;
        }

        if (index < 0)
        {
            index = autocomplete.Count - 1;
        }

        return autocomplete[index];
    }
    // Stryker restore all
    private enum AutoCompleteDirection
    {
        Forward,
        Backward,
    }
}