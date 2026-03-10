namespace Spectre.Console.Network.Protocol;

/// <summary>
/// Defines the types of messages in the network console protocol.
/// </summary>
public enum MessageType : byte
{
    /// <summary>
    /// Client-to-server handshake with terminal dimensions and capabilities.
    /// </summary>
    Handshake = 1,

    /// <summary>
    /// Server-to-client handshake acknowledgment.
    /// </summary>
    HandshakeAck = 2,

    /// <summary>
    /// Server-to-client output text (raw ANSI stream).
    /// </summary>
    Output = 3,

    /// <summary>
    /// Client-to-server key press event.
    /// </summary>
    KeyPress = 4,

    /// <summary>
    /// Client-to-server terminal resize event.
    /// </summary>
    Resize = 5,

    /// <summary>
    /// Connection termination (either direction).
    /// </summary>
    Disconnect = 6,
}
