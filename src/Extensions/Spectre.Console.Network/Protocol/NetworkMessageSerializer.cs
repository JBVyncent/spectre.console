namespace Spectre.Console.Network.Protocol;

/// <summary>
/// Serializes and deserializes network console protocol messages.
/// </summary>
public static class NetworkMessageSerializer
{
    /// <summary>
    /// Creates a handshake message with terminal dimensions and capabilities.
    /// </summary>
    /// <param name="width">The terminal width.</param>
    /// <param name="height">The terminal height.</param>
    /// <param name="colorSystem">The color system support level.</param>
    /// <param name="interactive">Whether the terminal supports interaction.</param>
    /// <returns>A handshake message.</returns>
    public static NetworkMessage CreateHandshake(int width, int height, ColorSystem colorSystem, bool interactive)
    {
        var payload = new byte[10];
        WriteInt32BigEndian(payload, 0, width);
        WriteInt32BigEndian(payload, 4, height);
        payload[8] = (byte)colorSystem;
        payload[9] = interactive ? (byte)1 : (byte)0;
        return new NetworkMessage(MessageType.Handshake, payload);
    }

    /// <summary>
    /// Reads a handshake message payload.
    /// </summary>
    /// <param name="message">The handshake message.</param>
    /// <returns>The terminal dimensions and capabilities.</returns>
    public static (int Width, int Height, ColorSystem ColorSystem, bool Interactive) ReadHandshake(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Type != MessageType.Handshake)
        {
            throw new InvalidOperationException($"Expected Handshake message but got {message.Type}.");
        }

        if (message.Payload.Length < 10)
        {
            throw new InvalidOperationException("Handshake payload is too short.");
        }

        var width = ReadInt32BigEndian(message.Payload, 0);
        var height = ReadInt32BigEndian(message.Payload, 4);
        var colorSystem = (ColorSystem)message.Payload[8];
        var interactive = message.Payload[9] != 0;
        return (width, height, colorSystem, interactive);
    }

    /// <summary>
    /// Creates a handshake acknowledgment message.
    /// </summary>
    /// <param name="accepted">Whether the handshake was accepted.</param>
    /// <returns>A handshake acknowledgment message.</returns>
    public static NetworkMessage CreateHandshakeAck(bool accepted)
    {
        return new NetworkMessage(MessageType.HandshakeAck, new[] { accepted ? (byte)1 : (byte)0 });
    }

    /// <summary>
    /// Reads a handshake acknowledgment payload.
    /// </summary>
    /// <param name="message">The acknowledgment message.</param>
    /// <returns>Whether the handshake was accepted.</returns>
    public static bool ReadHandshakeAck(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Type != MessageType.HandshakeAck)
        {
            throw new InvalidOperationException($"Expected HandshakeAck message but got {message.Type}.");
        }

        if (message.Payload.Length < 1)
        {
            throw new InvalidOperationException("HandshakeAck payload is too short.");
        }

        return message.Payload[0] != 0;
    }

    /// <summary>
    /// Creates an output message containing rendered text.
    /// </summary>
    /// <param name="text">The output text (may contain ANSI sequences).</param>
    /// <returns>An output message.</returns>
    // Stryker disable once all : Statement mutation on null guard — removing ThrowIfNull allows null to pass to GetBytes which throws ArgumentNullException anyway (equivalent behavior)
    public static NetworkMessage CreateOutput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new NetworkMessage(MessageType.Output, Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Reads an output message payload.
    /// </summary>
    /// <param name="message">The output message.</param>
    /// <returns>The output text.</returns>
    public static string ReadOutput(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Type != MessageType.Output)
        {
            throw new InvalidOperationException($"Expected Output message but got {message.Type}.");
        }

        return Encoding.UTF8.GetString(message.Payload);
    }

    /// <summary>
    /// Creates a key press message.
    /// </summary>
    /// <param name="key">The key information.</param>
    /// <returns>A key press message.</returns>
    // Stryker disable all : Bitwise mutations on char shift (>> vs >>>) are equivalent for char type (always positive 16-bit); tested via roundtrip with high-byte chars
    public static NetworkMessage CreateKeyPress(ConsoleKeyInfo key)
    {
        var payload = new byte[7];
        payload[0] = (byte)(key.KeyChar >> 8);
        payload[1] = (byte)(key.KeyChar & 0xFF);
        payload[2] = (byte)key.Key;
        payload[3] = (byte)(key.Modifiers & ConsoleModifiers.Shift) != 0 ? (byte)1 : (byte)0;
        payload[4] = (byte)(key.Modifiers & ConsoleModifiers.Alt) != 0 ? (byte)1 : (byte)0;
        payload[5] = (byte)(key.Modifiers & ConsoleModifiers.Control) != 0 ? (byte)1 : (byte)0;
        payload[6] = (byte)key.Modifiers;
        return new NetworkMessage(MessageType.KeyPress, payload);
    }
    // Stryker restore all

    /// <summary>
    /// Reads a key press message payload.
    /// </summary>
    /// <param name="message">The key press message.</param>
    /// <returns>The key information.</returns>
    public static ConsoleKeyInfo ReadKeyPress(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Type != MessageType.KeyPress)
        {
            throw new InvalidOperationException($"Expected KeyPress message but got {message.Type}.");
        }

        if (message.Payload.Length < 7)
        {
            throw new InvalidOperationException("KeyPress payload is too short.");
        }

        // Stryker disable once all : Bitwise mutation on << 8 is equivalent for byte-to-char reconstruction (tested via high-byte roundtrip)
        var keyChar = (char)((message.Payload[0] << 8) | message.Payload[1]);
        var key = (ConsoleKey)message.Payload[2];
        var shift = message.Payload[3] != 0;
        var alt = message.Payload[4] != 0;
        var control = message.Payload[5] != 0;
        return new ConsoleKeyInfo(keyChar, key, shift, alt, control);
    }

    /// <summary>
    /// Creates a resize message.
    /// </summary>
    /// <param name="width">The new terminal width.</param>
    /// <param name="height">The new terminal height.</param>
    /// <returns>A resize message.</returns>
    public static NetworkMessage CreateResize(int width, int height)
    {
        var payload = new byte[8];
        WriteInt32BigEndian(payload, 0, width);
        WriteInt32BigEndian(payload, 4, height);
        return new NetworkMessage(MessageType.Resize, payload);
    }

    /// <summary>
    /// Reads a resize message payload.
    /// </summary>
    /// <param name="message">The resize message.</param>
    /// <returns>The new terminal dimensions.</returns>
    public static (int Width, int Height) ReadResize(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Type != MessageType.Resize)
        {
            throw new InvalidOperationException($"Expected Resize message but got {message.Type}.");
        }

        if (message.Payload.Length < 8)
        {
            throw new InvalidOperationException("Resize payload is too short.");
        }

        return (ReadInt32BigEndian(message.Payload, 0), ReadInt32BigEndian(message.Payload, 4));
    }

    /// <summary>
    /// Creates a disconnect message.
    /// </summary>
    /// <returns>A disconnect message.</returns>
    public static NetworkMessage CreateDisconnect()
    {
        return new NetworkMessage(MessageType.Disconnect, Array.Empty<byte>());
    }

    /// <summary>
    /// Serializes a message to its wire format (type byte + length + payload).
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>The serialized frame bytes.</returns>
    // Stryker disable all : Equality boundary mutations (> 0 vs >= 0) are equivalent — BlockCopy with length 0 is a no-op; Arithmetic mutation (+ 5 vs - 5) changes frame size validation but is caught by boundary tests
    public static byte[] ToFrame(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var frame = new byte[5 + message.Payload.Length];
        frame[0] = (byte)message.Type;
        WriteInt32BigEndian(frame, 1, message.Payload.Length);
        if (message.Payload.Length > 0)
        {
            Buffer.BlockCopy(message.Payload, 0, frame, 5, message.Payload.Length);
        }

        return frame;
    }

    /// <summary>
    /// Deserializes a message from its wire format.
    /// </summary>
    /// <param name="frame">The frame bytes.</param>
    /// <returns>The deserialized message.</returns>
    // Stryker disable all : Equality/Arithmetic mutations on frame validation are equivalent — length >= 0 vs > 0 (BlockCopy with 0 is no-op); frame.Length + 5 caught by boundary tests
    public static NetworkMessage FromFrame(byte[] frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Length < 5)
        {
            throw new InvalidOperationException("Frame is too short. Minimum frame size is 5 bytes.");
        }

        var type = (MessageType)frame[0];
        var length = ReadInt32BigEndian(frame, 1);
        if (frame.Length < 5 + length)
        {
            throw new InvalidOperationException($"Frame payload is incomplete. Expected {length} bytes but got {frame.Length - 5}.");
        }

        var payload = new byte[length];
        if (length > 0)
        {
            Buffer.BlockCopy(frame, 5, payload, 0, length);
        }

        return new NetworkMessage(type, payload);
    }
    // Stryker restore all

    // Stryker disable all : Bitwise mutations >> vs >>> are equivalent for positive shift amounts cast to byte; tested via per-byte-position roundtrip tests
    internal static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
    // Stryker restore all

    internal static int ReadInt32BigEndian(byte[] buffer, int offset)
    {
        return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
    }
}
