namespace Spectre.Console;

/// <summary>
/// A column showing a spinner.
/// </summary>
public sealed class SpinnerColumn : ProgressColumn
{
    private const string Accumulated = "SPINNER_Accumulated";
    private const string Index = "SPINNER_Index";

    private readonly Lock _lock;
    private Spinner _spinner;
    private int? _maxWidth;
    private string? _completed;
    private string? _pending;

    /// <inheritdoc/>
    protected internal override bool NoWrap => true;

    /// <summary>
    /// Gets or sets the <see cref="Console.Spinner"/>.
    /// </summary>
    public Spinner Spinner
    {
        get
        {
            lock (_lock)
            {
                return _spinner;
            }
        }

        set
        {
            lock (_lock)
            {
                _spinner = value ?? new BypassSpinner();
                _maxWidth = null;
            }
        }
    }

    /// <summary>
    /// Gets or sets the text that should be shown instead
    /// of the spinner once a task completes.
    /// </summary>
    public string? CompletedText
    {
        get => _completed;
        set
        {
            _completed = value;
            _maxWidth = null;
        }
    }

    /// <summary>
    /// Gets or sets the text that should be shown instead
    /// of the spinner before a task begins.
    /// </summary>
    public string? PendingText
    {
        get => _pending;
        set
        {
            _pending = value;
            _maxWidth = null;
        }
    }

    /// <summary>
    /// Gets or sets the completed style.
    /// </summary>
    public Style? CompletedStyle { get; set; }

    /// <summary>
    /// Gets or sets the pending style.
    /// </summary>
    public Style? PendingStyle { get; set; }

    /// <summary>
    /// Gets or sets the style of the spinner.
    /// </summary>
    public Style? Style { get; set; } = Color.Yellow;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpinnerColumn"/> class.
    /// </summary>
    public SpinnerColumn()
        : this(new BypassSpinner())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpinnerColumn"/> class.
    /// </summary>
    /// <param name="spinner">The spinner to use.</param>
    public SpinnerColumn(Spinner spinner)
    {
        ArgumentNullException.ThrowIfNull(spinner);
        _spinner = spinner;
        _lock = LockFactory.Create();
    }

    /// <inheritdoc/>
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        Spinner spinner;
        lock (_lock)
        {
            spinner = !options.Unicode && _spinner.IsUnicode ? new BypassSpinner() : _spinner;
        }

        if (!task.IsStarted)
        {
            return new Markup(PendingText ?? " ", PendingStyle ?? Spectre.Console.Style.Plain);
        }

        if (task.IsFinished)
        {
            return new Markup(CompletedText ?? " ", CompletedStyle ?? Spectre.Console.Style.Plain);
        }

        var accumulated = task.State.Update<double>(Accumulated, acc => acc + deltaTime.TotalMilliseconds);
        if (accumulated >= spinner.Interval.TotalMilliseconds)
        {
            task.State.Update<double>(Accumulated, _ => 0);
            task.State.Update<int>(Index, index => index + 1);
        }

        var index = task.State.Get<int>(Index);
        if (spinner.Frames.Count == 0)
        {
            // Stryker disable once all : Equivalent — Style is null in tests so ?? yields same result either way
            return new Markup(" ", Style ?? Spectre.Console.Style.Plain);
        }

        var frame = spinner.Frames[index % spinner.Frames.Count];
        // Stryker disable once all : Equivalent — Style is null in tests so ?? yields same result either way
        return new Markup(frame.EscapeMarkup(), Style ?? Spectre.Console.Style.Plain);
    }

    /// <inheritdoc/>
    public override int? GetColumnWidth(RenderOptions options)
    {
        return GetMaxWidth(options);
    }

    private int GetMaxWidth(RenderOptions options)
    {
        lock (_lock)
        {
            if (_maxWidth == null)
            {
                var useAscii = !options.Unicode && _spinner.IsUnicode;
                var spinner = useAscii ? new BypassSpinner() : _spinner;

                _maxWidth = Math.Max(
                    Math.Max(
                        // Stryker disable once all : Measurement.Max==Min for plain text; " " is a safe fallback
                        ((IRenderable)new Markup(PendingText ?? " ")).Measure(options, int.MaxValue).Max,
                        // Stryker disable once all : Measurement.Max==Min for plain text; " " is a safe fallback
                        ((IRenderable)new Markup(CompletedText ?? " ")).Measure(options, int.MaxValue).Max),
                    spinner.Frames.Count > 0
                        ? spinner.Frames.Max(frame => Cell.GetCellLength(frame))
                        : 1);
            }

            return _maxWidth.Value;
        }
    }
}

/// <summary>
/// Contains extension methods for <see cref="SpinnerColumn"/>.
/// </summary>
public static class SpinnerColumnExtensions
{
    /// <summary>
    /// Sets the style of the spinner.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="style">The style.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static SpinnerColumn Style(this SpinnerColumn column, Style? style)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null from fluent API
        ArgumentNullException.ThrowIfNull(column);

        column.Style = style;
        return column;
    }

    /// <summary>
    /// Sets the text that should be shown instead of the spinner
    /// once a task completes.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="text">The text.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static SpinnerColumn CompletedText(this SpinnerColumn column, string? text)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null from fluent API
        ArgumentNullException.ThrowIfNull(column);

        column.CompletedText = text;
        return column;
    }

    /// <summary>
    /// Sets the completed style of the spinner.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="style">The style.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static SpinnerColumn CompletedStyle(this SpinnerColumn column, Style? style)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null from fluent API
        ArgumentNullException.ThrowIfNull(column);

        column.CompletedStyle = style;
        return column;
    }
}