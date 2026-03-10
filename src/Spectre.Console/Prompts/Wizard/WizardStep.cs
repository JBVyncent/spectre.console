namespace Spectre.Console;

/// <summary>
/// Represents a single step in a wizard prompt.
/// </summary>
public abstract class WizardStep
{
    /// <summary>
    /// Gets the unique key used to store this step's result.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the display title for this step.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets an optional condition that determines whether this step is shown.
    /// When <c>null</c>, the step is always shown.
    /// </summary>
    public Func<WizardResult, bool>? Condition { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardStep"/> class.
    /// </summary>
    /// <param name="key">The unique result key.</param>
    /// <param name="title">The display title.</param>
    /// <param name="condition">An optional visibility condition.</param>
    protected WizardStep(string key, string title, Func<WizardResult, bool>? condition)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(title);

        Key = key;
        Title = title;
        Condition = condition;
    }

    /// <summary>
    /// Shows the step's prompt and returns the result as an object.
    /// </summary>
    internal abstract object Show(IAnsiConsole console);

    /// <summary>
    /// Shows the step's prompt asynchronously and returns the result as an object.
    /// </summary>
    internal abstract Task<object> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken);

    /// <summary>
    /// Formats the result value for display in the summary.
    /// </summary>
    internal abstract string FormatResult(object value);
}

/// <summary>
/// A wizard step that wraps an <see cref="IPrompt{T}"/> and captures a typed result.
/// </summary>
/// <typeparam name="T">The prompt result type.</typeparam>
public sealed class WizardStep<T> : WizardStep
    where T : notnull
{
    private readonly IPrompt<T> _prompt;
    private readonly Func<T, string>? _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardStep{T}"/> class.
    /// </summary>
    /// <param name="key">The unique result key.</param>
    /// <param name="title">The display title.</param>
    /// <param name="prompt">The prompt to show for this step.</param>
    /// <param name="formatter">An optional formatter for the summary display.</param>
    /// <param name="condition">An optional visibility condition.</param>
    public WizardStep(
        string key,
        string title,
        IPrompt<T> prompt,
        Func<T, string>? formatter = null,
        Func<WizardResult, bool>? condition = null)
        : base(key, title, condition)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        _prompt = prompt;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    internal override object Show(IAnsiConsole console)
    {
        return _prompt.Show(console);
    }

    /// <inheritdoc/>
    // Stryker disable all : ConfigureAwait(false/true) is equivalent in test context
    internal override async Task<object> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        return await _prompt.ShowAsync(console, cancellationToken).ConfigureAwait(false);
    }
    // Stryker restore all

    /// <inheritdoc/>
    internal override string FormatResult(object value)
    {
        if (value is T typed && _formatter != null)
        {
            return _formatter(typed);
        }

        // Stryker disable once all : Equivalent — value is always non-null (T : notnull), so ?. and ?? are defensive only; String mutation on empty fallback is unreachable
        return value?.ToString() ?? string.Empty;
    }
}
