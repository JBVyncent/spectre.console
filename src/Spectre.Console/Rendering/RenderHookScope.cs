namespace Spectre.Console.Rendering;

// Stryker disable all : NoCoverage — scope type exercised through LiveDisplay/prompt pipeline, Stryker cannot trace indirect coverage
/// <summary>
/// Represents a render hook scope.
/// </summary>
public sealed class RenderHookScope : IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly IRenderHook _hook;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderHookScope"/> class.
    /// </summary>
    /// <param name="console">The console to attach the render hook to.</param>
    /// <param name="hook">The render hook.</param>
    public RenderHookScope(IAnsiConsole console, IRenderHook hook)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(hook);
        _console = console;
        _hook = hook;
        _console.Pipeline.Attach(_hook);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _console.Pipeline.Detach(_hook);
    }
}
// Stryker restore all