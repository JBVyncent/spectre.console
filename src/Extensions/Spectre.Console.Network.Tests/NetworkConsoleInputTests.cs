using Spectre.Console.Network.Protocol;
using Spectre.Console.Network.Transport;

namespace Spectre.Console.Network.Tests;

public sealed class NetworkConsoleInputTests
{
    [Fact]
    public async Task ReadKeyAsync_Should_Return_KeyPress()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        var key = new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false);
        await transport.SendAsync(NetworkMessageSerializer.CreateKeyPress(key));
        stream.Position = 0;

        var result = await input.ReadKeyAsync(false, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.KeyChar.Should().Be('x');
        result.Value.Key.Should().Be(ConsoleKey.X);
    }

    [Fact]
    public async Task ReadKeyAsync_Should_Handle_Resize_And_Continue()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);

        int? resizedWidth = null;
        int? resizedHeight = null;
        var input = new NetworkConsoleInput(transport, (w, h) =>
        {
            resizedWidth = w;
            resizedHeight = h;
        });

        // Send resize then keypress
        await transport.SendAsync(NetworkMessageSerializer.CreateResize(160, 50));
        await transport.SendAsync(NetworkMessageSerializer.CreateKeyPress(
            new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false)));
        stream.Position = 0;

        var result = await input.ReadKeyAsync(false, CancellationToken.None);

        // Should have processed resize callback and returned the keypress
        resizedWidth.Should().Be(160);
        resizedHeight.Should().Be(50);
        result.Should().NotBeNull();
        result!.Value.KeyChar.Should().Be('a');
    }

    [Fact]
    public async Task ReadKeyAsync_Should_Return_Null_On_Disconnect()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        await transport.SendAsync(NetworkMessageSerializer.CreateDisconnect());
        stream.Position = 0;

        var result = await input.ReadKeyAsync(false, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadKeyAsync_Should_Return_Null_On_Closed_Stream()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        var result = await input.ReadKeyAsync(false, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadKeyAsync_Should_Skip_Unknown_Message_Types()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        // Send an Output message (not expected by input) then a KeyPress
        await transport.SendAsync(NetworkMessageSerializer.CreateOutput("ignored"));
        await transport.SendAsync(NetworkMessageSerializer.CreateKeyPress(
            new ConsoleKeyInfo('z', ConsoleKey.Z, false, false, false)));
        stream.Position = 0;

        var result = await input.ReadKeyAsync(false, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Value.KeyChar.Should().Be('z');
    }

    [Fact]
    public void IsKeyAvailable_Should_Be_False_Initially()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        input.IsKeyAvailable().Should().BeFalse();
    }

    [Fact]
    public void EnqueueKey_Should_Make_Key_Available()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        input.EnqueueKey(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false));

        input.IsKeyAvailable().Should().BeTrue();
    }

    [Fact]
    public void ReadKey_Should_Return_Enqueued_Key()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        var key = new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false);
        input.EnqueueKey(key);

        var result = input.ReadKey(false);
        result.Should().NotBeNull();
        result!.Value.KeyChar.Should().Be('q');
        input.IsKeyAvailable().Should().BeFalse();
    }

    [Fact]
    public async Task ReadKeyAsync_Should_Return_Enqueued_Key_First()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        var key = new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false);
        input.EnqueueKey(key);

        var result = await input.ReadKeyAsync(false, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Value.KeyChar.Should().Be('b');
    }

    [Fact]
    public void ReadKey_From_Transport_Should_Work()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport);

        var key = new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false);
        transport.SendAsync(NetworkMessageSerializer.CreateKeyPress(key)).GetAwaiter().GetResult();
        stream.Position = 0;

        var result = input.ReadKey(false);
        result.Should().NotBeNull();
        result!.Value.KeyChar.Should().Be('t');
    }

    [Fact]
    public void Constructor_Should_Throw_On_Null_Transport()
    {
        var act = () => new NetworkConsoleInput(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadKeyAsync_Resize_Without_Callback_Should_Still_Skip()
    {
        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        var input = new NetworkConsoleInput(transport, null);

        await transport.SendAsync(NetworkMessageSerializer.CreateResize(100, 30));
        await transport.SendAsync(NetworkMessageSerializer.CreateKeyPress(
            new ConsoleKeyInfo('k', ConsoleKey.K, false, false, false)));
        stream.Position = 0;

        var result = await input.ReadKeyAsync(false, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Value.KeyChar.Should().Be('k');
    }
}
