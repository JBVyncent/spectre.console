namespace Spectre.Console.Network;

/// <summary>
/// An <see cref="IAnsiConsoleOutput"/> implementation that sends output over a network transport.
/// </summary>
internal sealed class NetworkConsoleOutput : IAnsiConsoleOutput
{
    private readonly int _width;
    private readonly int _height;

    /// <inheritdoc/>
    public TextWriter Writer { get; }

    /// <inheritdoc/>
    public bool IsTerminal => false;

    /// <inheritdoc/>
    public int Width => _width;

    /// <inheritdoc/>
    public int Height => _height;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConsoleOutput"/> class.
    /// </summary>
    /// <param name="writer">The text writer that sends output over the network.</param>
    /// <param name="width">The remote terminal width.</param>
    /// <param name="height">The remote terminal height.</param>
    public NetworkConsoleOutput(TextWriter writer, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(writer);

        Writer = writer;
        _width = width;
        _height = height;
    }

    /// <inheritdoc/>
    public void SetEncoding(Encoding encoding)
    {
        // Network transport always uses UTF-8; encoding changes are ignored.
    }
}
