namespace Spectre.Console.Network;

/// <summary>
/// A TCP server that accepts connections and creates <see cref="NetworkConsole"/>
/// instances for each connected client.
/// </summary>
// Stryker disable all : NoCoverage — TCP server I/O; requires actual network connections to test
public sealed class NetworkConsoleServer : IDisposable
{
    private readonly TcpListener _listener;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConsoleServer"/> class.
    /// </summary>
    /// <param name="port">The TCP port to listen on.</param>
    public NetworkConsoleServer(int port)
        : this(IPAddress.Any, port)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConsoleServer"/> class.
    /// </summary>
    /// <param name="address">The IP address to listen on.</param>
    /// <param name="port">The TCP port to listen on.</param>
    public NetworkConsoleServer(IPAddress address, int port)
    {
        ArgumentNullException.ThrowIfNull(address);
        _listener = new TcpListener(address, port);
    }

    /// <summary>
    /// Starts listening for connections.
    /// </summary>
    public void Start()
    {
        _listener.Start();
    }

    /// <summary>
    /// Accepts a client connection and performs the protocol handshake.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="NetworkConsole"/> for the connected client.</returns>
    public async Task<NetworkConsole> AcceptAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

#if NETSTANDARD2_0
        var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
#else
        var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
#endif
        var stream = client.GetStream();
        var transport = new StreamTransport(stream);
        return await NetworkConsole.AcceptAsync(transport, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops listening for connections.
    /// </summary>
    public void Stop()
    {
        _listener.Stop();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _listener.Stop();
        _disposed = true;
    }
}
// Stryker restore all
