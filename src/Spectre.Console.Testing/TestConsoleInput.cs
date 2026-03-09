namespace Spectre.Console.Testing;

/// <summary>
/// Represents a testable console input mechanism.
/// </summary>
public sealed class TestConsoleInput : IAnsiConsoleInput
{
    private readonly Queue<ConsoleKeyInfo> _input;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestConsoleInput"/> class.
    /// </summary>
    public TestConsoleInput()
    {
        _input = new Queue<ConsoleKeyInfo>();
    }

    /// <summary>
    /// Pushes the specified text to the input queue.
    /// </summary>
    /// <param name="input">The input string.</param>
    public void PushText(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        foreach (var character in input)
        {
            PushCharacter(character);
        }
    }

    /// <summary>
    /// Pushes the specified text followed by 'Enter' to the input queue.
    /// </summary>
    /// <param name="input">The input.</param>
    public void PushTextWithEnter(string input)
    {
        PushText(input);
        PushKey(ConsoleKey.Enter);
    }

    /// <summary>
    /// Pushes the specified character to the input queue.
    /// </summary>
    /// <param name="input">The input.</param>
    public void PushCharacter(char input)
    {
        var shift = char.IsUpper(input);
        _input.Enqueue(new ConsoleKeyInfo(input, (ConsoleKey)input, shift, alt: false, control: false));
    }

    // Keys whose ConsoleKey enum values coincide with printable ASCII characters
    // (e.g. UpArrow=38='&', DownArrow=40='(', F7=118='v').
    // A real terminal never delivers these as KeyChar — it sends ESC sequences that
    // the .NET runtime decodes to ConsoleKey.UpArrow etc. with KeyChar='\0'.
    // Emitting '\0' here matches that behaviour so input-reading code can safely
    // rely on char.IsControl(KeyChar) to distinguish character input from control input.
    private static readonly HashSet<ConsoleKey> _nonPrintingKeys =
    [
        ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.LeftArrow, ConsoleKey.RightArrow,
        ConsoleKey.Home, ConsoleKey.End, ConsoleKey.PageUp, ConsoleKey.PageDown,
        ConsoleKey.Insert, ConsoleKey.Delete, ConsoleKey.Enter, ConsoleKey.Escape,
        ConsoleKey.Tab, ConsoleKey.Backspace,
        ConsoleKey.F1,  ConsoleKey.F2,  ConsoleKey.F3,  ConsoleKey.F4,
        ConsoleKey.F5,  ConsoleKey.F6,  ConsoleKey.F7,  ConsoleKey.F8,
        ConsoleKey.F9,  ConsoleKey.F10, ConsoleKey.F11, ConsoleKey.F12,
    ];

    /// <summary>
    /// Pushes the specified key to the input queue.
    /// Navigation, function, and other non-printing keys are emitted with
    /// <c>KeyChar = '\0'</c> to match real terminal behaviour.
    /// </summary>
    /// <param name="input">The input.</param>
    public void PushKey(ConsoleKey input)
    {
        var ch = _nonPrintingKeys.Contains(input) ? '\0' : (char)input;
        // Stryker disable once Block : Equivalent mutant — any ConsoleKey not in _nonPrintingKeys
        // whose (char) cast is a control character already yields ch='\0' via the cast itself;
        // removing this guard produces the same '\0' result. The guard is defensive dead code.
        if (char.IsControl(ch))
        {
            ch = '\0';
        }

        _input.Enqueue(new ConsoleKeyInfo(ch, input, false, false, false));
    }

    /// <summary>
    /// Pushes the specified key to the input queue.
    /// </summary>
    /// <param name="consoleKeyInfo">The input.</param>
    public void PushKey(ConsoleKeyInfo consoleKeyInfo)
    {
        _input.Enqueue(consoleKeyInfo);
    }

    /// <inheritdoc/>
    public bool IsKeyAvailable()
    {
        return _input.Count > 0;
    }

    /// <inheritdoc/>
    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        if (_input.Count == 0)
        {
            throw new InvalidOperationException("No input available.");
        }

        return _input.Dequeue();
    }

    /// <inheritdoc/>
    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        return Task.FromResult(ReadKey(intercept));
    }
}