using Shouldly;
using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

public sealed class AnsiParserTests
{
    [Fact]
    public void Should_Parse_Plain_Text()
    {
        var result = AnsiParser.Parse("Hello World");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.Text>()
            .Content.ShouldBe("Hello World");
    }

    [Fact]
    public void Should_Parse_Empty_String()
    {
        var result = AnsiParser.Parse("");
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Parse_Newline()
    {
        var result = AnsiParser.Parse("A\nB");

        result.Count.ShouldBe(3);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("A");
        result[1].ShouldBeOfType<AnsiSequence.NewLine>();
        result[2].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("B");
    }

    [Fact]
    public void Should_Parse_Carriage_Return()
    {
        var result = AnsiParser.Parse("A\rB");

        result.Count.ShouldBe(3);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("A");
        result[1].ShouldBeOfType<AnsiSequence.CarriageReturn>();
        result[2].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("B");
    }

    [Fact]
    public void Should_Parse_Backspace()
    {
        var result = AnsiParser.Parse("AB\b");

        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("AB");
        result[1].ShouldBeOfType<AnsiSequence.Backspace>();
    }

    [Fact]
    public void Should_Parse_SGR_Reset()
    {
        var result = AnsiParser.Parse("\x1b[0m");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.Sgr>()
            .Parameters.ShouldBe(new[] { 0 });
    }

    [Fact]
    public void Should_Parse_SGR_Bold()
    {
        var result = AnsiParser.Parse("\x1b[1m");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.Sgr>()
            .Parameters.ShouldBe(new[] { 1 });
    }

    [Fact]
    public void Should_Parse_SGR_Multiple_Parameters()
    {
        var result = AnsiParser.Parse("\x1b[1;3;4m");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.Sgr>()
            .Parameters.ShouldBe(new[] { 1, 3, 4 });
    }

    [Fact]
    public void Should_Parse_SGR_8Bit_Foreground()
    {
        var result = AnsiParser.Parse("\x1b[38;5;196m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 38, 5, 196 });
    }

    [Fact]
    public void Should_Parse_SGR_24Bit_RGB()
    {
        var result = AnsiParser.Parse("\x1b[38;2;128;64;32m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 38, 2, 128, 64, 32 });
    }

    [Fact]
    public void Should_Parse_Cursor_Up()
    {
        var result = AnsiParser.Parse("\x1b[3A");

        var move = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorMove>();
        move.Direction.ShouldBe(CursorDirection.Up);
        move.Count.ShouldBe(3);
    }

    [Fact]
    public void Should_Parse_Cursor_Down_Default()
    {
        // No parameter means default of 1
        var result = AnsiParser.Parse("\x1b[B");

        var move = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorMove>();
        move.Direction.ShouldBe(CursorDirection.Down);
        move.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_Parse_Cursor_Right()
    {
        var result = AnsiParser.Parse("\x1b[5C");

        var move = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorMove>();
        move.Direction.ShouldBe(CursorDirection.Right);
        move.Count.ShouldBe(5);
    }

    [Fact]
    public void Should_Parse_Cursor_Left()
    {
        var result = AnsiParser.Parse("\x1b[2D");

        var move = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorMove>();
        move.Direction.ShouldBe(CursorDirection.Left);
        move.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_Parse_Cursor_Position()
    {
        var result = AnsiParser.Parse("\x1b[5;10H");

        var pos = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorPosition>();
        pos.Row.ShouldBe(5);
        pos.Column.ShouldBe(10);
    }

    [Fact]
    public void Should_Parse_Cursor_Home()
    {
        var result = AnsiParser.Parse("\x1b[H");

        var pos = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorPosition>();
        pos.Row.ShouldBe(1);
        pos.Column.ShouldBe(1);
    }

    [Fact]
    public void Should_Parse_Cursor_Horizontal_Absolute()
    {
        var result = AnsiParser.Parse("\x1b[15G");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.CursorHorizontalAbsolute>()
            .Column.ShouldBe(15);
    }

    [Fact]
    public void Should_Parse_Save_Cursor()
    {
        var result = AnsiParser.Parse("\x1b[s");
        result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.SaveCursor>();
    }

    [Fact]
    public void Should_Parse_Restore_Cursor()
    {
        var result = AnsiParser.Parse("\x1b[u");
        result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.RestoreCursor>();
    }

    [Fact]
    public void Should_Parse_Show_Cursor()
    {
        var result = AnsiParser.Parse("\x1b[?25h");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.CursorVisibility>()
            .Visible.ShouldBeTrue();
    }

    [Fact]
    public void Should_Parse_Hide_Cursor()
    {
        var result = AnsiParser.Parse("\x1b[?25l");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.CursorVisibility>()
            .Visible.ShouldBeFalse();
    }

    [Fact]
    public void Should_Parse_Erase_In_Display()
    {
        var result = AnsiParser.Parse("\x1b[0J");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInDisplay>()
            .Mode.ShouldBe(EraseMode.ToEnd);
    }

    [Fact]
    public void Should_Parse_Erase_In_Display_All()
    {
        var result = AnsiParser.Parse("\x1b[2J");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInDisplay>()
            .Mode.ShouldBe(EraseMode.All);
    }

    [Fact]
    public void Should_Parse_Erase_In_Line()
    {
        var result = AnsiParser.Parse("\x1b[2K");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInLine>()
            .Mode.ShouldBe(EraseMode.All);
    }

    [Fact]
    public void Should_Parse_Alternate_Screen_Enter()
    {
        var result = AnsiParser.Parse("\x1b[?1049h");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.AlternateScreen>()
            .Enter.ShouldBeTrue();
    }

    [Fact]
    public void Should_Parse_Alternate_Screen_Exit()
    {
        var result = AnsiParser.Parse("\x1b[?1049l");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.AlternateScreen>()
            .Enter.ShouldBeFalse();
    }

    [Fact]
    public void Should_Parse_Hyperlink()
    {
        var result = AnsiParser.Parse("\x1b]8;;https://example.com\x1b\\");

        var link = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Hyperlink>();
        link.Url.ShouldBe("https://example.com");
        link.Id.ShouldBeNull();
    }

    [Fact]
    public void Should_Parse_Hyperlink_With_Id()
    {
        var result = AnsiParser.Parse("\x1b]8;id=42;https://example.com\x1b\\");

        var link = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Hyperlink>();
        link.Url.ShouldBe("https://example.com");
        link.Id.ShouldBe("42");
    }

    [Fact]
    public void Should_Parse_Mixed_Text_And_Sequences()
    {
        var result = AnsiParser.Parse("\x1b[1mHello\x1b[0m World");

        result.Count.ShouldBe(4);
        result[0].ShouldBeOfType<AnsiSequence.Sgr>().Parameters.ShouldBe(new[] { 1 });
        result[1].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("Hello");
        result[2].ShouldBeOfType<AnsiSequence.Sgr>().Parameters.ShouldBe(new[] { 0 });
        result[3].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe(" World");
    }

    [Fact]
    public void Should_Parse_SGR_With_No_Parameters_As_Reset()
    {
        var result = AnsiParser.Parse("\x1b[m");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.Sgr>()
            .Parameters.ShouldBe(new[] { 0 });
    }

    [Fact]
    public void Should_Parse_Complex_Status_Output()
    {
        // Simulates what Spectre.Console Status produces:
        // Hide cursor, save, content, restore+erase, save, content, restore+erase, show
        var output = "\x1b[?25l\x1b[s* Working\x1b[u\x1b[0J\x1b[s* Done\x1b[u\x1b[0J\x1b[?25h";
        var result = AnsiParser.Parse(output);

        result.Count.ShouldBeGreaterThan(5);
        result[0].ShouldBeOfType<AnsiSequence.CursorVisibility>().Visible.ShouldBeFalse();
        result[1].ShouldBeOfType<AnsiSequence.SaveCursor>();
        result.Last().ShouldBeOfType<AnsiSequence.CursorVisibility>().Visible.ShouldBeTrue();
    }
}
