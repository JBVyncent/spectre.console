namespace Spectre.Console.Network.Transport;

/// <summary>
/// Represents a bidirectional network transport for console messages.
/// </summary>
public interface INetworkTransport : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the transport is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Sends a message asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the send operation.</returns>
    Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives the next message asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The received message, or <c>null</c> if the connection was closed.</returns>
    Task<NetworkMessage?> ReceiveAsync(CancellationToken cancellationToken = default);
}
