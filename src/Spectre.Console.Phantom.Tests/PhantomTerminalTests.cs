using Shouldly;
using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

public sealed class PhantomTerminalTests
{
    [Fact]
    public void Should_Write_Plain_Text()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello");

        terminal.GetRowText(0).ShouldBe("Hello");
        terminal.CursorRow.ShouldBe(0);
        terminal.CursorCol.ShouldBe(5);
    }

    [Fact]
    public void Should_Handle_Newline()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2");

        terminal.GetRowText(0).ShouldBe("Line 1");
        terminal.GetRowText(1).ShouldBe("Line 2");
        terminal.CursorRow.ShouldBe(1);
    }

    [Fact]
    public void Should_Handle_Carriage_Return()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello!\rWorld");

        // CR moves to col 0, "World" overwrites "Hello" but "!" at col 5 remains
        terminal.GetRowText(0).ShouldBe("World!");
        terminal.CursorCol.ShouldBe(5);
    }

    [Fact]
    public void Should_Handle_Line_Wrap()
    {
        var terminal = new PhantomTerminal(5, 24);
        terminal.Write("HelloWorld");

        terminal.GetRowText(0).ShouldBe("Hello");
        terminal.GetRowText(1).ShouldBe("World");
    }

    [Fact]
    public void Should_Handle_Scroll_On_Overflow()
    {
        var terminal = new PhantomTerminal(80, 3);
        terminal.Write("Line 1\nLine 2\nLine 3\nLine 4");

        // Line 1 should have scrolled off
        terminal.GetRowText(0).ShouldBe("Line 2");
        terminal.GetRowText(1).ShouldBe("Line 3");
        terminal.GetRowText(2).ShouldBe("Line 4");
    }

    [Fact]
    public void Should_Handle_Cursor_Up()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2\x1b[1AX");

        terminal.GetRowText(0).ShouldStartWith("Line 1");
        // X should be written at row 0, col 6 (cursor was at row 1, moved up 1)
        terminal.Screen[0, 6].Character.ShouldBe('X');
    }

    [Fact]
    public void Should_Handle_Cursor_Down()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("A\x1b[2BB");

        terminal.CursorRow.ShouldBe(2);
        terminal.Screen[2, 1].Character.ShouldBe('B');
    }

    [Fact]
    public void Should_Handle_Save_Restore_Cursor()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\x1b[s World\x1b[u!");

        // After save at col 5, write " World", then restore to col 5 and write "!"
        terminal.CursorCol.ShouldBe(6);
        terminal.Screen[0, 5].Character.ShouldBe('!');
    }

    [Fact]
    public void Should_Handle_Erase_In_Display_To_End()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2\nLine 3");
        terminal.Write("\x1b[1;1H"); // Move to top-left
        terminal.Write("\x1b[0J");   // Erase from cursor to end

        terminal.GetRowText(0).ShouldBe("");
        terminal.GetRowText(1).ShouldBe("");
        terminal.GetRowText(2).ShouldBe("");
    }

    [Fact]
    public void Should_Handle_Erase_In_Line()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello World");
        terminal.Write("\x1b[6G");   // Move to column 6 (0-indexed: 5)
        terminal.Write("\x1b[0K");   // Erase from cursor to end of line

        terminal.GetRowText(0).ShouldBe("Hello");
    }

    [Fact]
    public void Should_Handle_Erase_Entire_Line()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello World");
        terminal.Write("\x1b[2K");

        terminal.GetRowText(0).ShouldBe("");
    }

    [Fact]
    public void Should_Handle_Hide_Show_Cursor()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.CursorVisible.ShouldBeTrue();

        terminal.Write("\x1b[?25l");
        terminal.CursorVisible.ShouldBeFalse();

        terminal.Write("\x1b[?25h");
        terminal.CursorVisible.ShouldBeTrue();
    }

    [Fact]
    public void Should_Handle_Alternate_Screen()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Main screen");
        terminal.IsAlternateScreen.ShouldBeFalse();

        terminal.Write("\x1b[?1049h");
        terminal.IsAlternateScreen.ShouldBeTrue();
        terminal.GetRowText(0).ShouldBe(""); // Alternate is empty

        terminal.Write("Alt screen");
        terminal.GetRowText(0).ShouldBe("Alt screen");

        terminal.Write("\x1b[?1049l");
        terminal.IsAlternateScreen.ShouldBeFalse();
        terminal.GetRowText(0).ShouldBe("Main screen"); // Main preserved
    }

    [Fact]
    public void Should_Track_SGR_Colors()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[31mR\x1b[32mG\x1b[34mB\x1b[0m");

        terminal.Screen[0, 0].Foreground.ShouldNotBeNull();
        terminal.Screen[0, 0].Foreground!.Value.Index.ShouldBe(31); // Red

        terminal.Screen[0, 1].Foreground.ShouldNotBeNull();
        terminal.Screen[0, 1].Foreground!.Value.Index.ShouldBe(32); // Green

        terminal.Screen[0, 2].Foreground.ShouldNotBeNull();
        terminal.Screen[0, 2].Foreground!.Value.Index.ShouldBe(34); // Blue
    }

    [Fact]
    public void Should_Track_SGR_8Bit_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38;5;196mX");

        var cell = terminal.Screen[0, 0];
        cell.Foreground.ShouldNotBeNull();
        cell.Foreground!.Value.Mode.ShouldBe(ColorMode.EightBit);
        cell.Foreground!.Value.Index.ShouldBe(196);
    }

    [Fact]
    public void Should_Track_SGR_24Bit_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38;2;128;64;32mX");

        var cell = terminal.Screen[0, 0];
        cell.Foreground.ShouldNotBeNull();
        cell.Foreground!.Value.Mode.ShouldBe(ColorMode.TrueColor);
        cell.Foreground!.Value.R.ShouldBe((byte)128);
        cell.Foreground!.Value.G.ShouldBe((byte)64);
        cell.Foreground!.Value.B.ShouldBe((byte)32);
    }

    [Fact]
    public void Should_Track_Decorations()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[1;3;4mX");

        var cell = terminal.Screen[0, 0];
        cell.Decoration.HasFlag(CellDecoration.Bold).ShouldBeTrue();
        cell.Decoration.HasFlag(CellDecoration.Italic).ShouldBeTrue();
        cell.Decoration.HasFlag(CellDecoration.Underline).ShouldBeTrue();
    }

    [Fact]
    public void Should_Reset_Decorations()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[1mA\x1b[0mB");

        terminal.Screen[0, 0].Decoration.HasFlag(CellDecoration.Bold).ShouldBeTrue();
        terminal.Screen[0, 1].Decoration.ShouldBe(CellDecoration.None);
    }

    [Fact]
    public void Should_Handle_Backspace()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("AB\bC");

        // Backspace moves cursor left, then C overwrites B
        terminal.GetRowText(0).ShouldBe("AC");
    }

    [Fact]
    public void Should_Find_Text_In_Screen()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\nWorld\nFoo");

        terminal.ContainsText("World").ShouldBeTrue();
        terminal.ContainsText("Missing").ShouldBeFalse();

        var pos = terminal.FindText("World");
        pos.ShouldNotBeNull();
        pos!.Value.Row.ShouldBe(1);
        pos!.Value.Col.ShouldBe(0);
    }

    [Fact]
    public void Should_Handle_Cursor_Position_Absolute()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[3;10HX");

        // CSI 3;10 H = row 3, col 10 (1-indexed) = row 2, col 9 (0-indexed)
        terminal.Screen[2, 9].Character.ShouldBe('X');
    }

    [Fact]
    public void Should_Clamp_Cursor_To_Bounds()
    {
        var terminal = new PhantomTerminal(10, 5);

        terminal.Write("\x1b[100A"); // Try to move up 100 from row 0
        terminal.CursorRow.ShouldBe(0);

        terminal.Write("\x1b[100B"); // Try to move down 100
        terminal.CursorRow.ShouldBe(4); // Clamped to last row
    }

    [Fact]
    public void Should_Track_Sequence_History()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[sHello\x1b[u");

        terminal.SequenceHistory.Count.ShouldBe(3);
        terminal.SequenceHistory[0].ShouldBeOfType<AnsiSequence.SaveCursor>();
        terminal.SequenceHistory[1].ShouldBeOfType<AnsiSequence.Text>();
        terminal.SequenceHistory[2].ShouldBeOfType<AnsiSequence.RestoreCursor>();
    }

    [Fact]
    public void Should_Reset_Terminal_State()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\x1b[?25l");

        terminal.Reset();

        terminal.GetRowText(0).ShouldBe("");
        terminal.CursorVisible.ShouldBeTrue();
        terminal.CursorRow.ShouldBe(0);
        terminal.CursorCol.ShouldBe(0);
        terminal.SequenceHistory.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Handle_Erase_Line_To_Start()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello World");
        terminal.Write("\x1b[6G");  // Move to column 6 (0-indexed: 5)
        terminal.Write("\x1b[1K");  // Erase from start of line to cursor

        // Characters 0-5 should be erased, "World" (6-10) should remain
        terminal.Screen[0, 0].Character.ShouldBe(' ');
        terminal.Screen[0, 4].Character.ShouldBe(' ');
        terminal.Screen[0, 5].Character.ShouldBe(' ');
        terminal.Screen[0, 6].Character.ShouldBe('W');
    }

    [Fact]
    public void Should_Handle_Erase_In_Display_To_Start()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2\nLine 3");
        terminal.Write("\x1b[2;4H"); // Move to row 2, col 4 (1-indexed)
        terminal.Write("\x1b[1J");   // Erase from start to cursor

        // Row 0 should be fully erased
        terminal.GetRowText(0).ShouldBe("");
        // Row 1 should be erased up to col 3 (0-indexed)
        terminal.Screen[1, 0].Character.ShouldBe(' ');
        terminal.Screen[1, 3].Character.ShouldBe(' ');
    }

    [Fact]
    public void Should_Handle_Hyperlink()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b]8;;https://example.com\x1b\\Link\x1b]8;;\x1b\\");

        terminal.Screen[0, 0].HyperlinkUrl.ShouldBe("https://example.com");
        terminal.Screen[0, 3].HyperlinkUrl.ShouldBe("https://example.com");
        // After closing hyperlink, next char has no URL
    }
}
