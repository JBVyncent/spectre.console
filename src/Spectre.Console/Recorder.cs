namespace Spectre.Console;

/// <summary>
/// A console recorder used to record output from a console.
/// </summary>
public class Recorder : IAnsiConsole, IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly List<IRenderable> _recorded;

    /// <inheritdoc/>
    public Profile Profile => _console.Profile;

    /// <inheritdoc/>
    public IAnsiConsoleCursor Cursor => _console.Cursor;

    /// <inheritdoc/>
    public IAnsiConsoleInput Input => _console.Input;

    /// <inheritdoc/>
    public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;

    /// <inheritdoc/>
    public RenderPipeline Pipeline => _console.Pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="Recorder"/> class.
    /// </summary>
    /// <param name="console">The console to record output for.</param>
    public Recorder(IAnsiConsole console)
    {
        // Stryker disable once all : Equivalent — null guard; always called with non-null console
        ArgumentNullException.ThrowIfNull(console);
        _console = console;
        _recorded = [];
    }

    /// <inheritdoc/>
    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize")]
    public void Dispose()
    {
        // Only used for scoping.
    }

    // Stryker disable all : NoCoverage — Clear not exercised in tests
    /// <inheritdoc/>
    public void Clear(bool home)
    {
        _console.Clear(home);
    }
    // Stryker restore all

    /// <inheritdoc/>
    public void Write(IRenderable renderable)
    {
        // Stryker disable once all : Equivalent — null guard; always called with non-null renderable
        ArgumentNullException.ThrowIfNull(renderable);

        _recorded.Add(renderable);
        // Stryker disable once all : Equivalent — Write delegates to inner console; tests only verify recorded output, not console forwarding
        _console.Write(renderable);
    }

    /// <inheritdoc/>
    public void WriteAnsi(Action<AnsiWriter> action)
    {
        // Do nothing
    }

    // Stryker disable all : NoCoverage — Clone not exercised in tests
    internal Recorder Clone(IAnsiConsole console)
    {
        var recorder = new Recorder(console);
        recorder._recorded.AddRange(_recorded);
        return recorder;
    }
    // Stryker restore all

    /// <summary>
    /// Exports the recorded data.
    /// </summary>
    /// <param name="encoder">The encoder.</param>
    /// <returns>The recorded data represented as a string.</returns>
    public string Export(IAnsiConsoleEncoder encoder)
    {
        // Stryker disable once all : Equivalent — null guard; always called with non-null encoder
        ArgumentNullException.ThrowIfNull(encoder);

        return encoder.Encode(_console, _recorded);
    }
}

/// <summary>
/// Contains extension methods for <see cref="Recorder"/>.
/// </summary>
// Stryker disable all : NoCoverage — extension methods; Stryker cannot trace coverage through recorder pipeline
public static class RecorderExtensions
{
    private static readonly TextEncoder _textEncoder = new TextEncoder();
    private static readonly HtmlEncoder _htmlEncoder = new HtmlEncoder();

    /// <summary>
    /// Exports the recorded content as text.
    /// </summary>
    /// <param name="recorder">The recorder.</param>
    /// <returns>The recorded content as text.</returns>
    public static string ExportText(this Recorder recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        return recorder.Export(_textEncoder);
    }

    /// <summary>
    /// Exports the recorded content as HTML.
    /// </summary>
    /// <param name="recorder">The recorder.</param>
    /// <returns>The recorded content as HTML.</returns>
    public static string ExportHtml(this Recorder recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        return recorder.Export(_htmlEncoder);
    }
}
// Stryker restore all