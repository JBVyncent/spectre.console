namespace Spectre.Console;

/// <summary>
/// Represents console output.
/// </summary>
// Stryker disable all : NoCoverage — console output wrapper; Stryker cannot trace coverage through output pipeline
public sealed class AnsiConsoleOutput : IAnsiConsoleOutput
{
    /// <inheritdoc/>
    public TextWriter Writer { get; }

    /// <inheritdoc/>
    public bool IsTerminal
    {
        get
        {
            if (Writer.IsStandardOut())
            {
                return !System.Console.IsOutputRedirected;
            }

            if (Writer.IsStandardError())
            {
                return !System.Console.IsErrorRedirected;
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public int Width => ConsoleHelper.GetSafeWidth();

    /// <inheritdoc/>
    public int Height => ConsoleHelper.GetSafeHeight();

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiConsoleOutput"/> class.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    public AnsiConsoleOutput(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Writer = writer;
    }

    /// <inheritdoc/>
    public void SetEncoding(Encoding encoding)
    {
        if (Writer.IsStandardOut() || Writer.IsStandardError())
        {
            System.Console.OutputEncoding = encoding;
        }
    }
}
// Stryker restore all