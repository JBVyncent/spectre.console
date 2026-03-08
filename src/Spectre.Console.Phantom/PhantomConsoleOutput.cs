using System.Text;

namespace Spectre.Console.Phantom;

/// <summary>
/// An <see cref="IAnsiConsoleOutput"/> implementation that captures all output
/// and feeds it into a <see cref="PhantomTerminal"/> for screen-state assertions.
/// </summary>
public sealed class PhantomConsoleOutput : IAnsiConsoleOutput
{
    private readonly PhantomTerminal _terminal;
    private readonly StringBuilder _rawOutput = new();

    /// <summary>
    /// The virtual terminal that processes the captured output.
    /// </summary>
    public PhantomTerminal Terminal => _terminal;

    /// <summary>
    /// All raw output captured (including ANSI sequences).
    /// </summary>
    public string RawOutput => _rawOutput.ToString();

    /// <summary>
    /// The writer that captures output.
    /// </summary>
    public TextWriter Writer { get; }

    /// <summary>
    /// Whether the output target is a terminal (always true for Phantom).
    /// </summary>
    public bool IsTerminal => true;

    /// <summary>
    /// The width of the virtual terminal.
    /// </summary>
    public int Width => _terminal.Width;

    /// <summary>
    /// The height of the virtual terminal.
    /// </summary>
    public int Height => _terminal.Height;

    /// <summary>
    /// Sets the encoding (no-op for Phantom, always UTF-8).
    /// </summary>
    public void SetEncoding(Encoding encoding)
    {
        // No-op — we always use the string directly.
    }

    public PhantomConsoleOutput(PhantomTerminal terminal)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        _terminal = terminal;
        Writer = new PhantomTextWriter(this);
    }

    internal void Capture(string text)
    {
        _rawOutput.Append(text);
        _terminal.Write(text);
    }

    /// <summary>
    /// Clear all captured output and reset the terminal.
    /// </summary>
    public void Reset()
    {
        _rawOutput.Clear();
        _terminal.Reset();
    }

    private sealed class PhantomTextWriter : TextWriter
    {
        private readonly PhantomConsoleOutput _output;

        public override Encoding Encoding => Encoding.UTF8;

        public PhantomTextWriter(PhantomConsoleOutput output)
        {
            _output = output;
        }

        public override void Write(char value)
        {
            _output.Capture(value.ToString());
        }

        public override void Write(string? value)
        {
            if (value != null)
            {
                _output.Capture(value);
            }
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            _output.Capture(buffer.ToString());
        }

        public override void WriteLine(string? value)
        {
            if (value != null)
            {
                _output.Capture(value);
            }

            _output.Capture(NewLine);
        }

        public override void WriteLine()
        {
            _output.Capture(NewLine);
        }
    }
}
