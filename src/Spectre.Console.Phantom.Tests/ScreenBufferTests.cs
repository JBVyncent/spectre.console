using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

/// <summary>
/// Comprehensive unit tests for <see cref="ScreenBuffer"/>.
/// Covers construction, character writing, all erase operations,
/// scrolling, text retrieval, region queries, and boundary conditions.
/// </summary>
public sealed class ScreenBufferTests
{
    // ── Construction ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_Should_Create_Buffer_With_Correct_Dimensions()
    {
        var buffer = new ScreenBuffer(40, 10);
        buffer.Width.Should().Be(40);
        buffer.Height.Should().Be(10);
    }

    [Fact]
    public void Constructor_Should_Initialize_All_Cells_To_Space()
    {
        var buffer = new ScreenBuffer(5, 3);
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 5; c++)
            {
                buffer[r, c].Character.Should().Be(' ');
            }
        }
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(-100, 5)]
    public void Constructor_Should_Throw_For_Invalid_Width(int width, int height)
    {
        FluentActions.Invoking(() => new ScreenBuffer(width, height)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(5, -1)]
    [InlineData(5, -100)]
    public void Constructor_Should_Throw_For_Invalid_Height(int width, int height)
    {
        FluentActions.Invoking(() => new ScreenBuffer(width, height)).Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Indexer ──────────────────────────────────────────────────────

    [Fact]
    public void Indexer_Should_Return_Cell_At_Position()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell { Foreground = CellColor.FromLegacy(31) };
        buffer.WriteChar(2, 3, 'X', style);

        buffer[2, 3].Character.Should().Be('X');
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(5, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 10)]
    public void Indexer_Should_Throw_For_Out_Of_Bounds(int row, int col)
    {
        var buffer = new ScreenBuffer(10, 5);
        FluentActions.Invoking(() => _ = buffer[row, col]).Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── WriteChar ────────────────────────────────────────────────────

    [Fact]
    public void WriteChar_Should_Set_Character_And_Style()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell
        {
            Foreground = CellColor.FromLegacy(31),
            Background = CellColor.FromLegacy(42),
            Decoration = CellDecoration.Bold | CellDecoration.Italic,
            HyperlinkUrl = "https://example.com",
        };
        buffer.WriteChar(1, 2, 'Z', style);

        var cell = buffer[1, 2];
        cell.Character.Should().Be('Z');
        cell.Foreground!.Value.Index.Should().Be(31);
        cell.Background!.Value.Index.Should().Be(42);
        cell.Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
        cell.Decoration.HasFlag(CellDecoration.Italic).Should().BeTrue();
        cell.HyperlinkUrl.Should().Be("https://example.com");
    }

    [Fact]
    public void WriteChar_Should_Silently_Ignore_Out_Of_Bounds()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell();

        // None of these should throw
        FluentActions.Invoking(() => buffer.WriteChar(-1, 0, 'X', style)).Should().NotThrow();
        FluentActions.Invoking(() => buffer.WriteChar(5, 0, 'X', style)).Should().NotThrow();
        FluentActions.Invoking(() => buffer.WriteChar(0, -1, 'X', style)).Should().NotThrow();
        FluentActions.Invoking(() => buffer.WriteChar(0, 10, 'X', style)).Should().NotThrow();
    }

    [Fact]
    public void WriteChar_Should_Throw_For_Null_Style()
    {
        var buffer = new ScreenBuffer(10, 5);
        FluentActions.Invoking(() => buffer.WriteChar(0, 0, 'X', null!)).Should().Throw<ArgumentNullException>();
    }

    // ── EraseToEnd ───────────────────────────────────────────────────

    [Fact]
    public void EraseToEnd_Should_Clear_From_Cursor_To_End_Of_Screen()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();

        // Fill all cells with 'A'
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 10; c++)
            {
                buffer.WriteChar(r, c, 'A', style);
            }
        }

        buffer.EraseToEnd(1, 5);

        // Row 0 should be intact
        buffer[0, 0].Character.Should().Be('A');
        buffer[0, 9].Character.Should().Be('A');

        // Row 1: cols 0-4 intact, cols 5-9 erased
        buffer[1, 4].Character.Should().Be('A');
        buffer[1, 5].Character.Should().Be(' ');
        buffer[1, 9].Character.Should().Be(' ');

        // Row 2: all erased
        buffer[2, 0].Character.Should().Be(' ');
        buffer[2, 9].Character.Should().Be(' ');
    }

    [Fact]
    public void EraseToEnd_Should_Handle_Row_Out_Of_Bounds_Gracefully()
    {
        var buffer = new ScreenBuffer(5, 3);
        // Should not throw even with out-of-bounds row
        FluentActions.Invoking(() => buffer.EraseToEnd(-1, 0)).Should().NotThrow();
        FluentActions.Invoking(() => buffer.EraseToEnd(5, 0)).Should().NotThrow();
    }

    // ── EraseToStart ─────────────────────────────────────────────────

    [Fact]
    public void EraseToStart_Should_Clear_From_Start_To_Cursor()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 10; c++)
            {
                buffer.WriteChar(r, c, 'B', style);
            }
        }

        buffer.EraseToStart(1, 3);

        // Row 0: all erased
        buffer[0, 0].Character.Should().Be(' ');
        buffer[0, 9].Character.Should().Be(' ');

        // Row 1: cols 0-3 erased, cols 4-9 intact
        buffer[1, 3].Character.Should().Be(' ');
        buffer[1, 4].Character.Should().Be('B');

        // Row 2: all intact
        buffer[2, 0].Character.Should().Be('B');
    }

    // ── EraseAll ─────────────────────────────────────────────────────

    [Fact]
    public void EraseAll_Should_Clear_Entire_Buffer()
    {
        var buffer = new ScreenBuffer(5, 3);
        var style = new ScreenCell();
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 5; c++)
            {
                buffer.WriteChar(r, c, 'X', style);
            }
        }

        buffer.EraseAll();

        for (var r = 0; r < 3; r++)
        {
            buffer.GetRowText(r).Should().BeEmpty();
        }
    }

    // ── EraseLineToEnd ───────────────────────────────────────────────

    [Fact]
    public void EraseLineToEnd_Should_Clear_From_Col_To_End_Of_Row()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(0, c, 'C', style);
        }

        buffer.EraseLineToEnd(0, 5);

        buffer[0, 4].Character.Should().Be('C');
        buffer[0, 5].Character.Should().Be(' ');
        buffer[0, 9].Character.Should().Be(' ');
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void EraseLineToEnd_Should_Return_For_Out_Of_Bounds_Row(int row)
    {
        var buffer = new ScreenBuffer(10, 5);
        FluentActions.Invoking(() => buffer.EraseLineToEnd(row, 0)).Should().NotThrow();
    }

    // ── EraseLineToStart ─────────────────────────────────────────────

    [Fact]
    public void EraseLineToStart_Should_Clear_From_Start_To_Col()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(0, c, 'D', style);
        }

        buffer.EraseLineToStart(0, 4);

        buffer[0, 4].Character.Should().Be(' ');
        buffer[0, 5].Character.Should().Be('D');
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void EraseLineToStart_Should_Return_For_Out_Of_Bounds_Row(int row)
    {
        var buffer = new ScreenBuffer(10, 5);
        FluentActions.Invoking(() => buffer.EraseLineToStart(row, 0)).Should().NotThrow();
    }

    // ── EraseLine ────────────────────────────────────────────────────

    [Fact]
    public void EraseLine_Should_Clear_Entire_Row()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(1, c, 'E', style);
        }

        buffer.EraseLine(1);

        buffer.GetRowText(1).Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void EraseLine_Should_Return_For_Out_Of_Bounds_Row(int row)
    {
        var buffer = new ScreenBuffer(10, 5);
        FluentActions.Invoking(() => buffer.EraseLine(row)).Should().NotThrow();
    }

    // ── ScrollUp ─────────────────────────────────────────────────────

    [Fact]
    public void ScrollUp_Should_Shift_Rows_Up_And_Clear_Last()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();

        // Write different text to each row
        foreach (var ch in "ABC")
        {
            var row = ch - 'A';
            for (var c = 0; c < 1; c++)
            {
                buffer.WriteChar(row, c, ch, style);
            }
        }

        buffer.ScrollUp();

        buffer.GetRowText(0).Should().Be("B");
        buffer.GetRowText(1).Should().Be("C");
        buffer.GetRowText(2).Should().BeEmpty();
    }

    [Fact]
    public void ScrollUp_Should_Preserve_Cell_Styles()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell
        {
            Foreground = CellColor.FromLegacy(31),
            Decoration = CellDecoration.Bold,
        };
        buffer.WriteChar(1, 0, 'X', style);

        buffer.ScrollUp();

        // Row 1 should have scrolled to row 0
        var cell = buffer[0, 0];
        cell.Character.Should().Be('X');
        cell.Foreground!.Value.Index.Should().Be(31);
        cell.Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
    }

    // ── GetRowText ───────────────────────────────────────────────────

    [Fact]
    public void GetRowText_Should_Trim_Trailing_Spaces()
    {
        var buffer = new ScreenBuffer(20, 3);
        var style = new ScreenCell();
        var text = "Hello";
        for (var i = 0; i < text.Length; i++)
        {
            buffer.WriteChar(0, i, text[i], style);
        }

        buffer.GetRowText(0).Should().Be("Hello");
    }

    [Fact]
    public void GetRowText_Should_Throw_For_Invalid_Row()
    {
        var buffer = new ScreenBuffer(10, 5);
        FluentActions.Invoking(() => buffer.GetRowText(-1)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => buffer.GetRowText(5)).Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── GetText ──────────────────────────────────────────────────────

    [Fact]
    public void GetText_Should_Return_All_Rows_Joined_By_Newline()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell();
        for (var i = 0; i < 3; i++)
        {
            buffer.WriteChar(0, i, "Hi!"[i], style);
        }
        for (var i = 0; i < 5; i++)
        {
            buffer.WriteChar(1, i, "World"[i], style);
        }

        buffer.GetText().Should().Be("Hi!\nWorld");
    }

    [Fact]
    public void GetText_Should_Trim_Trailing_Empty_Lines()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell();
        buffer.WriteChar(0, 0, 'A', style);
        // Rows 1-4 are empty

        buffer.GetText().Should().Be("A");
    }

    [Fact]
    public void GetText_On_Empty_Buffer_Should_Return_Empty_String()
    {
        var buffer = new ScreenBuffer(10, 5);
        buffer.GetText().Should().BeEmpty();
    }

    // ── GetRegionText ────────────────────────────────────────────────

    [Fact]
    public void GetRegionText_Should_Return_Rectangular_Region()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell();
        var lines = new[] { "0123456789", "ABCDEFGHIJ", "abcdefghij" };
        for (var r = 0; r < lines.Length; r++)
        {
            for (var c = 0; c < 10; c++)
            {
                buffer.WriteChar(r, c, lines[r][c], style);
            }
        }

        var region = buffer.GetRegionText(0, 2, 2, 5);
        // Row 0: cols 2-5 = "2345"
        // Row 1: cols 0-5 = "ABCDEF"
        // Row 2: cols 0-5 = "abcdef"
        region.Should().Contain("2345");
        region.Should().Contain("ABCDEF");
        region.Should().Contain("abcdef");
    }

    [Fact]
    public void GetRegionText_Single_Row_Should_Return_Substring()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(0, c, (char)('A' + c), style);
        }

        var region = buffer.GetRegionText(0, 2, 0, 5);
        region.Should().Be("CDEF");
    }

    // ── HasCharAt ────────────────────────────────────────────────────

    [Fact]
    public void HasCharAt_Should_Return_True_For_Matching_Character()
    {
        var buffer = new ScreenBuffer(10, 5);
        buffer.WriteChar(0, 0, 'X', new ScreenCell());

        buffer.HasCharAt(0, 0, 'X').Should().BeTrue();
        buffer.HasCharAt(0, 0, 'Y').Should().BeFalse();
    }

    [Fact]
    public void HasCharAt_Should_Return_False_For_Out_Of_Bounds()
    {
        var buffer = new ScreenBuffer(10, 5);

        buffer.HasCharAt(-1, 0, ' ').Should().BeFalse();
        buffer.HasCharAt(5, 0, ' ').Should().BeFalse();
        buffer.HasCharAt(0, -1, ' ').Should().BeFalse();
        buffer.HasCharAt(0, 10, ' ').Should().BeFalse();
    }

    // ── FindText / ContainsText ──────────────────────────────────────

    [Fact]
    public void FindText_Should_Return_Position_Of_First_Match()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var i = 0; i < 5; i++)
        {
            buffer.WriteChar(1, i + 3, "Hello"[i], style);
        }

        var pos = buffer.FindText("Hello");
        pos.Should().NotBeNull();
        pos!.Value.Row.Should().Be(1);
        pos!.Value.Col.Should().Be(3);
    }

    [Fact]
    public void FindText_Should_Return_Null_When_Not_Found()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.FindText("Missing").Should().BeNull();
    }

    [Fact]
    public void FindText_Should_Throw_For_Null_Input()
    {
        var buffer = new ScreenBuffer(10, 3);
        FluentActions.Invoking(() => buffer.FindText(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ContainsText_Should_Return_True_When_Found()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'A', new ScreenCell());
        buffer.ContainsText("A").Should().BeTrue();
    }

    [Fact]
    public void ContainsText_Should_Return_False_When_Not_Found()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.ContainsText("Z").Should().BeFalse();
    }

    // ── Assertion Helpers Integration ────────────────────────────────

    [Fact]
    public void AssertRowContains_Should_Pass_When_Text_Present()
    {
        var buffer = new ScreenBuffer(20, 3);
        var style = new ScreenCell();
        var text = "Hello World";
        for (var i = 0; i < text.Length; i++)
        {
            buffer.WriteChar(0, i, text[i], style);
        }

        buffer.AssertRowContains(0, "World");
    }

    [Fact]
    public void AssertRowEquals_Should_Pass_When_Row_Matches()
    {
        var buffer = new ScreenBuffer(20, 3);
        var style = new ScreenCell();
        var text = "Test";
        for (var i = 0; i < text.Length; i++)
        {
            buffer.WriteChar(0, i, text[i], style);
        }

        buffer.AssertRowEquals(0, "Test");
    }

    [Fact]
    public void AssertRowStartsWith_Should_Pass_When_Row_Starts_With_Text()
    {
        var buffer = new ScreenBuffer(20, 3);
        var style = new ScreenCell();
        var text = "Prefix Suffix";
        for (var i = 0; i < text.Length; i++)
        {
            buffer.WriteChar(0, i, text[i], style);
        }

        buffer.AssertRowStartsWith(0, "Prefix");
    }

    [Fact]
    public void AssertRowEmpty_Should_Pass_For_Empty_Row()
    {
        var buffer = new ScreenBuffer(20, 3);
        buffer.AssertRowEmpty(0);
    }

    [Fact]
    public void AssertContainsText_Should_Pass()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'Q', new ScreenCell());
        buffer.AssertContainsText("Q");
    }

    [Fact]
    public void AssertNotContainsText_Should_Pass()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.AssertNotContainsText("Z");
    }

    [Fact]
    public void AssertTextAt_Should_Verify_String_At_Position()
    {
        var buffer = new ScreenBuffer(20, 3);
        var style = new ScreenCell();
        var text = "ABC";
        for (var i = 0; i < text.Length; i++)
        {
            buffer.WriteChar(1, 5 + i, text[i], style);
        }

        buffer.AssertTextAt(1, 5, "ABC");
    }

    [Fact]
    public void AssertCharAt_Should_Verify_Single_Character()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'Q', new ScreenCell());
        buffer.AssertCharAt(0, 0, 'Q');
    }

    [Fact]
    public void AssertCellDecoration_Should_Verify_Flags()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'X', new ScreenCell { Decoration = CellDecoration.Bold | CellDecoration.Underline });
        buffer.AssertCellDecoration(0, 0, CellDecoration.Bold);
        buffer.AssertCellDecoration(0, 0, CellDecoration.Underline);
    }

    [Fact]
    public void AssertCellNoDecoration_Should_Verify_No_Flags()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'X', new ScreenCell());
        buffer.AssertCellNoDecoration(0, 0);
    }

    [Fact]
    public void AssertCellHyperlink_Should_Verify_Url()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'L', new ScreenCell { HyperlinkUrl = "https://test.com" });
        buffer.AssertCellHyperlink(0, 0, "https://test.com");
    }

    [Fact]
    public void AssertCellNoHyperlink_Should_Verify_No_Url()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'X', new ScreenCell());
        buffer.AssertCellNoHyperlink(0, 0);
    }

    [Fact]
    public void AssertCellForeground_Should_Verify_Color()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'R', new ScreenCell { Foreground = CellColor.FromLegacy(31) });
        buffer.AssertCellForeground(0, 0, ColorMode.Legacy, 31);
    }

    [Fact]
    public void AssertCellForegroundRgb_Should_Verify_Rgb()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'X', new ScreenCell { Foreground = CellColor.FromRgb(100, 200, 50) });
        buffer.AssertCellForegroundRgb(0, 0, 100, 200, 50);
    }

    [Fact]
    public void AssertCellBackground_Should_Verify_Color()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'B', new ScreenCell { Background = CellColor.FromEightBit(196) });
        buffer.AssertCellBackground(0, 0, ColorMode.EightBit, 196);
    }

    [Fact]
    public void AssertCellBackgroundRgb_Should_Verify_Rgb()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'X', new ScreenCell { Background = CellColor.FromRgb(10, 20, 30) });
        buffer.AssertCellBackgroundRgb(0, 0, 10, 20, 30);
    }

    [Fact]
    public void AssertCellDefaultForeground_Should_Verify_Null()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'X', new ScreenCell());
        buffer.AssertCellDefaultForeground(0, 0);
    }

    [Fact]
    public void AssertCellDefaultBackground_Should_Verify_Null()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'X', new ScreenCell());
        buffer.AssertCellDefaultBackground(0, 0);
    }

    // ── EraseToEnd: boundary check at row == Height-1 ─────────────────

    [Fact]
    public void EraseToEnd_Should_Erase_At_Last_Valid_Row()
    {
        var buffer = new ScreenBuffer(5, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(2, c, 'Z', style);
        }

        buffer.EraseToEnd(2, 2);

        buffer[2, 1].Character.Should().Be('Z');
        buffer[2, 2].Character.Should().Be(' ');
        buffer[2, 4].Character.Should().Be(' ');
    }

    [Fact]
    public void EraseToEnd_Should_Not_Crash_When_Row_Equals_Height()
    {
        // Kills: L82 `row < Height` → `row <= Height`
        // With `<=`, row == Height passes the guard → IndexOutOfRangeException on _cells[3, c].
        var buffer = new ScreenBuffer(5, 3);
        FluentActions.Invoking(() => buffer.EraseToEnd(3, 0)).Should().NotThrow(); // row == Height
    }

    // ── EraseToStart: boundary at col == Width-1 ────────────────────

    [Fact]
    public void EraseToStart_Should_Erase_Up_To_Last_Column()
    {
        var buffer = new ScreenBuffer(5, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(2, c, 'X', style);
        }

        // col = Width-1 = 4: should erase all cols 0..4 on row 2 (last valid row)
        buffer.EraseToStart(2, 4);

        for (var c = 0; c < 5; c++)
        {
            buffer[2, c].Character.Should().Be(' ');
        }
    }

    [Fact]
    public void EraseToStart_Should_Not_Crash_When_Col_Equals_Width()
    {
        // Kills: L113 `c < Width` → `c <= Width`
        // With `<=`, c == Width passes → _cells[row, Width] → IndexOutOfRangeException.
        var buffer = new ScreenBuffer(5, 3);
        FluentActions.Invoking(() => buffer.EraseToStart(1, 5)).Should().NotThrow(); // col == Width
    }

    [Fact]
    public void EraseToStart_Should_Not_Erase_When_Row_Is_Negative()
    {
        // Kills: L115 `row >= 0 && row < Height` → `row >= 0 || row < Height`
        // With `||`, row=-1 would pass because `row < Height` is true, causing IndexOutOfRangeException.
        var buffer = new ScreenBuffer(5, 3);
        FluentActions.Invoking(() => buffer.EraseToStart(-1, 2)).Should().NotThrow();
    }

    [Fact]
    public void EraseToStart_Should_Not_Crash_When_Row_Equals_Height()
    {
        // Kills: L115 `row < Height` → `row <= Height`
        // With `<=`, row == Height passes the guard → IndexOutOfRangeException on _cells[3, c].
        var buffer = new ScreenBuffer(5, 3);
        FluentActions.Invoking(() => buffer.EraseToStart(3, 2)).Should().NotThrow(); // row == Height
    }

    // ── EraseLineToStart: boundary at col == Width-1 ─────────────────

    [Fact]
    public void EraseLineToStart_Should_Handle_Col_At_Width_Minus_One()
    {
        var buffer = new ScreenBuffer(5, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(0, c, 'Y', style);
        }

        buffer.EraseLineToStart(0, 4);

        for (var c = 0; c < 5; c++)
        {
            buffer[0, c].Character.Should().Be(' ');
        }
    }

    [Fact]
    public void EraseLineToStart_Should_Not_Crash_When_Col_Equals_Width()
    {
        // Kills: L162 `c < Width` → `c <= Width`
        // With `<=`, c == Width passes → _cells[row, Width] → IndexOutOfRangeException.
        var buffer = new ScreenBuffer(5, 3);
        FluentActions.Invoking(() => buffer.EraseLineToStart(0, 5)).Should().NotThrow(); // col == Width
    }

    // ── GetRegionText: endCol boundary at Width-1 ────────────────────

    [Fact]
    public void GetRegionText_EndCol_At_Last_Column_Should_Include_All()
    {
        // Kills: L258 `Width - 1` → `Width + 1` and L259 `c < Width` → `c <= Width`
        // When endRow != startRow on a middle row, cEnd = Width - 1.
        // If mutated to Width + 1, the `c < Width` guard still bounds it.
        // But if `c < Width` also mutates to `c <= Width`, it would access out of bounds.
        var buffer = new ScreenBuffer(5, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(0, c, 'A', style);
            buffer.WriteChar(1, c, 'B', style);
            buffer.WriteChar(2, c, 'C', style);
        }

        // startRow=0, startCol=0, endRow=2, endCol=4
        // Row 1 (middle) should use cEnd = Width - 1 = 4, getting all 5 chars
        var region = buffer.GetRegionText(0, 0, 2, 4);
        region.Should().Be("AAAAA\nBBBBB\nCCCCC");
    }

    [Fact]
    public void GetRegionText_Should_Not_Crash_When_EndCol_Equals_Width()
    {
        // Kills: L259 `c < Width` → `c <= Width`
        // With `<=`, c == Width → _cells[r, Width] → IndexOutOfRangeException.
        // Also kills: L258 `Width - 1` → `Width + 1` combined with L259 mutation.
        var buffer = new ScreenBuffer(3, 3);
        var style = new ScreenCell();
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 3; c++)
            {
                buffer.WriteChar(r, c, (char)('A' + r), style);
            }
        }

        // endCol == Width (3) which exceeds valid range — should still work
        // because `c < Width` prevents OOB access
        var region = buffer.GetRegionText(0, 0, 2, 3);
        region.Should().Be("AAA\nBBB\nCCC");
    }

    // ── Constructor: exact error messages (kills String mutations) ────

    [Fact]
    public void Constructor_Should_Include_Width_In_Error_Message()
    {
        var ex = FluentActions.Invoking(() => new ScreenBuffer(0, 5)).Should().Throw<ArgumentOutOfRangeException>().Which;
        ex.ParamName.Should().Be("width");
        ex.Message.Should().Contain("Width must be positive.");
    }

    [Fact]
    public void Constructor_Should_Include_Height_In_Error_Message()
    {
        var ex = FluentActions.Invoking(() => new ScreenBuffer(5, 0)).Should().Throw<ArgumentOutOfRangeException>().Which;
        ex.ParamName.Should().Be("height");
        ex.Message.Should().Contain("Height must be positive.");
    }

    // ── EraseToEnd: row=0 boundary (kills >= 0 → > 0 mutation) ──────

    [Fact]
    public void EraseToEnd_Should_Erase_Current_Row_When_Row_Is_Zero()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(0, c, 'A', style);
        }

        buffer.EraseToEnd(0, 5);

        // Cols 0-4 intact, cols 5-9 erased
        buffer[0, 4].Character.Should().Be('A');
        buffer[0, 5].Character.Should().Be(' ');
    }

    // ── EraseToStart: col boundary (kills <= col → < col mutation) ───

    [Fact]
    public void EraseToStart_Should_Erase_Col_Equal_To_Cursor_Position()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(1, c, 'B', style);
        }

        // col=5: positions 0..5 inclusive should be erased
        buffer.EraseToStart(1, 5);

        buffer[1, 5].Character.Should().Be(' ');
        buffer[1, 6].Character.Should().Be('B');
    }

    [Fact]
    public void EraseToStart_Should_Erase_When_Row_Is_Zero()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(0, c, 'C', style);
        }

        buffer.EraseToStart(0, 4);

        buffer[0, 0].Character.Should().Be(' ');
        buffer[0, 4].Character.Should().Be(' ');
        buffer[0, 5].Character.Should().Be('C');
    }

    // ── EraseLineToStart: col at Width-1 boundary ────────────────────

    [Fact]
    public void EraseLineToStart_Should_Erase_Entire_Row_When_Col_Is_Last()
    {
        var buffer = new ScreenBuffer(5, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(1, c, 'D', style);
        }

        // col = Width - 1 = 4: should erase all 5 columns
        buffer.EraseLineToStart(1, 4);

        for (var c = 0; c < 5; c++)
        {
            buffer[1, c].Character.Should().Be(' ');
        }
    }

    // ── GetRegionText: exact newline and content assertions ──────────

    [Fact]
    public void GetRegionText_Single_Row_Should_Not_Include_Newline()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(0, c, "Hello"[c], style);
        }

        var region = buffer.GetRegionText(0, 0, 0, 4);
        region.Should().Be("Hello");
        region.Should().NotContain("\n");
    }

    [Fact]
    public void GetRegionText_Multi_Row_Should_Include_Newline_Between_Rows()
    {
        // Use a 3-wide buffer so all chars fill the row — no trailing spaces
        var buffer = new ScreenBuffer(3, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 3; c++)
        {
            buffer.WriteChar(0, c, "ABC"[c], style);
        }
        for (var c = 0; c < 3; c++)
        {
            buffer.WriteChar(1, c, "DEF"[c], style);
        }

        var region = buffer.GetRegionText(0, 0, 1, 2);
        // Newline between rows, not before first row
        region.Should().Be("ABC\nDEF");
    }

    [Fact]
    public void GetRegionText_Should_Use_FullWidth_For_Middle_Rows()
    {
        var buffer = new ScreenBuffer(5, 5);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(0, c, 'A', style);
        }
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(1, c, 'B', style);
        }
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(2, c, 'C', style);
        }

        // startRow=0, startCol=2; endRow=2, endCol=2
        // Row 0: cols 2..4 = "AAA"
        // Row 1 (middle): full width 0..4 = "BBBBB"
        // Row 2: cols 0..2 = "CCC"
        var region = buffer.GetRegionText(0, 2, 2, 2);
        region.Should().Contain("AAA");
        region.Should().Contain("BBBBB");
        region.Should().Contain("CCC");
    }

    [Fact]
    public void GetRegionText_Should_Clamp_At_Buffer_Height()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        buffer.WriteChar(0, 0, 'X', style);
        buffer.WriteChar(1, 0, 'Y', style);

        // endRow beyond buffer height should clamp gracefully
        var region = buffer.GetRegionText(0, 0, 99, 0);
        region.Should().Contain("X");
        region.Should().Contain("Y");
    }

    [Fact]
    public void GetRegionText_Should_Include_EndRow_Column()
    {
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        for (var c = 0; c < 5; c++)
        {
            buffer.WriteChar(0, c, "ABCDE"[c], style);
        }

        // endCol=4: should include col 4 (inclusive)
        var region = buffer.GetRegionText(0, 0, 0, 4);
        region.Should().Be("ABCDE");
    }

    // ── ValidatePosition/ValidateRow: exact error messages ───────────

    [Fact]
    public void Indexer_Row_Error_Should_Include_Row_And_Height()
    {
        var buffer = new ScreenBuffer(10, 5);
        var ex = FluentActions.Invoking(() => _ = buffer[-1, 0]).Should().Throw<ArgumentOutOfRangeException>().Which;
        ex.ParamName.Should().Be("row");
        ex.Message.Should().Contain("Row -1 is out of range [0, 5).");
    }

    [Fact]
    public void Indexer_Col_Error_Should_Include_Col_And_Width()
    {
        var buffer = new ScreenBuffer(10, 5);
        var ex = FluentActions.Invoking(() => _ = buffer[0, 10]).Should().Throw<ArgumentOutOfRangeException>().Which;
        ex.ParamName.Should().Be("col");
        ex.Message.Should().Contain("Column 10 is out of range [0, 10).");
    }

    [Fact]
    public void GetRowText_Row_Error_Should_Include_Row_And_Height()
    {
        var buffer = new ScreenBuffer(10, 5);
        var ex = FluentActions.Invoking(() => buffer.GetRowText(5)).Should().Throw<ArgumentOutOfRangeException>().Which;
        ex.ParamName.Should().Be("row");
        ex.Message.Should().Contain("Row 5 is out of range [0, 5).");
    }

    // ── Erase bounds: row == Height exactly (kills < Height → <= Height mutation) ──

    [Fact]
    public void EraseToEnd_Should_Not_Throw_For_Row_Exactly_At_Height()
    {
        // row = Height is one past the last valid row; should be silently ignored
        var buffer = new ScreenBuffer(10, 3);
        FluentActions.Invoking(() => buffer.EraseToEnd(3, 0)).Should().NotThrow(); // row=Height=3
    }

    [Fact]
    public void EraseToStart_Should_Not_Throw_For_Row_Exactly_At_Height()
    {
        var buffer = new ScreenBuffer(10, 3);
        FluentActions.Invoking(() => buffer.EraseToStart(3, 0)).Should().NotThrow(); // row=Height=3
    }

    [Fact]
    public void EraseToStart_Should_Not_Throw_For_Negative_Row()
    {
        // row=-1 triggers the `||` logical mutation: `row >= 0 || row < Height`
        // would make condition true and attempt _cells[-1, c].Reset()
        var buffer = new ScreenBuffer(10, 3);
        FluentActions.Invoking(() => buffer.EraseToStart(-1, 0)).Should().NotThrow();
    }

    // ── Erase bounds: col == Width exactly (kills c < Width → c <= Width mutation) ──

    [Fact]
    public void EraseToStart_Should_Not_Throw_For_Col_Exceeding_Width()
    {
        // col=Width: with `c <= Width` mutation, loop would access _cells[row, Width]
        var buffer = new ScreenBuffer(5, 3);
        FluentActions.Invoking(() => buffer.EraseToStart(1, 5)).Should().NotThrow(); // col=Width=5
    }

    [Fact]
    public void EraseLineToStart_Should_Not_Throw_For_Col_Exceeding_Width()
    {
        // col=Width: with `c <= Width` mutation, loop would access _cells[row, Width]
        var buffer = new ScreenBuffer(5, 3);
        FluentActions.Invoking(() => buffer.EraseLineToStart(1, 5)).Should().NotThrow(); // col=Width=5
    }

    // ── GetRegionText: endCol stops at endCol, not Width (kills c <= cEnd → c <= Width) ──

    [Fact]
    public void GetRegionText_Should_Stop_At_EndCol_Not_Width()
    {
        // Write "ABCDEFGHIJ" to a 10-wide row
        // Request region stopping at col 3 — should return "ABCD" not "ABCDEFGHIJ"
        var buffer = new ScreenBuffer(10, 3);
        var style = new ScreenCell();
        var text = "ABCDEFGHIJ";
        for (var c = 0; c < 10; c++)
        {
            buffer.WriteChar(0, c, text[c], style);
        }

        var region = buffer.GetRegionText(0, 0, 0, 3);
        region.Should().Be("ABCD");
    }
}
