namespace Spectre.Console;

/// <summary>
/// An interactive <see cref="IRenderable"/> that wraps a <see cref="SelectionPrompt{T}"/>
/// so it can be embedded inside a <see cref="LiveDisplay"/> or composed with other
/// renderables without blocking the thread.
/// </summary>
/// <remarks>
/// Obtain an instance via <see cref="SelectionPrompt{T}.AsRenderable"/>.
/// Drive the prompt by calling <see cref="Update"/> with each key the user presses,
/// then read the result via <see cref="GetResult"/> once <see cref="IsDone"/> is <c>true</c>.
/// </remarks>
/// <typeparam name="T">The prompt result type.</typeparam>
public sealed class SelectionPromptRenderable<T> : IRenderable
    where T : notnull
{
    private readonly SelectionPrompt<T> _prompt;
    private readonly IAnsiConsole _console;
    private readonly ListPromptState<T> _state;

    /// <summary>
    /// Gets a value indicating whether the user has submitted a selection or cancelled.
    /// Once <c>true</c>, <see cref="GetResult"/> may be called.
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the prompt was aborted (e.g. via Escape with
    /// a configured cancel result). Always <c>false</c> when no cancel result is set.
    /// </summary>
    public bool IsCancelled => _state.IsCancelled;

    internal SelectionPromptRenderable(
        SelectionPrompt<T> prompt,
        IAnsiConsole console,
        ListPromptState<T> state)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(state);
        _prompt = prompt;
        _console = console;
        _state = state;
    }

    /// <summary>
    /// Processes a key press and updates the internal prompt state.
    /// </summary>
    /// <param name="key">The key the user pressed.</param>
    /// <returns>
    /// <c>true</c> if the state changed and the renderable should be refreshed;
    /// <c>false</c> if nothing changed (key was ignored) or the prompt is already done.
    /// </returns>
    public bool Update(ConsoleKeyInfo key)
    {
        if (IsDone)
        {
            return false;
        }

        var result = ((IListPromptStrategy<T>)_prompt).HandleInput(key, _state);

        if (result == ListPromptInputResult.Submit)
        {
            IsDone = true;
            return true;
        }

        if (result == ListPromptInputResult.Abort)
        {
            _state.Cancel();
            IsDone = true;
            return true;
        }

        return _state.Update(key);
    }

    /// <summary>
    /// Returns the selected value. Must only be called after <see cref="IsDone"/> is <c>true</c>.
    /// </summary>
    /// <returns>
    /// The cancel-result value when <see cref="IsCancelled"/> is <c>true</c> and a cancel
    /// result was configured; otherwise the item the cursor was on when submitted.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the prompt has not yet been submitted or cancelled.
    /// </exception>
    public T GetResult()
    {
        if (!IsDone)
        {
            throw new InvalidOperationException(
                "The prompt has not been submitted yet. " +
                "Wait until IsDone is true before calling GetResult().");
        }

        if (_state.IsCancelled && _prompt.CancelResult is not null)
        {
            return _prompt.CancelResult();
        }

        return _state.Items[_state.Index].Data;
    }

    // Stryker disable all : NoCoverage — rendering pipeline; visual correctness covered by snapshot tests
    /// <inheritdoc/>
    Measurement IRenderable.Measure(RenderOptions options, int maxWidth)
    {
        var inner = ListPrompt<T>.ComputeRenderable(_prompt, _console, _state);
        return ((IRenderable)inner).Measure(options, maxWidth);
    }

    /// <inheritdoc/>
    IEnumerable<Segment> IRenderable.Render(RenderOptions options, int maxWidth)
    {
        var inner = ListPrompt<T>.ComputeRenderable(_prompt, _console, _state);
        return ((IRenderable)inner).Render(options, maxWidth);
    }
    // Stryker restore all
}
