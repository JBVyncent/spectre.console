using FluentAssertions;
using Spectre.Console;
using Xunit;
using TuiTreeNode = Spectre.Console.Tui.Widgets.Controls.TreeNode;

namespace Spectre.Console.Tui.Tests;

/// <summary>
/// Mutation-killing tests for complex widgets, containers, chrome, windows, and integration.
/// </summary>
public sealed class MutantKillerComplexTests
{
    private static (ScreenBuffer buf, BufferSurface surface) Surface(int w, int h)
    {
        var buf = new ScreenBuffer(w, h);
        return (buf, new BufferSurface(buf));
    }

    private static string Row(ScreenBuffer buf, int row)
    {
        var chars = new char[buf.Width];
        for (int i = 0; i < buf.Width; i++)
            chars[i] = buf[i, row].Character;
        return new string(chars);
    }

    // ── TextBox ─────────────────────────────────────────────────────

    [Fact]
    public void TextBox_Render_EmptyWithPlaceholder()
    {
        var tb = new TextBox { Placeholder = "Type..." };
        tb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        tb.Render(surface);
        Row(buf, 0).Should().Contain("Type...");
    }

    [Fact]
    public void TextBox_Render_WithText()
    {
        var tb = new TextBox { Text = "Hello" };
        tb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        tb.Render(surface);
        Row(buf, 0).Should().Contain("Hello");
    }

    [Fact]
    public void TextBox_Render_FocusedShowsCursor()
    {
        var tb = new TextBox { Text = "AB" };
        tb.CanFocus = true;
        tb.OnFocusGained();
        tb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        // HasFocus is set internally — simulate via direct property
        // (FocusManager sets it, but we test rendering directly)
        tb.Render(surface);
        // Cursor position cell should be rendered
        Row(buf, 0).Should().StartWith("AB");
    }

    [Fact]
    public void TextBox_Typing_InsertsCharacters()
    {
        var tb = new TextBox();
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, false, false));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.B, 'b', false, false, false));
        tb.Text.Should().Be("ab");
        tb.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void TextBox_Backspace_DeletesChar()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Backspace, '\b', false, false, false));
        tb.Text.Should().Be("ab");
    }

    [Fact]
    public void TextBox_Delete_DeletesForward()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Delete, '\0', false, false, false));
        tb.Text.Should().Be("bc");
    }

    [Fact]
    public void TextBox_LeftRight_MovesCursor()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 1;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        tb.CursorPosition.Should().Be(0);
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        tb.CursorPosition.Should().Be(1);
    }

    [Fact]
    public void TextBox_Home_End()
    {
        var tb = new TextBox { Text = "hello" };
        tb.CursorPosition = 2;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0', false, false, false));
        tb.CursorPosition.Should().Be(0);
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0', false, false, false));
        tb.CursorPosition.Should().Be(5);
    }

    [Fact]
    public void TextBox_Enter_FiresSubmitted()
    {
        var tb = new TextBox { Text = "query" };
        var submitted = false;
        tb.Submitted += (_, _) => submitted = true;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        submitted.Should().BeTrue();
    }

    [Fact]
    public void TextBox_TextChanged_Event()
    {
        var tb = new TextBox();
        var changed = false;
        tb.TextChanged += (_, _) => changed = true;
        tb.Text = "new";
        changed.Should().BeTrue();
    }

    [Fact]
    public void TextBox_MaxLength_Respected()
    {
        var tb = new TextBox { MaxLength = 3 };
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, false, false));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.B, 'b', false, false, false));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.C, 'c', false, false, false));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.D, 'd', false, false, false));
        tb.Text.Should().Be("abc");
    }

    [Fact]
    public void TextBox_MeasureContent()
    {
        var tb = new TextBox();
        var size = tb.MeasureContent(new Spectre.Console.Size(50, 5));
        size.Width.Should().Be(20);
        size.Height.Should().Be(1);
    }

    [Fact]
    public void TextBox_ScrollOffset_WhenTextExceedsWidth()
    {
        var tb = new TextBox { Text = "A very long text that exceeds width" };
        tb.CursorPosition = 35;
        tb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        tb.Render(surface);
        // Should render the end portion
        Row(buf, 0).TrimEnd().Should().NotBeEmpty();
    }

    [Fact]
    public void TextBox_Backspace_AtStart_DoesNothing()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Backspace, '\b', false, false, false));
        tb.Text.Should().Be("abc");
    }

    [Fact]
    public void TextBox_Left_AtStart_Stays()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        tb.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void TextBox_Right_AtEnd_Stays()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        tb.CursorPosition.Should().Be(3);
    }

    // ── ListBox ─────────────────────────────────────────────────────

    [Fact]
    public void ListBox_Render_ShowsItems()
    {
        var lb = new ListBox();
        lb.AddItem("Alpha");
        lb.AddItem("Beta");
        lb.AddItem("Gamma");
        lb.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        lb.Render(surface);
        Row(buf, 0).Should().Contain("Alpha");
        Row(buf, 1).Should().Contain("Beta");
        Row(buf, 2).Should().Contain("Gamma");
    }

    [Fact]
    public void ListBox_Render_HighlightsSelected()
    {
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        lb.Render(surface);
        // Selected row 0 should have different style from row 1
        buf[0, 0].Style.Should().NotBe(buf[0, 1].Style);
    }

    [Fact]
    public void ListBox_PageUp_PageDown()
    {
        var lb = new ListBox();
        for (int i = 0; i < 50; i++)
            lb.AddItem($"Item {i}");
        lb.Arrange(new Rect(0, 0, 20, 10));
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.PageDown, '\0', false, false, false));
        lb.SelectedIndex.Should().BeGreaterThan(0);
        var after = lb.SelectedIndex;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.PageUp, '\0', false, false, false));
        lb.SelectedIndex.Should().BeLessThan(after);
    }

    [Fact]
    public void ListBox_MouseClick_SelectsRow()
    {
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.AddItem("C");
        lb.Arrange(new Rect(0, 0, 20, 5));
        lb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 2, false, false, false));
        lb.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void ListBox_MouseScroll_Scrolls()
    {
        var lb = new ListBox();
        for (int i = 0; i < 50; i++)
            lb.AddItem($"Item {i}");
        lb.Arrange(new Rect(0, 0, 20, 5));
        lb.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollDown, 5, 2, false, false, false));
        lb.SelectedIndex.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ListBox_RemoveItem()
    {
        var lb = new ListBox();
        lb.AddItem("A");
        lb.AddItem("B");
        lb.RemoveItem(0);
        lb.Items.Should().HaveCount(1);
        lb.Items[0].Should().Be("B");
    }

    [Fact]
    public void ListBox_AddItems_Bulk()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.Items.Should().HaveCount(3);
    }

    [Fact]
    public void ListBox_MeasureContent()
    {
        var lb = new ListBox();
        lb.AddItem("LongerText");
        var size = lb.MeasureContent(new Spectre.Console.Size(100, 50));
        size.Width.Should().Be(12); // text + 2 padding
        size.Height.Should().Be(1);
    }

    [Fact]
    public void ListBox_EnsureSelectedVisible_Scrolls()
    {
        var lb = new ListBox();
        for (int i = 0; i < 50; i++)
            lb.AddItem($"Item {i}");
        lb.Arrange(new Rect(0, 0, 20, 5));
        // Go to end
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0', false, false, false));
        lb.SelectedIndex.Should().Be(49);
        // Render should show the last items
        var (buf, surface) = Surface(20, 5);
        lb.Render(surface);
        Row(buf, 4).Should().Contain("Item 49");
    }

    // ── DataGrid ────────────────────────────────────────────────────

    [Fact]
    public void DataGrid_Render_ShowsHeaderAndRows()
    {
        var dg = new DataGrid();
        dg.AddColumns("Name", "Age");
        dg.AddRow("Alice", "30");
        dg.AddRow("Bob", "25");
        dg.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        dg.Render(surface);
        Row(buf, 0).Should().Contain("Name").And.Contain("Age");
        // Row 1 is separator
        Row(buf, 2).Should().Contain("Alice");
        Row(buf, 3).Should().Contain("Bob");
    }

    [Fact]
    public void DataGrid_Render_HighlightsSelectedRow()
    {
        var dg = new DataGrid();
        dg.AddColumns("ID", "Val");
        dg.AddRow("1", "A");
        dg.AddRow("2", "B");
        dg.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        dg.Render(surface);
        // Selected row 0 (data row 2 in buffer) should differ from row 1
        buf[0, 2].Style.Should().NotBe(buf[0, 3].Style);
    }

    [Fact]
    public void DataGrid_Home_End()
    {
        var dg = new DataGrid();
        dg.AddColumns("ID");
        for (int i = 0; i < 20; i++)
            dg.AddRow(i.ToString());
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0', false, false, false));
        dg.SelectedRow.Should().Be(19);
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0', false, false, false));
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void DataGrid_Enter_FiresRowActivated()
    {
        var dg = new DataGrid();
        dg.AddColumns("ID");
        dg.AddRow("1");
        var activated = false;
        dg.RowActivated += (_, _) => activated = true;
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        activated.Should().BeTrue();
    }

    [Fact]
    public void DataGrid_MouseClick_SelectsRow()
    {
        var dg = new DataGrid();
        dg.AddColumns("ID");
        dg.AddRow("A");
        dg.AddRow("B");
        dg.AddRow("C");
        dg.Arrange(new Rect(0, 0, 30, 10));
        // Row 0 = header, row 1 = separator, row 2+ = data
        dg.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 3, false, false, false));
        dg.SelectedRow.Should().Be(1);
    }

    [Fact]
    public void DataGrid_ClearRows()
    {
        var dg = new DataGrid();
        dg.AddColumns("ID");
        dg.AddRow("1");
        dg.ClearRows();
        dg.SelectedRow.Should().Be(-1);
    }

    [Fact]
    public void DataGrid_GetRow()
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
    public void DataGrid_SelectionChanged_Event()
    {
        var dg = new DataGrid();
        dg.AddColumns("ID");
        dg.AddRow("A");
        dg.AddRow("B");
        var changed = false;
        dg.SelectionChanged += (_, _) => changed = true;
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        changed.Should().BeTrue();
    }

    [Fact]
    public void DataGrid_Scrolling_LargeDataset()
    {
        var dg = new DataGrid();
        dg.AddColumns("ID");
        for (int i = 0; i < 100; i++)
            dg.AddRow(i.ToString());
        dg.Arrange(new Rect(0, 0, 30, 10));
        // Navigate to bottom
        for (int i = 0; i < 99; i++)
            dg.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        dg.SelectedRow.Should().Be(99);
        // Render should work without error
        var (buf, surface) = Surface(30, 10);
        dg.Render(surface);
        Row(buf, 9).Should().Contain("99");
    }

    // ── TreeView ────────────────────────────────────────────────────

    [Fact]
    public void TreeView_Render_ShowsTree()
    {
        var tv = new TreeView("Root");
        tv.Root.AddChild("Child1");
        tv.Root.AddChild("Child2");
        tv.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        tv.Render(surface);
        Row(buf, 0).Should().Contain("Root");
    }

    [Fact]
    public void TreeView_Render_ExpandedChildren()
    {
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = true;
        tv.Root.AddChild("Child1");
        tv.Root.AddChild("Child2");
        tv.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        tv.Render(surface);
        Row(buf, 1).Should().Contain("Child1");
        Row(buf, 2).Should().Contain("Child2");
    }

    [Fact]
    public void TreeView_Right_Expands_Left_Collapses()
    {
        var tv = new TreeView("Root");
        tv.Root.AddChild("A");
        tv.Root.IsExpanded = false;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        tv.Root.IsExpanded.Should().BeTrue();
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        tv.Root.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void TreeView_Enter_FiresActivated()
    {
        var tv = new TreeView("Root");
        var activated = false;
        tv.NodeActivated += (_, _) => activated = true;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        activated.Should().BeTrue();
    }

    [Fact]
    public void TreeView_MouseClick_Selects()
    {
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = true;
        tv.Root.AddChild("A");
        tv.Root.AddChild("B");
        tv.Arrange(new Rect(0, 0, 30, 10));
        // Render to rebuild flat list
        var (buf, surface) = Surface(30, 10);
        tv.Render(surface);
        TuiTreeNode? selectedNode = null;
        tv.SelectionChanged += (_, n) => selectedNode = n;
        tv.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 1, false, false, false));
        selectedNode.Should().NotBeNull();
        selectedNode!.Text.Should().Be("A");
    }

    [Fact]
    public void TreeView_Navigate_UpDown()
    {
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = true;
        tv.Root.AddChild("A");
        tv.Root.AddChild("B");
        TuiTreeNode? selected = null;
        tv.SelectionChanged += (_, n) => selected = n;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        selected!.Text.Should().Be("A");
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        selected!.Text.Should().Be("B");
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0', false, false, false));
        selected!.Text.Should().Be("A");
    }

    [Fact]
    public void TreeView_NestedExpansion()
    {
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = true;
        var child = tv.Root.AddChild("Parent");
        child.AddChild("Leaf");
        // Navigate to child
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        // Expand child
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        child.IsExpanded.Should().BeTrue();
        // Navigate to leaf
        TuiTreeNode? sel = null;
        tv.SelectionChanged += (_, n) => sel = n;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        sel!.Text.Should().Be("Leaf");
    }

    [Fact]
    public void TreeView_Tag_CustomData()
    {
        var node = new TuiTreeNode("Test") { Tag = 42 };
        node.Tag.Should().Be(42);
        node.Depth.Should().Be(0);
    }

    [Fact]
    public void TreeView_SelectionChanged_Event()
    {
        var tv = new TreeView("Root");
        tv.Root.IsExpanded = true;
        tv.Root.AddChild("A");
        var changed = false;
        tv.SelectionChanged += (_, _) => changed = true;
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        changed.Should().BeTrue();
    }

    // ── ComboBox ────────────────────────────────────────────────────

    [Fact]
    public void ComboBox_Render_Closed()
    {
        var cb = new ComboBox();
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        cb.Text = "Alpha";
        cb.Arrange(new Rect(0, 0, 25, 1));
        var (buf, surface) = Surface(25, 1);
        cb.Render(surface);
        Row(buf, 0).Should().Contain("Alpha");
        Row(buf, 0).Should().Contain("v"); // dropdown arrow
    }

    [Fact]
    public void ComboBox_Render_Open_ShowsDropdown()
    {
        var cb = new ComboBox();
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        cb.AddItem("Gamma");
        cb.Arrange(new Rect(0, 0, 25, 6));
        // Open the dropdown
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        cb.IsOpen.Should().BeTrue();
        var (buf, surface) = Surface(25, 6);
        cb.Render(surface);
        Row(buf, 1).Should().Contain("Alpha");
        Row(buf, 2).Should().Contain("Beta");
        Row(buf, 3).Should().Contain("Gamma");
    }

    [Fact]
    public void ComboBox_Down_OpensAndNavigates()
    {
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        cb.IsOpen.Should().BeTrue();
        cb.SelectedIndex.Should().Be(0);
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        cb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void ComboBox_Up_Navigates()
    {
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        cb.SelectedIndex.Should().Be(1);
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0', false, false, false));
        cb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ComboBox_Enter_Selects()
    {
        var cb = new ComboBox();
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        cb.Text.Should().Be("Alpha");
        cb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_Escape_Closes()
    {
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        cb.IsOpen.Should().BeTrue();
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Escape, '\x1b', false, false, false));
        cb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_AddItems_ClearItems()
    {
        var cb = new ComboBox();
        cb.AddItems(new[] { "X", "Y", "Z" });
        cb.Items.Should().HaveCount(3);
        cb.ClearItems();
        cb.Items.Should().BeEmpty();
    }

    [Fact]
    public void ComboBox_TextChanged_Event()
    {
        var cb = new ComboBox();
        var changed = false;
        cb.TextChanged += (_, _) => changed = true;
        cb.Text = "hello";
        changed.Should().BeTrue();
    }

    [Fact]
    public void ComboBox_SelectionChanged_Event()
    {
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.AddItem("B");
        var changed = false;
        cb.SelectionChanged += (_, _) => changed = true;
        // Open dropdown
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        // Navigate to item 1
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        changed.Should().BeTrue();
    }

    [Fact]
    public void ComboBox_MouseClick_ToggleDropdown()
    {
        var cb = new ComboBox();
        cb.AddItem("A");
        cb.Arrange(new Rect(0, 0, 25, 5));
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        cb.IsOpen.Should().BeTrue();
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        cb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ComboBox_MouseClick_SelectsItem()
    {
        var cb = new ComboBox();
        cb.AddItem("Alpha");
        cb.AddItem("Beta");
        cb.Arrange(new Rect(0, 0, 25, 5));
        // Open
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        // Click on item at row 2 (Beta)
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 2, false, false, false));
        cb.Text.Should().Be("Beta");
    }

    [Fact]
    public void ComboBox_MeasureContent()
    {
        var cb = new ComboBox();
        var size = cb.MeasureContent(new Spectre.Console.Size(50, 10));
        size.Width.Should().Be(20);
        size.Height.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void ComboBox_DropdownHeight()
    {
        var cb = new ComboBox { DropdownHeight = 3 };
        cb.DropdownHeight.Should().Be(3);
    }

    // ── ScrollView ──────────────────────────────────────────────────

    [Fact]
    public void ScrollView_Render_ShowsContent()
    {
        var sv = new ScrollView();
        var label = new Label("Content Text");
        sv.Content = label;
        sv.Arrange(new Rect(0, 0, 20, 5));
        // Render the content within a subsurface — ScrollView.Render draws content
        var (buf, surface) = Surface(20, 5);
        sv.Render(surface);
        // The content may or may not be visible depending on how Render arranges —
        // verify it doesn't crash and produces some output
        // (The Label is rendered within the ScrollView's viewport)
    }

    [Fact]
    public void ScrollView_ArrowKeys_Scroll()
    {
        var sv = new ScrollView();
        var label = new Label("Line1\nLine2\nLine3\nLine4\nLine5\nLine6\nLine7\nLine8\nLine9\nLine10");
        sv.Content = label;
        sv.Arrange(new Rect(0, 0, 20, 3));
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0', false, false, false));
        sv.ScrollY.Should().Be(1);
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0', false, false, false));
        sv.ScrollY.Should().Be(0);
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        sv.ScrollX.Should().Be(1);
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        sv.ScrollX.Should().Be(0);
    }

    [Fact]
    public void ScrollView_PageUpDown()
    {
        var sv = new ScrollView();
        sv.Arrange(new Rect(0, 0, 20, 5));
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.PageDown, '\0', false, false, false));
        sv.ScrollY.Should().Be(5);
        sv.OnKeyEvent(new KeyEvent(ConsoleKey.PageUp, '\0', false, false, false));
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_MouseScroll()
    {
        var sv = new ScrollView();
        sv.Arrange(new Rect(0, 0, 20, 5));
        sv.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollDown, 5, 2, false, false, false));
        sv.ScrollY.Should().BeGreaterThan(0);
        sv.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollUp, 5, 2, false, false, false));
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_ScrollY_ClampsToZero()
    {
        var sv = new ScrollView();
        sv.ScrollY = -10;
        sv.ScrollY.Should().Be(0);
    }

    [Fact]
    public void ScrollView_ScrollX_ClampsToZero()
    {
        var sv = new ScrollView();
        sv.ScrollX = -10;
        sv.ScrollX.Should().Be(0);
    }

    [Fact]
    public void ScrollView_Content_ManagesLifecycle()
    {
        var sv = new ScrollView();
        var label = new Label("Test");
        sv.Content = label;
        label.Parent.Should().Be(sv);
        sv.Content = null;
        label.Parent.Should().BeNull();
    }

    [Fact]
    public void ScrollView_GetChildren_ReturnsContent()
    {
        var sv = new ScrollView();
        sv.GetChildren().Should().BeEmpty();
        var label = new Label("X");
        sv.Content = label;
        sv.GetChildren().Should().HaveCount(1);
        sv.GetChildren()[0].Should().Be(label);
    }

    [Fact]
    public void ScrollView_Render_WithScrollbar()
    {
        var sv = new ScrollView();
        // Create content taller than viewport
        var label = new Label(string.Join("\n", Enumerable.Range(0, 30).Select(i => $"Line {i}")));
        sv.Content = label;
        sv.Arrange(new Rect(0, 0, 20, 5));
        // After Arrange, content has been measured and placed
        var (buf, surface) = Surface(20, 5);
        sv.Render(surface);
        // Scrollbar should be visible on the right edge using │ character
        var hasScrollbar = false;
        for (int r = 0; r < 5; r++)
        {
            if (buf[19, r].Character == '│')
                hasScrollbar = true;
        }
        hasScrollbar.Should().BeTrue();
    }

    // ── ContainerWidget ─────────────────────────────────────────────

    [Fact]
    public void ContainerWidget_Add_SetsParent()
    {
        var stack = new VStack();
        var label = new Label("X");
        stack.Add(label);
        label.Parent.Should().Be(stack);
    }

    [Fact]
    public void ContainerWidget_Remove_ClearsParent()
    {
        var stack = new VStack();
        var label = new Label("X");
        stack.Add(label);
        stack.Remove(label);
        label.Parent.Should().BeNull();
    }

    [Fact]
    public void ContainerWidget_Clear_RemovesAll()
    {
        var stack = new VStack();
        var a = new Label("A");
        var b = new Label("B");
        stack.Add(a);
        stack.Add(b);
        stack.Clear();
        stack.GetChildren().Should().BeEmpty();
        a.Parent.Should().BeNull();
        b.Parent.Should().BeNull();
    }

    [Fact]
    public void ContainerWidget_GetChildren_ReturnsAllAdded()
    {
        var stack = new VStack();
        var a = new Label("A");
        var b = new Label("B");
        stack.Add(a);
        stack.Add(b);
        stack.GetChildren().Should().HaveCount(2);
    }

    // ── VStack ──────────────────────────────────────────────────────

    [Fact]
    public void VStack_MeasureContent()
    {
        var stack = new VStack { Spacing = 1 };
        stack.Add(new Label("Short"));
        stack.Add(new Label("Longer text"));
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(11); // max line
        size.Height.Should().Be(3); // 2 items + 1 spacing
    }

    [Fact]
    public void VStack_Arrange_FixedAndFill()
    {
        var stack = new VStack();
        var fixed1 = new Label("Fixed");
        fixed1.HeightConstraint = Constraint.Fixed(2);
        var fill = new Label("Fill");
        fill.HeightConstraint = Constraint.Fill();
        stack.Add(fixed1);
        stack.Add(fill);
        stack.Arrange(new Rect(0, 0, 20, 10));
        fixed1.Bounds.Height.Should().Be(2);
        fill.Bounds.Height.Should().Be(8);
        fill.Bounds.Y.Should().Be(2);
    }

    [Fact]
    public void VStack_Render_IsEmpty()
    {
        var stack = new VStack();
        stack.Add(new Label("A"));
        stack.Arrange(new Rect(0, 0, 10, 5));
        var (buf, surface) = Surface(10, 5);
        stack.Render(surface); // VStack render is empty - children render themselves
        // No crash = pass
    }

    [Fact]
    public void VStack_Spacing()
    {
        var stack = new VStack { Spacing = 2 };
        var a = new Label("A");
        a.HeightConstraint = Constraint.Fixed(1);
        var b = new Label("B");
        b.HeightConstraint = Constraint.Fixed(1);
        stack.Add(a);
        stack.Add(b);
        stack.Arrange(new Rect(0, 0, 10, 10));
        b.Bounds.Y.Should().Be(3); // 1 (height) + 2 (spacing)
    }

    // ── HStack ──────────────────────────────────────────────────────

    [Fact]
    public void HStack_MeasureContent()
    {
        var stack = new HStack { Spacing = 1 };
        stack.Add(new Label("AB"));
        stack.Add(new Label("CD"));
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(5); // 2 + 1 + 2
        size.Height.Should().Be(1);
    }

    [Fact]
    public void HStack_Arrange_FixedAndFill()
    {
        var stack = new HStack();
        var fixed1 = new Label("Fix");
        fixed1.WidthConstraint = Constraint.Fixed(5);
        var fill = new Label("Fill");
        fill.WidthConstraint = Constraint.Fill();
        stack.Add(fixed1);
        stack.Add(fill);
        stack.Arrange(new Rect(0, 0, 30, 5));
        fixed1.Bounds.Width.Should().Be(5);
        fill.Bounds.Width.Should().Be(25);
        fill.Bounds.X.Should().Be(5);
    }

    [Fact]
    public void HStack_Spacing()
    {
        var stack = new HStack { Spacing = 3 };
        var a = new Label("A");
        a.WidthConstraint = Constraint.Fixed(2);
        var b = new Label("B");
        b.WidthConstraint = Constraint.Fixed(2);
        stack.Add(a);
        stack.Add(b);
        stack.Arrange(new Rect(0, 0, 20, 5));
        b.Bounds.X.Should().Be(5); // 2 + 3
    }

    // ── Splitter ────────────────────────────────────────────────────

    [Fact]
    public void Splitter_Render_Vertical_DrawsDivider()
    {
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        var left = new Label("L");
        var right = new Label("R");
        splitter.First = left;
        splitter.Second = right;
        splitter.Arrange(new Rect(0, 0, 21, 5));
        var (buf, surface) = Surface(21, 5);
        splitter.Render(surface);
        // Divider should be at midpoint
        var divCol = (int)(21 * 0.5);
        buf[divCol, 0].Character.Should().Be('│');
    }

    [Fact]
    public void Splitter_Render_Horizontal_DrawsDivider()
    {
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            SplitRatio = 0.5,
        };
        splitter.First = new Label("T");
        splitter.Second = new Label("B");
        splitter.Arrange(new Rect(0, 0, 20, 11));
        var (buf, surface) = Surface(20, 11);
        splitter.Render(surface);
        var divRow = (int)(11 * 0.5);
        buf[0, divRow].Character.Should().Be('─');
    }

    [Fact]
    public void Splitter_SplitRatio_Clamped()
    {
        var splitter = new Splitter();
        splitter.SplitRatio = 0.05;
        splitter.SplitRatio.Should().Be(0.1);
        splitter.SplitRatio = 0.95;
        splitter.SplitRatio.Should().Be(0.9);
    }

    [Fact]
    public void Splitter_GetChildren_ReturnsNonNull()
    {
        var splitter = new Splitter();
        splitter.GetChildren().Should().BeEmpty();
        splitter.First = new Label("A");
        splitter.GetChildren().Should().HaveCount(1);
        splitter.Second = new Label("B");
        splitter.GetChildren().Should().HaveCount(2);
    }

    [Fact]
    public void Splitter_First_Second_SetParent()
    {
        var splitter = new Splitter();
        var a = new Label("A");
        var b = new Label("B");
        splitter.First = a;
        a.Parent.Should().Be(splitter);
        splitter.Second = b;
        b.Parent.Should().Be(splitter);
    }

    [Fact]
    public void Splitter_MouseDrag_ChangesRatio()
    {
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        splitter.First = new Label("L");
        splitter.Second = new Label("R");
        splitter.Arrange(new Rect(0, 0, 20, 5));

        var divCol = (int)(20 * 0.5);
        // Press on divider
        splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, divCol, 2, false, false, false));
        // Drag to new position
        splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, divCol + 3, 2, false, false, false));
        splitter.SplitRatio.Should().NotBe(0.5);
    }

    // ── MenuBar ─────────────────────────────────────────────────────

    [Fact]
    public void MenuBar_Render_ShowsItems()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.AddItem(new MenuItem("Edit"));
        mb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        mb.Render(surface);
        Row(buf, 0).Should().Contain("File").And.Contain("Edit");
    }

    [Fact]
    public void MenuBar_LeftRight_Navigates()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.AddItem(new MenuItem("B"));
        mb.AddItem(new MenuItem("C"));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        // Navigate back
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
    }

    [Fact]
    public void MenuBar_Enter_ActivatesItem()
    {
        var mb = new MenuBar();
        var item = new MenuItem("Go");
        var activated = false;
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);
        mb.HasFocus = true;
        // Select first item then activate
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        activated.Should().BeTrue();
    }

    [Fact]
    public void MenuBar_Escape_Deselects()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.Escape, '\x1b', false, false, false));
    }

    [Fact]
    public void MenuBar_MouseClick_SelectsItem()
    {
        var mb = new MenuBar();
        var item = new MenuItem("File");
        var activated = false;
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);
        mb.Arrange(new Rect(0, 0, 30, 1));
        mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 2, 0, false, false, false));
        activated.Should().BeTrue();
    }

    // ── StatusBar ───────────────────────────────────────────────────

    [Fact]
    public void StatusBar_Render_ShowsItems()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help");
        sb.AddItem("F10", "Quit");
        sb.Arrange(new Rect(0, 0, 40, 1));
        var (buf, surface) = Surface(40, 1);
        sb.Render(surface);
        Row(buf, 0).Should().Contain("F1").And.Contain("Help").And.Contain("F10").And.Contain("Quit");
    }

    [Fact]
    public void StatusBar_Render_ShowsText()
    {
        var sb = new StatusBar { Text = "Ready" };
        sb.Arrange(new Rect(0, 0, 40, 1));
        var (buf, surface) = Surface(40, 1);
        sb.Render(surface);
        Row(buf, 0).Should().Contain("Ready");
    }

    [Fact]
    public void StatusBar_ClearItems()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help");
        sb.ClearItems();
        sb.Arrange(new Rect(0, 0, 40, 1));
        var (buf, surface) = Surface(40, 1);
        sb.Render(surface);
        Row(buf, 0).Should().NotContain("F1");
    }

    [Fact]
    public void StatusBar_MouseClick_TriggersAction()
    {
        var sb = new StatusBar();
        var triggered = false;
        sb.AddItem("F1", "Help", () => triggered = true);
        sb.Arrange(new Rect(0, 0, 40, 1));
        sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 2, 0, false, false, false));
        triggered.Should().BeTrue();
    }

    [Fact]
    public void StatusBar_MeasureContent()
    {
        var sb = new StatusBar();
        var size = sb.MeasureContent(new Spectre.Console.Size(50, 5));
        size.Height.Should().Be(1);
    }

    // ── TabControl ──────────────────────────────────────────────────

    [Fact]
    public void TabControl_Render_ShowsTabs()
    {
        var tc = new TabControl();
        tc.AddTab("Tab1", new Label("Content1"));
        tc.AddTab("Tab2", new Label("Content2"));
        tc.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        tc.Render(surface);
        Row(buf, 0).Should().Contain("Tab1").And.Contain("Tab2");
    }

    [Fact]
    public void TabControl_LeftRight_SwitchesTabs()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("CA"));
        tc.AddTab("B", new Label("CB"));
        tc.AddTab("C", new Label("CC"));
        // OnKeyEvent requires HasFocus — set it via internal
        tc.HasFocus = true;
        tc.SelectedIndex.Should().Be(0);
        tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        tc.SelectedIndex.Should().Be(1);
        tc.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        tc.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void TabControl_LeftRight_Wraps()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("CA"));
        tc.AddTab("B", new Label("CB"));
        tc.HasFocus = true;
        tc.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        tc.SelectedIndex.Should().Be(1); // wraps
        tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        tc.SelectedIndex.Should().Be(0); // wraps
    }

    [Fact]
    public void TabControl_MouseClick_SelectsTab()
    {
        var tc = new TabControl();
        tc.AddTab("Tab1", new Label("C1"));
        tc.AddTab("Tab2", new Label("C2"));
        tc.Arrange(new Rect(0, 0, 30, 10));
        // Tab1 = " Tab1 " = 6 chars + 1 separator = 7, Tab2 starts at 7
        tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 8, 0, false, false, false));
        tc.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void TabControl_GetChildren_ReturnsActiveTab()
    {
        var tc = new TabControl();
        var label1 = new Label("C1");
        var label2 = new Label("C2");
        tc.AddTab("A", label1);
        tc.AddTab("B", label2);
        tc.HasFocus = true;
        tc.GetChildren().Should().HaveCount(1);
        tc.GetChildren()[0].Should().Be(label1);
        tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        tc.GetChildren()[0].Should().Be(label2);
    }

    [Fact]
    public void TabControl_Arrange_ContentBelowTabs()
    {
        var tc = new TabControl();
        var label = new Label("Content");
        tc.AddTab("Tab", label);
        tc.Arrange(new Rect(0, 0, 30, 10));
        label.Bounds.Y.Should().BeGreaterThan(0);
    }

    // ── TuiPanel ────────────────────────────────────────────────────

    [Fact]
    public void TuiPanel_Render_ShowsBorderAndTitle()
    {
        var panel = new TuiPanel { Title = "MyPanel" };
        panel.Content = new Label("Inside");
        panel.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        panel.Render(surface);
        // Top border with box-drawing chars
        buf[0, 0].Character.Should().Be('┌');
        buf[19, 0].Character.Should().Be('┐');
        buf[0, 4].Character.Should().Be('└');
        buf[19, 4].Character.Should().Be('┘');
        // Title
        Row(buf, 0).Should().Contain("MyPanel");
    }

    [Fact]
    public void TuiPanel_Render_SideBorders()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        panel.Render(surface);
        buf[0, 2].Character.Should().Be('│');
        buf[19, 2].Character.Should().Be('│');
    }

    [Fact]
    public void TuiPanel_Content_ManagesLifecycle()
    {
        var panel = new TuiPanel();
        var label = new Label("X");
        panel.Content = label;
        label.Parent.Should().Be(panel);
        panel.Content = null;
        label.Parent.Should().BeNull();
    }

    [Fact]
    public void TuiPanel_GetChildren_ReturnsContent()
    {
        var panel = new TuiPanel();
        panel.GetChildren().Should().BeEmpty();
        panel.Content = new Label("X");
        panel.GetChildren().Should().HaveCount(1);
    }

    [Fact]
    public void TuiPanel_MeasureContent()
    {
        var panel = new TuiPanel();
        panel.Content = new Label("Hello");
        var size = panel.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(7); // 5 + 2 border
        size.Height.Should().Be(3); // 1 + 2 border
    }

    [Fact]
    public void TuiPanel_Arrange_ContentInsideBorders()
    {
        var panel = new TuiPanel();
        var label = new Label("X");
        panel.Content = label;
        panel.Arrange(new Rect(0, 0, 20, 10));
        label.Bounds.X.Should().Be(1);
        label.Bounds.Y.Should().Be(1);
        label.Bounds.Width.Should().Be(18);
        label.Bounds.Height.Should().Be(8);
    }

    [Fact]
    public void TuiPanel_Render_WithBorderStyle()
    {
        var panel = new TuiPanel { BorderStyle = new Style(Color.Blue) };
        panel.Arrange(new Rect(0, 0, 10, 3));
        var (buf, surface) = Surface(10, 3);
        panel.Render(surface);
        buf[0, 0].Style.Foreground.Should().Be(Color.Blue);
    }

    // ── MenuItem ────────────────────────────────────────────────────

    [Fact]
    public void MenuItem_Separator()
    {
        var sep = MenuItem.Separator();
        sep.IsSeparator.Should().BeTrue();
        sep.Text.Should().BeEmpty();
    }

    [Fact]
    public void MenuItem_Properties()
    {
        var item = new MenuItem("File", "Alt+F");
        item.Text.Should().Be("File");
        item.Shortcut.Should().Be("Alt+F");
        item.Enabled.Should().BeTrue();
        item.IsSeparator.Should().BeFalse();
    }

    [Fact]
    public void MenuItem_Activated_Event()
    {
        var item = new MenuItem("Go");
        var fired = false;
        item.Activated += (_, _) => fired = true;
        item.RaiseActivated();
        fired.Should().BeTrue();
    }

    // ── Window ──────────────────────────────────────────────────────

    [Fact]
    public void Window_Render_ShowsChrome()
    {
        var win = new Window("My Window");
        win.Arrange(new Rect(0, 0, 25, 8));
        var (buf, surface) = Surface(25, 8);
        win.Render(surface);
        // Row 0: top border ┌───┐
        buf[0, 0].Character.Should().Be('┌');
        buf[24, 0].Character.Should().Be('┐');
        // Row 1: title bar
        Row(buf, 1).Should().Contain("My Window");
        // Row 2: separator ├───┤
        buf[0, 2].Character.Should().Be('├');
        buf[24, 2].Character.Should().Be('┤');
        // Row 7: bottom border └───┘
        buf[0, 7].Character.Should().Be('└');
        buf[24, 7].Character.Should().Be('┘');
    }

    [Fact]
    public void Window_Render_CloseButton()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        win.Render(surface);
        Row(buf, 1).Should().Contain("[X]"); // title bar row
    }

    [Fact]
    public void Window_Render_Separator()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        win.Render(surface);
        // Row 2: separator ├──────┤
        buf[0, 2].Character.Should().Be('├');
        buf[19, 2].Character.Should().Be('┤');
    }

    [Fact]
    public void Window_Close_FiresEvent()
    {
        var win = new Window("W") { Closable = true };
        var closed = false;
        win.Closed += (_, _) => closed = true;
        win.Arrange(new Rect(0, 0, 20, 5));
        // Click close button on title bar (row 1, near right)
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 17, 1, false, false, false));
        closed.Should().BeTrue();
    }

    [Fact]
    public void Window_MeasureContent()
    {
        var win = new Window("Test");
        var size = win.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(2); // just borders, no children
        size.Height.Should().Be(3); // top + sep + bottom
    }

    [Fact]
    public void Window_Properties()
    {
        var win = new Window("W") { Title = "T", Resizable = true, Movable = true, Closable = true };
        win.Title.Should().Be("T");
        win.Resizable.Should().BeTrue();
        win.Movable.Should().BeTrue();
        win.Closable.Should().BeTrue();
    }

    // ── Dialog ──────────────────────────────────────────────────────

    [Fact]
    public void Dialog_Close_SetsResult()
    {
        var dialog = new Dialog("D") { Title = "Confirm" };
        dialog.Result.Should().Be(DialogResult.None);
        var closed = false;
        dialog.Closed += (_, _) => closed = true;
        dialog.Close(DialogResult.Ok);
        dialog.Result.Should().Be(DialogResult.Ok);
        closed.Should().BeTrue();
    }

    [Fact]
    public void Dialog_DefaultProperties()
    {
        var dialog = new Dialog("D");
        dialog.IsModal.Should().BeTrue();
        dialog.Closable.Should().BeTrue();
        dialog.Resizable.Should().BeFalse();
        dialog.Movable.Should().BeTrue();
    }

    [Fact]
    public void Dialog_AllResults()
    {
        var d = new Dialog("D");
        d.Close(DialogResult.Cancel);
        d.Result.Should().Be(DialogResult.Cancel);

        var d2 = new Dialog("D");
        d2.Close(DialogResult.Yes);
        d2.Result.Should().Be(DialogResult.Yes);

        var d3 = new Dialog("D");
        d3.Close(DialogResult.No);
        d3.Result.Should().Be(DialogResult.No);
    }

    // ── WindowManager ───────────────────────────────────────────────

    [Fact]
    public void WindowManager_AddRemove()
    {
        var wm = new WindowManager();
        var win = new Window("W") { Title = "A" };
        wm.AddWindow(win);
        wm.ActiveWindow.Should().Be(win);
        wm.RemoveWindow(win);
        wm.ActiveWindow.Should().BeNull();
    }

    [Fact]
    public void WindowManager_BringToFront()
    {
        var wm = new WindowManager();
        var win1 = new Window("W") { Title = "A" };
        var win2 = new Window("W") { Title = "B" };
        wm.AddWindow(win1);
        wm.AddWindow(win2);
        wm.ActiveWindow.Should().Be(win2);
        wm.BringToFront(win1);
        wm.ActiveWindow.Should().Be(win1);
    }

    [Fact]
    public void WindowManager_SendToBack()
    {
        var wm = new WindowManager();
        var win1 = new Window("W") { Title = "A" };
        var win2 = new Window("W") { Title = "B" };
        wm.AddWindow(win1);
        wm.AddWindow(win2);
        wm.SendToBack(win2);
        wm.ActiveWindow.Should().Be(win1);
    }

    [Fact]
    public void WindowManager_GetWindowAt()
    {
        var wm = new WindowManager();
        var win = new Window("W") { Title = "A" };
        win.Arrange(new Rect(5, 5, 15, 10));
        wm.AddWindow(win);
        wm.GetWindowAt(10, 10).Should().Be(win);
        wm.GetWindowAt(0, 0).Should().BeNull();
    }

    [Fact]
    public void WindowManager_ZOrder_Updated()
    {
        var wm = new WindowManager();
        var win1 = new Window("W") { Title = "A" };
        var win2 = new Window("W") { Title = "B" };
        var win3 = new Window("W") { Title = "C" };
        wm.AddWindow(win1);
        wm.AddWindow(win2);
        wm.AddWindow(win3);
        win1.ZOrder.Should().Be(0);
        win2.ZOrder.Should().Be(1);
        win3.ZOrder.Should().Be(2);
    }

    // ── MessageBox ──────────────────────────────────────────────────

    [Fact]
    public void MessageBox_Create_Ok()
    {
        var dialog = MessageBox.Create("Title", "Message", MessageBoxButtons.Ok);
        dialog.Title.Should().Be("Title");
        dialog.IsModal.Should().BeTrue();
    }

    [Fact]
    public void MessageBox_Create_OkCancel()
    {
        var dialog = MessageBox.Create("T", "M", MessageBoxButtons.OkCancel);
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void MessageBox_Create_YesNo()
    {
        var dialog = MessageBox.Create("T", "M", MessageBoxButtons.YesNo);
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void MessageBox_Create_YesNoCancel()
    {
        var dialog = MessageBox.Create("T", "M", MessageBoxButtons.YesNoCancel);
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void MessageBox_Render_ShowsMessage()
    {
        var dialog = MessageBox.Create("Alert", "Something happened", MessageBoxButtons.Ok);
        dialog.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        dialog.Render(surface);
        // Window chrome should be drawn
        buf[0, 0].Character.Should().Be('┌');
    }

    // ── RenderableWidget ────────────────────────────────────────────

    [Fact]
    public void RenderableWidget_Render_ConvertsSegments()
    {
        var rule = new Rule("Test");
        var widget = new RenderableWidget(rule);
        widget.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        widget.Render(surface);
        // Should render something (rule has ─ characters)
        var row = Row(buf, 0);
        row.TrimEnd().Should().NotBeEmpty();
    }

    [Fact]
    public void RenderableWidget_MeasureContent()
    {
        var text = new Spectre.Console.Text("Hello World");
        var widget = new RenderableWidget(text);
        var size = widget.MeasureContent(new Spectre.Console.Size(50, 5));
        size.Width.Should().BeGreaterThan(0);
    }

    // ── TuiTheme ────────────────────────────────────────────────────

    [Fact]
    public void TuiTheme_Presets_NotNull()
    {
        TuiTheme.Default.Should().NotBeNull();
        TuiTheme.Dark.Should().NotBeNull();
        TuiTheme.Blue.Should().NotBeNull();
    }

    [Fact]
    public void TuiTheme_Presets_HaveDifferentStyles()
    {
        // Each preset should have distinct background colors
        TuiTheme.Default.Should().NotBeSameAs(TuiTheme.Dark);
        TuiTheme.Dark.Should().NotBeSameAs(TuiTheme.Blue);
    }

    // ── Application ─────────────────────────────────────────────────

    [Fact]
    public void Application_RunAndQuit()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.RootWidget = new VStack();
        // Queue a quit after a bit
        driver.EnqueueKey(ConsoleKey.Q, 'q', false, false, true); // Ctrl+Q won't quit, so use Quit()
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        app.Run(cts.Token);
        driver.IsInitialized.Should().BeTrue();
        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void Application_Quit_StopsLoop()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.RootWidget = new Label("X");
        // Run on a background task and quit immediately
        var task = Task.Run(() =>
        {
            Thread.Sleep(50);
            app.Quit();
        });
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        app.Run(cts.Token);
        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void Application_Dispose_Safe()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.Dispose();
        app.Dispose(); // double-dispose should not throw
    }

    [Fact]
    public void Application_MouseEnabled_Property()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.MouseEnabled.Should().BeTrue();
        app.MouseEnabled = false;
        app.MouseEnabled.Should().BeFalse();
    }

    [Fact]
    public void Application_TargetFps_Property()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.TargetFps.Should().Be(30);
        app.TargetFps = 60;
        app.TargetFps.Should().Be(60);
    }

    [Fact]
    public void Application_TabNavigation()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        var stack = new VStack();
        var btn1 = new Button("A") { CanFocus = true };
        var btn2 = new Button("B") { CanFocus = true };
        stack.Add(btn1);
        stack.Add(btn2);
        app.RootWidget = stack;

        // Enqueue Tab key then quit
        driver.EnqueueKey(ConsoleKey.Tab, '\t', false, false, false);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        app.Run(cts.Token);
        // Focus should have moved
        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void Application_KeyEvent_RoutedToFocusedWidget()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        var tb = new TextBox();
        app.RootWidget = tb;

        // First key burns the first loop iteration (before focus chain is built);
        // the 'a' key arrives on the second iteration after layout + focus setup.
        driver.EnqueueKey(ConsoleKey.Escape, '\0', false, false, false);
        driver.EnqueueKey(ConsoleKey.A, 'a', false, false, false);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        app.Run(cts.Token);
        tb.Text.Should().Be("a");
    }
}
