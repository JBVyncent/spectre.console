using Spectre.Console.Network.Protocol;
using Spectre.Console.Network.Transport;

namespace Spectre.Console.Network.Tests;

public sealed class NetworkConsoleTests
{
    [Fact]
    public void Create_Should_Return_Configured_Console()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport, 120, 40, ColorSystem.TrueColor, true);

        console.Profile.Width.Should().Be(120);
        console.Profile.Height.Should().Be(40);
        console.Profile.Capabilities.Ansi.Should().BeTrue();
        console.Profile.Capabilities.Unicode.Should().BeTrue();
        console.Profile.Capabilities.Interactive.Should().BeTrue();
    }

    [Fact]
    public void Create_NonInteractive_Should_Work()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport, 80, 24, ColorSystem.Standard, false);

        console.Profile.Width.Should().Be(80);
        console.Profile.Height.Should().Be(24);
        console.Profile.Capabilities.Interactive.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_Throw_On_Null_Transport()
    {
        var act = () => NetworkConsole.Create(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Write_Should_Send_Output_Over_Transport()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Write(new Text("Hello, Network!"));

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        msg.Should().NotBeNull();
        msg!.Type.Should().Be(MessageType.Output);
        NetworkMessageSerializer.ReadOutput(msg).Should().Contain("Hello, Network!");
    }

    [Fact]
    public void Write_Table_Should_Send_Output()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        var table = new Table().AddColumn("Name").AddColumn("Value");
        table.AddRow("Key", "123");
        console.Write(table);

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        msg.Should().NotBeNull();
        var output = NetworkMessageSerializer.ReadOutput(msg!);
        output.Should().Contain("Name");
        output.Should().Contain("Value");
        output.Should().Contain("Key");
        output.Should().Contain("123");
    }

    [Fact]
    public void Clear_Should_Send_Output()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Clear(true);

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        // Clear emits ANSI clear sequences which get sent as output
        msg.Should().NotBeNull();
        msg!.Type.Should().Be(MessageType.Output);
    }

    [Fact]
    public void WriteAnsi_Should_Send_Output()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.WriteAnsi(writer => writer.Write("direct ANSI"));

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        msg.Should().NotBeNull();
        var output = NetworkMessageSerializer.ReadOutput(msg!);
        output.Should().Contain("direct ANSI");
    }

    [Fact]
    public void Transport_Property_Should_Return_Transport()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Transport.Should().BeSameAs(transport);
    }

    [Fact]
    public void Pipeline_Should_Not_Be_Null()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Pipeline.Should().NotBeNull();
    }

    [Fact]
    public void Cursor_Should_Not_Be_Null()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Cursor.Should().NotBeNull();
    }

    [Fact]
    public void ExclusivityMode_Should_Not_Be_Null()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.ExclusivityMode.Should().NotBeNull();
    }

    [Fact]
    public void Input_Should_Not_Be_Null()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Input.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptAsync_Should_Perform_Handshake()
    {
        // Simulate a client sending handshake, then read server's ack
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        // Write handshake to stream
        await transport.SendAsync(NetworkMessageSerializer.CreateHandshake(100, 30, ColorSystem.TrueColor, true));
        stream.Position = 0;

        var console = await NetworkConsole.AcceptAsync(transport);

        console.Profile.Width.Should().Be(100);
        console.Profile.Height.Should().Be(30);
        console.Profile.Capabilities.Interactive.Should().BeTrue();

        console.Dispose();
    }

    [Fact]
    public async Task AcceptAsync_Null_Transport_Should_Throw()
    {
        var act = async () => await NetworkConsole.AcceptAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AcceptAsync_Closed_Connection_Should_Throw()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        using var transport = new StreamTransport(stream);

        var act = async () => await NetworkConsole.AcceptAsync(transport);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*closed*handshake*");
    }

    [Fact]
    public void Create_Default_Parameters_Should_Work()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Profile.Width.Should().Be(80);
        console.Profile.Height.Should().Be(24);
    }

    [Fact]
    public void Dispose_Should_Dispose_Transport()
    {
        var stream = new MemoryStream();
        var transport = new StreamTransport(stream);
        var console = NetworkConsole.Create(transport);

        console.Dispose();

        transport.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Write_Markup_Should_Send_ANSI_Output()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Write(new Markup("[bold]Bold text[/]"));

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        msg.Should().NotBeNull();
        var output = NetworkMessageSerializer.ReadOutput(msg!);
        output.Should().Contain("Bold text");
    }

    [Fact]
    public void Write_Rule_Should_Send_Output()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = NetworkConsole.Create(transport);

        console.Write(new Rule("Title"));

        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        msg.Should().NotBeNull();
        var output = NetworkMessageSerializer.ReadOutput(msg!);
        output.Should().Contain("Title");
    }
}
