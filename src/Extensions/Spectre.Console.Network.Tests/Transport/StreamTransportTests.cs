using Spectre.Console.Network.Protocol;
using Spectre.Console.Network.Transport;

namespace Spectre.Console.Network.Tests.Transport;

public sealed class StreamTransportTests
{
    [Fact]
    public async Task SendAsync_And_ReceiveAsync_Should_Roundtrip()
    {
        // Arrange — use a pair of streams (pipe-like) via MemoryStream
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        var original = NetworkMessageSerializer.CreateOutput("Hello!");

        // Act — send, then rewind and receive
        await transport.SendAsync(original);
        stream.Position = 0;
        var received = await transport.ReceiveAsync();

        // Assert
        received.Should().NotBeNull();
        received!.Type.Should().Be(MessageType.Output);
        NetworkMessageSerializer.ReadOutput(received).Should().Be("Hello!");
    }

    [Fact]
    public async Task SendAsync_Multiple_Messages_Should_Roundtrip()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        await transport.SendAsync(NetworkMessageSerializer.CreateOutput("First"));
        await transport.SendAsync(NetworkMessageSerializer.CreateOutput("Second"));
        await transport.SendAsync(NetworkMessageSerializer.CreateDisconnect());

        stream.Position = 0;

        var msg1 = await transport.ReceiveAsync();
        var msg2 = await transport.ReceiveAsync();
        var msg3 = await transport.ReceiveAsync();

        msg1.Should().NotBeNull();
        NetworkMessageSerializer.ReadOutput(msg1!).Should().Be("First");
        msg2.Should().NotBeNull();
        NetworkMessageSerializer.ReadOutput(msg2!).Should().Be("Second");
        msg3.Should().NotBeNull();
        msg3!.Type.Should().Be(MessageType.Disconnect);
    }

    [Fact]
    public async Task ReceiveAsync_On_Closed_Stream_Should_Return_Null()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        using var transport = new StreamTransport(stream);

        var result = await transport.ReceiveAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReceiveAsync_Partial_Header_Should_Return_Null()
    {
        // Only 3 bytes when header needs 5
        using var stream = new MemoryStream(new byte[] { 1, 0, 0 });
        using var transport = new StreamTransport(stream);

        var result = await transport.ReceiveAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReceiveAsync_Partial_Payload_Should_Return_Null()
    {
        // Header says 10 bytes of payload, but only 2 present
        var frame = new byte[] { (byte)MessageType.Output, 0, 0, 0, 10, 0x41, 0x42 };
        using var stream = new MemoryStream(frame);
        using var transport = new StreamTransport(stream);

        var result = await transport.ReceiveAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReceiveAsync_Negative_Length_Should_Throw()
    {
        // Negative length in big-endian: 0xFF 0xFF 0xFF 0xFF = -1
        var frame = new byte[] { (byte)MessageType.Output, 0xFF, 0xFF, 0xFF, 0xFF };
        using var stream = new MemoryStream(frame);
        using var transport = new StreamTransport(stream);

        var act = async () => await transport.ReceiveAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*negative*");
    }

    [Fact]
    public async Task SendAsync_After_Dispose_Should_Throw()
    {
        var stream = new MemoryStream();
        var transport = new StreamTransport(stream);
        transport.Dispose();

        var act = async () => await transport.SendAsync(NetworkMessageSerializer.CreateDisconnect());
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ReceiveAsync_After_Dispose_Should_Throw()
    {
        var stream = new MemoryStream();
        var transport = new StreamTransport(stream);
        transport.Dispose();

        var act = async () => await transport.ReceiveAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void IsConnected_Should_Be_True_Initially()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        transport.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void IsConnected_Should_Be_False_After_Dispose()
    {
        var stream = new MemoryStream();
        var transport = new StreamTransport(stream);
        transport.Dispose();

        transport.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Should_Throw_On_Null_Stream()
    {
        var act = () => new StreamTransport(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Two_Streams_Should_Throw_On_Null_ReadStream()
    {
        using var write = new MemoryStream();
        var act = () => new StreamTransport(null!, write);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Two_Streams_Should_Throw_On_Null_WriteStream()
    {
        using var read = new MemoryStream();
        var act = () => new StreamTransport(read, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_Should_Throw_On_Null_Message()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        var act = async () => await transport.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Separate_Read_Write_Streams_Should_Work()
    {
        using var writeBuffer = new MemoryStream();
        using var readBuffer = new MemoryStream();
        using var transport = new StreamTransport(readBuffer, writeBuffer);

        // Send writes to writeBuffer
        await transport.SendAsync(NetworkMessageSerializer.CreateOutput("Test"));
        writeBuffer.Length.Should().BeGreaterThan(0);

        // Nothing to read from readBuffer (it's empty)
        readBuffer.Position = 0;
        var result = await transport.ReceiveAsync();
        result.Should().BeNull();
    }

    [Fact]
    public void Dispose_Twice_Should_Not_Throw()
    {
        var stream = new MemoryStream();
        var transport = new StreamTransport(stream);
        transport.Dispose();

        var act = () => transport.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_Should_Dispose_Both_Streams()
    {
        var readStream = new MemoryStream();
        var writeStream = new MemoryStream();
        var transport = new StreamTransport(readStream, writeStream);

        transport.Dispose();

        // MemoryStream.Dispose prevents further writes
        var actRead = () => readStream.WriteByte(1);
        actRead.Should().Throw<ObjectDisposedException>();

        var actWrite = () => writeStream.WriteByte(1);
        actWrite.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task ReceiveAsync_Zero_Length_Payload_Should_Work()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        await transport.SendAsync(NetworkMessageSerializer.CreateDisconnect());
        stream.Position = 0;

        var msg = await transport.ReceiveAsync();
        msg.Should().NotBeNull();
        msg!.Type.Should().Be(MessageType.Disconnect);
        msg.Payload.Should().BeEmpty();
    }

    [Fact]
    public async Task Handshake_Full_Flow_Over_Stream()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        // Client sends handshake
        await transport.SendAsync(NetworkMessageSerializer.CreateHandshake(120, 40, ColorSystem.TrueColor, true));
        // Server sends ack
        await transport.SendAsync(NetworkMessageSerializer.CreateHandshakeAck(true));

        stream.Position = 0;

        // Read handshake
        var handshake = await transport.ReceiveAsync();
        handshake.Should().NotBeNull();
        var (w, h, cs, inter) = NetworkMessageSerializer.ReadHandshake(handshake!);
        w.Should().Be(120);
        h.Should().Be(40);
        cs.Should().Be(ColorSystem.TrueColor);
        inter.Should().BeTrue();

        // Read ack
        var ack = await transport.ReceiveAsync();
        ack.Should().NotBeNull();
        NetworkMessageSerializer.ReadHandshakeAck(ack!).Should().BeTrue();
    }
}
