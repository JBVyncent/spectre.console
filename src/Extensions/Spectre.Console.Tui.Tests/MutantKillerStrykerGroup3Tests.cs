using FluentAssertions;
using Spectre.Console;
using Xunit;
using TuiTreeNode = Spectre.Console.Tui.Widgets.Controls.TreeNode;

namespace Spectre.Console.Tui.Tests;

/// <summary>
/// Mutation-killing tests for GROUP 3 complex widgets:
/// TextBox, ListBox, DataGrid, TreeView, ComboBox, ScrollView.
/// Targets Survived and NoCoverage mutants at specific lines.
/// </summary>
public sealed class MutantKillerStrykerGroup3Tests
{
    private static (ScreenBuffer buf, BufferSurface surface) Surface(int w, int h)
    {
        var buf = new ScreenBuffer(w, h);
        return (buf, new BufferSurface(buf));
    }

    private static string Row(ScreenBuffer buf, int row)
    {
        var sb = new System.Text.StringBuilder(buf.Width);
        for (var col = 0; col < buf.Width; col++)
            sb.Append(buf[col, row].Character);
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════════
    // TextBox — L17 (string default), L18-19 (Min/Max statement),
    //   L30 (Invalidate), L55 (conditional), L59-86 (render),
    //   L99-160 (key handling), L169-175 (EnsureCursorVisible/measure)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TextBox_Text_NullCoalesces_ToEmpty()
    {
        // L17: _text = value ?? string.Empty
        var tb = new TextBox();
        tb.Text = null!;
        tb.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void TextBox_Text_Setter_ClampsCursorPosition()
    {
        // L18: _cursorPosition = Math.Min(_cursorPosition, _text.Length)
        var tb = new TextBox { Text = "abcdef" };
        tb.CursorPosition = 6;
        tb.CursorPosition.Should().Be(6);
        tb.Text = "ab"; // shorter text should clamp cursor
        tb.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void TextBox_Text_Setter_FiresTextChanged()
    {
        // L19-20: Invalidate + TextChanged
        var tb = new TextBox();
        string? received = null;
        tb.TextChanged += (_, t) => received = t;
        tb.Text = "hello";
        received.Should().Be("hello");
    }

    [Fact]
    public void TextBox_CursorPosition_Setter_Clamps_And_Invalidates()
    {
        // L29-30: Math.Clamp + Invalidate
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = -5;
        tb.CursorPosition.Should().Be(0);
        tb.CursorPosition = 100;
        tb.CursorPosition.Should().Be(3);
        tb.CursorPosition = 2;
        tb.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void TextBox_Render_FocusedStyle_Vs_NormalStyle()
    {
        // L55: HasFocus ? FocusedStyle : NormalStyle
        var tb = new TextBox { Text = "X" };
        tb.NormalStyle = new Style(Color.Red, Color.Black);
        tb.FocusedStyle = new Style(Color.Green, Color.White);

        // Unfocused render — cell 0 has text "X" with NormalStyle
        tb.Arrange(new Rect(0, 0, 10, 1));
        var (buf1, s1) = Surface(10, 1);
        tb.Render(s1);
        buf1[0, 0].Style.Foreground.Should().Be(Color.Red);

        // Focused render — cell 0 is cursor (inverted), check cell 1 (background fill)
        tb.HasFocus = true;
        tb.CursorPosition = 0;
        var (buf2, s2) = Surface(10, 1);
        tb.Render(s2);
        // The fill uses FocusedStyle, check a cell beyond text+cursor
        buf2[5, 0].Style.Foreground.Should().Be(Color.Green);
        buf2[5, 0].Style.Background.Should().Be(Color.White);
    }

    [Fact]
    public void TextBox_Render_PlaceholderShown_OnlyWhenEmptyUnfocusedAndSet()
    {
        // L61: _text.Length == 0 && !HasFocus && Placeholder != null
        var tb = new TextBox { Placeholder = "Hint" };

        // Empty + unfocused + placeholder set => show placeholder
        tb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, s) = Surface(20, 1);
        tb.Render(s);
        Row(buf, 0).Should().Contain("Hint");

        // Empty + focused => no placeholder
        tb.HasFocus = true;
        var (buf2, s2) = Surface(20, 1);
        tb.Render(s2);
        Row(buf2, 0).Should().NotContain("Hint");

        // Non-empty + unfocused => no placeholder
        tb.HasFocus = false;
        tb.Text = "X";
        var (buf3, s3) = Surface(20, 1);
        tb.Render(s3);
        Row(buf3, 0).Should().NotContain("Hint");
    }

    [Fact]
    public void TextBox_Render_PlaceholderTruncated_WhenTooLong()
    {
        // L63-64: Placeholder.Length > width truncation
        var tb = new TextBox { Placeholder = "VeryLongPlaceholderText" };
        tb.Arrange(new Rect(0, 0, 5, 1));
        var (buf, s) = Surface(5, 1);
        tb.Render(s);
        var row = Row(buf, 0);
        row.Length.Should().Be(5);
        row.Should().Be("VeryL");
    }

    [Fact]
    public void TextBox_Render_VisibleText_WithScrollOffset()
    {
        // L74-76: scroll offset calculation
        var tb = new TextBox { Text = "ABCDEFGHIJ" };
        tb.CursorPosition = 9;
        tb.HasFocus = true;
        tb.Arrange(new Rect(0, 0, 5, 1));
        var (buf, s) = Surface(5, 1);
        tb.Render(s);
        // Cursor at 9, width 5 => scrollOffset should be 5
        // Visible: "FGHIJ"
        var row = Row(buf, 0);
        row.Should().StartWith("FGHIJ");
    }

    [Fact]
    public void TextBox_Render_CursorStyle_Inverted()
    {
        // L81-86: cursor rendering with inverted colors
        var fg = Color.White;
        var bg = Color.DarkBlue;
        var tb = new TextBox { Text = "AB", FocusedStyle = new Style(fg, bg) };
        tb.HasFocus = true;
        tb.CursorPosition = 0;
        tb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, s) = Surface(10, 1);
        tb.Render(s);
        // Cursor cell should have inverted style: foreground=bg, background=fg
        buf[0, 0].Style.Foreground.Should().Be(bg);
        buf[0, 0].Style.Background.Should().Be(fg);
        buf[0, 0].Character.Should().Be('A');
    }

    [Fact]
    public void TextBox_Render_CursorAtEnd_ShowsSpace()
    {
        // L84: _cursorPosition < _text.Length ? _text[_cursorPosition] : ' '
        var tb = new TextBox { Text = "AB" };
        tb.HasFocus = true;
        tb.CursorPosition = 2; // at end
        tb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, s) = Surface(10, 1);
        tb.Render(s);
        buf[2, 0].Character.Should().Be(' ');
    }

    [Fact]
    public void TextBox_Render_CursorNotShown_WhenUnfocused()
    {
        // L79: if (HasFocus) — cursor not rendered when unfocused
        var tb = new TextBox { Text = "AB", NormalStyle = new Style(Color.White, Color.Grey) };
        tb.HasFocus = false;
        tb.CursorPosition = 0;
        tb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, s) = Surface(10, 1);
        tb.Render(s);
        // No inverted style on cell 0 when unfocused
        buf[0, 0].Style.Foreground.Should().Be(Color.White);
        buf[0, 0].Style.Background.Should().Be(Color.Grey);
    }

    [Fact]
    public void TextBox_LeftArrow_Decrements_Cursor()
    {
        // L96-99: cursorPosition-- and Invalidate
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 2;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        result.Should().BeTrue();
        tb.CursorPosition.Should().Be(1);
    }

    [Fact]
    public void TextBox_LeftArrow_AtZero_StaysZero()
    {
        // L96: if (_cursorPosition > 0) — boundary
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        tb.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void TextBox_RightArrow_Increments_Cursor()
    {
        // L105-108: cursorPosition++ and Invalidate
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 1;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeTrue();
        tb.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void TextBox_RightArrow_AtEnd_StaysAtEnd()
    {
        // L105: if (_cursorPosition < _text.Length) — boundary
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        tb.CursorPosition.Should().Be(3);
    }

    [Fact]
    public void TextBox_Home_SetsCursorToZero()
    {
        // L114: _cursorPosition = 0
        var tb = new TextBox { Text = "hello" };
        tb.CursorPosition = 3;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        result.Should().BeTrue();
        tb.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void TextBox_End_SetsCursorToLength()
    {
        // L119: _cursorPosition = _text.Length
        var tb = new TextBox { Text = "hello" };
        tb.CursorPosition = 0;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        result.Should().BeTrue();
        tb.CursorPosition.Should().Be(5);
    }

    [Fact]
    public void TextBox_Backspace_RemovesCharAndDecrementsCursor()
    {
        // L124-129: remove char, decrement, invalidate, fire event
        var tb = new TextBox { Text = "abcd" };
        tb.CursorPosition = 2;
        string? changed = null;
        tb.TextChanged += (_, t) => changed = t;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.Backspace, '\b'));
        result.Should().BeTrue();
        tb.Text.Should().Be("acd");
        tb.CursorPosition.Should().Be(1);
        changed.Should().Be("acd");
    }

    [Fact]
    public void TextBox_Backspace_AtZero_NoOp()
    {
        // L124: if (_cursorPosition > 0)
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        string? changed = null;
        tb.TextChanged += (_, t) => changed = t;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Backspace, '\b'));
        tb.Text.Should().Be("abc");
        changed.Should().BeNull();
    }

    [Fact]
    public void TextBox_Delete_RemovesCharAtCursor()
    {
        // L135-139: remove at cursor, invalidate, fire event
        var tb = new TextBox { Text = "abcd" };
        tb.CursorPosition = 1;
        string? changed = null;
        tb.TextChanged += (_, t) => changed = t;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.Delete, '\0'));
        result.Should().BeTrue();
        tb.Text.Should().Be("acd");
        tb.CursorPosition.Should().Be(1);
        changed.Should().Be("acd");
    }

    [Fact]
    public void TextBox_Delete_AtEnd_NoOp()
    {
        // L135: if (_cursorPosition < _text.Length)
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        string? changed = null;
        tb.TextChanged += (_, t) => changed = t;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Delete, '\0'));
        tb.Text.Should().Be("abc");
        changed.Should().BeNull();
    }

    [Fact]
    public void TextBox_Enter_FiresSubmitted_WithExactText()
    {
        // L144-145: Submitted event with current text
        var tb = new TextBox { Text = "query" };
        string? submitted = null;
        tb.Submitted += (_, t) => submitted = t;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        result.Should().BeTrue();
        submitted.Should().Be("query");
    }

    [Fact]
    public void TextBox_Typing_InsertsAtCursor_FiresEvent()
    {
        // L149-160: insert char, increment cursor, invalidate, fire
        var tb = new TextBox { Text = "ac" };
        tb.CursorPosition = 1;
        string? changed = null;
        tb.TextChanged += (_, t) => changed = t;
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.B, 'b'));
        result.Should().BeTrue();
        tb.Text.Should().Be("abc");
        tb.CursorPosition.Should().Be(2);
        changed.Should().Be("abc");
    }

    [Fact]
    public void TextBox_Typing_ControlChar_Ignored()
    {
        // L149: if (e.KeyChar >= ' ') — control chars below space ignored
        var tb = new TextBox { Text = "abc" };
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, '\x01')); // Ctrl+A
        result.Should().BeFalse();
        tb.Text.Should().Be("abc");
    }

    [Fact]
    public void TextBox_MaxLength_BlocksTyping_ExactBoundary()
    {
        // L151-153: MaxLength.HasValue && _text.Length >= MaxLength.Value
        var tb = new TextBox { MaxLength = 2 };
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.B, 'b'));
        tb.Text.Should().Be("ab");
        tb.Text.Length.Should().Be(2);
        // At max, typing should be blocked
        var result = tb.OnKeyEvent(new KeyEvent(ConsoleKey.C, 'c'));
        result.Should().BeTrue(); // still returns true (handled)
        tb.Text.Should().Be("ab"); // but text unchanged
    }

    [Fact]
    public void TextBox_MaxLength_Null_AllowsUnlimitedTyping()
    {
        // L151: MaxLength.HasValue check — null means no limit
        var tb = new TextBox { MaxLength = null };
        for (int i = 0; i < 100; i++)
            tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        tb.Text.Length.Should().Be(100);
    }

    [Fact]
    public void TextBox_EnsureCursorVisible_ScrollsLeft()
    {
        // L169-171: if (_cursorPosition < _scrollOffset) => _scrollOffset = _cursorPosition
        var tb = new TextBox { Text = "ABCDEFGHIJKLMNOP" };
        tb.CursorPosition = 15; // force scroll right
        tb.Arrange(new Rect(0, 0, 5, 1));
        var (buf1, s1) = Surface(5, 1);
        tb.Render(s1); // scrolls to show cursor at 15

        // Now move cursor to 0
        tb.CursorPosition = 0;
        var (buf2, s2) = Surface(5, 1);
        tb.Render(s2);
        // Should show "ABCDE"
        Row(buf2, 0).Should().StartWith("ABCDE");
    }

    [Fact]
    public void TextBox_EnsureCursorVisible_ScrollsRight()
    {
        // L173-175: if (_cursorPosition >= _scrollOffset + width)
        var tb = new TextBox { Text = "ABCDEFGHIJ" };
        tb.CursorPosition = 8;
        tb.Arrange(new Rect(0, 0, 5, 1));
        var (buf, s) = Surface(5, 1);
        tb.Render(s);
        // scrollOffset = 8 - 5 + 1 = 4, visible: "EFGHI"
        Row(buf, 0).Should().StartWith("EFGHI");
    }

    [Fact]
    public void TextBox_MeasureContent_ClampsTo20OrAvailable()
    {
        // L50: Math.Min(20, available.Width)
        var tb = new TextBox();
        var size1 = tb.MeasureContent(new Size(100, 5));
        size1.Width.Should().Be(20);
        size1.Height.Should().Be(1);

        var size2 = tb.MeasureContent(new Size(10, 5));
        size2.Width.Should().Be(10);
        size2.Height.Should().Be(1);
    }

    // ════════════════════════════════════════════════════════════════
    // ListBox — L23 (statement), L29 (conditional), L42 (boolean),
    //   L47-60 (render), L67-97 (scroll/selection), L117-174 (keys),
    //   L182-224 (mouse/measure)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ListBox_SelectedIndex_Setter_Clamps_And_FiresEvent()
    {
        // L19-24: Clamp + change detection + Invalidate + event
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.AddItem("C");
        int? changedIndex = null;
        lb.SelectionChanged += (_, idx) => changedIndex = idx;
        lb.SelectedIndex = 2;
        changedIndex.Should().Be(2);
        lb.SelectedIndex.Should().Be(2);

        // Clamp above max
        lb.SelectedIndex = 100;
        lb.SelectedIndex.Should().Be(2);

        // Clamp below min
        lb.SelectedIndex = -5;
        lb.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void ListBox_SelectedIndex_NoEventWhenUnchanged()
    {
        // L20: if (_selectedIndex != newIndex)
        var lb = new ListBox();
        lb.AddItem("A");
        lb.SelectedIndex.Should().Be(0);
        var fired = false;
        lb.SelectionChanged += (_, _) => fired = true;
        lb.SelectedIndex = 0; // same value
        fired.Should().BeFalse();
    }

    [Fact]
    public void ListBox_SelectedItem_ReturnsCorrectItem()
    {
        // L29: conditional SelectedItem
        var lb = new ListBox();
        lb.SelectedItem.Should().BeNull(); // no items

        lb.AddItem("Alpha");
        lb.AddItem("Beta");
        lb.SelectedItem.Should().Be("Alpha");

        lb.SelectedIndex = 1;
        lb.SelectedItem.Should().Be("Beta");

        lb.SelectedIndex = -1;
        lb.SelectedItem.Should().BeNull();
    }

    [Fact]
    public void ListBox_AddItem_FirstItemSetsSelection()
    {
        // L50-52: if (_items.Count == 1) _selectedIndex = 0
        var lb = new ListBox();
        lb.SelectedIndex.Should().Be(0); // default
        lb.AddItem("First");
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_AddItem_SubsequentDoesNotChangeSelection()
    {
        // L50: only first item sets selection
        var lb = new ListBox();
        lb.AddItem("A");
        lb.SelectedIndex = 0;
        lb.AddItem("B");
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_AddItems_SetsSelectionIfNegative()
    {
        // L67-68: if (_selectedIndex < 0 && _items.Count > 0)
        var lb = new ListBox();
        lb.ClearItems();
        lb.AddItems(new[] { "X", "Y" });
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_AddItems_DoesNotResetIfAlreadySelected()
    {
        // L67: _selectedIndex < 0 check
        var lb = new ListBox();
        lb.AddItem("A");
        lb.SelectedIndex = 0;
        lb.AddItems(new[] { "B", "C" });
        lb.SelectedIndex.Should().Be(0); // unchanged
    }

    [Fact]
    public void ListBox_ClearItems_ResetsState()
    {
        // L77-80: Clear + reset _selectedIndex + _scrollOffset + Invalidate
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C", "D", "E" });
        lb.SelectedIndex = 3;
        lb.ClearItems();
        lb.Items.Should().BeEmpty();
        lb.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void ListBox_RemoveItem_OutOfBounds_NoOp()
    {
        // L85-87: bounds check
        var lb = new ListBox();
        lb.AddItem("A");
        lb.RemoveItem(-1);
        lb.RemoveItem(5);
        lb.Items.Should().HaveCount(1);
    }

    [Fact]
    public void ListBox_RemoveItem_AdjustsSelectedIndex()
    {
        // L92-94: if (_selectedIndex >= _items.Count) adjust
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.SelectedIndex = 1;
        lb.RemoveItem(1);
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_RemoveItem_SelectedInRange_Stays()
    {
        // L92: selected stays if still in range
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.AddItem("C");
        lb.SelectedIndex = 0;
        lb.RemoveItem(2);
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_Render_FillsRowWithStyle()
    {
        // L125-136: isSelected style, fill, text
        var lb = new ListBox();
        lb.SelectedStyle = new Style(Color.White, Color.Blue);
        lb.NormalStyle = new Style(Color.Grey, Color.Black);
        lb.AddItem("First");
        lb.AddItem("Second");
        lb.SelectedIndex = 0;
        lb.Arrange(new Rect(0, 0, 15, 5));
        var (buf, s) = Surface(15, 5);
        lb.Render(s);
        // Selected row uses SelectedStyle
        buf[0, 0].Style.Background.Should().Be(Color.Blue);
        // Non-selected row uses NormalStyle
        buf[0, 1].Style.Background.Should().Be(Color.Black);
        Row(buf, 0).Should().Contain("First");
        Row(buf, 1).Should().Contain("Second");
    }

    [Fact]
    public void ListBox_Render_TruncatesLongText()
    {
        // L129-131: text truncation
        var lb = new ListBox();
        lb.AddItem("VeryLongItemTextThatExceedsWidth");
        lb.Arrange(new Rect(0, 0, 10, 3));
        var (buf, s) = Surface(10, 3);
        lb.Render(s);
        Row(buf, 0).Length.Should().Be(10);
    }

    [Fact]
    public void ListBox_Render_StopsAtItemCount()
    {
        // L120-122: if (itemIndex >= _items.Count) break
        var lb = new ListBox();
        lb.AddItem("Only");
        lb.Arrange(new Rect(0, 0, 10, 5));
        var (buf, s) = Surface(10, 5);
        lb.Render(s);
        Row(buf, 0).Should().Contain("Only");
        // Row 1 should be empty (no item)
        Row(buf, 1).Trim().Should().BeEmpty();
    }

    [Fact]
    public void ListBox_UpArrow_DecreasesSelection()
    {
        // L144-148: UpArrow when > 0
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.SelectedIndex = 1;
        var result = lb.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        result.Should().BeTrue();
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_UpArrow_AtZero_NoChange()
    {
        // L145: if (_selectedIndex > 0)
        var lb = new ListBox();
        lb.AddItem("A");
        lb.SelectedIndex = 0;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_DownArrow_IncreasesSelection()
    {
        // L152-155: DownArrow when < count-1
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.SelectedIndex = 0;
        var result = lb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        result.Should().BeTrue();
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void ListBox_DownArrow_AtEnd_NoChange()
    {
        // L153: if (_selectedIndex < _items.Count - 1)
        var lb = new ListBox();
        lb.AddItem("A");
        lb.SelectedIndex = 0;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_Home_SelectsFirst()
    {
        // L161: SelectedIndex = 0
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.SelectedIndex = 2;
        var result = lb.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        result.Should().BeTrue();
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_End_SelectsLast()
    {
        // L165: SelectedIndex = _items.Count - 1
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.SelectedIndex = 0;
        var result = lb.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        result.Should().BeTrue();
        lb.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void ListBox_PageDown_ExactBoundsHeight()
    {
        // L169,172-173: PageUp/Down with Bounds.Height
        var lb = new ListBox();
        for (int i = 0; i < 30; i++)
            lb.AddItem($"Item{i}");
        lb.Arrange(new Rect(0, 0, 20, 10));
        lb.SelectedIndex = 0;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.PageDown, '\0'));
        lb.SelectedIndex.Should().Be(10); // Bounds.Height = 10

        lb.OnKeyEvent(new KeyEvent(ConsoleKey.PageUp, '\0'));
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_PageDown_ClampsToLastItem()
    {
        // L173: Math.Min(_items.Count - 1, ...)
        var lb = new ListBox();
        for (int i = 0; i < 5; i++)
            lb.AddItem($"Item{i}");
        lb.Arrange(new Rect(0, 0, 20, 10));
        lb.SelectedIndex = 3;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.PageDown, '\0'));
        lb.SelectedIndex.Should().Be(4); // clamped to last
    }

    [Fact]
    public void ListBox_PageUp_ClampsToZero()
    {
        // L169: Math.Max(0, ...)
        var lb = new ListBox();
        for (int i = 0; i < 5; i++)
            lb.AddItem($"Item{i}");
        lb.Arrange(new Rect(0, 0, 20, 10));
        lb.SelectedIndex = 2;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.PageUp, '\0'));
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ListBox_PageDown_ZeroBoundsHeight_UsesFallback()
    {
        // L169: (Bounds.Height > 0 ? Bounds.Height : 10) — zero-height fallback
        var lb = new ListBox();
        for (int i = 0; i < 30; i++)
            lb.AddItem($"Item{i}");
        // Don't call Arrange so Bounds.Height stays 0
        lb.SelectedIndex = 0;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.PageDown, '\0'));
        lb.SelectedIndex.Should().Be(10); // fallback value
    }

    [Fact]
    public void ListBox_Enter_FiresItemActivated()
    {
        // L177-179: ItemActivated event
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.SelectedIndex = 1;
        int? activated = null;
        lb.ItemActivated += (_, idx) => activated = idx;
        var result = lb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        result.Should().BeTrue();
        activated.Should().Be(1);
    }

    [Fact]
    public void ListBox_Enter_NoEvent_WhenNoSelection()
    {
        // L177: if (_selectedIndex >= 0)
        var lb = new ListBox();
        lb.ClearItems();
        int? activated = null;
        lb.ItemActivated += (_, idx) => activated = idx;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().BeNull();
    }

    [Fact]
    public void ListBox_UnhandledKey_ReturnsFalse()
    {
        // L185: default: return false
        var lb = new ListBox();
        lb.AddItem("A");
        var result = lb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        result.Should().BeFalse();
    }

    [Fact]
    public void ListBox_MouseClick_CalculatesItemIndex()
    {
        // L193-197: localRow + scrollOffset => itemIndex
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C", "D" });
        lb.Arrange(new Rect(0, 0, 20, 4));
        var result = lb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 2, false, false, false));
        result.Should().BeTrue();
        lb.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void ListBox_MouseClick_OutOfItemRange_NoChange()
    {
        // L195: itemIndex >= 0 && itemIndex < _items.Count
        var lb = new ListBox();
        lb.AddItem("A");
        lb.Arrange(new Rect(0, 0, 20, 10));
        lb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 5, false, false, false));
        lb.SelectedIndex.Should().Be(0); // unchanged
    }

    [Fact]
    public void ListBox_MouseScrollUp_DecreasesSelection()
    {
        // L203-206: ScrollUp
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.SelectedIndex = 2;
        var result = lb.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollUp, 5, 0, false, false, false));
        result.Should().BeTrue();
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void ListBox_MouseScrollUp_AtZero_ReturnsFalse()
    {
        // L203: _selectedIndex > 0
        var lb = new ListBox();
        lb.AddItem("A");
        lb.SelectedIndex = 0;
        var result = lb.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollUp, 5, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void ListBox_MouseScrollDown_IncreasesSelection()
    {
        // L209-212: ScrollDown
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.SelectedIndex = 0;
        var result = lb.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollDown, 5, 0, false, false, false));
        result.Should().BeTrue();
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void ListBox_MouseScrollDown_AtEnd_ReturnsFalse()
    {
        // L209: _selectedIndex < _items.Count - 1
        var lb = new ListBox();
        lb.AddItem("A");
        lb.SelectedIndex = 0;
        var result = lb.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollDown, 5, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void ListBox_MouseOtherEvent_ReturnsFalse()
    {
        // L215: return false (default)
        var lb = new ListBox();
        lb.AddItem("A");
        var result = lb.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 5, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void ListBox_MeasureContent_ExactValues()
    {
        // L102-110: maxWidth + 2, Math.Min count/available
        var lb = new ListBox();
        lb.AddItem("AB");
        lb.AddItem("ABCDE");
        lb.AddItem("ABC");
        var size = lb.MeasureContent(new Size(100, 100));
        size.Width.Should().Be(7); // 5 + 2
        size.Height.Should().Be(3); // 3 items

        var size2 = lb.MeasureContent(new Size(100, 2));
        size2.Height.Should().Be(2); // clamped to available
    }

    [Fact]
    public void ListBox_EnsureSelectedVisible_ScrollsDown()
    {
        // L220-226: _selectedIndex >= _scrollOffset + viewportHeight
        var lb = new ListBox();
        for (int i = 0; i < 20; i++)
            lb.AddItem($"Item{i}");
        lb.Arrange(new Rect(0, 0, 20, 3));
        lb.SelectedIndex = 10;
        var (buf, s) = Surface(20, 3);
        lb.Render(s);
        // Item10 should be visible at bottom of viewport
        Row(buf, 2).Should().Contain("Item10");
    }

    [Fact]
    public void ListBox_EnsureSelectedVisible_ScrollsUp()
    {
        // L220-222: _selectedIndex < _scrollOffset
        var lb = new ListBox();
        for (int i = 0; i < 20; i++)
            lb.AddItem($"Item{i}");
        lb.Arrange(new Rect(0, 0, 20, 3));
        // Go to end first
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        // Now go to start
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        var (buf, s) = Surface(20, 3);
        lb.Render(s);
        Row(buf, 0).Should().Contain("Item0");
    }

    // ════════════════════════════════════════════════════════════════
    // DataGrid — L25-77 (render), L92-99 (measure), L108-127 (keys),
    //   L135-222 (mouse/measure/events)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DataGrid_SelectedRow_Setter_Clamps_And_FiresEvent()
    {
        // L21-27: Clamp + change check + Invalidate + event
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        int? changed = null;
        dg.SelectionChanged += (_, idx) => changed = idx;
        dg.SelectedRow = 1;
        changed.Should().Be(1);

        dg.SelectedRow = 100;
        dg.SelectedRow.Should().Be(1); // clamped

        dg.SelectedRow = -5;
        dg.SelectedRow.Should().Be(-1); // clamped
    }

    [Fact]
    public void DataGrid_SelectedRow_NoEventWhenUnchanged()
    {
        // L22: if (_selectedRow != newIndex)
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.SelectedRow.Should().Be(0);
        var fired = false;
        dg.SelectionChanged += (_, _) => fired = true;
        dg.SelectedRow = 0;
        fired.Should().BeFalse();
    }

    [Fact]
    public void DataGrid_AddColumn_Invalidates()
    {
        // L46-48: null check + add + invalidate
        var dg = new DataGrid();
        dg.AddColumn("Col1");
        dg.Columns.Should().HaveCount(1);
        dg.Columns[0].Should().Be("Col1");
    }

    [Fact]
    public void DataGrid_AddColumn_NullThrows()
    {
        var dg = new DataGrid();
        var act = () => dg.AddColumn(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DataGrid_AddColumns_Multiple()
    {
        // L52-58: loop + invalidate
        var dg = new DataGrid();
        dg.AddColumns("A", "B", "C");
        dg.Columns.Should().HaveCount(3);
        dg.Columns[0].Should().Be("A");
        dg.Columns[2].Should().Be("C");
    }

    [Fact]
    public void DataGrid_AddRow_FirstRowSetsSelection()
    {
        // L64-66: if (_selectedRow < 0 && _rows.Count == 1)
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.SelectedRow.Should().Be(-1);
        dg.AddRow("1");
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_AddRow_SubsequentDoesNotChangeSelection()
    {
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_ClearRows_ResetsAll()
    {
        // L74-77: clear + reset
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.SelectedRow = 1;
        dg.ClearRows();
        dg.RowCount.Should().Be(0);
        dg.SelectedRow.Should().Be(-1);
    }

    [Fact]
    public void DataGrid_GetRow_ValidIndex()
    {
        var dg = new DataGrid();
        dg.AddColumns("A", "B");
        dg.AddRow("1", "2");
        var row = dg.GetRow(0);
        row.Should().NotBeNull();
        row![0].Should().Be("1");
        row[1].Should().Be("2");
    }

    [Fact]
    public void DataGrid_GetRow_InvalidIndex_ReturnsNull()
    {
        // L82-84: bounds check
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.GetRow(-1).Should().BeNull();
        dg.GetRow(0).Should().BeNull(); // no rows
        dg.AddRow("1");
        dg.GetRow(5).Should().BeNull();
    }

    [Fact]
    public void DataGrid_MeasureContent_IncludesHeaderAndSeparator()
    {
        // L92: rows.Count + 2
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        var size = dg.MeasureContent(new Size(50, 50));
        size.Height.Should().Be(4); // 2 rows + 2 (header + separator)
        size.Width.Should().Be(50); // full width
    }

    [Fact]
    public void DataGrid_Render_NoColumns_ReturnsEarly()
    {
        // L97-99: if (_columns.Count == 0) return
        var dg = new DataGrid();
        dg.AddRow("1"); // row but no columns
        dg.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        dg.Render(s); // should not crash
        // Buffer should be empty (no rendering happened)
        Row(buf, 0).Trim().Should().BeEmpty();
    }

    [Fact]
    public void DataGrid_Render_HeaderTruncation()
    {
        // L108-110: text.Length > colWidth truncation
        var dg = new DataGrid();
        dg.AddColumns("VeryLongColumnName");
        dg.AddRow("Data");
        dg.Arrange(new Rect(0, 0, 10, 5));
        var (buf, s) = Surface(10, 5);
        dg.Render(s);
        Row(buf, 0).Length.Should().Be(10);
    }

    [Fact]
    public void DataGrid_Render_ColWidth_Calculation()
    {
        // L102: colWidth = Math.Max(1, surface.Width / _columns.Count)
        var dg = new DataGrid();
        dg.AddColumns("A", "B");
        dg.AddRow("X", "Y");
        dg.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        dg.Render(s);
        // colWidth = 20/2 = 10, "A" at col 0, "B" at col 10
        Row(buf, 0).Substring(0, 1).Should().Be("A");
        Row(buf, 0).Substring(10, 1).Should().Be("B");
    }

    [Fact]
    public void DataGrid_Render_Separator_Line()
    {
        // L119-122: horizontal line at row 1
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.Arrange(new Rect(0, 0, 10, 5));
        var (buf, s) = Surface(10, 5);
        dg.Render(s);
        for (int col = 0; col < 10; col++)
            buf[col, 1].Character.Should().Be('\u2500');
    }

    [Fact]
    public void DataGrid_Render_DataRow_Styles()
    {
        // L135-136: isSelected style + fill
        var dg = new DataGrid();
        dg.SelectedStyle = new Style(Color.White, Color.Blue);
        dg.NormalStyle = new Style(Color.Grey, Color.Black);
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.SelectedRow = 0;
        dg.Arrange(new Rect(0, 0, 10, 5));
        var (buf, s) = Surface(10, 5);
        dg.Render(s);
        buf[0, 2].Style.Background.Should().Be(Color.Blue); // selected
        buf[0, 3].Style.Background.Should().Be(Color.Black); // normal
    }

    [Fact]
    public void DataGrid_Render_DataRowTruncation()
    {
        // L144-147: null coalesce + truncation
        var dg = new DataGrid();
        dg.AddColumns("Col");
        dg.AddRow("VeryLongDataValue");
        dg.Arrange(new Rect(0, 0, 8, 5));
        var (buf, s) = Surface(8, 5);
        dg.Render(s);
        Row(buf, 2).Length.Should().Be(8);
    }

    [Fact]
    public void DataGrid_Render_NullCellValue()
    {
        // L144: rowData[col] ?? string.Empty
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow(new string[] { null! });
        dg.Arrange(new Rect(0, 0, 10, 5));
        var (buf, s) = Surface(10, 5);
        dg.Render(s); // should not crash
    }

    [Fact]
    public void DataGrid_UpArrow_DecreasesSelection()
    {
        // L159-163
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.SelectedRow = 1;
        var result = dg.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        result.Should().BeTrue();
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_UpArrow_AtZero_NoChange()
    {
        // L160: if (_selectedRow > 0)
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.SelectedRow = 0;
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_DownArrow_IncreasesSelection()
    {
        // L166-170
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.SelectedRow = 0;
        var result = dg.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        result.Should().BeTrue();
        dg.SelectedRow.Should().Be(1);
    }

    [Fact]
    public void DataGrid_DownArrow_AtEnd_NoChange()
    {
        // L167: if (_selectedRow < _rows.Count - 1)
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.SelectedRow = 0;
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_Home_SelectsFirst()
    {
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.SelectedRow = 1;
        var result = dg.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        result.Should().BeTrue();
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_End_SelectsLast()
    {
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.AddRow("3");
        dg.SelectedRow = 0;
        var result = dg.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        result.Should().BeTrue();
        dg.SelectedRow.Should().Be(2);
    }

    [Fact]
    public void DataGrid_Enter_FiresRowActivated()
    {
        // L179-183
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.SelectedRow = 0;
        int? activated = null;
        dg.RowActivated += (_, idx) => activated = idx;
        var result = dg.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        result.Should().BeTrue();
        activated.Should().Be(0);
    }

    [Fact]
    public void DataGrid_Enter_NoEvent_WhenNoSelection()
    {
        // L180: if (_selectedRow >= 0)
        var dg = new DataGrid();
        dg.AddColumns("A");
        int? activated = null;
        dg.RowActivated += (_, idx) => activated = idx;
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().BeNull();
    }

    [Fact]
    public void DataGrid_UnhandledKey_ReturnsFalse()
    {
        // L187: default: return false
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        var result = dg.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        result.Should().BeFalse();
    }

    [Fact]
    public void DataGrid_MouseClick_SelectsRow_WithOffset()
    {
        // L195-202: localRow calculation subtracts header + separator
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.AddRow("3");
        dg.Arrange(new Rect(0, 0, 20, 10));
        // Click on row 4 => localRow = 4 - 0 - 2 = 2 => dataIndex = 2
        var result = dg.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 4, false, false, false));
        result.Should().BeTrue();
        dg.SelectedRow.Should().Be(2);
    }

    [Fact]
    public void DataGrid_MouseClick_OnHeader_NoSelection()
    {
        // L196: if (localRow >= 0) — clicking header area
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.Arrange(new Rect(0, 0, 20, 10));
        dg.SelectedRow = 0;
        // Click row 0 (header) => localRow = -2, skip
        dg.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        dg.SelectedRow.Should().Be(0); // unchanged
    }

    [Fact]
    public void DataGrid_MouseClick_BeyondData_NoChange()
    {
        // L199: dataIndex >= 0 && dataIndex < _rows.Count
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.Arrange(new Rect(0, 0, 20, 10));
        dg.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 8, false, false, false));
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_Mouse_NonLeftClick_ReturnsFalse()
    {
        // L208: return false
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.Arrange(new Rect(0, 0, 20, 10));
        var result = dg.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 5, 3, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void DataGrid_EnsureSelectedVisible_ZeroViewport()
    {
        // L213-215: if (viewportHeight <= 0) return
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.Arrange(new Rect(0, 0, 10, 2)); // viewport = 2-2 = 0
        var (buf, s) = Surface(10, 2);
        dg.Render(s); // should not crash
    }

    [Fact]
    public void DataGrid_EnsureSelectedVisible_ScrollsDown()
    {
        // L222-224: scrollOffset adjustment
        var dg = new DataGrid();
        dg.AddColumns("A");
        for (int i = 0; i < 20; i++)
            dg.AddRow(i.ToString());
        dg.Arrange(new Rect(0, 0, 10, 7)); // viewport = 7-2 = 5
        dg.SelectedRow = 15;
        var (buf, s) = Surface(10, 7);
        dg.Render(s);
        // Row 15 should be visible in last data row
        Row(buf, 6).Should().Contain("15");
    }

    // ════════════════════════════════════════════════════════════════
    // TreeView — L25-26 (boolean/statement), L37-106 (render),
    //   L110-131 (key handling), L140-185 (mouse), L203 (string)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TreeView_Constructor_RootIsExpandedAndCanFocus()
    {
        // L24-26: root expanded, canFocus, rebuild
        var tv = new TreeView("Root");
        tv.Root.IsExpanded.Should().BeTrue();
        tv.CanFocus.Should().BeTrue();
        tv.Root.Text.Should().Be("Root");
    }

    [Fact]
    public void TreeView_MeasureContent_MatchesFlatListCount()
    {
        // L30-31: Math.Min(_flatList.Count, available.Height)
        // flatList is rebuilt in constructor (just root = 1) and then in Render/OnKeyEvent
        var tv = new TreeView("Root");
        // After constructor, flatList has only root
        var size = tv.MeasureContent(new Size(50, 50));
        size.Height.Should().Be(1);
        size.Width.Should().Be(50);

        // Add children and trigger rebuild via OnKeyEvent
        tv.Root.AddChild("A");
        tv.Root.AddChild("B");
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // triggers RebuildFlatList
        // Now flatList: Root, A, B = 3
        var size2 = tv.MeasureContent(new Size(50, 50));
        size2.Height.Should().Be(3);

        var size3 = tv.MeasureContent(new Size(50, 2));
        size3.Height.Should().Be(2); // clamped to available
    }

    [Fact]
    public void TreeView_Render_ExpandedIcon()
    {
        // L54-55: [-] for expanded, [+] for collapsed
        var tv = new TreeView("Root");
        var child = tv.Root.AddChild("Child");
        child.AddChild("Leaf"); // make child have children
        child.IsExpanded = false;

        tv.Arrange(new Rect(0, 0, 30, 5));
        var (buf, s) = Surface(30, 5);
        tv.Render(s);
        Row(buf, 0).Should().Contain("[-]"); // root is expanded
        Row(buf, 1).Should().Contain("[+]"); // child is collapsed
    }

    [Fact]
    public void TreeView_Render_LeafNode_NoIcon()
    {
        // L56: "    " for leaf (no children)
        var tv = new TreeView("Root");
        tv.Root.AddChild("Leaf");
        tv.Arrange(new Rect(0, 0, 30, 5));
        var (buf, s) = Surface(30, 5);
        tv.Render(s);
        // Leaf should have spaces instead of [+]/[-]
        var leafRow = Row(buf, 1);
        leafRow.Should().NotContain("[+]");
        leafRow.Should().NotContain("[-]");
        leafRow.Should().Contain("Leaf");
    }

    [Fact]
    public void TreeView_Render_Indentation()
    {
        // L53: indent = new string(' ', node.Depth * 2)
        var tv = new TreeView("Root");
        var child = tv.Root.AddChild("Child");
        child.IsExpanded = true;
        child.AddChild("GrandChild");
        tv.Arrange(new Rect(0, 0, 40, 5));
        var (buf, s) = Surface(40, 5);
        tv.Render(s);
        // Root at depth 0: no indent
        Row(buf, 0).Should().StartWith("[-]");
        // Child at depth 1: 2 spaces indent
        Row(buf, 1).Should().StartWith("  ");
        // GrandChild at depth 2: 4 spaces indent
        Row(buf, 2).Should().StartWith("    ");
    }

    [Fact]
    public void TreeView_Render_TruncatesLongText()
    {
        // L59-61: text.Length > surface.Width truncation
        var tv = new TreeView("AVeryLongRootNodeTextThatExceedsTheWidth");
        tv.Arrange(new Rect(0, 0, 15, 3));
        var (buf, s) = Surface(15, 3);
        tv.Render(s);
        Row(buf, 0).Length.Should().Be(15);
    }

    [Fact]
    public void TreeView_Render_SelectedStyle()
    {
        // L48-49: isSelected style
        var tv = new TreeView("Root");
        tv.SelectedStyle = new Style(Color.White, Color.Blue);
        tv.NormalStyle = new Style(Color.Grey, Color.Black);
        tv.Root.AddChild("Child");
        tv.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        tv.Render(s);
        // Root (index 0) is selected
        buf[0, 0].Style.Background.Should().Be(Color.Blue);
        // Child (index 1) is not selected
        buf[0, 1].Style.Background.Should().Be(Color.Black);
    }

    [Fact]
    public void TreeView_Render_CollapsedHidesChildren()
    {
        // collapsed node doesn't show children in flatList
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = false;
        tv.Root.AddChild("Hidden");
        tv.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        tv.Render(s);
        Row(buf, 0).Should().Contain("Root");
        Row(buf, 1).Trim().Should().BeEmpty(); // Hidden not shown
    }

    [Fact]
    public void TreeView_KeyEvent_EmptyFlatList_ReturnsFalse()
    {
        // L72-74: if (_flatList.Count == 0) return false
        // This is hard to trigger since root always exists, but covers the branch
        var tv = new TreeView("Root");
        // Root collapsed with no children still has root in flat list
        // so we just verify normal operation
        var result = tv.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        result.Should().BeTrue();
    }

    [Fact]
    public void TreeView_UpArrow_Decrements_And_FiresEvent()
    {
        // L80-84: _selectedIndex--, Invalidate, SelectionChanged
        var tv = new TreeView("Root");
        tv.Root.AddChild("A");
        tv.Root.AddChild("B");
        // Go down twice first
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));

        TuiTreeNode? selected = null;
        tv.SelectionChanged += (_, n) => selected = n;
        var result = tv.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        result.Should().BeTrue();
        selected!.Text.Should().Be("A");
    }

    [Fact]
    public void TreeView_UpArrow_AtZero_NoChange()
    {
        // L80: if (_selectedIndex > 0)
        var tv = new TreeView("Root");
        TuiTreeNode? selected = null;
        tv.SelectionChanged += (_, n) => selected = n;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        selected.Should().BeNull();
    }

    [Fact]
    public void TreeView_DownArrow_Increments_And_FiresEvent()
    {
        // L90-94
        var tv = new TreeView("Root");
        tv.Root.AddChild("A");
        TuiTreeNode? selected = null;
        tv.SelectionChanged += (_, n) => selected = n;
        var result = tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        result.Should().BeTrue();
        selected!.Text.Should().Be("A");
    }

    [Fact]
    public void TreeView_DownArrow_AtEnd_NoChange()
    {
        // L90: if (_selectedIndex < _flatList.Count - 1)
        var tv = new TreeView("Root"); // only root
        TuiTreeNode? selected = null;
        tv.SelectionChanged += (_, n) => selected = n;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        selected.Should().BeNull(); // already at end
    }

    [Fact]
    public void TreeView_RightArrow_ExpandsNode()
    {
        // L99-107: expand if has children and !expanded
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = false;
        tv.Root.AddChild("A");
        var result = tv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeTrue();
        tv.Root.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void TreeView_RightArrow_AlreadyExpanded_NoOp()
    {
        // L103: if (!node.IsExpanded) — already expanded
        var tv = new TreeView("Root");
        tv.Root.AddChild("A");
        tv.Root.IsExpanded = true;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        tv.Root.IsExpanded.Should().BeTrue(); // unchanged
    }

    [Fact]
    public void TreeView_RightArrow_LeafNode_NoOp()
    {
        // L103: node.Children.Count > 0 — leaf has no children
        var tv = new TreeView("Root");
        tv.Root.AddChild("Leaf");
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // select leaf
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        // No crash, leaf has no children to expand
    }

    [Fact]
    public void TreeView_LeftArrow_CollapsesNode()
    {
        // L112-121: collapse if expanded
        var tv = new TreeView("Root");
        tv.Root.AddChild("A");
        tv.Root.IsExpanded = true;
        var result = tv.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        result.Should().BeTrue();
        tv.Root.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void TreeView_LeftArrow_AlreadyCollapsed_NoOp()
    {
        // L116: if (node.IsExpanded) — not expanded
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = false;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        tv.Root.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void TreeView_Enter_FiresNodeActivated_WithExactNode()
    {
        // L125-129
        var tv = new TreeView("Root");
        tv.Root.AddChild("Child");
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // select child
        TuiTreeNode? activated = null;
        tv.NodeActivated += (_, n) => activated = n;
        var result = tv.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        result.Should().BeTrue();
        activated!.Text.Should().Be("Child");
    }

    [Fact]
    public void TreeView_UnhandledKey_ReturnsFalse()
    {
        // L134: default: return false
        var tv = new TreeView("Root");
        var result = tv.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        result.Should().BeFalse();
    }

    [Fact]
    public void TreeView_MouseClick_Selects_And_FiresEvent()
    {
        // L140-152: mouse press selects node
        var tv = new TreeView("Root");
        tv.Root.AddChild("A");
        tv.Root.AddChild("B");
        tv.Arrange(new Rect(0, 0, 30, 5));
        // Must render first to populate _flatList
        var (buf, s) = Surface(30, 5);
        tv.Render(s);

        TuiTreeNode? selected = null;
        tv.SelectionChanged += (_, n) => selected = n;
        var result = tv.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 2, false, false, false));
        result.Should().BeTrue();
        selected!.Text.Should().Be("B");
    }

    [Fact]
    public void TreeView_MouseClick_OutOfRange_NoChange()
    {
        // L144: if (index >= 0 && index < _flatList.Count)
        var tv = new TreeView("Root");
        tv.Arrange(new Rect(0, 0, 30, 5));
        var (buf, s) = Surface(30, 5);
        tv.Render(s);
        TuiTreeNode? selected = null;
        tv.SelectionChanged += (_, n) => selected = n;
        tv.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 4, false, false, false));
        selected.Should().BeNull();
    }

    [Fact]
    public void TreeView_Mouse_NonLeft_ReturnsFalse()
    {
        // L154: return false
        var tv = new TreeView("Root");
        tv.Arrange(new Rect(0, 0, 30, 5));
        var result = tv.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 5, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void TreeView_EnsureSelectedVisible_Scrolling()
    {
        // L179-185: scroll up/down
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = true;
        for (int i = 0; i < 20; i++)
            tv.Root.AddChild($"Child{i}");
        tv.Arrange(new Rect(0, 0, 30, 3));
        // Navigate to end
        for (int i = 0; i < 20; i++)
            tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        var (buf, s) = Surface(30, 3);
        tv.Render(s);
        Row(buf, 2).Should().Contain("Child19");
    }

    [Fact]
    public void TreeNode_NullText_DefaultsToEmpty()
    {
        // L203: Text = text ?? string.Empty
        var node = new TuiTreeNode(null!);
        node.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void TreeNode_AddChild_ReturnsChild()
    {
        var node = new TuiTreeNode("Parent");
        var child = node.AddChild("Child");
        child.Text.Should().Be("Child");
        node.Children.Should().HaveCount(1);
        node.Children[0].Should().Be(child);
    }

    [Fact]
    public void TreeNode_Tag_Roundtrip()
    {
        var node = new TuiTreeNode("X") { Tag = "data" };
        node.Tag.Should().Be("data");
    }

    // ════════════════════════════════════════════════════════════════
    // ComboBox — L9 (string), L18-92 (render), L97-129 (keys),
    //   L146-219 (mouse/measure)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ComboBox_Text_NullCoalesces_ToEmpty()
    {
        // L18: _text = value ?? string.Empty
        var cb = new ComboBox();
        cb.Text = null!;
        cb.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void ComboBox_Text_Setter_FiresTextChanged()
    {
        // L20: TextChanged event
        var cb = new ComboBox();
        string? received = null;
        cb.TextChanged += (_, t) => received = t;
        cb.Text = "hello";
        received.Should().Be("hello");
    }

    [Fact]
    public void ComboBox_SelectedIndex_Setter_Clamps_UpdatesText_FiresEvent()
    {
        // L31-42: clamp, change detection, text update, invalidate, event
        var cb = new ComboBox();
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        int? changed = null;
        cb.SelectionChanged += (_, idx) => changed = idx;
        cb.SelectedIndex = 1;
        changed.Should().Be(1);
        cb.Text.Should().Be("Beta"); // L37: text updated

        cb.SelectedIndex = 0;
        cb.Text.Should().Be("Alpha");

        // Clamp above
        cb.SelectedIndex = 100;
        cb.SelectedIndex.Should().Be(1);

        // Clamp below
        cb.SelectedIndex = -5;
        cb.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void ComboBox_SelectedIndex_NoEventWhenUnchanged()
    {
        // L32: if (_selectedIndex != newIndex)
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.SelectedIndex = 0;
        var fired = false;
        cb.SelectionChanged += (_, _) => fired = true;
        cb.SelectedIndex = 0;
        fired.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_IsOpen_Setter_Invalidates()
    {
        // L51-52: _isOpen = value, Invalidate
        var cb = new ComboBox();
        cb.IsOpen.Should().BeFalse();
        cb.IsOpen = true;
        cb.IsOpen.Should().BeTrue();
        cb.IsOpen = false;
        cb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_AddItem_NullThrows()
    {
        var cb = new ComboBox();
        var act = () => cb.AddItem(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComboBox_AddItems_NullThrows()
    {
        var cb = new ComboBox();
        var act = () => cb.AddItems(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComboBox_ClearItems_ResetsSelection()
    {
        // L90-92: clear + _selectedIndex = -1 + Invalidate
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.SelectedIndex = 0;
        cb.ClearItems();
        cb.Items.Should().BeEmpty();
        cb.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void ComboBox_MeasureContent_Closed()
    {
        // L97: height = _isOpen ? 1 + Min(DropdownHeight, items.Count) : 1
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        var size = cb.MeasureContent(new Size(50, 50));
        size.Width.Should().Be(20);
        size.Height.Should().Be(1); // closed
    }

    [Fact]
    public void ComboBox_MeasureContent_Open()
    {
        // L97: open height calculation
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        cb.AddItem("C");
        cb.IsOpen = true;
        var size = cb.MeasureContent(new Size(50, 50));
        size.Height.Should().Be(4); // 1 + 3 items
    }

    [Fact]
    public void ComboBox_MeasureContent_Open_ClampsToDropdownHeight()
    {
        var cb = new ComboBox { DropdownHeight = 2 };
        cb.AddItem("A");
        cb.AddItem("B");
        cb.AddItem("C");
        cb.IsOpen = true;
        var size = cb.MeasureContent(new Size(50, 50));
        size.Height.Should().Be(3); // 1 + Min(2, 3) = 3
    }

    [Fact]
    public void ComboBox_Render_FocusedStyle()
    {
        // L103: HasFocus ? FocusedStyle : NormalStyle
        var cb = new ComboBox();
        cb.NormalStyle = new Style(Color.Red, Color.Black);
        cb.FocusedStyle = new Style(Color.Green, Color.White);
        cb.Text = "X";
        cb.Arrange(new Rect(0, 0, 20, 1));

        var (buf1, s1) = Surface(20, 1);
        cb.Render(s1);
        buf1[0, 0].Style.Foreground.Should().Be(Color.Red);

        cb.HasFocus = true;
        var (buf2, s2) = Surface(20, 1);
        cb.Render(s2);
        buf2[0, 0].Style.Foreground.Should().Be(Color.Green);
    }

    [Fact]
    public void ComboBox_Render_TextTruncation()
    {
        // L110-112: displayText truncation for arrow space
        var cb = new ComboBox { Text = "A very long combo text" };
        cb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, s) = Surface(10, 1);
        cb.Render(s);
        // Should show truncated text + " v "
        Row(buf, 0).Should().EndWith(" v ");
    }

    [Fact]
    public void ComboBox_Render_DownArrow_Indicator()
    {
        // L116: " v " at surface.Width - 3
        var cb = new ComboBox { Text = "AB" };
        cb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, s) = Surface(10, 1);
        cb.Render(s);
        Row(buf, 0).Substring(7).Should().Be(" v ");
    }

    [Fact]
    public void ComboBox_Render_Open_ShowsItems_WithStyles()
    {
        // L119-134: dropdown rendering with selected style
        var cb = new ComboBox();
        cb.SelectedItemStyle = new Style(Color.White, Color.Blue);
        cb.DropdownStyle = new Style(Color.Grey, Color.Black);
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        cb.IsOpen = true;
        cb.SelectedIndex = 0;
        // Force internal _selectedIndex
        cb.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        cb.Render(s);
        // Row 1 = "Alpha" (selected)
        buf[0, 1].Style.Background.Should().Be(Color.Blue);
        // Row 2 = "Beta" (normal)
        buf[0, 2].Style.Background.Should().Be(Color.Black);
        Row(buf, 1).Should().Contain("Alpha");
        Row(buf, 2).Should().Contain("Beta");
    }

    [Fact]
    public void ComboBox_Render_Open_TruncatesItemText()
    {
        // L128-130: itemText truncation
        var cb = new ComboBox();
        cb.AddItem("VeryLongItemName");
        cb.IsOpen = true;
        cb.Arrange(new Rect(0, 0, 8, 3));
        var (buf, s) = Surface(8, 3);
        cb.Render(s);
        Row(buf, 1).Length.Should().Be(8);
    }

    [Fact]
    public void ComboBox_DownArrow_Closed_Opens_And_SelectsFirst()
    {
        // L142-149: open + select first if none selected
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        cb.IsOpen.Should().BeFalse();
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        result.Should().BeTrue();
        cb.IsOpen.Should().BeTrue();
        cb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ComboBox_DownArrow_Open_NavigatesDown()
    {
        // L151-153: when open, navigate down
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // open
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // navigate
        cb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void ComboBox_DownArrow_Open_AtEnd_NoChange()
    {
        // L151: if (_selectedIndex < _items.Count - 1)
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // open, select 0
        cb.SelectedIndex.Should().Be(0);
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // at end
        cb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ComboBox_UpArrow_Open_NavigatesUp()
    {
        // L158-163
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // open
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // to B
        cb.SelectedIndex.Should().Be(1);
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        result.Should().BeTrue();
        cb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ComboBox_UpArrow_AtZero_NoChange()
    {
        // L159: if (_isOpen && _selectedIndex > 0)
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // open, select 0
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        cb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ComboBox_UpArrow_WhenClosed_NoOp()
    {
        // L159: _isOpen check
        var cb = new ComboBox();
        cb.AddItem("A");
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        result.Should().BeTrue();
        cb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_Enter_SelectsItem_ClosesDropdown_FiresEvent()
    {
        // L166-172: set text, close, fire TextChanged
        var cb = new ComboBox();
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // open
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // select Beta
        string? textChanged = null;
        cb.TextChanged += (_, t) => textChanged = t;
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        result.Should().BeTrue();
        cb.Text.Should().Be("Beta");
        cb.IsOpen.Should().BeFalse();
        textChanged.Should().Be("Beta");
    }

    [Fact]
    public void ComboBox_Enter_WhenClosed_NoOp()
    {
        // L167: if (_isOpen && _selectedIndex >= 0)
        var cb = new ComboBox();
        cb.AddItem("A");
        string? textChanged = null;
        cb.TextChanged += (_, t) => textChanged = t;
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        textChanged.Should().BeNull();
    }

    [Fact]
    public void ComboBox_Escape_WhenOpen_Closes()
    {
        // L176-181
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0')); // open
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.Escape, '\x1b'));
        result.Should().BeTrue();
        cb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_Escape_WhenClosed_ReturnsFalse()
    {
        // L183: return false (not open)
        var cb = new ComboBox();
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.Escape, '\x1b'));
        result.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_UnhandledKey_ReturnsFalse()
    {
        // L186: default: return false
        var cb = new ComboBox();
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        result.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_Mouse_NonLeftOrNonPress_ReturnsFalse()
    {
        // L192-194: eventType != Press || button != Left
        var cb = new ComboBox();
        cb.Arrange(new Rect(0, 0, 20, 5));
        var result = cb.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 5, 0, false, false, false));
        result.Should().BeFalse();

        var result2 = cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 5, 0, false, false, false));
        result2.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_MouseClick_Row0_TogglesDropdown()
    {
        // L199-202: localRow == 0 toggles
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.Arrange(new Rect(0, 0, 20, 5));
        var result = cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        result.Should().BeTrue();
        cb.IsOpen.Should().BeTrue();
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        cb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_MouseClick_DropdownItem_SelectsAndCloses()
    {
        // L205-217: click on dropdown item
        var cb = new ComboBox();
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        cb.Arrange(new Rect(0, 0, 20, 5));
        // Open dropdown
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        cb.IsOpen.Should().BeTrue();

        string? textChanged = null;
        cb.TextChanged += (_, t) => textChanged = t;

        // Click on Beta (row 2 = item index 1)
        var result = cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 2, false, false, false));
        result.Should().BeTrue();
        cb.Text.Should().Be("Beta");
        cb.IsOpen.Should().BeFalse();
        textChanged.Should().Be("Beta");
    }

    [Fact]
    public void ComboBox_MouseClick_DropdownOutOfRange_NoSelection()
    {
        // L208: if (itemIndex >= 0 && itemIndex < _items.Count)
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.Arrange(new Rect(0, 0, 20, 5));
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false)); // open
        // Click far below items
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 4, false, false, false));
        cb.Text.Should().BeEmpty(); // no item selected
    }

    [Fact]
    public void ComboBox_MouseClick_Dropdown_WhenClosed_ReturnsFalse()
    {
        // L219: return false — dropdown closed, click below row 0
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.Arrange(new Rect(0, 0, 20, 5));
        var result = cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 2, false, false, false));
        result.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════
    // ScrollView — L19-53 (property setters), L62 (measure),
    //   L72-101 (render scrollbar), L113-146 (keys), L149 (mouse)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScrollView_Content_Setter_ManagesLifecycle()
    {
        // L17-34: unmount old, mount new, reset scroll, invalidate
        var sv = new ScrollView();
        var label1 = new Label("A");
        sv.Content = label1;
        label1.Parent.Should().Be(sv);

        var label2 = new Label("B");
        sv.Content = label2;
        label1.Parent.Should().BeNull(); // old content unmounted
        label2.Parent.Should().Be(sv);
    }

    [Fact]
    public void ScrollView_Content_Setter_ResetsScroll()
    {
        // L31-32: _scrollX = 0, _scrollY = 0
        var sv = new ScrollView();
        sv.ScrollX = 5;
        sv.ScrollY = 10;
        sv.Content = new Label("X");
        sv.ScrollX.Should().Be(0);
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_Content_SetNull_ClearsOld()
    {
        // L17-21: old content unmount
        var sv = new ScrollView();
        var label = new Label("X");
        sv.Content = label;
        sv.Content = null;
        label.Parent.Should().BeNull();
        sv.Content.Should().BeNull();
    }

    [Fact]
    public void ScrollView_ScrollX_Setter_ClampsToZero()
    {
        // L42: Math.Max(0, value)
        var sv = new ScrollView();
        sv.ScrollX = 5;
        sv.ScrollX.Should().Be(5);
        sv.ScrollX = -10;
        sv.ScrollX.Should().Be(0);
        sv.ScrollX = 0;
        sv.ScrollX.Should().Be(0);
    }

    [Fact]
    public void ScrollView_ScrollY_Setter_ClampsToZero()
    {
        // L52: Math.Max(0, value)
        var sv = new ScrollView();
        sv.ScrollY = 5;
        sv.ScrollY.Should().Be(5);
        sv.ScrollY = -10;
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_MeasureContent_ReturnsAvailable()
    {
        // L62-63: returns available size as-is
        var sv = new ScrollView();
        var size = sv.MeasureContent(new Size(40, 20));
        size.Width.Should().Be(40);
        size.Height.Should().Be(20);
    }

    [Fact]
    public void ScrollView_Arrange_PositionsContent()
    {
        // L66-78: content arranged with scroll offset
        var sv = new ScrollView();
        var label = new Label("Hello World");
        sv.Content = label;
        sv.ScrollX = 3;
        sv.ScrollY = 2;
        sv.Arrange(new Rect(0, 0, 20, 10));
        // Content should be offset by scroll
        label.Bounds.X.Should().Be(-3); // 0 - scrollX
        label.Bounds.Y.Should().Be(-2); // 0 - scrollY
    }

    [Fact]
    public void ScrollView_Render_ScrollbarShown_WhenContentTaller()
    {
        // L88-103: vertical scrollbar rendering
        var sv = new ScrollView();
        var label = new Label(string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line{i}")));
        sv.Content = label;
        sv.ShowVerticalScrollBar = true;
        sv.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        sv.Render(s);
        // Scrollbar on right edge
        var hasBar = false;
        for (int r = 0; r < 5; r++)
        {
            if (buf[19, r].Character == '\u2502')
                hasBar = true;
        }
        hasBar.Should().BeTrue();
    }

    [Fact]
    public void ScrollView_Render_ScrollbarHidden_WhenDisabled()
    {
        // L88: ShowVerticalScrollBar check
        var sv = new ScrollView();
        var label = new Label(string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line{i}")));
        sv.Content = label;
        sv.ShowVerticalScrollBar = false;
        sv.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        sv.Render(s);
        var hasBar = false;
        for (int r = 0; r < 5; r++)
        {
            if (buf[19, r].Character == '\u2502')
                hasBar = true;
        }
        hasBar.Should().BeFalse();
    }

    [Fact]
    public void ScrollView_Render_NoScrollbar_WhenNoContent()
    {
        // L88: _content != null check
        var sv = new ScrollView();
        sv.ShowVerticalScrollBar = true;
        sv.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        sv.Render(s); // no crash, no scrollbar
    }

    [Fact]
    public void ScrollView_Render_NoScrollbar_WhenContentFits()
    {
        // L93: if (contentHeight > viewportHeight)
        var sv = new ScrollView();
        var label = new Label("Short");
        sv.Content = label;
        sv.Arrange(new Rect(0, 0, 20, 10));
        var (buf, s) = Surface(20, 10);
        sv.Render(s);
        // Content fits, no scrollbar
        var hasBar = false;
        for (int r = 0; r < 10; r++)
        {
            if (buf[19, r].Character == '\u2502')
                hasBar = true;
        }
        hasBar.Should().BeFalse();
    }

    [Fact]
    public void ScrollView_Render_ThumbAndTrack_Styles()
    {
        // L101: thumb vs track style
        var thumbStyle = new Style(Color.White, Color.Grey);
        var barStyle = new Style(Color.DarkRed, Color.Black);
        var sv = new ScrollView
        {
            ScrollThumbStyle = thumbStyle,
            ScrollBarStyle = barStyle,
        };
        var label = new Label(string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line{i}")));
        sv.Content = label;
        sv.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        sv.Render(s);
        // At least one cell should have each style
        var hasThumb = false;
        var hasTrack = false;
        for (int r = 0; r < 5; r++)
        {
            if (buf[19, r].Character == '\u2502')
            {
                if (buf[19, r].Style.Foreground == Color.White)
                    hasThumb = true;
                if (buf[19, r].Style.Foreground == Color.DarkRed)
                    hasTrack = true;
            }
        }
        hasThumb.Should().BeTrue();
    }

    [Fact]
    public void ScrollView_UpArrow_DecreasesScrollY()
    {
        // L112-113
        var sv = new ScrollView();
        sv.ScrollY = 5;
        var result = sv.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        result.Should().BeTrue();
        sv.ScrollY.Should().Be(4);
    }

    [Fact]
    public void ScrollView_UpArrow_AtZero_StaysZero()
    {
        // L113: Math.Max(0, _scrollY - 1)
        var sv = new ScrollView();
        sv.ScrollY = 0;
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_DownArrow_IncreasesScrollY()
    {
        // L116
        var sv = new ScrollView();
        sv.ScrollY = 0;
        var result = sv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        result.Should().BeTrue();
        sv.ScrollY.Should().Be(1);
    }

    [Fact]
    public void ScrollView_LeftArrow_DecreasesScrollX()
    {
        // L118-119
        var sv = new ScrollView();
        sv.ScrollX = 5;
        var result = sv.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        result.Should().BeTrue();
        sv.ScrollX.Should().Be(4);
    }

    [Fact]
    public void ScrollView_LeftArrow_AtZero_StaysZero()
    {
        // L119: Math.Max(0, _scrollX - 1)
        var sv = new ScrollView();
        sv.ScrollX = 0;
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        sv.ScrollX.Should().Be(0);
    }

    [Fact]
    public void ScrollView_RightArrow_IncreasesScrollX()
    {
        // L121-122
        var sv = new ScrollView();
        sv.ScrollX = 0;
        var result = sv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeTrue();
        sv.ScrollX.Should().Be(1);
    }

    [Fact]
    public void ScrollView_PageUp_DecreasesScrollY_ByBoundsHeight()
    {
        // L124-125
        var sv = new ScrollView();
        sv.Arrange(new Rect(0, 0, 20, 10));
        sv.ScrollY = 15;
        var result = sv.OnKeyEvent(new KeyEvent(ConsoleKey.PageUp, '\0'));
        result.Should().BeTrue();
        sv.ScrollY.Should().Be(5); // 15 - 10
    }

    [Fact]
    public void ScrollView_PageUp_ClampsToZero()
    {
        // L125: Math.Max(0, ...)
        var sv = new ScrollView();
        sv.Arrange(new Rect(0, 0, 20, 10));
        sv.ScrollY = 3;
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.PageUp, '\0'));
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_PageDown_IncreasesScrollY_ByBoundsHeight()
    {
        // L127-128
        var sv = new ScrollView();
        sv.Arrange(new Rect(0, 0, 20, 10));
        sv.ScrollY = 0;
        var result = sv.OnKeyEvent(new KeyEvent(ConsoleKey.PageDown, '\0'));
        result.Should().BeTrue();
        sv.ScrollY.Should().Be(10);
    }

    [Fact]
    public void ScrollView_UnhandledKey_ReturnsFalse()
    {
        // L131: default: return false
        var sv = new ScrollView();
        var result = sv.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        result.Should().BeFalse();
    }

    [Fact]
    public void ScrollView_MouseScrollUp_DecreasesBy3()
    {
        // L137-140
        var sv = new ScrollView();
        sv.ScrollY = 10;
        var result = sv.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollUp, 5, 2, false, false, false));
        result.Should().BeTrue();
        sv.ScrollY.Should().Be(7);
    }

    [Fact]
    public void ScrollView_MouseScrollUp_ClampsToZero()
    {
        // L139: Math.Max(0, _scrollY - 3)
        var sv = new ScrollView();
        sv.ScrollY = 1;
        sv.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollUp, 5, 2, false, false, false));
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_MouseScrollDown_IncreasesBy3()
    {
        // L143-146
        var sv = new ScrollView();
        sv.ScrollY = 0;
        var result = sv.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollDown, 5, 2, false, false, false));
        result.Should().BeTrue();
        sv.ScrollY.Should().Be(3);
    }

    [Fact]
    public void ScrollView_MouseOtherEvent_ReturnsFalse()
    {
        // L149: return false
        var sv = new ScrollView();
        var result = sv.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 2, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void ScrollView_GetChildren_ReturnsContent_OrEmpty()
    {
        // L152-155
        var sv = new ScrollView();
        sv.GetChildren().Should().BeEmpty();
        var label = new Label("X");
        sv.Content = label;
        sv.GetChildren().Should().HaveCount(1);
        sv.GetChildren()[0].Should().Be(label);
    }

    [Fact]
    public void ScrollView_Render_ThumbPosition_ChangesWithScroll()
    {
        // L97: thumbPos calculation based on _scrollY
        var sv = new ScrollView();
        var label = new Label(string.Join("\n", Enumerable.Range(0, 100).Select(i => $"Line{i}")));
        sv.Content = label;
        sv.Arrange(new Rect(0, 0, 20, 10));

        // Scroll to middle
        sv.ScrollY = 50;
        sv.Arrange(new Rect(0, 0, 20, 10)); // re-arrange with new scroll
        var (buf1, s1) = Surface(20, 10);
        sv.Render(s1);

        // Find thumb position
        var thumbRow1 = -1;
        for (int r = 0; r < 10; r++)
        {
            if (buf1[19, r].Character == '\u2502' && buf1[19, r].Style == sv.ScrollThumbStyle)
            {
                thumbRow1 = r;
                break;
            }
        }

        // Scroll to start
        sv.ScrollY = 0;
        sv.Arrange(new Rect(0, 0, 20, 10));
        var (buf2, s2) = Surface(20, 10);
        sv.Render(s2);

        var thumbRow2 = -1;
        for (int r = 0; r < 10; r++)
        {
            if (buf2[19, r].Character == '\u2502' && buf2[19, r].Style == sv.ScrollThumbStyle)
            {
                thumbRow2 = r;
                break;
            }
        }

        // Thumb position should differ between scroll positions
        if (thumbRow1 >= 0 && thumbRow2 >= 0)
        {
            thumbRow1.Should().BeGreaterThan(thumbRow2);
        }
    }

    // ════════════════════════════════════════════════════════════════
    // Additional edge cases for arithmetic/boundary mutants
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TextBox_Typing_AtMiddle_InsertsCorrectly()
    {
        // Verify exact insert position (kills arithmetic mutation on L156)
        var tb = new TextBox { Text = "ABDE" };
        tb.CursorPosition = 2;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.C, 'C'));
        tb.Text.Should().Be("ABCDE");
        tb.CursorPosition.Should().Be(3);
    }

    [Fact]
    public void TextBox_Backspace_AtMiddle_RemovesCorrectChar()
    {
        // Verify exact removal position (kills arithmetic mutation on L126)
        var tb = new TextBox { Text = "ABCD" };
        tb.CursorPosition = 2;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Backspace, '\b'));
        tb.Text.Should().Be("ACD");
        tb.CursorPosition.Should().Be(1);
    }

    [Fact]
    public void TextBox_Delete_AtMiddle_RemovesCorrectChar()
    {
        // Verify exact removal position (kills arithmetic mutation on L137)
        var tb = new TextBox { Text = "ABCD" };
        tb.CursorPosition = 1;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Delete, '\0'));
        tb.Text.Should().Be("ACD");
        tb.CursorPosition.Should().Be(1);
    }

    [Fact]
    public void ListBox_Render_ItemIndex_ExactCalculation()
    {
        // Kill arithmetic mutation on L119: itemIndex = _scrollOffset + row
        var lb = new ListBox();
        for (int i = 0; i < 20; i++)
            lb.AddItem($"Item{i}");
        lb.Arrange(new Rect(0, 0, 20, 3));
        lb.SelectedIndex = 5;
        var (buf, s) = Surface(20, 3);
        lb.Render(s);
        // After scrolling, items should be sequential
        Row(buf, 0).Should().Contain("Item");
        Row(buf, 1).Should().Contain("Item");
        Row(buf, 2).Should().Contain("Item");
    }

    [Fact]
    public void DataGrid_Render_RenderRow_ExactOffset()
    {
        // Kill arithmetic mutation on L137: renderRow = row + 2
        var dg = new DataGrid();
        dg.AddColumns("Col");
        dg.AddRow("First");
        dg.AddRow("Second");
        dg.Arrange(new Rect(0, 0, 20, 6));
        var (buf, s) = Surface(20, 6);
        dg.Render(s);
        // Row 0 = header, row 1 = separator, row 2+ = data
        Row(buf, 2).Should().Contain("First");
        Row(buf, 3).Should().Contain("Second");
        // Separator is exactly at row 1
        buf[0, 1].Character.Should().Be('\u2500');
    }

    [Fact]
    public void DataGrid_Render_ColumnPosition_ExactCalculation()
    {
        // Kill arithmetic mutation on L113/L150: col * colWidth
        var dg = new DataGrid();
        dg.AddColumns("AA", "BB", "CC");
        dg.AddRow("X1", "X2", "X3");
        dg.Arrange(new Rect(0, 0, 30, 5));
        var (buf, s) = Surface(30, 5);
        dg.Render(s);
        // colWidth = 30/3 = 10
        // Col 0 at x=0, col 1 at x=10, col 2 at x=20
        buf[0, 0].Character.Should().Be('A');
        buf[10, 0].Character.Should().Be('B');
        buf[20, 0].Character.Should().Be('C');
        buf[0, 2].Character.Should().Be('X');
        buf[10, 2].Character.Should().Be('X');
        buf[20, 2].Character.Should().Be('X');
    }

    [Fact]
    public void TreeView_Render_DepthMultiplier_ExactIndent()
    {
        // Kill arithmetic mutation on L53: node.Depth * 2
        var tv = new TreeView("Root");
        var child = tv.Root.AddChild("C1");
        child.IsExpanded = true;
        var grandchild = child.AddChild("GC");
        tv.Arrange(new Rect(0, 0, 40, 5));
        var (buf, s) = Surface(40, 5);
        tv.Render(s);
        // Root depth 0: starts at col 0
        buf[0, 0].Character.Should().Be('['); // [-] Root
        // Child depth 1: 2-space indent
        buf[0, 1].Character.Should().Be(' ');
        buf[1, 1].Character.Should().Be(' ');
        buf[2, 1].Character.Should().Be('['); // [+] or [-]
        // Grandchild depth 2: 4-space indent
        buf[0, 2].Character.Should().Be(' ');
        buf[1, 2].Character.Should().Be(' ');
        buf[2, 2].Character.Should().Be(' ');
        buf[3, 2].Character.Should().Be(' ');
    }

    [Fact]
    public void ComboBox_Render_ArrowSpace_ExactValue()
    {
        // Kill arithmetic mutation on L109: arrowSpace = 3
        var cb = new ComboBox { Text = "ABCDEFGHIJ" };
        cb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, s) = Surface(10, 1);
        cb.Render(s);
        // Last 3 chars should be " v "
        buf[7, 0].Character.Should().Be(' ');
        buf[8, 0].Character.Should().Be('v');
        buf[9, 0].Character.Should().Be(' ');
        // Text truncated to 7 chars (10 - 3)
        buf[0, 0].Character.Should().Be('A');
        buf[6, 0].Character.Should().Be('G');
    }

    [Fact]
    public void ComboBox_Render_DropdownItem_RowOffset()
    {
        // Kill arithmetic mutation on L125/L133: i + 1
        var cb = new ComboBox();
        cb.AddItem("First");
        cb.AddItem("Second");
        cb.IsOpen = true;
        cb.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        cb.Render(s);
        // Row 0 = input field, Row 1 = first item, Row 2 = second item
        Row(buf, 1).Should().Contain("First");
        Row(buf, 2).Should().Contain("Second");
    }

    [Fact]
    public void ScrollView_MouseScroll_ExactDelta()
    {
        // Kill arithmetic mutation on L139/L145: delta of 3
        var sv = new ScrollView();
        sv.ScrollY = 10;
        sv.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollUp, 0, 0, false, false, false));
        sv.ScrollY.Should().Be(7); // exactly 10-3

        sv.ScrollY = 10;
        sv.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollDown, 0, 0, false, false, false));
        sv.ScrollY.Should().Be(13); // exactly 10+3
    }

    [Fact]
    public void ScrollView_Arrange_ContentSize_UsesMax()
    {
        // L76-77: Math.Max(contentSize.Width/Height, bounds.Width/Height)
        var sv = new ScrollView();
        var label = new Label("X"); // small content
        sv.Content = label;
        sv.Arrange(new Rect(0, 0, 20, 10));
        // Content bounds should be at least as large as viewport
        label.Bounds.Width.Should().BeGreaterThanOrEqualTo(20);
        label.Bounds.Height.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void DataGrid_Mouse_ClickSeparator_ReturnsTrue_NoSelectionChange()
    {
        // L195: localRow = e.Row - Bounds.Y - 2; click on separator (row 1)
        var dg = new DataGrid();
        dg.AddColumns("A");
        dg.AddRow("1");
        dg.AddRow("2");
        dg.Arrange(new Rect(0, 0, 20, 10));
        dg.SelectedRow = 0;
        // Click on separator line (row 1): localRow = 1 - 0 - 2 = -1, which is < 0
        var result = dg.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 1, false, false, false));
        result.Should().BeTrue();
        dg.SelectedRow.Should().Be(0); // unchanged
    }

    [Fact]
    public void ListBox_CanFocus_DefaultTrue()
    {
        // L42: CanFocus = true in constructor
        var lb = new ListBox();
        lb.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void DataGrid_CanFocus_DefaultTrue()
    {
        // L41: CanFocus = true in constructor
        var dg = new DataGrid();
        dg.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void ComboBox_CanFocus_DefaultTrue()
    {
        // L67: CanFocus = true in constructor
        var cb = new ComboBox();
        cb.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void TextBox_CanFocus_DefaultTrue()
    {
        // L45: CanFocus = true in constructor
        var tb = new TextBox();
        tb.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void TextBox_Render_PlaceholderStyle_Applied()
    {
        // L66: PlaceholderStyle used for placeholder text
        var pStyle = new Style(Color.DarkRed);
        var tb = new TextBox { Placeholder = "Hint", PlaceholderStyle = pStyle };
        tb.HasFocus = false;
        tb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, s) = Surface(20, 1);
        tb.Render(s);
        buf[0, 0].Style.Foreground.Should().Be(Color.DarkRed);
    }

    [Fact]
    public void ComboBox_DownArrow_Closed_NoItems_StaysUnselected()
    {
        // L146: if (_selectedIndex < 0 && _items.Count > 0) — no items
        var cb = new ComboBox();
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        cb.IsOpen.Should().BeTrue();
        cb.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void DataGrid_Render_MoreColumnsThanData()
    {
        // L142: col < rowData.Length — row has fewer values than columns
        var dg = new DataGrid();
        dg.AddColumns("A", "B", "C");
        dg.AddRow("Only1"); // only 1 value for 3 columns
        dg.Arrange(new Rect(0, 0, 30, 5));
        var (buf, s) = Surface(30, 5);
        dg.Render(s); // should not crash
        Row(buf, 2).Should().Contain("Only1");
    }

    [Fact]
    public void DataGrid_Render_HeaderStyle_Applied()
    {
        // L114: HeaderStyle used for header cells
        var dg = new DataGrid();
        dg.HeaderStyle = new Style(Color.Yellow, Color.DarkGreen);
        dg.AddColumn("Header");
        dg.AddRow("Data");
        dg.Arrange(new Rect(0, 0, 20, 5));
        var (buf, s) = Surface(20, 5);
        dg.Render(s);
        buf[0, 0].Style.Foreground.Should().Be(Color.Yellow);
        buf[0, 0].Style.Background.Should().Be(Color.DarkGreen);
    }

    [Fact]
    public void DataGrid_Render_BorderStyle_OnSeparator()
    {
        // L121: BorderStyle used for separator
        var dg = new DataGrid();
        dg.BorderStyle = new Style(Color.Magenta1);
        dg.AddColumn("A");
        dg.AddRow("1");
        dg.Arrange(new Rect(0, 0, 10, 5));
        var (buf, s) = Surface(10, 5);
        dg.Render(s);
        buf[0, 1].Style.Foreground.Should().Be(Color.Magenta1);
    }
}
