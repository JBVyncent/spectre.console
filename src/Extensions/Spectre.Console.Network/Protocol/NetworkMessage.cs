namespace Spectre.Console.Network.Protocol;

/// <summary>
/// Represents a message in the network console protocol.
/// </summary>
public sealed class NetworkMessage
{
    /// <summary>
    /// Gets the message type.
    /// </summary>
    public MessageType Type { get; }

    /// <summary>
    /// Gets the message payload.
    /// </summary>
    public byte[] Payload { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkMessage"/> class.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="payload">The message payload.</param>
    public NetworkMessage(MessageType type, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        Type = type;
        Payload = payload;
    }
}
