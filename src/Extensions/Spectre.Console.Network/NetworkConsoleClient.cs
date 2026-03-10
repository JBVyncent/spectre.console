namespace Spectre.Console.Network;

/// <summary>
/// A TCP client that connects to a <see cref="NetworkConsoleServer"/>
/// and bridges remote console output to a local terminal.
/// </summary>
// Stryker disable all : NoCoverage — TCP client I/O; requires actual network connections and interactive terminal
public sealed class NetworkConsoleClient : IDisposable
{
    private readonly INetworkTransport _transport;
    private bool _disposed;

    private NetworkConsoleClient(INetworkTransport transport)
    {
        _transport = transport;
    }

    /// <summary>
    /// Connects to a network console server and performs the handshake.
    /// </summary>
    /// <param name="host">The server hostname or IP address.</param>
    /// <param name="port">The server port.</param>
    /// <param name="width">The local terminal width.</param>
    /// <param name="height">The local terminal height.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A connected <see cref="NetworkConsoleClient"/>.</returns>
    public static async Task<NetworkConsoleClient> ConnectAsync(
        string host,
        int port,
        int width = 80,
        int height = 24,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port).ConfigureAwait(false);
        var transport = new StreamTransport(tcpClient.GetStream());

        // Send handshake
        var handshake = NetworkMessageSerializer.CreateHandshake(width, height, ColorSystem.TrueColor, true);
        await transport.SendAsync(handshake, cancellationToken).ConfigureAwait(false);

        // Wait for acknowledgment
        var ack = await transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        if (ack == null || !NetworkMessageSerializer.ReadHandshakeAck(ack))
        {
            throw new InvalidOperationException("Server rejected the connection.");
        }

        return new NetworkConsoleClient(transport);
    }

    /// <summary>
    /// Runs the client bridge, forwarding remote output to the local console
    /// and local input to the remote server.
    /// </summary>
    /// <param name="localConsole">The local console to render output to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    public async Task RunAsync(IAnsiConsole localConsole, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localConsole);

        while (!cancellationToken.IsCancellationRequested && _transport.IsConnected)
        {
            var message = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (message == null)
            {
                break;
            }

            switch (message.Type)
            {
                case MessageType.Output:
                    var text = NetworkMessageSerializer.ReadOutput(message);
                    localConsole.Profile.Out.Writer.Write(text);
                    localConsole.Profile.Out.Writer.Flush();
                    break;

                case MessageType.Disconnect:
                    return;
            }
        }
    }

    /// <summary>
    /// Sends a key press to the remote server.
    /// </summary>
    /// <param name="key">The key to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the send operation.</returns>
    public async Task SendKeyAsync(ConsoleKeyInfo key, CancellationToken cancellationToken = default)
    {
        var message = NetworkMessageSerializer.CreateKeyPress(key);
        await _transport.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a terminal resize notification to the remote server.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the send operation.</returns>
    public async Task SendResizeAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        var message = NetworkMessageSerializer.CreateResize(width, height);
        await _transport.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _transport.Dispose();
        _disposed = true;
    }
}
// Stryker restore all
