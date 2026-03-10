using Spectre.Console.Network.Protocol;
using Spectre.Console.Network.Transport;

namespace Spectre.Console.Network.Tests;

public sealed class NetworkTextWriterTests
{
    [Fact]
    public void Write_And_Flush_Should_Send_Output_Message()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        // Act
        writer.Write("Hello");
        writer.Flush();

        // Assert
        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        msg.Should().NotBeNull();
        msg!.Type.Should().Be(MessageType.Output);
        NetworkMessageSerializer.ReadOutput(msg).Should().Be("Hello");
    }

    [Fact]
    public void Write_Char_Should_Buffer()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        writer.Write('A');
        writer.Write('B');
        writer.Write('C');
        writer.Flush();

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        NetworkMessageSerializer.ReadOutput(msg!).Should().Be("ABC");
    }

    [Fact]
    public void Write_CharArray_Should_Buffer()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        writer.Write(new[] { 'X', 'Y', 'Z' }, 1, 2);
        writer.Flush();

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        NetworkMessageSerializer.ReadOutput(msg!).Should().Be("YZ");
    }

    [Fact]
    public void Write_Null_String_Should_Be_Ignored()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        writer.Write((string?)null);
        writer.Flush();

        // Nothing sent since buffer was empty
        stream.Length.Should().Be(0);
    }

    [Fact]
    public void Write_CharArray_Null_Should_Throw()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        var act = () => writer.Write((char[])null!, 0, 0);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Flush_With_Empty_Buffer_Should_Not_Send()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        writer.Flush();

        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task FlushAsync_Should_Send_Buffered_Content()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        writer.Write("async test");
        await writer.FlushAsync();

        stream.Position = 0;
        var msg = await transport.ReceiveAsync();
        NetworkMessageSerializer.ReadOutput(msg!).Should().Be("async test");
    }

    [Fact]
    public async Task FlushAsync_Empty_Buffer_Should_Not_Send()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        await writer.FlushAsync();

        stream.Length.Should().Be(0);
    }

    [Fact]
    public void Encoding_Should_Be_UTF8()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        writer.Encoding.Should().Be(System.Text.Encoding.UTF8);
    }

    [Fact]
    public void Multiple_Flushes_Should_Send_Separate_Messages()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var writer = new NetworkTextWriter(transport);

        writer.Write("First");
        writer.Flush();
        writer.Write("Second");
        writer.Flush();

        stream.Position = 0;
        var msg1 = transport.ReceiveAsync().GetAwaiter().GetResult();
        var msg2 = transport.ReceiveAsync().GetAwaiter().GetResult();

        NetworkMessageSerializer.ReadOutput(msg1!).Should().Be("First");
        NetworkMessageSerializer.ReadOutput(msg2!).Should().Be("Second");
    }
}
