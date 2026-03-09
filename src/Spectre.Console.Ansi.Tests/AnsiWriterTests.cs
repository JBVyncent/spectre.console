namespace Spectre.Console.Ansi.Tests;

public sealed class AnsiWriterTests
{
    [Fact]
    public void Should_Write_Expected_Ansi()
    {
        // Given
        var fixture = new AnsiFixture();
        fixture.Capabilities.Ansi = true;
        fixture.Capabilities.Links = true;

        // When
        fixture.Writer
            .BeginLink("https://spectreconsole.net", linkId: 123)
            .Decoration(Decoration.Bold | Decoration.Italic)
            .Foreground(Color.Yellow)
            .Write("Spectre Console")
            .ResetStyle()
            .EndLink();

        // Then
        fixture.Output.Should().Be("\e]8;id=123;https://spectreconsole.net\e\\\e[1;3m\e[38;5;11mSpectre Console\e[0m\e]8;;\e\\");
    }

    [Fact]
    public void Should_Write_OSC8_With_Empty_Params_When_No_LinkId()
    {
        // Given
        var fixture = new AnsiFixture();
        fixture.Capabilities.Ansi = true;
        fixture.Capabilities.Links = true;

        // When — no linkId parameter
        fixture.Writer
            .BeginLink("https://example.com")
            .Write("click")
            .EndLink();

        // Then — OSC 8 format requires `8;;url` (empty params field, two semicolons)
        fixture.Output.Should().Contain("]8;;https://example.com\e\\");
    }

    [Fact]
    public void Should_Not_Write_Link_If_Not_Supported()
    {
        // Given
        var fixture = new AnsiFixture();
        fixture.Capabilities.Ansi = true;
        fixture.Capabilities.Links = false;

        // When
        fixture.Writer
            .BeginLink("https://spectreconsole.net", linkId: 123)
            .Decoration(Decoration.Bold | Decoration.Italic)
            .Foreground(Color.Yellow)
            .Write("Spectre Console")
            .ResetStyle()
            .EndLink();

        // Then
        fixture.Output.Should().Be("\e[1;3m\e[38;5;11mSpectre Console\e[0m");
    }

    [Fact]
    public void Should_Not_Write_Ansi_If_Not_Supported()
    {
        // Given
        var fixture = new AnsiFixture();
        fixture.Capabilities.Ansi = false;

        // When
        fixture.Writer
            .BeginLink("https://spectreconsole.net", linkId: 123)
            .Decoration(Decoration.Bold | Decoration.Italic)
            .Foreground(Color.Yellow)
            .Write("Spectre Console")
            .ResetStyle()
            .EndLink();

        // Then
        fixture.Output.Should().Be("Spectre Console");
    }
}