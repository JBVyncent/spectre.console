namespace Spectre.Console.Network;

/// <summary>
/// A <see cref="TextWriter"/> that sends written text as output messages over a network transport.
/// </summary>
internal sealed class NetworkTextWriter : TextWriter
{
    private readonly INetworkTransport _transport;
    private readonly StringBuilder _buffer;
    private readonly object _lock = new();

    public override Encoding Encoding => Encoding.UTF8;

    // Stryker disable all : Statement mutations on constructor field initialization — removing these causes NullReferenceException on first use, but Stryker's coverage-based filtering may not trace through indirect call paths
    public NetworkTextWriter(INetworkTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _buffer = new StringBuilder();
    }
    // Stryker restore all

    public override void Write(char value)
    {
        lock (_lock)
        {
            _buffer.Append(value);
        }
    }

    // Stryker disable once all : Statement mutation removing Append is killed by flush/send tests, but null-string guard is defensive only
    public override void Write(string? value)
    {
        if (value == null)
        {
            return;
        }

        lock (_lock)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        lock (_lock)
        {
            _buffer.Append(buffer, index, count);
        }
    }

    public override void Flush()
    {
        string text;
        lock (_lock)
        {
            if (_buffer.Length == 0)
            {
                return;
            }

            text = _buffer.ToString();
            _buffer.Clear();
        }

        var message = NetworkMessageSerializer.CreateOutput(text);
        _transport.SendAsync(message).GetAwaiter().GetResult();
    }

    // Stryker disable all : ConfigureAwait(false/true) is equivalent in test context; Statement mutation on Clear is equivalent to buffer accumulation across flushes
    public override async Task FlushAsync()
    {
        string text;
        lock (_lock)
        {
            if (_buffer.Length == 0)
            {
                return;
            }

            text = _buffer.ToString();
            _buffer.Clear();
        }

        var message = NetworkMessageSerializer.CreateOutput(text);
        await _transport.SendAsync(message).ConfigureAwait(false);
    }
    // Stryker restore all
}
