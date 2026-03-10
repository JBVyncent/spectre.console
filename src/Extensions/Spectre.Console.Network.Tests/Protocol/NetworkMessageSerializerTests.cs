using Spectre.Console.Network.Protocol;

namespace Spectre.Console.Network.Tests.Protocol;

public sealed class NetworkMessageSerializerTests
{
    // --- Handshake ---

    [Fact]
    public void Handshake_Should_Roundtrip()
    {
        var msg = NetworkMessageSerializer.CreateHandshake(120, 40, ColorSystem.TrueColor, true);
        var (width, height, colorSystem, interactive) = NetworkMessageSerializer.ReadHandshake(msg);

        width.Should().Be(120);
        height.Should().Be(40);
        colorSystem.Should().Be(ColorSystem.TrueColor);
        interactive.Should().BeTrue();
    }

    [Fact]
    public void Handshake_NonInteractive_Should_Roundtrip()
    {
        var msg = NetworkMessageSerializer.CreateHandshake(80, 24, ColorSystem.Standard, false);
        var (_, _, colorSystem, interactive) = NetworkMessageSerializer.ReadHandshake(msg);

        colorSystem.Should().Be(ColorSystem.Standard);
        interactive.Should().BeFalse();
    }

    [Fact]
    public void ReadHandshake_Wrong_Type_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.Output, new byte[10]);
        var act = () => NetworkMessageSerializer.ReadHandshake(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Handshake*Output*");
    }

    [Fact]
    public void ReadHandshake_Short_Payload_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.Handshake, new byte[5]);
        var act = () => NetworkMessageSerializer.ReadHandshake(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*too short*");
    }

    [Fact]
    public void ReadHandshake_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.ReadHandshake(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- HandshakeAck ---

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandshakeAck_Should_Roundtrip(bool accepted)
    {
        var msg = NetworkMessageSerializer.CreateHandshakeAck(accepted);
        var result = NetworkMessageSerializer.ReadHandshakeAck(msg);
        result.Should().Be(accepted);
    }

    [Fact]
    public void ReadHandshakeAck_Wrong_Type_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.Handshake, new byte[] { 1 });
        var act = () => NetworkMessageSerializer.ReadHandshakeAck(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*HandshakeAck*Handshake*");
    }

    [Fact]
    public void ReadHandshakeAck_Empty_Payload_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.HandshakeAck, Array.Empty<byte>());
        var act = () => NetworkMessageSerializer.ReadHandshakeAck(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*too short*");
    }

    [Fact]
    public void ReadHandshakeAck_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.ReadHandshakeAck(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- Output ---

    [Fact]
    public void Output_Should_Roundtrip()
    {
        var msg = NetworkMessageSerializer.CreateOutput("Hello, World!");
        var text = NetworkMessageSerializer.ReadOutput(msg);
        text.Should().Be("Hello, World!");
    }

    [Fact]
    public void Output_Empty_String_Should_Roundtrip()
    {
        var msg = NetworkMessageSerializer.CreateOutput(string.Empty);
        var text = NetworkMessageSerializer.ReadOutput(msg);
        text.Should().BeEmpty();
    }

    [Fact]
    public void Output_Unicode_Should_Roundtrip()
    {
        var msg = NetworkMessageSerializer.CreateOutput("\u2603 \U0001F600");
        var text = NetworkMessageSerializer.ReadOutput(msg);
        text.Should().Be("\u2603 \U0001F600");
    }

    [Fact]
    public void CreateOutput_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.CreateOutput(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadOutput_Wrong_Type_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.Handshake, Array.Empty<byte>());
        var act = () => NetworkMessageSerializer.ReadOutput(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Output*Handshake*");
    }

    [Fact]
    public void ReadOutput_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.ReadOutput(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- KeyPress ---

    [Fact]
    public void KeyPress_Should_Roundtrip()
    {
        var key = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        result.KeyChar.Should().Be('a');
        result.Key.Should().Be(ConsoleKey.A);
        result.Modifiers.Should().Be((ConsoleModifiers)0);
    }

    [Fact]
    public void KeyPress_With_Modifiers_Should_Roundtrip()
    {
        var key = new ConsoleKeyInfo('A', ConsoleKey.A, true, true, true);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        result.KeyChar.Should().Be('A');
        result.Key.Should().Be(ConsoleKey.A);
        (result.Modifiers & ConsoleModifiers.Shift).Should().NotBe((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Alt).Should().NotBe((ConsoleModifiers)0);
        (result.Modifiers & ConsoleModifiers.Control).Should().NotBe((ConsoleModifiers)0);
    }

    [Fact]
    public void KeyPress_Enter_Should_Roundtrip()
    {
        var key = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        var msg = NetworkMessageSerializer.CreateKeyPress(key);
        var result = NetworkMessageSerializer.ReadKeyPress(msg);

        result.KeyChar.Should().Be('\r');
        result.Key.Should().Be(ConsoleKey.Enter);
    }

    [Fact]
    public void ReadKeyPress_Wrong_Type_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.Output, new byte[7]);
        var act = () => NetworkMessageSerializer.ReadKeyPress(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*KeyPress*Output*");
    }

    [Fact]
    public void ReadKeyPress_Short_Payload_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.KeyPress, new byte[3]);
        var act = () => NetworkMessageSerializer.ReadKeyPress(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*too short*");
    }

    [Fact]
    public void ReadKeyPress_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.ReadKeyPress(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- Resize ---

    [Fact]
    public void Resize_Should_Roundtrip()
    {
        var msg = NetworkMessageSerializer.CreateResize(160, 50);
        var (width, height) = NetworkMessageSerializer.ReadResize(msg);

        width.Should().Be(160);
        height.Should().Be(50);
    }

    [Fact]
    public void ReadResize_Wrong_Type_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.Output, new byte[8]);
        var act = () => NetworkMessageSerializer.ReadResize(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Resize*Output*");
    }

    [Fact]
    public void ReadResize_Short_Payload_Should_Throw()
    {
        var msg = new NetworkMessage(MessageType.Resize, new byte[4]);
        var act = () => NetworkMessageSerializer.ReadResize(msg);
        act.Should().Throw<InvalidOperationException>().WithMessage("*too short*");
    }

    [Fact]
    public void ReadResize_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.ReadResize(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- Disconnect ---

    [Fact]
    public void Disconnect_Should_Have_Empty_Payload()
    {
        var msg = NetworkMessageSerializer.CreateDisconnect();
        msg.Type.Should().Be(MessageType.Disconnect);
        msg.Payload.Should().BeEmpty();
    }

    // --- Frame serialization ---

    [Fact]
    public void ToFrame_Should_Produce_Correct_Wire_Format()
    {
        var msg = new NetworkMessage(MessageType.Output, new byte[] { 0x41, 0x42 });
        var frame = NetworkMessageSerializer.ToFrame(msg);

        frame.Length.Should().Be(7); // 1 type + 4 length + 2 payload
        frame[0].Should().Be((byte)MessageType.Output);
        // Length = 2 in big-endian
        frame[1].Should().Be(0);
        frame[2].Should().Be(0);
        frame[3].Should().Be(0);
        frame[4].Should().Be(2);
        frame[5].Should().Be(0x41);
        frame[6].Should().Be(0x42);
    }

    [Fact]
    public void ToFrame_Empty_Payload_Should_Work()
    {
        var msg = NetworkMessageSerializer.CreateDisconnect();
        var frame = NetworkMessageSerializer.ToFrame(msg);

        frame.Length.Should().Be(5);
        frame[0].Should().Be((byte)MessageType.Disconnect);
        frame[1].Should().Be(0);
        frame[2].Should().Be(0);
        frame[3].Should().Be(0);
        frame[4].Should().Be(0);
    }

    [Fact]
    public void ToFrame_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.ToFrame(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromFrame_Should_Deserialize_Correctly()
    {
        var frame = new byte[] { (byte)MessageType.Output, 0, 0, 0, 3, 0x48, 0x69, 0x21 };
        var msg = NetworkMessageSerializer.FromFrame(frame);

        msg.Type.Should().Be(MessageType.Output);
        msg.Payload.Should().Equal(0x48, 0x69, 0x21);
    }

    [Fact]
    public void FromFrame_Empty_Payload_Should_Work()
    {
        var frame = new byte[] { (byte)MessageType.Disconnect, 0, 0, 0, 0 };
        var msg = NetworkMessageSerializer.FromFrame(frame);

        msg.Type.Should().Be(MessageType.Disconnect);
        msg.Payload.Should().BeEmpty();
    }

    [Fact]
    public void FromFrame_Too_Short_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.FromFrame(new byte[] { 1, 2, 3 });
        act.Should().Throw<InvalidOperationException>().WithMessage("*too short*");
    }

    [Fact]
    public void FromFrame_Incomplete_Payload_Should_Throw()
    {
        var frame = new byte[] { (byte)MessageType.Output, 0, 0, 0, 10, 0x41 };
        var act = () => NetworkMessageSerializer.FromFrame(frame);
        act.Should().Throw<InvalidOperationException>().WithMessage("*incomplete*");
    }

    [Fact]
    public void FromFrame_Null_Should_Throw()
    {
        var act = () => NetworkMessageSerializer.FromFrame(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Frame_Full_Roundtrip()
    {
        var original = NetworkMessageSerializer.CreateOutput("Test data \u2603");
        var frame = NetworkMessageSerializer.ToFrame(original);
        var restored = NetworkMessageSerializer.FromFrame(frame);

        restored.Type.Should().Be(original.Type);
        restored.Payload.Should().Equal(original.Payload);
    }

    // --- Big-endian helpers ---

    [Fact]
    public void WriteInt32BigEndian_Should_Write_Correctly()
    {
        var buffer = new byte[8];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 2, 0x01020304);

        buffer[2].Should().Be(0x01);
        buffer[3].Should().Be(0x02);
        buffer[4].Should().Be(0x03);
        buffer[5].Should().Be(0x04);
    }

    [Fact]
    public void ReadInt32BigEndian_Should_Read_Correctly()
    {
        var buffer = new byte[] { 0, 0x01, 0x02, 0x03, 0x04, 0 };
        var value = NetworkMessageSerializer.ReadInt32BigEndian(buffer, 1);
        value.Should().Be(0x01020304);
    }

    [Fact]
    public void BigEndian_Roundtrip()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, 123456789);
        var result = NetworkMessageSerializer.ReadInt32BigEndian(buffer, 0);
        result.Should().Be(123456789);
    }

    [Fact]
    public void BigEndian_Zero_Roundtrip()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, 0);
        var result = NetworkMessageSerializer.ReadInt32BigEndian(buffer, 0);
        result.Should().Be(0);
    }

    [Fact]
    public void BigEndian_Negative_Roundtrip()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, -1);
        var result = NetworkMessageSerializer.ReadInt32BigEndian(buffer, 0);
        result.Should().Be(-1);
    }

    [Fact]
    public void BigEndian_MaxValue_Roundtrip()
    {
        var buffer = new byte[4];
        NetworkMessageSerializer.WriteInt32BigEndian(buffer, 0, int.MaxValue);
        var result = NetworkMessageSerializer.ReadInt32BigEndian(buffer, 0);
        result.Should().Be(int.MaxValue);
    }
}
