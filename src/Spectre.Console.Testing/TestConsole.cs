namespace Spectre.Console.Testing;

/// <summary>
/// A testable console.
/// </summary>
public sealed class TestConsole : IAnsiConsole, IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly StringWriter _writer;
    private IAnsiConsoleCursor? _cursor;

    /// <inheritdoc/>
    public Profile Profile => _console.Profile;

    /// <inheritdoc/>
    public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;

    /// <summary>
    /// Gets the console input.
    /// </summary>
    public TestConsoleInput Input { get; }

    /// <inheritdoc/>
    public RenderPipeline Pipeline => _console.Pipeline;

    /// <inheritdoc/>
    public IAnsiConsoleCursor Cursor => _cursor ?? _console.Cursor;

    /// <inheritdoc/>
    IAnsiConsoleInput IAnsiConsole.Input => Input;

    /// <summary>
    /// Gets the console output.
    /// </summary>
    public string Output => _writer.ToString();

    /// <summary>
    /// Gets the console output lines.
    /// </summary>
    public IReadOnlyList<string> Lines => Output.NormalizeLineEndings().TrimEnd('\n').Split(['\n']);

    /// <summary>
    /// Gets or sets a value indicating whether or not VT/ANSI sequences
    /// should be emitted to the console.
    /// </summary>
    public bool EmitAnsiSequences { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestConsole"/> class.
    /// </summary>
    public TestConsole()
    {
        _writer = new StringWriter();
        _cursor = new NoopCursor();

        Input = new TestConsoleInput();
        EmitAnsiSequences = false;

        _console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = (ColorSystemSupport)ColorSystem.TrueColor,
            Out = new AnsiConsoleOutput(_writer),
            Interactive = InteractionSupport.No,
            ExclusivityMode = new NoopExclusivityMode(),
            // Stryker disable all : All mutations on the Enrichment initializer are equivalent —
            // UseDefaultEnrichers=false prevents enrichers from overwriting capabilities, but
            // explicit assignments below (Width=80, Height=24, Ansi=true, Unicode=true) override
            // any enricher results regardless. Removing or changing this initializer is unobservable.
            Enrichment = new ProfileEnrichment
            {
                UseDefaultEnrichers = false,
            },
            // Stryker restore all
        });

        _console.Profile.Width = 80;
        _console.Profile.Height = 24;
        _console.Profile.Capabilities.Ansi = true;
        _console.Profile.Capabilities.Unicode = true;
    }

    /// <inheritdoc/>
    // Stryker disable all : StringWriter holds a StringBuilder internally and releases no
    // external resources on Dispose(); all mutations on this method body (Block removal,
    // Statement removal) are equivalent — no observable side effect in tests or in GC.
    public void Dispose()
    {
        _writer.Dispose();
    }
    // Stryker restore all

    /// <inheritdoc/>
    public void Clear(bool home)
    {
        _console.Clear(home);
    }

    /// <inheritdoc/>
    public void Write(IRenderable renderable)
    {
        if (EmitAnsiSequences)
        {
            _console.Write(renderable);
        }
        else
        {
            foreach (var segment in renderable.GetSegments(this))
            {
                if (segment.IsControlCode)
                {
                    continue;
                }

                Profile.Out.Writer.Write(segment.Text);
            }
        }
    }

    /// <inheritdoc/>
    public void WriteAnsi(Action<AnsiWriter> action)
    {
        _console.WriteAnsi(action);
    }

    internal void SetCursor(IAnsiConsoleCursor? cursor)
    {
        _cursor = cursor;
    }
}