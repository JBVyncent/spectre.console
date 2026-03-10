namespace Spectre.Console.Network.Tests;

public sealed class NetworkConsoleOutputTests
{
    [Fact]
    public void Constructor_Should_Store_Properties()
    {
        var writer = new StringWriter();
        var output = new NetworkConsoleOutput(writer, 120, 40);

        output.Writer.Should().BeSameAs(writer);
        output.Width.Should().Be(120);
        output.Height.Should().Be(40);
    }

    [Fact]
    public void IsTerminal_Should_Be_False()
    {
        var writer = new StringWriter();
        var output = new NetworkConsoleOutput(writer, 80, 24);

        output.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void SetEncoding_Should_Not_Throw()
    {
        var writer = new StringWriter();
        var output = new NetworkConsoleOutput(writer, 80, 24);

        var act = () => output.SetEncoding(System.Text.Encoding.UTF8);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_Should_Throw_On_Null_Writer()
    {
        var act = () => new NetworkConsoleOutput(null!, 80, 24);
        act.Should().Throw<ArgumentNullException>();
    }
}
