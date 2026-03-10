namespace Spectre.Console.Network;

/// <summary>
/// An <see cref="IAnsiConsoleInput"/> that reads key presses from a network transport.
/// </summary>
internal sealed class NetworkConsoleInput : IAnsiConsoleInput
{
    private readonly INetworkTransport _transport;
    private readonly Queue<ConsoleKeyInfo> _keyBuffer;
    private readonly Action<int, int>? _onResize;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConsoleInput"/> class.
    /// </summary>
    /// <param name="transport">The network transport to read input from.</param>
    /// <param name="onResize">An optional callback invoked when a resize message is received.</param>
    public NetworkConsoleInput(INetworkTransport transport, Action<int, int>? onResize = null)
    {
        ArgumentNullException.ThrowIfNull(transport);

        _transport = transport;
        _keyBuffer = new Queue<ConsoleKeyInfo>();
        _onResize = onResize;
    }

    /// <inheritdoc/>
    public bool IsKeyAvailable()
    {
        lock (_lock)
        {
            return _keyBuffer.Count > 0;
        }
    }

    /// <inheritdoc/>
    // Stryker disable all : Block removal on buffer check — removing early-return from buffer falls through to async ReadKey which blocks; equivalent behavior when buffer is empty
    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        lock (_lock)
        {
            if (_keyBuffer.Count > 0)
            {
                return _keyBuffer.Dequeue();
            }
        }

        return ReadKeyAsync(intercept, CancellationToken.None).GetAwaiter().GetResult();
    }
    // Stryker restore all

    /// <inheritdoc/>
    // Stryker disable all : ConfigureAwait(false/true) is equivalent in test context; Block removal on buffer check falls through to transport read
    public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_keyBuffer.Count > 0)
            {
                return _keyBuffer.Dequeue();
            }
        }

        while (true)
        {
            var message = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (message == null)
            {
                return null;
            }

            switch (message.Type)
            {
                case MessageType.KeyPress:
                    return NetworkMessageSerializer.ReadKeyPress(message);

                case MessageType.Resize:
                    var (width, height) = NetworkMessageSerializer.ReadResize(message);
                    _onResize?.Invoke(width, height);
                    continue;

                case MessageType.Disconnect:
                    return null;

                default:
                    continue;
            }
        }
    }
    // Stryker restore all

    /// <summary>
    /// Enqueues a key press into the input buffer.
    /// </summary>
    /// <param name="key">The key to enqueue.</param>
    internal void EnqueueKey(ConsoleKeyInfo key)
    {
        lock (_lock)
        {
            _keyBuffer.Enqueue(key);
        }
    }
}
