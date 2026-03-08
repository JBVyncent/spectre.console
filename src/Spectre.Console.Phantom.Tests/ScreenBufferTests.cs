using Shouldly;
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
        buffer.Width.ShouldBe(40);
        buffer.Height.ShouldBe(10);
    }

    [Fact]
    public void Constructor_Should_Initialize_All_Cells_To_Space()
    {
        var buffer = new ScreenBuffer(5, 3);
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 5; c++)
            {
                buffer[r, c].Character.ShouldBe(' ');
            }
        }
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(-100, 5)]
    public void Constructor_Should_Throw_For_Invalid_Width(int width, int height)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ScreenBuffer(width, height));
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(5, -1)]
    [InlineData(5, -100)]
    public void Constructor_Should_Throw_For_Invalid_Height(int width, int height)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ScreenBuffer(width, height));
    }

    // ── Indexer ──────────────────────────────────────────────────────

    [Fact]
    public void Indexer_Should_Return_Cell_At_Position()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell { Foreground = CellColor.FromLegacy(31) };
        buffer.WriteChar(2, 3, 'X', style);

        buffer[2, 3].Character.ShouldBe('X');
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(5, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 10)]
    public void Indexer_Should_Throw_For_Out_Of_Bounds(int row, int col)
    {
        var buffer = new ScreenBuffer(10, 5);
        Should.Throw<ArgumentOutOfRangeException>(() => _ = buffer[row, col]);
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
        cell.Character.ShouldBe('Z');
        cell.Foreground!.Value.Index.ShouldBe(31);
        cell.Background!.Value.Index.ShouldBe(42);
        cell.Decoration.HasFlag(CellDecoration.Bold).ShouldBeTrue();
        cell.Decoration.HasFlag(CellDecoration.Italic).ShouldBeTrue();
        cell.HyperlinkUrl.ShouldBe("https://example.com");
    }

    [Fact]
    public void WriteChar_Should_Silently_Ignore_Out_Of_Bounds()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell();

        // None of these should throw
        Should.NotThrow(() => buffer.WriteChar(-1, 0, 'X', style));
        Should.NotThrow(() => buffer.WriteChar(5, 0, 'X', style));
        Should.NotThrow(() => buffer.WriteChar(0, -1, 'X', style));
        Should.NotThrow(() => buffer.WriteChar(0, 10, 'X', style));
    }

    [Fact]
    public void WriteChar_Should_Throw_For_Null_Style()
    {
        var buffer = new ScreenBuffer(10, 5);
        Should.Throw<ArgumentNullException>(() => buffer.WriteChar(0, 0, 'X', null!));
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
        buffer[0, 0].Character.ShouldBe('A');
        buffer[0, 9].Character.ShouldBe('A');

        // Row 1: cols 0-4 intact, cols 5-9 erased
        buffer[1, 4].Character.ShouldBe('A');
        buffer[1, 5].Character.ShouldBe(' ');
        buffer[1, 9].Character.ShouldBe(' ');

        // Row 2: all erased
        buffer[2, 0].Character.ShouldBe(' ');
        buffer[2, 9].Character.ShouldBe(' ');
    }

    [Fact]
    public void EraseToEnd_Should_Handle_Row_Out_Of_Bounds_Gracefully()
    {
        var buffer = new ScreenBuffer(5, 3);
        // Should not throw even with out-of-bounds row
        Should.NotThrow(() => buffer.EraseToEnd(-1, 0));
        Should.NotThrow(() => buffer.EraseToEnd(5, 0));
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
        buffer[0, 0].Character.ShouldBe(' ');
        buffer[0, 9].Character.ShouldBe(' ');

        // Row 1: cols 0-3 erased, cols 4-9 intact
        buffer[1, 3].Character.ShouldBe(' ');
        buffer[1, 4].Character.ShouldBe('B');

        // Row 2: all intact
        buffer[2, 0].Character.ShouldBe('B');
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
            buffer.GetRowText(r).ShouldBeEmpty();
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

        buffer[0, 4].Character.ShouldBe('C');
        buffer[0, 5].Character.ShouldBe(' ');
        buffer[0, 9].Character.ShouldBe(' ');
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void EraseLineToEnd_Should_Return_For_Out_Of_Bounds_Row(int row)
    {
        var buffer = new ScreenBuffer(10, 5);
        Should.NotThrow(() => buffer.EraseLineToEnd(row, 0));
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

        buffer[0, 4].Character.ShouldBe(' ');
        buffer[0, 5].Character.ShouldBe('D');
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void EraseLineToStart_Should_Return_For_Out_Of_Bounds_Row(int row)
    {
        var buffer = new ScreenBuffer(10, 5);
        Should.NotThrow(() => buffer.EraseLineToStart(row, 0));
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

        buffer.GetRowText(1).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void EraseLine_Should_Return_For_Out_Of_Bounds_Row(int row)
    {
        var buffer = new ScreenBuffer(10, 5);
        Should.NotThrow(() => buffer.EraseLine(row));
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

        buffer.GetRowText(0).ShouldBe("B");
        buffer.GetRowText(1).ShouldBe("C");
        buffer.GetRowText(2).ShouldBeEmpty();
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
        cell.Character.ShouldBe('X');
        cell.Foreground!.Value.Index.ShouldBe(31);
        cell.Decoration.HasFlag(CellDecoration.Bold).ShouldBeTrue();
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

        buffer.GetRowText(0).ShouldBe("Hello");
    }

    [Fact]
    public void GetRowText_Should_Throw_For_Invalid_Row()
    {
        var buffer = new ScreenBuffer(10, 5);
        Should.Throw<ArgumentOutOfRangeException>(() => buffer.GetRowText(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => buffer.GetRowText(5));
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

        buffer.GetText().ShouldBe("Hi!\nWorld");
    }

    [Fact]
    public void GetText_Should_Trim_Trailing_Empty_Lines()
    {
        var buffer = new ScreenBuffer(10, 5);
        var style = new ScreenCell();
        buffer.WriteChar(0, 0, 'A', style);
        // Rows 1-4 are empty

        buffer.GetText().ShouldBe("A");
    }

    [Fact]
    public void GetText_On_Empty_Buffer_Should_Return_Empty_String()
    {
        var buffer = new ScreenBuffer(10, 5);
        buffer.GetText().ShouldBeEmpty();
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
        region.ShouldContain("2345");
        region.ShouldContain("ABCDEF");
        region.ShouldContain("abcdef");
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
        region.ShouldBe("CDEF");
    }

    // ── HasCharAt ────────────────────────────────────────────────────

    [Fact]
    public void HasCharAt_Should_Return_True_For_Matching_Character()
    {
        var buffer = new ScreenBuffer(10, 5);
        buffer.WriteChar(0, 0, 'X', new ScreenCell());

        buffer.HasCharAt(0, 0, 'X').ShouldBeTrue();
        buffer.HasCharAt(0, 0, 'Y').ShouldBeFalse();
    }

    [Fact]
    public void HasCharAt_Should_Return_False_For_Out_Of_Bounds()
    {
        var buffer = new ScreenBuffer(10, 5);

        buffer.HasCharAt(-1, 0, ' ').ShouldBeFalse();
        buffer.HasCharAt(5, 0, ' ').ShouldBeFalse();
        buffer.HasCharAt(0, -1, ' ').ShouldBeFalse();
        buffer.HasCharAt(0, 10, ' ').ShouldBeFalse();
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
        pos.ShouldNotBeNull();
        pos!.Value.Row.ShouldBe(1);
        pos!.Value.Col.ShouldBe(3);
    }

    [Fact]
    public void FindText_Should_Return_Null_When_Not_Found()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.FindText("Missing").ShouldBeNull();
    }

    [Fact]
    public void FindText_Should_Throw_For_Null_Input()
    {
        var buffer = new ScreenBuffer(10, 3);
        Should.Throw<ArgumentNullException>(() => buffer.FindText(null!));
    }

    [Fact]
    public void ContainsText_Should_Return_True_When_Found()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.WriteChar(0, 0, 'A', new ScreenCell());
        buffer.ContainsText("A").ShouldBeTrue();
    }

    [Fact]
    public void ContainsText_Should_Return_False_When_Not_Found()
    {
        var buffer = new ScreenBuffer(10, 3);
        buffer.ContainsText("Z").ShouldBeFalse();
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
}
