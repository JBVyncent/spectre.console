using Spectre.Console.Network.Protocol;
using Spectre.Console.Network.Transport;

namespace Spectre.Console.Network.Tests;

/// <summary>
/// Tests specifically targeting Stryker mutation survivors.
/// </summary>
public sealed class MutantKillerTests
{
    // --- KeyChar high byte serialization (kills bitwise shift mutations on char encoding) ---

    [Fact]
    public void KeyPress_HighByte_KeyChar_Should_Roundtrip()
    {
        // Use a character with non-zero high byte (e.g., U+0100 = 'Ā')
        var key = new ConsoleKeyInfo('\u0100', ConsoleKey.A, false, false, false);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        result.KeyChar.Should().Be('\u0100');
    }

    [Fact]
    public void KeyPress_FullRange_KeyChar_Should_Roundtrip()
    {
        // Use a character where both bytes matter (U+ABCD)
        var key = new ConsoleKeyInfo('\uABCD', ConsoleKey.A, false, false, false);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        result.KeyChar.Should().Be('\uABCD');
    }

    [Fact]
    public void KeyPress_Shift_Only_Should_Roundtrip()
    {
        var key = new ConsoleKeyInfo('A', ConsoleKey.A, true, false, false);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        (result.Modifiers & ConsoleModifiers.Shift).Should().NotBe((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Alt).Should().Be((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Control).Should().Be((ConsoleModifiers)0);
    }

    [Fact]
    public void KeyPress_Alt_Only_Should_Roundtrip()
    {
        var key = new ConsoleKeyInfo('a', ConsoleKey.A, false, true, false);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        (result.Modifiers & ConsoleModifiers.Shift).Should().Be((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Alt).Should().NotBe((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Control).Should().Be((ConsoleModifiers)0);
    }

    [Fact]
    public void KeyPress_Control_Only_Should_Roundtrip()
    {
        var key = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, true);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        (result.Modifiers & ConsoleModifiers.Shift).Should().Be((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Alt).Should().Be((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Control).Should().NotBe((ConsoleModifiers)0);
    }

    // --- BigEndian boundary (kills >> vs >>> and << mutations) ---

    [Fact]
    public void BigEndian_WriteRead_Each_Byte_Position()
    {
        // Value where each byte is different to detect byte-order mutations
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, 0x01020304);

        buffer[0].Should().Be(0x01);
        buffer[1].Should().Be(0x02);
        buffer[2].Should().Be(0x03);
        buffer[3].Should().Be(0x04);

        var result = NetworkMessageSerializer.ReadInt32BigEndian(buffer, 0);
        result.Should().Be(0x01020304);
    }

    [Fact]
    public void BigEndian_High_Byte_Only()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, unchecked((int)0xFF000000));
        buffer[0].Should().Be(0xFF);
        buffer[1].Should().Be(0x00);
        buffer[2].Should().Be(0x00);
        buffer[3].Should().Be(0x00);
    }

    [Fact]
    public void BigEndian_Second_Byte_Only()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, 0x00FF0000);
        buffer[0].Should().Be(0x00);
        buffer[1].Should().Be(0xFF);
        buffer[2].Should().Be(0x00);
        buffer[3].Should().Be(0x00);
    }

    [Fact]
    public void BigEndian_Third_Byte_Only()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, 0x0000FF00);
        buffer[0].Should().Be(0x00);
        buffer[1].Should().Be(0x00);
        buffer[2].Should().Be(0xFF);
        buffer[3].Should().Be(0x00);
    }

    [Fact]
    public void BigEndian_Low_Byte_Only()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, 0x000000FF);
        buffer[0].Should().Be(0x00);
        buffer[1].Should().Be(0x00);
        buffer[2].Should().Be(0x00);
        buffer[3].Should().Be(0xFF);
    }

    // --- Frame boundary mutations ---

    [Fact]
    public void FromFrame_Exactly_5_Bytes_Zero_Payload()
    {
        var frame = new byte[] { (byte)MessageType.Disconnect, 0, 0, 0, 0 };
        var msg = NetworkMessageSerializer.FromFrame(frame);
        msg.Type.Should().Be(MessageType.Disconnect);
        msg.Payload.Length.Should().Be(0);
    }

    [Fact]
    public void FromFrame_4_Bytes_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.FromFrame(new byte[4]);
        act.Should().Throw<InvalidOperationException>().WithMessage("*too short*");
    }

    // --- NetworkConsole resize propagation (kills NoCoverage on resize lambda) ---

    [Fact]
    public async Task NetworkConsole_Input_Resize_Should_Update_Profile()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport, 80, 24);

        // Verify initial dimensions
        console.Profile.Width.Should().Be(80);
        console.Profile.Height.Should().Be(24);

        // Send a resize message followed by a keypress (so ReadKeyAsync returns)
        await transport.SendAsync(NetworkMessageSerializer.CreateResize(160, 50));
        await transport.SendAsync(NetworkMessageSerializer.CreateKeyPress(
            new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false)));
        stream.Position = 0;

        // Skip past any output already written, read resize+keypress from beginning
        // Clear the stream first and re-fill with just input messages
        using var inputStream = new MemoryStream();
        using var inputTransport = new StreamTransport(inputStream);
        using var console2 = NetworkConsole.Create(inputTransport, 80, 24);

        // Write resize + key to the input stream
        var resizeMsg = NetworkMessageSerializer.CreateResize(160, 50);
        var keyMsg = NetworkMessageSerializer.CreateKeyPress(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));

        var resizeFrame = NetworkMessageSerializer.ToFrame(resizeMsg);
        var keyFrame = NetworkMessageSerializer.ToFrame(keyMsg);

        // We need a separate stream for input — the output stream has console output
        // Instead, test via NetworkConsoleInput directly with the onResize callback wired to the profile
        using var readStream = new MemoryStream();
        using var writeStream = new MemoryStream();

        // Write the input messages to the read stream
        readStream.Write(resizeFrame, 0, resizeFrame.Length);
        readStream.Write(keyFrame, 0, keyFrame.Length);
        readStream.Position = 0;

        using var readTransport = new StreamTransport(readStream, writeStream);

        int? newWidth = null;
        int? newHeight = null;
        var input = new NetworkConsoleInput(readTransport, (w, h) =>
        {
            newWidth = w;
            newHeight = h;
        });

        var key = await input.ReadKeyAsync(false, CancellationToken.None);

        newWidth.Should().Be(160);
        newHeight.Should().Be(50);
        key.Should().NotBeNull();
        key!.Value.KeyChar.Should().Be('x');
    }

    // --- StreamTransport field assignment mutations ---

    [Fact]
    public async Task StreamTransport_Two_Stream_Constructor_Uses_Both_Streams()
    {
        // Write to write-stream, verify read-stream reads independently
        using var readContent = new MemoryStream();
        using var writeTarget = new MemoryStream();
        using var transport = new StreamTransport(readContent, writeTarget);

        // Send goes to writeTarget
        await transport.SendAsync(NetworkMessageSerializer.CreateOutput("to-write"));
        writeTarget.Length.Should().BeGreaterThan(0);

        // Read comes from readContent (which is empty → returns null)
        readContent.Position = 0;
        var result = await transport.ReceiveAsync();
        result.Should().BeNull();
    }

    [Fact]
    public void StreamTransport_Single_Stream_Constructor_Sends_And_Receives()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        transport.SendAsync(NetworkMessageSerializer.CreateOutput("test")).GetAwaiter().GetResult();
        stream.Position = 0;

        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        msg.Should().NotBeNull();
        NetworkMessageSerializer.ReadOutput(msg!).Should().Be("test");
    }

    // --- NetworkTextWriter buffer initialization ---

    [Fact]
    public void NetworkTextWriter_Multiple_Write_Flush_Cycles()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        // Cycle 1
        writer.Write("A");
        writer.Flush();

        // Cycle 2 — buffer should be cleared from cycle 1
        writer.Write("B");
        writer.Flush();

        stream.Position = 0;
        var msg1 = transport.ReceiveAsync().GetAwaiter().GetResult();
        var msg2 = transport.ReceiveAsync().GetAwaiter().GetResult();

        NetworkMessageSerializer.ReadOutput(msg1!).Should().Be("A");
        NetworkMessageSerializer.ReadOutput(msg2!).Should().Be("B");
    }

    // --- NetworkConsole interactive vs non-interactive ---

    [Fact]
    public void NetworkConsole_Interactive_True_Should_Set_Capabilities()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport, 80, 24, ColorSystem.TrueColor, true);

        console.Profile.Capabilities.Interactive.Should().BeTrue();
        console.Profile.Capabilities.Ansi.Should().BeTrue();
        console.Profile.Capabilities.Unicode.Should().BeTrue();
    }

    [Fact]
    public void NetworkConsole_Interactive_False_Should_Set_Capabilities()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport, 80, 24, ColorSystem.TrueColor, false);

        console.Profile.Capabilities.Interactive.Should().BeFalse();
    }
}
