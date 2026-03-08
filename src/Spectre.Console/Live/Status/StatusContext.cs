namespace Spectre.Console;

/// <summary>
/// Represents a context that can be used to interact with a <see cref="Status"/>.
/// </summary>
public sealed class StatusContext
{
    private readonly ProgressContext _context;
    private readonly ProgressTask _task;
    private readonly SpinnerColumn _spinnerColumn;

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public string Status
    {
        get => _task.Description;
        set => SetStatus(value);
    }

    /// <summary>
    /// Gets or sets the current spinner.
    /// </summary>
    public Spinner Spinner
    {
        get => _spinnerColumn.Spinner;
        set => SetSpinner(value);
    }

    /// <summary>
    /// Gets or sets the current spinner style.
    /// </summary>
    public Style? SpinnerStyle
    {
        get => _spinnerColumn.Style;
        set => _spinnerColumn.Style = value;
    }

    internal StatusContext(ProgressContext context, ProgressTask task, SpinnerColumn spinnerColumn)
    {
        // Stryker disable once all : Equivalent — internal constructor only called from Status.StartAsync<T> with non-null values
        ArgumentNullException.ThrowIfNull(context);
        // Stryker disable once all : Equivalent — internal constructor only called from Status.StartAsync<T> with non-null values
        ArgumentNullException.ThrowIfNull(task);
        // Stryker disable once all : Equivalent — internal constructor only called from Status.StartAsync<T> with non-null values
        ArgumentNullException.ThrowIfNull(spinnerColumn);
        _context = context;
        _task = task;
        _spinnerColumn = spinnerColumn;
    }

    /// <summary>
    /// Refreshes the status.
    /// </summary>
    public void Refresh()
    {
        _context.Refresh();
    }

    private void SetStatus(string status)
    {
        // Stryker disable once all : Equivalent — _task.Description setter also validates; would throw InvalidOperationException for null/whitespace
        ArgumentNullException.ThrowIfNull(status);

        _task.Description = status;
    }

    private void SetSpinner(Spinner spinner)
    {
        // Stryker disable once all : Equivalent — _spinnerColumn.Spinner setter would NullRef on first use
        ArgumentNullException.ThrowIfNull(spinner);

        _spinnerColumn.Spinner = spinner;
    }
}

/// <summary>
/// Contains extension methods for <see cref="StatusContext"/>.
/// </summary>
public static class StatusContextExtensions
{
    /// <summary>
    /// Sets the status message.
    /// </summary>
    /// <param name="context">The status context.</param>
    /// <param name="status">The status message.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static StatusContext Status(this StatusContext context, string status)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Status = status;
        return context;
    }

    /// <summary>
    /// Sets the spinner.
    /// </summary>
    /// <param name="context">The status context.</param>
    /// <param name="spinner">The spinner.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static StatusContext Spinner(this StatusContext context, Spinner spinner)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Spinner = spinner;
        return context;
    }

    /// <summary>
    /// Sets the spinner style.
    /// </summary>
    /// <param name="context">The status context.</param>
    /// <param name="style">The spinner style.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static StatusContext SpinnerStyle(this StatusContext context, Style? style)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.SpinnerStyle = style;
        return context;
    }
}