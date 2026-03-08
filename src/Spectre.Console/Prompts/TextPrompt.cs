namespace Spectre.Console;

/// <summary>
/// Represents a prompt.
/// </summary>
/// <typeparam name="T">The prompt result type.</typeparam>
public sealed class TextPrompt<T> : IPrompt<T>, IHasCulture
{
    private readonly string _prompt;
    private readonly StringComparer? _comparer;

    /// <summary>
    /// Gets or sets the prompt style.
    /// </summary>
    public Style? PromptStyle { get; set; }

    /// <summary>
    /// Gets the list of choices.
    /// </summary>
    public List<T> Choices { get; } = [];

    /// <summary>
    /// Gets or sets the culture to use when converting input to object.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Gets or sets the message for invalid choices.
    /// </summary>
    public string InvalidChoiceMessage { get; set; } = "[red]Please select one of the available options[/]";

    /// <summary>
    /// Gets or sets a value indicating whether input should
    /// be hidden in the console.
    /// </summary>
    public bool IsSecret { get; set; }

    /// <summary>
    /// Gets or sets the character to use while masking
    /// a secret prompt.
    /// </summary>
    public char? Mask { get; set; } = '*';

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    public string ValidationErrorMessage { get; set; } = "[red]Invalid input[/]";

    /// <summary>
    /// Gets or sets a value indicating whether or not
    /// choices should be shown.
    /// </summary>
    public bool ShowChoices { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not
    /// default values should be shown.
    /// </summary>
    public bool ShowDefaultValue { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not an empty result is valid.
    /// </summary>
    public bool AllowEmpty { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prompt line
    /// should be cleared after a successful input.
    /// </summary>
    public bool ClearOnFinish { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the default value should be
    /// written into the input buffer so the user can edit it before confirming.
    /// When <see langword="true"/> and a <see cref="DefaultValue"/> is set, the
    /// default's display string is pre-filled and the cursor is positioned after
    /// the last character.  The user may backspace over it, append to it, or press
    /// Enter to accept it unchanged.
    /// Has no effect when no <see cref="DefaultValue"/> is configured.
    /// </summary>
    public bool PrefillDefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the converter to get the display string for a choice. By default
    /// the corresponding <see cref="TypeConverter"/> is used.
    /// </summary>
    public Func<T, string>? Converter { get; set; } = TypeConverterHelper.ConvertToString;

    /// <summary>
    /// Gets or sets the synchronous validator.
    /// </summary>
    public Func<T, ValidationResult>? Validator { get; set; }

    /// <summary>
    /// Gets or sets the asynchronous validator.
    /// When set, this takes precedence over <see cref="Validator"/>.
    /// The <see cref="CancellationToken"/> parameter is the token passed to
    /// <see cref="ShowAsync"/>, allowing long-running validators to cancel early.
    /// </summary>
    public Func<T, CancellationToken, Task<ValidationResult>>? AsyncValidator { get; set; }

    /// <summary>
    /// Gets or sets the style in which the default value is displayed. Defaults to green when <see langword="null"/>.
    /// </summary>
    public Style? DefaultValueStyle { get; set; }

    /// <summary>
    /// Gets or sets the style in which the list of choices is displayed. Defaults to blue when <see langword="null"/>.
    /// </summary>
    public Style? ChoicesStyle { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    internal DefaultPromptValue<T>? DefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPrompt{T}"/> class.
    /// </summary>
    /// <param name="prompt">The prompt markup text.</param>
    /// <param name="comparer">The comparer used for choices.</param>
    public TextPrompt(string prompt, StringComparer? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        _prompt = prompt;
        _comparer = comparer;
    }

    /// <summary>
    /// Shows the prompt and requests input from the user.
    /// </summary>
    /// <param name="console">The console to show the prompt in.</param>
    /// <returns>The user input converted to the expected type.</returns>
    /// <inheritdoc/>
    public T Show(IAnsiConsole console)
    {
        return ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<T> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(console);

        return await console.RunExclusive(async () =>
        {
            var promptStyle = PromptStyle ?? Style.Plain;
            var converter = Converter ?? TypeConverterHelper.ConvertToString;
            var choices = Choices.Select(choice => converter(choice)).ToList();
            var choiceMap = Choices.ToDictionary(choice => converter(choice), choice => choice, _comparer);

            WritePrompt(console);

            while (true)
            {
                var prefillText = (PrefillDefaultValue && DefaultValue != null)
                    ? converter(DefaultValue.Value)
                    : null;
                var input = await console.ReadLine(promptStyle, IsSecret, Mask, choices, prefillText, cancellationToken).ConfigureAwait(false);

                // Nothing entered?
                if (string.IsNullOrWhiteSpace(input))
                {
                    if (DefaultValue != null)
                    {
                        var defaultValue = converter(DefaultValue.Value);
                        console.Write(IsSecret ? defaultValue.Mask(Mask) : defaultValue, promptStyle);
                        console.WriteLine();

                        ClearPromptLine(console);
                        return DefaultValue.Value;
                    }

                    if (!AllowEmpty)
                    {
                        continue;
                    }
                }

                console.WriteLine();

                T? result;
                if (Choices.Count > 0)
                {
                    if (choiceMap.TryGetValue(input, out result) && result != null)
                    {
                        ClearPromptLine(console);
                        return result;
                    }

                    console.MarkupLine(InvalidChoiceMessage);
                    WritePrompt(console);
                    continue;
                }
                else if (!TypeConverterHelper.TryConvertFromStringWithCulture<T>(input, Culture, out result) ||
                         (result == null && !string.IsNullOrWhiteSpace(input)))
                {
                    console.MarkupLine(ValidationErrorMessage);
                    WritePrompt(console);
                    continue;
                }

                // Run validators — async takes precedence over sync.
                // result! is safe: null is only possible when T is a nullable type (e.g. Uri?)
                // and AllowEmpty is set, in which case null is a valid T value.
                var (valid, message) = await ValidateResultAsync(result!, cancellationToken).ConfigureAwait(false);
                if (!valid)
                {
                    console.MarkupLine(message!);
                    WritePrompt(console);
                    continue;
                }

                ClearPromptLine(console);
                return result!;
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the prompt to the console.
    /// </summary>
    /// <param name="console">The console to write the prompt to.</param>
    private void WritePrompt(IAnsiConsole console)
    {
        // Stryker disable once all : Equivalent — only called from ShowAsync which already validated console
        ArgumentNullException.ThrowIfNull(console);

        var builder = new StringBuilder();
        builder.Append(_prompt.TrimEnd());

        var appendSuffix = false;
        if (ShowChoices && Choices.Count > 0)
        {
            appendSuffix = true;
            var converter = Converter ?? TypeConverterHelper.ConvertToString;
            var choices = string.Join("/", Choices.Select(choice => converter(choice).EscapeMarkup()));
            var choicesStyle = ChoicesStyle?.ToMarkup() ?? "blue";
            builder.AppendFormat(CultureInfo.InvariantCulture, " [{0}][[{1}]][/]", choicesStyle, choices);
        }

        if (ShowDefaultValue && DefaultValue != null)
        {
            appendSuffix = true;
            var converter = Converter ?? TypeConverterHelper.ConvertToString;
            var defaultValueStyle = DefaultValueStyle?.ToMarkup() ?? "green";
            var defaultValue = converter(DefaultValue.Value);

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                " [{0}]({1})[/]",
                defaultValueStyle,
                IsSecret ? defaultValue.Mask(Mask) : defaultValue.EscapeMarkup());
        }

        var markup = builder.ToString().Trim();
        if (appendSuffix)
        {
            markup += ":";
        }

        console.Markup(markup + " ");
    }

    /// <summary>
    /// Clears the prompt line when enabled.
    /// </summary>
    /// <param name="console">The console to clear the prompt from.</param>
    private void ClearPromptLine(IAnsiConsole console)
    {
        if (!ClearOnFinish)
        {
            return;
        }

        // Stryker disable once all : Equivalent — only called from ShowAsync which already validated console
        ArgumentNullException.ThrowIfNull(console);

        // Stryker disable once all : NoCoverage false positive — Ansi is always true in TestConsole
        if (!console.Profile.Capabilities.Ansi)
        {
            return;
        }

        // Stryker disable once all : Equivalent — null guard in extension method; always called with non-null prompt from fluent API
        console.Cursor.MoveUp();
        console.Write(ControlCode.Create(console, writer =>
        {
            writer.Write("\r");
            writer.EraseInLine(2);
        }));
    }

    /// <summary>
    /// Runs whichever validator is configured, async first.
    /// Returns <c>(true, null)</c> on success, or <c>(false, errorMessage)</c> on failure.
    /// </summary>
    private async Task<(bool Valid, string? Message)> ValidateResultAsync(T value, CancellationToken cancellationToken)
    {
        if (AsyncValidator != null)
        {
            var result = await AsyncValidator(value, cancellationToken).ConfigureAwait(false);
            if (!result.Successful)
            {
                return (false, result.Message ?? ValidationErrorMessage);
            }

            return (true, null);
        }

        if (Validator != null)
        {
            var result = Validator(value);
            if (!result.Successful)
            {
                return (false, result.Message ?? ValidationErrorMessage);
            }
        }

        return (true, null);
    }
}