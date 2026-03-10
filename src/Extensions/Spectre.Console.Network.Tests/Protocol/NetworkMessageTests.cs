using Spectre.Console.Network.Protocol;

namespace Spectre.Console.Network.Tests.Protocol;

public sealed class NetworkMessageTests
{
    [Fact]
    public void Constructor_Should_Store_Type_And_Payload()
    {
        // Arrange & Act
        var payload = new byte[] { 1, 2, 3 };
        var message = new NetworkMessage(MessageType.Output, payload);

        // Assert
        message.Type.Should().Be(MessageType.Output);
        message.Payload.Should().BeSameAs(payload);
    }

    [Fact]
    public void Constructor_Should_Accept_Empty_Payload()
    {
        // Arrange & Act
        var message = new NetworkMessage(MessageType.Disconnect, Array.Empty<byte>());

        // Assert
        message.Type.Should().Be(MessageType.Disconnect);
        message.Payload.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_Should_Throw_On_Null_Payload()
    {
        // Act & Assert
        var act = () => new NetworkMessage(MessageType.Output, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("payload");
    }

    [Theory]
    [InlineData(MessageType.Handshake)]
    [InlineData(MessageType.HandshakeAck)]
    [InlineData(MessageType.Output)]
    [InlineData(MessageType.KeyPress)]
    [InlineData(MessageType.Resize)]
    [InlineData(MessageType.Disconnect)]
    public void Constructor_Should_Store_All_Message_Types(MessageType type)
    {
        // Act
        var message = new NetworkMessage(type, new byte[] { 42 });

        // Assert
        message.Type.Should().Be(type);
    }
}
