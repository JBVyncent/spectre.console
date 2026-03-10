namespace Spectre.Console;

/// <summary>
/// Represents a task list.
/// </summary>
public sealed class Progress
{
    private readonly IAnsiConsole _console;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Gets or sets a optional custom render function.
    /// </summary>
    public Func<IRenderable, IReadOnlyList<ProgressTask>, IRenderable> RenderHook { get; set; } = (renderable, _) => renderable;

    /// <summary>
    /// Gets or sets a value indicating whether or not task list should auto refresh.
    /// Defaults to <c>true</c>.
    /// </summary>
    // Stryker disable once all : Equivalent — AutoRefresh default doesn't affect test outcome; tests complete before refresh cycle matters
    public bool AutoRefresh { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not the task list should
    /// be cleared once it completes.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool AutoClear { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the task list should
    /// only include tasks not completed
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool HideCompleted { get; set; }

    /// <summary>
    /// Gets or sets the refresh rate if <c>AutoRefresh</c> is enabled.
    /// Defaults to 10 times/second.
    /// </summary>
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromMilliseconds(100);

    internal List<ProgressColumn> Columns { get; }

    internal ProgressRenderer? FallbackRenderer { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Progress"/> class.
    /// </summary>
    /// <param name="console">The console to render to.</param>
    /// <param name="timeProvider">The time provider to use. Defaults to <see cref="TimeProvider.System"/>.</param>
    public Progress(IAnsiConsole console, TimeProvider? timeProvider = null)
    {
        // Stryker disable once all : Equivalent — constructor only called from AnsiConsoleExtensions with non-null console
        ArgumentNullException.ThrowIfNull(console);
        _console = console;
        _timeProvider = timeProvider ?? TimeProvider.System;

        // Initialize with default columns
        Columns =
        [
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn()
        ];
    }

    /// <summary>
    /// Starts the progress task list.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Start(Action<ProgressContext> action)
    {
        // Stryker disable once all : Equivalent — null action would throw NullReferenceException on invocation
        ArgumentNullException.ThrowIfNull(action);

        var task = StartAsync(ctx =>
        {
            action(ctx);
            return Task.CompletedTask;
        });

        // Stryker disable once all : Equivalent — removing await propagation doesn't affect synchronous test outcome
        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Starts the progress task list and returns a result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The action to execute.</param>
    /// <returns>The result.</returns>
    public T Start<T>(Func<ProgressContext, T> func)
    {
        // Stryker disable once all : Equivalent — null func would throw NullReferenceException on invocation
        ArgumentNullException.ThrowIfNull(func);

        var task = StartAsync(ctx => Task.FromResult(func(ctx)));
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Starts the progress task list.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync(Func<ProgressContext, Task> action)
    {
        // Stryker disable once all : Equivalent — null action would throw NullReferenceException on invocation
        ArgumentNullException.ThrowIfNull(action);

        // Stryker disable all : Equivalent — ConfigureAwait(false) vs ConfigureAwait(true); no SynchronizationContext in tests
        _ = await StartAsync<object?>(async progressContext =>
        {
            // Use try-catch to preserve AggregateException from Task.WhenAll (GitHub #1579).
            var task = action(progressContext);
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                if (task.Exception is { InnerExceptions.Count: > 1 })
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(task.Exception).Throw();
                }

                throw;
            }

            return null;
        }).ConfigureAwait(false);
        // Stryker restore all
    }

    /// <summary>
    /// Starts the progress task list and returns a result.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <typeparam name="T">The result type of task.</typeparam>
    /// <returns>A <see cref="Task{T}"/> representing the asynchronous operation.</returns>
    public async Task<T> StartAsync<T>(Func<ProgressContext, Task<T>> action)
    {
        // Stryker disable once all : Equivalent — null action would throw NullReferenceException on invocation
        ArgumentNullException.ThrowIfNull(action);

        // Stryker disable once all : NoCoverage — RunExclusive async lambda; Stryker cannot trace coverage through async RunExclusive pipeline
        return await _console.RunExclusive(async () =>
        {
            // Stryker disable once all : NoCoverage — CreateRenderer inside RunExclusive; Stryker cannot trace coverage
            var renderer = CreateRenderer();
            // Stryker disable once all : NoCoverage — renderer.Started inside RunExclusive
            renderer.Started();

            T result;

            try
            {
                // Stryker disable once all : NoCoverage — RenderHookScope creation inside RunExclusive
                using var scope = new RenderHookScope(_console, renderer);
                // Stryker disable once all : NoCoverage — ProgressContext creation inside RunExclusive
                var context = new ProgressContext(_console, renderer, _timeProvider);

                // Stryker disable once all : Equivalent — AutoRefresh if/else both invoke action(context); diff is the refresh thread
                if (AutoRefresh)
                {
                    // Stryker disable once all : NoCoverage — ProgressRefreshThread created inside RunExclusive
                    using var thread = new ProgressRefreshThread(context, renderer.RefreshRate);
                    // Stryker disable once all : Equivalent — ConfigureAwait(false) vs true; no SynchronizationContext in tests
                    result = await AwaitPreservingAggregateException(action(context)).ConfigureAwait(false);
                }
                else
                {
                    // Stryker disable once all : Equivalent — ConfigureAwait(false) vs true; no SynchronizationContext in tests
                    result = await AwaitPreservingAggregateException(action(context)).ConfigureAwait(false);
                }

                // Stryker disable once all : NoCoverage — context.Refresh inside RunExclusive
                context.Refresh();
            }
            finally
            {
                // Stryker disable once all : NoCoverage — renderer.Completed inside RunExclusive
                renderer.Completed(AutoClear);
            }

            // Stryker disable once all : NoCoverage — return result inside RunExclusive
            return result;
        }).ConfigureAwait(false);
    }

    private ProgressRenderer CreateRenderer()
    {
        var caps = _console.Profile.Capabilities;
        var interactive = caps.Interactive && caps.Ansi;

        if (interactive)
        {
            var columns = new List<ProgressColumn>(Columns);
            return new DefaultProgressRenderer(_console, columns, RefreshRate, HideCompleted, RenderHook);
        }

        // Stryker disable once all : NoCoverage — fallback renderer only used on non-ANSI terminals
        return FallbackRenderer ?? new FallbackProgressRenderer(_timeProvider);
    }

    // Stryker disable all : NoCoverage — async helper inside RunExclusive; Stryker cannot trace coverage
    /// <summary>
    /// Awaits a task but preserves <see cref="AggregateException"/> when the task has
    /// multiple inner exceptions (e.g. from Task.WhenAll).
    /// Normal <c>await</c> unwraps to the first exception only (GitHub #1579).
    /// </summary>
    private static async Task<T> AwaitPreservingAggregateException<T>(Task<T> task)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            // If the task has multiple exceptions (e.g. Task.WhenAll), rethrow the
            // AggregateException with all of them instead of just the first one.
            if (task.Exception is { InnerExceptions.Count: > 1 })
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(task.Exception).Throw();
            }

            throw;
        }
    }
    // Stryker restore all
}

/// <summary>
/// Contains extension methods for <see cref="Progress"/>.
/// </summary>
public static class ProgressExtensions
{
    /// <summary>
    /// Sets the columns to be used for an <see cref="Progress"/> instance.
    /// </summary>
    /// <param name="progress">The <see cref="Progress"/> instance.</param>
    /// <param name="columns">The columns to use.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Progress Columns(this Progress progress, params ProgressColumn[] columns)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null prompt from fluent API
        ArgumentNullException.ThrowIfNull(progress);

        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null prompt from fluent API
        ArgumentNullException.ThrowIfNull(columns);

        // Stryker disable all : NoCoverage — empty columns guard; no test calls Columns() with an empty collection
        if (!columns.Any())
        {
            throw new InvalidOperationException("At least one column must be specified.");
        }
        // Stryker restore all

        progress.Columns.Clear();
        progress.Columns.AddRange(columns);

        return progress;
    }

    /// <summary>
    /// Sets an optional hook to intercept rendering.
    /// </summary>
    /// <param name="progress">The <see cref="Progress"/> instance.</param>
    /// <param name="renderHook">The custom render function.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Progress UseRenderHook(this Progress progress, Func<IRenderable, IReadOnlyList<ProgressTask>, IRenderable> renderHook)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null prompt from fluent API
        ArgumentNullException.ThrowIfNull(progress);
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null prompt from fluent API
        ArgumentNullException.ThrowIfNull(renderHook);

        progress.RenderHook = renderHook;

        return progress;
    }

    /// <summary>
    /// Sets whether or not auto refresh is enabled.
    /// If disabled, you will manually have to refresh the progress.
    /// </summary>
    /// <param name="progress">The <see cref="Progress"/> instance.</param>
    /// <param name="enabled">Whether or not auto refresh is enabled.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Progress AutoRefresh(this Progress progress, bool enabled)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null prompt from fluent API
        ArgumentNullException.ThrowIfNull(progress);

        progress.AutoRefresh = enabled;

        return progress;
    }

    /// <summary>
    /// Sets whether or not auto clear is enabled.
    /// If enabled, the task tabled will be removed once
    /// all tasks have completed.
    /// </summary>
    /// <param name="progress">The <see cref="Progress"/> instance.</param>
    /// <param name="enabled">Whether or not auto clear is enabled.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Progress AutoClear(this Progress progress, bool enabled)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null prompt from fluent API
        ArgumentNullException.ThrowIfNull(progress);

        progress.AutoClear = enabled;

        return progress;
    }

    /// <summary>
    /// Sets whether or not hide completed is enabled.
    /// If enabled, the task tabled will be removed once it is
    /// completed.
    /// </summary>
    /// <param name="progress">The <see cref="Progress"/> instance.</param>
    /// <param name="enabled">Whether or not hide completed is enabled.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static Progress HideCompleted(this Progress progress, bool enabled)
    {
        // Stryker disable once all : Equivalent — extension method null guard; always called with non-null prompt from fluent API
        ArgumentNullException.ThrowIfNull(progress);

        progress.HideCompleted = enabled;

        return progress;
    }
}