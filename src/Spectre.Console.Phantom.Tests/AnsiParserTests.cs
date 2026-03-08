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

    // ── Edge Cases: Truncated/Malformed Input ────────────────────────

    [Fact]
    public void Should_Handle_Truncated_Escape_At_End_Of_Input()
    {
        // ESC at end of string with no following char
        var result = AnsiParser.Parse("Hello\x1b");

        // Should have Text("Hello") and nothing else (ESC consumed but no sequence produced)
        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("Hello");
    }

    [Fact]
    public void Should_Handle_Unknown_Escape_Sequence()
    {
        // ESC followed by something other than [ or ]
        var result = AnsiParser.Parse("A\x1bXB");

        // The unknown sequence (\x1bX) is skipped, leaving text "A" and "B"
        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("A");
        result[1].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("B");
    }

    [Fact]
    public void Should_Handle_CSI_With_No_Command_Byte()
    {
        // ESC [ at end of string (no command)
        var result = AnsiParser.Parse("A\x1b[");

        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("A");
    }

    [Fact]
    public void Should_Handle_CSI_With_Only_Params_No_Command()
    {
        // ESC [ 1;2 at end of string (params but no command)
        var result = AnsiParser.Parse("A\x1b[1;2");

        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("A");
    }

    [Fact]
    public void Should_Handle_Unknown_CSI_Command()
    {
        // Unknown CSI command letter 'Z' — should not produce any sequence
        var result = AnsiParser.Parse("\x1b[5Z");
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Handle_Unknown_Private_Sequence()
    {
        // Unknown DEC private mode param 999 — should not produce any sequence
        var result = AnsiParser.Parse("\x1b[?999h");
        result.ShouldBeEmpty();
    }

    // ── Edge Cases: OSC ──────────────────────────────────────────────

    [Fact]
    public void Should_Parse_Hyperlink_With_BEL_Terminator()
    {
        // BEL (\a) as OSC terminator instead of ST (ESC \)
        var result = AnsiParser.Parse("\x1b]8;;https://bel.example\aLink\x1b]8;;\a");

        result.Count.ShouldBe(3);
        result[0].ShouldBeOfType<AnsiSequence.Hyperlink>().Url.ShouldBe("https://bel.example");
        result[1].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("Link");
        result[2].ShouldBeOfType<AnsiSequence.Hyperlink>().Url.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Handle_OSC_Without_Terminator()
    {
        // OSC sequence that runs to end of input without ST or BEL
        var result = AnsiParser.Parse("A\x1b]8;;https://unterminated");

        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<AnsiSequence.Text>().Content.ShouldBe("A");
    }

    [Fact]
    public void Should_Ignore_Non_Hyperlink_OSC()
    {
        // OSC sequence that is NOT 8; (e.g., OSC 0 — set title)
        var result = AnsiParser.Parse("\x1b]0;My Title\a");

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Ignore_Non_Hyperlink_OSC_With_Internal_Semicolons()
    {
        // OSC content "2;Title;Extra" does NOT start with "8;" so must be ignored.
        // Kills string mutation "8;" → "" which would make StartsWith("") always true,
        // falling through to misparse this as a hyperlink.
        var result = AnsiParser.Parse("\x1b]2;Title;Extra\a");

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Ignore_OSC_8_Without_Second_Semicolon()
    {
        // OSC 8 with params but no second semicolon
        var result = AnsiParser.Parse("\x1b]8;no-second-semi\a");

        // ParseOscContent finds "8;" prefix but no second ";" in the rest → returns without adding
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Parse_Hyperlink_Close_As_Empty_Url()
    {
        // Closing hyperlink: 8;; (empty URL)
        var result = AnsiParser.Parse("\x1b]8;;\x1b\\");

        var link = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Hyperlink>();
        link.Url.ShouldBeEmpty();
        link.Id.ShouldBeNull();
    }

    // ── Edge Cases: Erase Modes ──────────────────────────────────────

    [Fact]
    public void Should_Parse_Erase_In_Display_To_Start()
    {
        var result = AnsiParser.Parse("\x1b[1J");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInDisplay>()
            .Mode.ShouldBe(EraseMode.ToStart);
    }

    [Fact]
    public void Should_Parse_Erase_In_Display_Scrollback()
    {
        var result = AnsiParser.Parse("\x1b[3J");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInDisplay>()
            .Mode.ShouldBe(EraseMode.Scrollback);
    }

    [Fact]
    public void Should_Parse_Erase_In_Display_Default_As_ToEnd()
    {
        // No parameter defaults to 0 (ToEnd)
        var result = AnsiParser.Parse("\x1b[J");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInDisplay>()
            .Mode.ShouldBe(EraseMode.ToEnd);
    }

    [Fact]
    public void Should_Parse_Erase_In_Line_To_End()
    {
        var result = AnsiParser.Parse("\x1b[0K");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInLine>()
            .Mode.ShouldBe(EraseMode.ToEnd);
    }

    [Fact]
    public void Should_Parse_Erase_In_Line_To_Start()
    {
        var result = AnsiParser.Parse("\x1b[1K");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInLine>()
            .Mode.ShouldBe(EraseMode.ToStart);
    }

    [Fact]
    public void Should_Parse_Erase_In_Line_Default_As_ToEnd()
    {
        // No parameter defaults to 0 (ToEnd)
        var result = AnsiParser.Parse("\x1b[K");

        result.ShouldHaveSingleItem()
            .ShouldBeOfType<AnsiSequence.EraseInLine>()
            .Mode.ShouldBe(EraseMode.ToEnd);
    }

    // ── Edge Cases: SGR extended colors ──────────────────────────────

    [Fact]
    public void Should_Parse_SGR_8Bit_Background()
    {
        var result = AnsiParser.Parse("\x1b[48;5;42m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 48, 5, 42 });
    }

    [Fact]
    public void Should_Parse_SGR_24Bit_Background()
    {
        var result = AnsiParser.Parse("\x1b[48;2;10;20;30m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 48, 2, 10, 20, 30 });
    }

    [Fact]
    public void Should_Parse_SGR_Default_Foreground()
    {
        var result = AnsiParser.Parse("\x1b[39m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 39 });
    }

    [Fact]
    public void Should_Parse_SGR_Default_Background()
    {
        var result = AnsiParser.Parse("\x1b[49m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 49 });
    }

    [Fact]
    public void Should_Parse_All_Decoration_Codes()
    {
        // SGR codes 1-9 for decorations
        var result = AnsiParser.Parse("\x1b[1;2;3;4;5;6;7;8;9m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
    }

    [Fact]
    public void Should_Parse_Bright_Foreground_Colors()
    {
        // SGR 90-97 for bright foreground
        var result = AnsiParser.Parse("\x1b[91m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 91 });
    }

    [Fact]
    public void Should_Parse_Bright_Background_Colors()
    {
        // SGR 100-107 for bright background
        var result = AnsiParser.Parse("\x1b[104m");

        var sgr = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Sgr>();
        sgr.Parameters.ShouldBe(new[] { 104 });
    }

    // ── Null input ───────────────────────────────────────────────────

    [Fact]
    public void Should_Throw_For_Null_Input()
    {
        Should.Throw<ArgumentNullException>(() => AnsiParser.Parse(null!));
    }

    // ── Cursor Up default ────────────────────────────────────────────

    [Fact]
    public void Should_Parse_Cursor_Up_Default()
    {
        var result = AnsiParser.Parse("\x1b[A");

        var move = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorMove>();
        move.Direction.ShouldBe(CursorDirection.Up);
        move.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_Parse_Cursor_Left_Default()
    {
        var result = AnsiParser.Parse("\x1b[D");

        var move = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorMove>();
        move.Direction.ShouldBe(CursorDirection.Left);
        move.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_Parse_Cursor_Right_Default()
    {
        var result = AnsiParser.Parse("\x1b[C");

        var move = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.CursorMove>();
        move.Direction.ShouldBe(CursorDirection.Right);
        move.Count.ShouldBe(1);
    }

    // ── Multiple sequences in one string ─────────────────────────────

    [Fact]
    public void Should_Parse_Multiple_Control_Characters()
    {
        var result = AnsiParser.Parse("\n\r\b\n");

        result.Count.ShouldBe(4);
        result[0].ShouldBeOfType<AnsiSequence.NewLine>();
        result[1].ShouldBeOfType<AnsiSequence.CarriageReturn>();
        result[2].ShouldBeOfType<AnsiSequence.Backspace>();
        result[3].ShouldBeOfType<AnsiSequence.NewLine>();
    }

    [Fact]
    public void Should_Parse_Multiple_CSI_Sequences_In_Row()
    {
        var result = AnsiParser.Parse("\x1b[1A\x1b[2B\x1b[3C\x1b[4D");

        result.Count.ShouldBe(4);
        result[0].ShouldBeOfType<AnsiSequence.CursorMove>().Direction.ShouldBe(CursorDirection.Up);
        result[1].ShouldBeOfType<AnsiSequence.CursorMove>().Direction.ShouldBe(CursorDirection.Down);
        result[2].ShouldBeOfType<AnsiSequence.CursorMove>().Direction.ShouldBe(CursorDirection.Right);
        result[3].ShouldBeOfType<AnsiSequence.CursorMove>().Direction.ShouldBe(CursorDirection.Left);
    }

    // ── OSC: ST terminator boundary conditions ────────────────────────

    [Fact]
    public void Should_Handle_OSC_Ending_In_ESC_Without_Backslash()
    {
        // OSC that ends with just ESC (no following backslash) — not a valid ST terminator
        // The bounds guard `pos + 1 < input.Length` prevents out-of-bounds access
        var result = AnsiParser.Parse("\x1b]8;;https://example.com\x1b");

        // ESC at end of input without a following `\\` is not an ST terminator,
        // so the OSC is unterminated — result should be empty (no text escapes)
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Handle_OSC_With_Non_ESC_Char_Followed_By_Backslash_In_Content()
    {
        // OSC 8 URL that happens to contain a backslash character (not preceded by ESC)
        // The `input[pos] == Escape` guard ensures only ESC+\\ terminates
        var result = AnsiParser.Parse("\x1b]8;;http://a.com/x\x5cy\x1b\\");

        // Backslash in URL is valid; only ESC+\\ terminates the sequence
        var link = result.ShouldHaveSingleItem().ShouldBeOfType<AnsiSequence.Hyperlink>();
        link.Url.ShouldBe("http://a.com/x\x5cy");
    }

    // ── Private sequence with unrecognized command ────────────────────

    [Fact]
    public void Should_Handle_Private_Sequence_With_Unknown_Command()
    {
        // ? prefix with recognized param (25) but unrecognized command ('x')
        var result = AnsiParser.Parse("\x1b[?25x");
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Handle_CSI_Private_With_No_Params()
    {
        var result = AnsiParser.Parse("\x1b[?h");
        result.ShouldBeEmpty();
    }
}
