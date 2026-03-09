using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

public sealed class PhantomTerminalTests
{
    [Fact]
    public void Should_Write_Plain_Text()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello");

        terminal.GetRowText(0).Should().Be("Hello");
        terminal.CursorRow.Should().Be(0);
        terminal.CursorCol.Should().Be(5);
    }

    [Fact]
    public void Should_Handle_Newline()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2");

        terminal.GetRowText(0).Should().Be("Line 1");
        terminal.GetRowText(1).Should().Be("Line 2");
        terminal.CursorRow.Should().Be(1);
    }

    [Fact]
    public void Should_Handle_Carriage_Return()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello!\rWorld");

        // CR moves to col 0, "World" overwrites "Hello" but "!" at col 5 remains
        terminal.GetRowText(0).Should().Be("World!");
        terminal.CursorCol.Should().Be(5);
    }

    [Fact]
    public void Should_Handle_Line_Wrap()
    {
        var terminal = new PhantomTerminal(5, 24);
        terminal.Write("HelloWorld");

        terminal.GetRowText(0).Should().Be("Hello");
        terminal.GetRowText(1).Should().Be("World");
    }

    [Fact]
    public void Should_Handle_Scroll_On_Overflow()
    {
        var terminal = new PhantomTerminal(80, 3);
        terminal.Write("Line 1\nLine 2\nLine 3\nLine 4");

        // Line 1 should have scrolled off
        terminal.GetRowText(0).Should().Be("Line 2");
        terminal.GetRowText(1).Should().Be("Line 3");
        terminal.GetRowText(2).Should().Be("Line 4");
    }

    [Fact]
    public void Should_Handle_Cursor_Up()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2\x1b[1AX");

        terminal.GetRowText(0).Should().StartWith("Line 1");
        // X should be written at row 0, col 6 (cursor was at row 1, moved up 1)
        terminal.Screen[0, 6].Character.Should().Be('X');
    }

    [Fact]
    public void Should_Handle_Cursor_Down()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("A\x1b[2BB");

        terminal.CursorRow.Should().Be(2);
        terminal.Screen[2, 1].Character.Should().Be('B');
    }

    [Fact]
    public void Should_Handle_Save_Restore_Cursor()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\x1b[s World\x1b[u!");

        // After save at col 5, write " World", then restore to col 5 and write "!"
        terminal.CursorCol.Should().Be(6);
        terminal.Screen[0, 5].Character.Should().Be('!');
    }

    [Fact]
    public void Should_Handle_Erase_In_Display_To_End()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2\nLine 3");
        terminal.Write("\x1b[1;1H"); // Move to top-left
        terminal.Write("\x1b[0J");   // Erase from cursor to end

        terminal.GetRowText(0).Should().Be("");
        terminal.GetRowText(1).Should().Be("");
        terminal.GetRowText(2).Should().Be("");
    }

    [Fact]
    public void Should_Handle_Erase_In_Line()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello World");
        terminal.Write("\x1b[6G");   // Move to column 6 (0-indexed: 5)
        terminal.Write("\x1b[0K");   // Erase from cursor to end of line

        terminal.GetRowText(0).Should().Be("Hello");
    }

    [Fact]
    public void Should_Handle_Erase_Entire_Line()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello World");
        terminal.Write("\x1b[2K");

        terminal.GetRowText(0).Should().Be("");
    }

    [Fact]
    public void Should_Handle_Hide_Show_Cursor()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.CursorVisible.Should().BeTrue();

        terminal.Write("\x1b[?25l");
        terminal.CursorVisible.Should().BeFalse();

        terminal.Write("\x1b[?25h");
        terminal.CursorVisible.Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Alternate_Screen()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Main screen");
        terminal.IsAlternateScreen.Should().BeFalse();

        terminal.Write("\x1b[?1049h");
        terminal.IsAlternateScreen.Should().BeTrue();
        terminal.GetRowText(0).Should().Be(""); // Alternate is empty

        terminal.Write("Alt screen");
        terminal.GetRowText(0).Should().Be("Alt screen");

        terminal.Write("\x1b[?1049l");
        terminal.IsAlternateScreen.Should().BeFalse();
        terminal.GetRowText(0).Should().Be("Main screen"); // Main preserved
    }

    [Fact]
    public void Should_Track_SGR_Colors()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[31mR\x1b[32mG\x1b[34mB\x1b[0m");

        terminal.Screen[0, 0].Foreground.Should().NotBeNull();
        terminal.Screen[0, 0].Foreground!.Value.Index.Should().Be(31); // Red

        terminal.Screen[0, 1].Foreground.Should().NotBeNull();
        terminal.Screen[0, 1].Foreground!.Value.Index.Should().Be(32); // Green

        terminal.Screen[0, 2].Foreground.Should().NotBeNull();
        terminal.Screen[0, 2].Foreground!.Value.Index.Should().Be(34); // Blue
    }

    [Fact]
    public void Should_Track_SGR_8Bit_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38;5;196mX");

        var cell = terminal.Screen[0, 0];
        cell.Foreground.Should().NotBeNull();
        cell.Foreground!.Value.Mode.Should().Be(ColorMode.EightBit);
        cell.Foreground!.Value.Index.Should().Be(196);
    }

    [Fact]
    public void Should_Track_SGR_24Bit_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38;2;128;64;32mX");

        var cell = terminal.Screen[0, 0];
        cell.Foreground.Should().NotBeNull();
        cell.Foreground!.Value.Mode.Should().Be(ColorMode.TrueColor);
        cell.Foreground!.Value.R.Should().Be((byte)128);
        cell.Foreground!.Value.G.Should().Be((byte)64);
        cell.Foreground!.Value.B.Should().Be((byte)32);
    }

    [Fact]
    public void Should_Track_Decorations()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[1;3;4mX");

        var cell = terminal.Screen[0, 0];
        cell.Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
        cell.Decoration.HasFlag(CellDecoration.Italic).Should().BeTrue();
        cell.Decoration.HasFlag(CellDecoration.Underline).Should().BeTrue();
    }

    [Fact]
    public void Should_Reset_Decorations()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[1mA\x1b[0mB");

        terminal.Screen[0, 0].Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
        terminal.Screen[0, 1].Decoration.Should().Be(CellDecoration.None);
    }

    [Fact]
    public void Should_Handle_Backspace()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("AB\bC");

        // Backspace moves cursor left, then C overwrites B
        terminal.GetRowText(0).Should().Be("AC");
    }

    [Fact]
    public void Should_Find_Text_In_Screen()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\nWorld\nFoo");

        terminal.ContainsText("World").Should().BeTrue();
        terminal.ContainsText("Missing").Should().BeFalse();

        var pos = terminal.FindText("World");
        pos.Should().NotBeNull();
        pos!.Value.Row.Should().Be(1);
        pos!.Value.Col.Should().Be(0);
    }

    [Fact]
    public void Should_Handle_Cursor_Position_Absolute()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[3;10HX");

        // CSI 3;10 H = row 3, col 10 (1-indexed) = row 2, col 9 (0-indexed)
        terminal.Screen[2, 9].Character.Should().Be('X');
    }

    [Fact]
    public void Should_Clamp_Cursor_To_Bounds()
    {
        var terminal = new PhantomTerminal(10, 5);

        terminal.Write("\x1b[100A"); // Try to move up 100 from row 0
        terminal.CursorRow.Should().Be(0);

        terminal.Write("\x1b[100B"); // Try to move down 100
        terminal.CursorRow.Should().Be(4); // Clamped to last row
    }

    [Fact]
    public void Should_Track_Sequence_History()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[sHello\x1b[u");

        terminal.SequenceHistory.Count.Should().Be(3);
        terminal.SequenceHistory[0].Should().BeOfType<AnsiSequence.SaveCursor>();
        terminal.SequenceHistory[1].Should().BeOfType<AnsiSequence.Text>();
        terminal.SequenceHistory[2].Should().BeOfType<AnsiSequence.RestoreCursor>();
    }

    [Fact]
    public void Should_Reset_Terminal_State()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\x1b[?25l");

        terminal.Reset();

        terminal.GetRowText(0).Should().Be("");
        terminal.CursorVisible.Should().BeTrue();
        terminal.CursorRow.Should().Be(0);
        terminal.CursorCol.Should().Be(0);
        terminal.SequenceHistory.Should().BeEmpty();
    }

    [Fact]
    public void Should_Handle_Erase_Line_To_Start()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello World");
        terminal.Write("\x1b[6G");  // Move to column 6 (0-indexed: 5)
        terminal.Write("\x1b[1K");  // Erase from start of line to cursor

        // Characters 0-5 should be erased, "World" (6-10) should remain
        terminal.Screen[0, 0].Character.Should().Be(' ');
        terminal.Screen[0, 4].Character.Should().Be(' ');
        terminal.Screen[0, 5].Character.Should().Be(' ');
        terminal.Screen[0, 6].Character.Should().Be('W');
    }

    [Fact]
    public void Should_Handle_Erase_In_Display_To_Start()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2\nLine 3");
        terminal.Write("\x1b[2;4H"); // Move to row 2, col 4 (1-indexed)
        terminal.Write("\x1b[1J");   // Erase from start to cursor

        // Row 0 should be fully erased
        terminal.GetRowText(0).Should().Be("");
        // Row 1 should be erased up to col 3 (0-indexed)
        terminal.Screen[1, 0].Character.Should().Be(' ');
        terminal.Screen[1, 3].Character.Should().Be(' ');
    }

    [Fact]
    public void Should_Handle_Hyperlink()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b]8;;https://example.com\x1b\\Link\x1b]8;;\x1b\\");

        terminal.Screen[0, 0].HyperlinkUrl.Should().Be("https://example.com");
        terminal.Screen[0, 3].HyperlinkUrl.Should().Be("https://example.com");
        // After closing hyperlink, next char has no URL
    }

    // ── SGR: Background Colors ───────────────────────────────────────

    [Fact]
    public void Should_Track_SGR_Background_Legacy()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[42mX");

        var cell = terminal.Screen[0, 0];
        cell.Background.Should().NotBeNull();
        cell.Background!.Value.Mode.Should().Be(ColorMode.Legacy);
        cell.Background!.Value.Index.Should().Be(42);
    }

    [Fact]
    public void Should_Track_SGR_Background_Bright()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[104mX");

        var cell = terminal.Screen[0, 0];
        cell.Background.Should().NotBeNull();
        cell.Background!.Value.Index.Should().Be(104);
    }

    [Fact]
    public void Should_Track_SGR_Background_8Bit()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[48;5;220mX");

        var cell = terminal.Screen[0, 0];
        cell.Background.Should().NotBeNull();
        cell.Background!.Value.Mode.Should().Be(ColorMode.EightBit);
        cell.Background!.Value.Index.Should().Be(220);
    }

    [Fact]
    public void Should_Track_SGR_Background_24Bit()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[48;2;10;20;30mX");

        var cell = terminal.Screen[0, 0];
        cell.Background.Should().NotBeNull();
        cell.Background!.Value.Mode.Should().Be(ColorMode.TrueColor);
        cell.Background!.Value.R.Should().Be((byte)10);
        cell.Background!.Value.G.Should().Be((byte)20);
        cell.Background!.Value.B.Should().Be((byte)30);
    }

    // ── SGR: Default Color Reset ─────────────────────────────────────

    [Fact]
    public void Should_Reset_Foreground_To_Default()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[31mA\x1b[39mB");

        terminal.Screen[0, 0].Foreground.Should().NotBeNull();
        terminal.Screen[0, 1].Foreground.Should().BeNull();
    }

    [Fact]
    public void Should_Reset_Background_To_Default()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[42mA\x1b[49mB");

        terminal.Screen[0, 0].Background.Should().NotBeNull();
        terminal.Screen[0, 1].Background.Should().BeNull();
    }

    // ── SGR: All Decorations ─────────────────────────────────────────

    [Theory]
    [InlineData(2, CellDecoration.Dim)]
    [InlineData(5, CellDecoration.SlowBlink)]
    [InlineData(6, CellDecoration.RapidBlink)]
    [InlineData(7, CellDecoration.Reverse)]
    [InlineData(8, CellDecoration.Conceal)]
    [InlineData(9, CellDecoration.Strikethrough)]
    public void Should_Track_All_SGR_Decorations(int sgrCode, CellDecoration expected)
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write($"\x1b[{sgrCode}mX");

        terminal.Screen[0, 0].Decoration.HasFlag(expected).Should().BeTrue();
    }

    // ── SGR: Extended Color Edge Cases ───────────────────────────────

    [Fact]
    public void Should_Handle_Truncated_Extended_Foreground_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38mX");

        terminal.GetRowText(0).Should().Be("X");
    }

    [Fact]
    public void Should_Handle_Extended_Color_Unknown_Mode()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38;9mX");

        terminal.GetRowText(0).Should().Be("X");
    }

    [Fact]
    public void Should_Handle_Truncated_8Bit_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38;5mX");

        terminal.GetRowText(0).Should().Be("X");
    }

    [Fact]
    public void Should_Handle_Truncated_24Bit_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[38;2;100;200mX");

        terminal.GetRowText(0).Should().Be("X");
    }

    // ── Cursor Movement ──────────────────────────────────────────────

    [Fact]
    public void Should_Handle_Cursor_Left()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("ABCD\x1b[2DXY");

        terminal.GetRowText(0).Should().Be("ABXY");
    }

    [Fact]
    public void Should_Handle_Cursor_Right()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("A\x1b[3CB");

        terminal.Screen[0, 0].Character.Should().Be('A');
        terminal.Screen[0, 4].Character.Should().Be('B');
    }

    [Fact]
    public void Should_Clamp_Cursor_Left_To_Zero()
    {
        var terminal = new PhantomTerminal(10, 5);
        terminal.Write("\x1b[100D");
        terminal.CursorCol.Should().Be(0);
    }

    [Fact]
    public void Should_Clamp_Cursor_Right_To_Width_Minus_One()
    {
        var terminal = new PhantomTerminal(10, 5);
        terminal.Write("\x1b[100C");
        terminal.CursorCol.Should().Be(9);
    }

    // ── Cursor Position ──────────────────────────────────────────────

    [Fact]
    public void Should_Handle_Cursor_Home()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\nWorld");
        terminal.Write("\x1b[H");

        terminal.CursorRow.Should().Be(0);
        terminal.CursorCol.Should().Be(0);
    }

    [Fact]
    public void Should_Handle_Cursor_Horizontal_Absolute()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello");
        terminal.Write("\x1b[3G");

        terminal.CursorCol.Should().Be(2);
    }

    [Fact]
    public void Should_Clamp_Cursor_Position_To_Bounds()
    {
        var terminal = new PhantomTerminal(10, 5);

        terminal.Write("\x1b[100;3H");
        terminal.CursorRow.Should().Be(4);

        terminal.Write("\x1b[1;100H");
        terminal.CursorCol.Should().Be(9);
    }

    [Fact]
    public void Should_Clamp_Cursor_Horizontal_Absolute_To_Bounds()
    {
        var terminal = new PhantomTerminal(10, 5);
        terminal.Write("\x1b[100G");
        terminal.CursorCol.Should().Be(9);
    }

    // ── Erase Modes ──────────────────────────────────────────────────

    [Fact]
    public void Should_Handle_Erase_In_Display_All()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Line 1\nLine 2\nLine 3");
        terminal.Write("\x1b[2J");

        terminal.GetRowText(0).Should().BeEmpty();
        terminal.GetRowText(1).Should().BeEmpty();
        terminal.GetRowText(2).Should().BeEmpty();
    }

    [Fact]
    public void Should_Handle_Erase_In_Display_Scrollback()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Content");
        terminal.Write("\x1b[3J");

        terminal.GetRowText(0).Should().BeEmpty();
    }

    // ── Line Wrap With Scroll ────────────────────────────────────────

    [Fact]
    public void Should_Scroll_When_Line_Wraps_Past_Bottom()
    {
        var terminal = new PhantomTerminal(5, 2);

        terminal.Write("1234567890");
        terminal.GetRowText(0).Should().Be("12345");
        terminal.GetRowText(1).Should().Be("67890");

        terminal.Write("X");
        terminal.GetRowText(0).Should().Be("67890");
        terminal.GetRowText(1).Should().Be("X");
    }

    // ── Alternate Screen ─────────────────────────────────────────────

    [Fact]
    public void Should_Reset_Cursor_When_Entering_Alternate_Screen()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\nWorld");

        terminal.Write("\x1b[?1049h");

        terminal.CursorRow.Should().Be(0);
        terminal.CursorCol.Should().Be(0);
    }

    [Fact]
    public void Should_Restore_Main_Cursor_When_Leaving_Alternate_Screen()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\nWorld");
        var mainRow = terminal.CursorRow;
        var mainCol = terminal.CursorCol;

        terminal.Write("\x1b[?1049h");
        terminal.Write("Alt content");

        terminal.Write("\x1b[?1049l");
        terminal.CursorRow.Should().Be(mainRow);
        terminal.CursorCol.Should().Be(mainCol);
    }

    [Fact]
    public void Should_Reuse_Existing_Alternate_Buffer()
    {
        var terminal = new PhantomTerminal(80, 24);

        terminal.Write("\x1b[?1049h");
        terminal.Write("First");
        terminal.Write("\x1b[?1049l");

        terminal.Write("\x1b[?1049h");
        terminal.IsAlternateScreen.Should().BeTrue();
    }

    // ── GetScreenText / GetCell ───────────────────────────────────────

    [Fact]
    public void GetScreenText_Should_Return_All_Content()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("Hello\nWorld");

        var text = terminal.GetScreenText();
        text.Should().Contain("Hello");
        text.Should().Contain("World");
    }

    [Fact]
    public void GetCell_Should_Return_Cell_With_Style()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[1;31mX");

        var cell = terminal.GetCell(0, 0);
        cell.Character.Should().Be('X');
        cell.Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
        cell.Foreground.Should().NotBeNull();
    }

    // ── Width / Height ───────────────────────────────────────────────

    [Fact]
    public void Width_And_Height_Should_Match_Constructor()
    {
        var terminal = new PhantomTerminal(132, 50);
        terminal.Width.Should().Be(132);
        terminal.Height.Should().Be(50);
    }

    [Fact]
    public void Default_Constructor_Should_Use_80x24()
    {
        var terminal = new PhantomTerminal();
        terminal.Width.Should().Be(80);
        terminal.Height.Should().Be(24);
    }

    // ── Null Input ───────────────────────────────────────────────────

    [Fact]
    public void Write_Should_Throw_For_Null()
    {
        var terminal = new PhantomTerminal(80, 24);
        FluentActions.Invoking(() => terminal.Write(null!)).Should().Throw<ArgumentNullException>();
    }

    // ── Backspace at column 0 ────────────────────────────────────────

    [Fact]
    public void Backspace_At_Column_Zero_Should_Not_Move()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\b");
        terminal.CursorCol.Should().Be(0);
    }

    // ── Hyperlink Tracking ───────────────────────────────────────────

    [Fact]
    public void Should_Clear_Hyperlink_After_Close()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b]8;;https://example.com\x1b\\AB\x1b]8;;\x1b\\CD");

        terminal.Screen[0, 0].HyperlinkUrl.Should().Be("https://example.com");
        terminal.Screen[0, 1].HyperlinkUrl.Should().Be("https://example.com");
        terminal.Screen[0, 2].HyperlinkUrl.Should().BeNull();
        terminal.Screen[0, 3].HyperlinkUrl.Should().BeNull();
    }

    // ── Assertion Helper Integration ─────────────────────────────────

    [Fact]
    public void AssertCursorAt_Should_Work()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("AB");
        terminal.AssertCursorAt(0, 2);
    }

    [Fact]
    public void AssertCursorVisible_Should_Work()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.AssertCursorVisible(true);
        terminal.Write("\x1b[?25l");
        terminal.AssertCursorVisible(false);
    }

    [Fact]
    public void AssertAlternateScreen_Should_Work()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.AssertAlternateScreen(false);
        terminal.Write("\x1b[?1049h");
        terminal.AssertAlternateScreen(true);
    }

    [Fact]
    public void AssertHistoryContains_Should_Work()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[s\x1b[u");
        terminal.AssertHistoryContains<AnsiSequence.SaveCursor>();
        terminal.AssertHistoryContains<AnsiSequence.RestoreCursor>();
        terminal.AssertHistoryCount<AnsiSequence.SaveCursor>(1);
    }

    // ── Combined foreground and background ───────────────────────────

    [Fact]
    public void Should_Track_Combined_Foreground_And_Background()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[31;42mX");

        var cell = terminal.Screen[0, 0];
        cell.Foreground!.Value.Index.Should().Be(31);
        cell.Background!.Value.Index.Should().Be(42);
    }

    [Fact]
    public void Should_Track_Combined_Decoration_And_Color()
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write("\x1b[1;3;31mX");

        var cell = terminal.Screen[0, 0];
        cell.Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
        cell.Decoration.HasFlag(CellDecoration.Italic).Should().BeTrue();
        cell.Foreground!.Value.Index.Should().Be(31);
    }

    // ── SGR foreground range ─────────────────────────────────────────

    [Theory]
    [InlineData(90)]
    [InlineData(91)]
    [InlineData(97)]
    public void Should_Track_Bright_Foreground_Colors(int sgrCode)
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write($"\x1b[{sgrCode}mX");

        terminal.Screen[0, 0].Foreground!.Value.Index.Should().Be(sgrCode);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(101)]
    [InlineData(107)]
    public void Should_Track_Bright_Background_Colors(int sgrCode)
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write($"\x1b[{sgrCode}mX");

        terminal.Screen[0, 0].Background!.Value.Index.Should().Be(sgrCode);
    }

    // ── Reset: complete state check ──────────────────────────────────

    [Fact]
    public void Reset_Should_Set_Alternate_Screen_To_False()
    {
        var terminal = new PhantomTerminal(80, 24);

        // Enter alternate screen, then reset
        terminal.Write("\x1b[?1049h");
        terminal.IsAlternateScreen.Should().BeTrue();

        terminal.Reset();

        terminal.IsAlternateScreen.Should().BeFalse();
    }

    [Fact]
    public void Reset_Should_Clear_Current_Style()
    {
        var terminal = new PhantomTerminal(80, 24);

        // Set bold + red foreground style, then reset
        terminal.Write("\x1b[1;31mA");
        terminal.Reset();

        // After reset, new text should have no decoration or color
        terminal.Write("X");
        var cell = terminal.Screen[0, 0];
        cell.Character.Should().Be('X');
        cell.Decoration.Should().Be(CellDecoration.None);
        cell.Foreground.Should().BeNull();
    }

    // ── Alternate screen: buffer preservation (??= semantics) ────────

    [Fact]
    public void Should_Preserve_Alternate_Buffer_Content_When_Reusing()
    {
        var terminal = new PhantomTerminal(80, 24);

        // Enter alt screen, write content, exit, then re-enter
        terminal.Write("\x1b[?1049h");
        terminal.Write("AltContent");
        terminal.Write("\x1b[?1049l");

        // Re-enter: existing alternate buffer reused (??= not = semantics)
        terminal.Write("\x1b[?1049h");
        terminal.GetRowText(0).Should().Be("AltContent");
    }

    // ── SGR: decoration idempotency (|= not ^=) ──────────────────────

    [Theory]
    [InlineData(1, CellDecoration.Bold)]
    [InlineData(2, CellDecoration.Dim)]
    [InlineData(3, CellDecoration.Italic)]
    [InlineData(4, CellDecoration.Underline)]
    [InlineData(5, CellDecoration.SlowBlink)]
    [InlineData(6, CellDecoration.RapidBlink)]
    [InlineData(7, CellDecoration.Reverse)]
    [InlineData(8, CellDecoration.Conceal)]
    [InlineData(9, CellDecoration.Strikethrough)]
    public void Should_Keep_Decoration_When_Same_SGR_Applied_Twice(int sgrCode, CellDecoration expected)
    {
        // `|=` means applying the same decoration twice still sets it.
        // `^=` (XOR) would cancel it out — this test verifies |= semantics.
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write($"\x1b[{sgrCode};{sgrCode}mX");

        terminal.Screen[0, 0].Decoration.HasFlag(expected).Should().BeTrue();
    }

    // ── SGR: boundary foreground/background colors ───────────────────

    [Theory]
    [InlineData(30)]
    [InlineData(37)]
    public void Should_Track_Legacy_Foreground_Color_At_Boundary(int sgrCode)
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write($"\x1b[{sgrCode}mX");

        var cell = terminal.Screen[0, 0];
        cell.Foreground.Should().NotBeNull();
        cell.Foreground!.Value.Index.Should().Be(sgrCode);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(47)]
    public void Should_Track_Legacy_Background_Color_At_Boundary(int sgrCode)
    {
        var terminal = new PhantomTerminal(80, 24);
        terminal.Write($"\x1b[{sgrCode}mX");

        var cell = terminal.Screen[0, 0];
        cell.Background.Should().NotBeNull();
        cell.Background!.Value.Index.Should().Be(sgrCode);
    }
}
