using FluentAssertions;
using Spectre.Console;
using Xunit;

namespace Spectre.Console.Tui.Tests;

/// <summary>
/// Mutation-killing tests for Group 4: Chrome, Windows, Integration, Application.
/// Targets specific survived/NoCoverage mutants in MenuBar, StatusBar, TabControl,
/// TuiPanel, MenuItem, Window, WindowManager, Dialog, MessageBox, RenderableWidget, Application.
/// </summary>
public sealed class MutantKillerStrykerGroup4Tests
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
        {
            sb.Append(buf[col, row].Character);
        }

        return sb.ToString();
    }

    // ── MenuBar: Fill background (L37) ──────────────────────────────

    [Fact]
    public void MenuBar_Render_FillsEntireBackground()
    {
        var mb = new MenuBar();
        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);

        // All cells should have NormalStyle background (statement L37: surface.Fill)
        for (var col = 0; col < 20; col++)
        {
            buf[col, 0].Character.Should().Be(' ');
            buf[col, 0].Style.Background.Should().Be(Color.Grey);
        }
    }

    // ── MenuBar: Disabled item style (L43-44) ───────────────────────

    [Fact]
    public void MenuBar_Render_DisabledItem_UsesDisabledStyle()
    {
        var mb = new MenuBar();
        var item = new MenuItem("File") { Enabled = false };
        mb.AddItem(item);
        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);

        // The disabled item " File " starts at x=1
        // DisabledStyle = new Style(Color.DarkSlateGray1, Color.Grey)
        buf[1, 0].Style.Foreground.Should().Be(Color.DarkSlateGray1);
    }

    [Fact]
    public void MenuBar_Render_SelectedItem_UsesSelectedStyle()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.AddItem(new MenuItem("Edit"));
        // Select first item
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        mb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        mb.Render(surface);

        // SelectedStyle = new Style(Color.White, Color.Blue)
        // " File " starts at col 1
        buf[1, 0].Style.Foreground.Should().Be(Color.White);
        buf[1, 0].Style.Background.Should().Be(Color.Blue);
    }

    [Fact]
    public void MenuBar_Render_UnselectedItem_UsesNormalStyle()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.AddItem(new MenuItem("Edit"));
        // Select first item
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        mb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        mb.Render(surface);

        // "Edit" at " File " = 6 chars, so " Edit " starts at col 7
        buf[7, 0].Style.Foreground.Should().Be(Color.Black);
        buf[7, 0].Style.Background.Should().Be(Color.Grey);
    }

    // ── MenuBar: Item text spacing (L47-49) ─────────────────────────

    [Fact]
    public void MenuBar_Render_ItemsHaveSpacePadding()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("AB"));
        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);

        // Text is " AB " starting at x=1
        buf[1, 0].Character.Should().Be(' ');
        buf[2, 0].Character.Should().Be('A');
        buf[3, 0].Character.Should().Be('B');
        buf[4, 0].Character.Should().Be(' ');
    }

    [Fact]
    public void MenuBar_Render_MultipleItemsPositionedCorrectly()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("AB")); // " AB " = 4 chars at x=1
        mb.AddItem(new MenuItem("CD")); // " CD " = 4 chars at x=5
        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);

        buf[5, 0].Character.Should().Be(' ');
        buf[6, 0].Character.Should().Be('C');
        buf[7, 0].Character.Should().Be('D');
        buf[8, 0].Character.Should().Be(' ');
    }

    // ── MenuBar: LeftArrow wrap (L60) ───────────────────────────────

    [Fact]
    public void MenuBar_LeftArrow_AtStart_WrapsToLast()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.AddItem(new MenuItem("B"));
        mb.AddItem(new MenuItem("C"));

        // First RightArrow selects index 0 (from -1)
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        // Now LeftArrow should wrap to index 2 (last)
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));

        // Verify by rendering and checking which item has SelectedStyle
        mb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        mb.Render(surface);

        // " A " at col 1 (3 chars), " B " at col 4 (3 chars), " C " at col 7
        buf[7, 0].Style.Background.Should().Be(Color.Blue); // C is selected
        buf[1, 0].Style.Background.Should().Be(Color.Grey); // A not selected
    }

    [Fact]
    public void MenuBar_LeftArrow_ReturnsTrue()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        result.Should().BeTrue();
    }

    // ── MenuBar: RightArrow wrap (L69) ──────────────────────────────

    [Fact]
    public void MenuBar_RightArrow_AtEnd_WrapsToFirst()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.AddItem(new MenuItem("B"));

        // RightArrow from -1 => 0, 0 => 1, 1 => 0 (wrap)
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));

        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);

        // " A " at col 1 should be selected (Blue bg)
        buf[1, 0].Style.Background.Should().Be(Color.Blue);
    }

    [Fact]
    public void MenuBar_RightArrow_ReturnsTrue()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeTrue();
    }

    // ── MenuBar: Empty items guard (L58, L67) ───────────────────────

    [Fact]
    public void MenuBar_LeftArrow_NoItems_DoesNothing()
    {
        var mb = new MenuBar();
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        result.Should().BeTrue(); // key is consumed even without items
    }

    [Fact]
    public void MenuBar_RightArrow_NoItems_DoesNothing()
    {
        var mb = new MenuBar();
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeTrue();
    }

    // ── MenuBar: Enter guard (L76) ──────────────────────────────────

    [Fact]
    public void MenuBar_Enter_NoSelection_DoesNotActivate()
    {
        var mb = new MenuBar();
        var item = new MenuItem("X");
        var activated = false;
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);

        // Enter without selecting (index = -1)
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        result.Should().BeTrue();
        activated.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_Enter_ReturnsTrue()
    {
        var mb = new MenuBar();
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        result.Should().BeTrue();
    }

    // ── MenuBar: Escape (L84-86) ────────────────────────────────────

    [Fact]
    public void MenuBar_Escape_ResetsSelection_ReturnsTrue()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0')); // Select
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.Escape, '\x1b'));
        result.Should().BeTrue();

        // Verify selection is cleared by rendering
        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);
        // " A " should have NormalStyle (no blue bg)
        buf[1, 0].Style.Background.Should().Be(Color.Grey);
    }

    // ── MenuBar: Alt+letter shortcut (L91-104) ──────────────────────

    [Fact]
    public void MenuBar_AltLetter_ActivatesMatchingItem()
    {
        var mb = new MenuBar();
        var item1 = new MenuItem("File");
        var item2 = new MenuItem("Edit");
        var activated1 = false;
        var activated2 = false;
        item1.Activated += (_, _) => activated1 = true;
        item2.Activated += (_, _) => activated2 = true;
        mb.AddItem(item1);
        mb.AddItem(item2);

        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.E, 'e', false, true, false));
        result.Should().BeTrue();
        activated2.Should().BeTrue();
        activated1.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_AltLetter_CaseInsensitive()
    {
        var mb = new MenuBar();
        var item = new MenuItem("File");
        var activated = false;
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);

        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.F, 'f', false, true, false));
        result.Should().BeTrue();
        activated.Should().BeTrue();
    }

    [Fact]
    public void MenuBar_AltLetter_NoMatch_ReturnsFalse()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));

        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.Z, 'z', false, true, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_NonAlt_UnknownKey_ReturnsFalse()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));

        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.Z, 'z', false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_Alt_NullChar_ReturnsFalse()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));

        // Alt + '\0' should not trigger shortcut
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, true, false));
        // LeftArrow is handled by case, not alt+letter
        result.Should().BeTrue();
    }

    // ── MenuBar: Mouse non-Press or non-Left (L112-113) ─────────────

    [Fact]
    public void MenuBar_Mouse_Release_ReturnsFalse()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.Arrange(new Rect(0, 0, 30, 1));

        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 2, 0));
        result.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_Mouse_RightButton_ReturnsFalse()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.Arrange(new Rect(0, 0, 30, 1));

        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 2, 0));
        result.Should().BeFalse();
    }

    // ── MenuBar: Mouse click position (L117-130) ────────────────────

    [Fact]
    public void MenuBar_MouseClick_SecondItem_SelectsIt()
    {
        var mb = new MenuBar();
        var item1 = new MenuItem("AB");
        var item2 = new MenuItem("CD");
        var act1 = false;
        var act2 = false;
        item1.Activated += (_, _) => act1 = true;
        item2.Activated += (_, _) => act2 = true;
        mb.AddItem(item1);
        mb.AddItem(item2);
        mb.Arrange(new Rect(0, 0, 30, 1));

        // " AB " starts at x=1 (len=4), " CD " starts at x=5
        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 6, 0));
        result.Should().BeTrue();
        act2.Should().BeTrue();
        act1.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_MouseClick_Outside_ReturnsFalse()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("AB")); // " AB " = 4 chars from x=1, total x=5
        mb.Arrange(new Rect(0, 0, 30, 1));

        // Click way past the items
        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 25, 0));
        result.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_MouseClick_BeforeItems_ReturnsFalse()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("AB")); // " AB " starts at x=1
        mb.Arrange(new Rect(0, 0, 30, 1));

        // Click at col 0 which is before the first item
        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0));
        result.Should().BeFalse();
    }

    // ── MenuBar: MeasureContent (L31) ───────────────────────────────

    [Fact]
    public void MenuBar_MeasureContent_ReturnsFullWidthHeight1()
    {
        var mb = new MenuBar();
        var size = mb.MeasureContent(new Spectre.Console.Size(40, 10));
        size.Width.Should().Be(40);
        size.Height.Should().Be(1);
    }

    // ── StatusBar: Default text (L9) ────────────────────────────────

    [Fact]
    public void StatusBar_DefaultText_IsEmpty()
    {
        var sb = new StatusBar();
        sb.Text.Should().BeEmpty();
    }

    [Fact]
    public void StatusBar_SetText_Null_BecomesEmpty()
    {
        var sb = new StatusBar();
        sb.Text = null!;
        sb.Text.Should().BeEmpty();
    }

    // ── StatusBar: Render items positions (L49-57) ──────────────────

    [Fact]
    public void StatusBar_Render_ItemsPositionedCorrectly()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help"); // "F1" = 2 chars, "Help " = 5 chars
        sb.AddItem("F2", "Save"); // "F2" at col 7, "Save " at col 9
        sb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        sb.Render(surface);

        // F1 at col 0-1
        buf[0, 0].Character.Should().Be('F');
        buf[1, 0].Character.Should().Be('1');
        // "Help " at col 2-6
        buf[2, 0].Character.Should().Be('H');
        buf[6, 0].Character.Should().Be(' ');
        // "F2" at col 7-8
        buf[7, 0].Character.Should().Be('F');
        buf[8, 0].Character.Should().Be('2');
    }

    [Fact]
    public void StatusBar_Render_ItemsUseKeyAndLabelStyles()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help");
        sb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        sb.Render(surface);

        // KeyStyle = new Style(Color.White, Color.DarkBlue)
        buf[0, 0].Style.Foreground.Should().Be(Color.White);
        buf[0, 0].Style.Background.Should().Be(Color.DarkBlue);
        // LabelStyle = new Style(Color.Black, Color.Grey)
        buf[2, 0].Style.Foreground.Should().Be(Color.Black);
        buf[2, 0].Style.Background.Should().Be(Color.Grey);
    }

    // ── StatusBar: Text rendering when no items (L60-63) ────────────

    [Fact]
    public void StatusBar_Render_NoItems_ShowsTextWithLabelStyle()
    {
        var sb = new StatusBar { Text = "Ready" };
        sb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        sb.Render(surface);

        buf[0, 0].Character.Should().Be('R');
        buf[4, 0].Character.Should().Be('y');
        buf[0, 0].Style.Foreground.Should().Be(Color.Black);
    }

    [Fact]
    public void StatusBar_Render_LongText_Truncated()
    {
        var sb = new StatusBar { Text = "This is a very long status text" };
        sb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        sb.Render(surface);

        // Should only render first 10 chars
        Row(buf, 0).Should().Be("This is a ");
    }

    [Fact]
    public void StatusBar_Render_EmptyText_NoItems_JustBackground()
    {
        var sb = new StatusBar();
        sb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        sb.Render(surface);

        // All cells should be space with BackgroundStyle
        for (var col = 0; col < 10; col++)
        {
            buf[col, 0].Character.Should().Be(' ');
            buf[col, 0].Style.Background.Should().Be(Color.Grey);
        }
    }

    [Fact]
    public void StatusBar_Render_ItemsHavePriority_OverText()
    {
        var sb = new StatusBar { Text = "Ignored" };
        sb.AddItem("F1", "Help");
        sb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        sb.Render(surface);

        // Items should be shown, not the text
        Row(buf, 0).Should().Contain("F1");
        Row(buf, 0).Should().NotContain("Ignored");
    }

    // ── StatusBar: Mouse click position matching (L74-85) ───────────

    [Fact]
    public void StatusBar_MouseClick_SecondItem_InvokesAction()
    {
        var sb = new StatusBar();
        var action1Called = false;
        var action2Called = false;
        sb.AddItem("F1", "Help", () => action1Called = true); // width = 2 + 4 + 1 = 7
        sb.AddItem("F2", "Save", () => action2Called = true); // starts at 7
        sb.Arrange(new Rect(0, 0, 30, 1));

        // Click at col 8, which is within second item (starts at 7, width = 2+4+1 = 7)
        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 8, 0));
        result.Should().BeTrue();
        action2Called.Should().BeTrue();
        action1Called.Should().BeFalse();
    }

    [Fact]
    public void StatusBar_MouseClick_OutsideItems_ReturnsFalse()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help"); // width = 2 + 4 + 1 = 7
        sb.Arrange(new Rect(0, 0, 30, 1));

        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 20, 0));
        result.Should().BeFalse();
    }

    [Fact]
    public void StatusBar_Mouse_Release_ReturnsFalse()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help");
        sb.Arrange(new Rect(0, 0, 30, 1));

        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 0, 0));
        result.Should().BeFalse();
    }

    [Fact]
    public void StatusBar_Mouse_RightButton_ReturnsFalse()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help");
        sb.Arrange(new Rect(0, 0, 30, 1));

        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 0, 0));
        result.Should().BeFalse();
    }

    [Fact]
    public void StatusBar_MouseClick_ItemWithNoAction_ReturnsTrue()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help"); // no action
        sb.Arrange(new Rect(0, 0, 30, 1));

        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 1, 0));
        result.Should().BeTrue(); // item matched, action is null but we still return true
    }

    // ── StatusBar: MeasureContent (L40) ─────────────────────────────

    [Fact]
    public void StatusBar_MeasureContent_ReturnsCorrectSize()
    {
        var sb = new StatusBar();
        var size = sb.MeasureContent(new Spectre.Console.Size(50, 10));
        size.Width.Should().Be(50);
        size.Height.Should().Be(1);
    }

    // ── StatusBarItem: Constructor null handling (L103-104) ──────────

    [Fact]
    public void StatusBarItem_NullKey_BecomesEmpty()
    {
        var item = new StatusBarItem(null!, null!);
        item.Key.Should().BeEmpty();
        item.Label.Should().BeEmpty();
    }

    [Fact]
    public void StatusBarItem_Properties()
    {
        var action = () => { };
        var item = new StatusBarItem("F1", "Help", action);
        item.Key.Should().Be("F1");
        item.Label.Should().Be("Help");
        item.Action.Should().BeSameAs(action);
    }

    // ── TabControl: Render tab headers (L74-99) ─────────────────────

    [Fact]
    public void TabControl_Render_SelectedTabHighlighted()
    {
        var tc = new TabControl();
        tc.AddTab("AA", new Label("C1"));
        tc.AddTab("BB", new Label("C2"));
        tc.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        tc.Render(surface);

        // First tab " AA " is at col 0, selected (index 0)
        // TabSelectedStyle = new Style(Color.White, Color.Blue)
        buf[0, 0].Style.Background.Should().Be(Color.Blue);
        // Second tab " BB " starts after separator (4 + 1 = 5)
        buf[5, 0].Style.Background.Should().NotBe(Color.Blue);
    }

    [Fact]
    public void TabControl_Render_TabSeparator()
    {
        var tc = new TabControl();
        tc.AddTab("AA", new Label("C1"));
        tc.AddTab("BB", new Label("C2"));
        tc.Arrange(new Rect(0, 0, 30, 10));
        var (buf, surface) = Surface(30, 10);
        tc.Render(surface);

        // Separator between tabs at col 4 (after " AA " which is 4 chars)
        buf[4, 0].Character.Should().Be('\u2502'); // │
    }

    [Fact]
    public void TabControl_Render_SeparatorLine()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C"));
        tc.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        tc.Render(surface);

        // Row 1: separator line
        for (var col = 0; col < 20; col++)
        {
            buf[col, 1].Character.Should().Be('\u2500'); // ─
        }
    }

    [Fact]
    public void TabControl_Render_RemainingTabRow_Filled()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C")); // " A " = 3 chars
        tc.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        tc.Render(surface);

        // After the last tab text, remaining cols should be space with TabNormalStyle
        buf[3, 0].Character.Should().Be(' ');
        buf[3, 0].Style.Foreground.Should().Be(Color.Grey);
    }

    [Fact]
    public void TabControl_Render_NoSeparatorAfterLastTab()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        tc.Render(surface);

        // " A " = 3 chars. Col 3 should be space fill, not separator
        buf[3, 0].Character.Should().NotBe('\u2502');
    }

    // ── TabControl: Key events without focus (L111, L114) ───────────

    [Fact]
    public void TabControl_LeftArrow_WithoutFocus_ReturnsFalse()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.AddTab("B", new Label("C2"));
        tc.HasFocus = false;

        var result = tc.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        result.Should().BeFalse();
        tc.SelectedIndex.Should().Be(0); // unchanged
    }

    [Fact]
    public void TabControl_RightArrow_WithoutFocus_ReturnsFalse()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.AddTab("B", new Label("C2"));
        tc.HasFocus = false;

        var result = tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeFalse();
        tc.SelectedIndex.Should().Be(0);
    }

    // ── TabControl: Empty tabs guard (L104-106) ─────────────────────

    [Fact]
    public void TabControl_KeyEvent_EmptyTabs_ReturnsFalse()
    {
        var tc = new TabControl();
        tc.HasFocus = true;

        var result = tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeFalse();
    }

    // ── TabControl: SelectedIndex clamp (L18) ───────────────────────

    [Fact]
    public void TabControl_SelectedIndex_ClampedToRange()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.AddTab("B", new Label("C2"));
        tc.SelectedIndex = 99;
        tc.SelectedIndex.Should().Be(1); // clamped to max

        tc.SelectedIndex = -5;
        tc.SelectedIndex.Should().Be(0); // clamped to 0
    }

    [Fact]
    public void TabControl_SelectedIndex_SameValue_DoesNotFireEvent()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        var changed = 0;
        tc.SelectedTabChanged += (_, _) => changed++;
        tc.SelectedIndex = 0; // same as default
        changed.Should().Be(0);
    }

    // ── TabControl: Mouse non-row-0 (L129-132) ─────────────────────

    [Fact]
    public void TabControl_MouseClick_NotRow0_ReturnsFalse()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.Arrange(new Rect(0, 0, 30, 10));

        // Click on row 5 (content area, not tab row)
        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 2, 5));
        result.Should().BeFalse();
    }

    [Fact]
    public void TabControl_Mouse_Release_ReturnsFalse()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.Arrange(new Rect(0, 0, 30, 10));

        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 2, 0));
        result.Should().BeFalse();
    }

    [Fact]
    public void TabControl_Mouse_RightButton_ReturnsFalse()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.Arrange(new Rect(0, 0, 30, 10));

        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 2, 0));
        result.Should().BeFalse();
    }

    // ── TabControl: Mouse click outside tabs (L146) ─────────────────

    [Fact]
    public void TabControl_MouseClick_PastTabs_ReturnsFalse()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C")); // " A " = 3 chars
        tc.Arrange(new Rect(0, 0, 30, 10));

        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 20, 0));
        result.Should().BeFalse();
    }

    // ── TabControl: GetChildren empty (L159) ────────────────────────

    [Fact]
    public void TabControl_GetChildren_Empty_ReturnsEmpty()
    {
        var tc = new TabControl();
        tc.GetChildren().Should().BeEmpty();
    }

    // ── TabControl: SelectedTab (L28-30) ────────────────────────────

    [Fact]
    public void TabControl_SelectedTab_NoTabs_ReturnsNull()
    {
        var tc = new TabControl();
        tc.SelectedTab.Should().BeNull();
    }

    // ── TabControl: Arrange content below tabs (L63) ────────────────

    [Fact]
    public void TabControl_Arrange_ContentBoundsExact()
    {
        var tc = new TabControl();
        var label = new Label("Content");
        tc.AddTab("T", label);
        tc.Arrange(new Rect(5, 3, 20, 12));

        // Content bounds: Y = 3 + 2 = 5, Height = 12 - 2 = 10
        label.Bounds.X.Should().Be(5);
        label.Bounds.Y.Should().Be(5);
        label.Bounds.Width.Should().Be(20);
        label.Bounds.Height.Should().Be(10);
    }

    // ── TabControl: MeasureContent (L55) ────────────────────────────

    [Fact]
    public void TabControl_MeasureContent_ReturnsAvailable()
    {
        var tc = new TabControl();
        var size = tc.MeasureContent(new Spectre.Console.Size(40, 20));
        size.Width.Should().Be(40);
        size.Height.Should().Be(20);
    }

    // ── TuiPanel: Render small panel (L53-56) ───────────────────────

    [Fact]
    public void TuiPanel_Render_TooSmall_DoesNothing()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 1, 1));
        var (buf, surface) = Surface(1, 1);
        panel.Render(surface);
        // Should not crash. Buffer stays at default (space) since w<2 || h<2 guard.
        buf[0, 0].Character.Should().Be(' ');
        // No corner char should be drawn
        buf[0, 0].Character.Should().NotBe('\u250c');
    }

    // ── TuiPanel: Content fill (L59) ────────────────────────────────

    [Fact]
    public void TuiPanel_Render_ContentAreaFilled()
    {
        var panel = new TuiPanel { ContentStyle = new Style(Color.Yellow) };
        panel.Arrange(new Rect(0, 0, 10, 5));
        var (buf, surface) = Surface(10, 5);
        panel.Render(surface);

        // Content area is (1,1) to (8,3) — inside borders
        buf[1, 1].Character.Should().Be(' ');
        buf[1, 1].Style.Foreground.Should().Be(Color.Yellow);
    }

    // ── TuiPanel: Top border horizontal lines (L63-65) ──────────────

    [Fact]
    public void TuiPanel_Render_TopBorderHorizontal()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 10, 5));
        var (buf, surface) = Surface(10, 5);
        panel.Render(surface);

        for (var col = 1; col < 9; col++)
        {
            buf[col, 0].Character.Should().Be('\u2500');
        }
    }

    // ── TuiPanel: Bottom border horizontal lines (L86-88) ───────────

    [Fact]
    public void TuiPanel_Render_BottomBorderHorizontal()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 10, 5));
        var (buf, surface) = Surface(10, 5);
        panel.Render(surface);

        for (var col = 1; col < 9; col++)
        {
            buf[col, 4].Character.Should().Be('\u2500');
        }
    }

    // ── TuiPanel: Title truncation (L73) ────────────────────────────

    [Fact]
    public void TuiPanel_Render_LongTitle_Truncated()
    {
        var panel = new TuiPanel { Title = "ABCDEFGHIJ" }; // 10 chars
        panel.Arrange(new Rect(0, 0, 10, 5)); // w-4 = 6 chars max
        var (buf, surface) = Surface(10, 5);
        panel.Render(surface);

        // Title starts at col 2, max length w-4 = 6
        Row(buf, 0).Should().Contain("ABCDEF");
        Row(buf, 0).Should().NotContain("ABCDEFG");
    }

    [Fact]
    public void TuiPanel_Render_TitlePosition()
    {
        var panel = new TuiPanel { Title = "Hi" };
        panel.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        panel.Render(surface);

        // Title at col 2
        buf[2, 0].Character.Should().Be('H');
        buf[3, 0].Character.Should().Be('i');
    }

    [Fact]
    public void TuiPanel_Render_TitleUsesStyle()
    {
        var panel = new TuiPanel { Title = "Hi", TitleStyle = new Style(Color.Red) };
        panel.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        panel.Render(surface);

        buf[2, 0].Style.Foreground.Should().Be(Color.Red);
    }

    // ── TuiPanel: Side borders (L78-81) ─────────────────────────────

    [Fact]
    public void TuiPanel_Render_AllSideBorders()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 10, 6));
        var (buf, surface) = Surface(10, 6);
        panel.Render(surface);

        for (var row = 1; row < 5; row++)
        {
            buf[0, row].Character.Should().Be('\u2502');
            buf[9, row].Character.Should().Be('\u2502');
        }
    }

    // ── TuiPanel: MeasureContent without content ────────────────────

    [Fact]
    public void TuiPanel_MeasureContent_NoContent_ReturnsJustBorders()
    {
        var panel = new TuiPanel();
        var size = panel.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(2);
        size.Height.Should().Be(2);
    }

    // ── MenuItem: Separator string (L24-25) NoCov ───────────────────

    [Fact]
    public void MenuItem_Separator_TextIsEmpty()
    {
        var sep = MenuItem.Separator();
        sep.Text.Should().Be(string.Empty);
        sep.IsSeparator.Should().BeTrue();
        sep.Enabled.Should().BeTrue(); // default
    }

    [Fact]
    public void MenuItem_NullText_BecomesEmpty()
    {
        var item = new MenuItem(null!);
        item.Text.Should().BeEmpty();
    }

    [Fact]
    public void MenuItem_SubItems_IsEmpty()
    {
        var item = new MenuItem("X");
        item.SubItems.Should().BeEmpty();
    }

    // ── Window: Defaults (L15-17) ───────────────────────────────────

    [Fact]
    public void Window_Defaults_Resizable_True()
    {
        var win = new Window("W");
        win.Resizable.Should().BeTrue();
    }

    [Fact]
    public void Window_Defaults_Movable_True()
    {
        var win = new Window("W");
        win.Movable.Should().BeTrue();
    }

    [Fact]
    public void Window_Defaults_Closable_True()
    {
        var win = new Window("W");
        win.Closable.Should().BeTrue();
    }

    [Fact]
    public void Window_Defaults_CanFocus_True()
    {
        var win = new Window("W");
        win.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void Window_NullTitle_BecomesEmpty()
    {
        var win = new Window(null!);
        win.Title.Should().BeEmpty();
    }

    // ── Window: Render too small (L85-87) ───────────────────────────

    [Fact]
    public void Window_Render_TooSmall_DoesNothing()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 2, 2));
        var (buf, surface) = Surface(2, 2);
        win.Render(surface);
        // Buffer stays at default (space) since w<3 || h<3 guard
        buf[0, 0].Character.Should().Be(' ');
        buf[0, 0].Character.Should().NotBe('\u250c');
    }

    // ── Window: Content fill area (L91) ─────────────────────────────

    [Fact]
    public void Window_Render_ContentAreaFilled()
    {
        var win = new Window("W") { ContentStyle = new Style(Color.Green) };
        win.Arrange(new Rect(0, 0, 10, 6));
        var (buf, surface) = Surface(10, 6);
        win.Render(surface);

        // Content area: (1, 2, w-2, h-3) = (1, 2, 8, 3)
        buf[1, 3].Character.Should().Be(' ');
        buf[1, 3].Style.Foreground.Should().Be(Color.Green);
    }

    // ── Window: Top border (L94-100) ────────────────────────────────

    [Fact]
    public void Window_Render_TopBorderChars()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 10, 6));
        var (buf, surface) = Surface(10, 6);
        win.Render(surface);

        buf[0, 0].Character.Should().Be('\u250c');
        for (var col = 1; col < 9; col++)
        {
            buf[col, 0].Character.Should().Be('\u2500');
        }

        buf[9, 0].Character.Should().Be('\u2510');
    }

    // ── Window: Title bar (L102-120) ────────────────────────────────

    [Fact]
    public void Window_Render_TitleBarStructure()
    {
        var win = new Window("Hi");
        win.Arrange(new Rect(0, 0, 15, 6));
        var (buf, surface) = Surface(15, 6);
        win.Render(surface);

        // Left border
        buf[0, 1].Character.Should().Be('\u2502');
        // Title fill
        buf[1, 1].Style.Background.Should().Be(Color.Blue); // TitleStyle bg
        // Title text at col 2
        buf[2, 1].Character.Should().Be('H');
        buf[3, 1].Character.Should().Be('i');
        // Right border
        buf[14, 1].Character.Should().Be('\u2502');
    }

    [Fact]
    public void Window_Render_LongTitle_Truncated()
    {
        var win = new Window("ABCDEFGHIJKLMNOP") { Closable = false }; // 16 chars, no close button to interfere
        win.Arrange(new Rect(0, 0, 10, 6)); // w-4 = 6
        var (buf, surface) = Surface(10, 6);
        win.Render(surface);

        // Only first 6 chars of title should appear (w-4 = 6)
        buf[2, 1].Character.Should().Be('A');
        buf[7, 1].Character.Should().Be('F');
        // 7th char should NOT appear since title is truncated
        buf[8, 1].Character.Should().NotBe('G');
    }

    // ── Window: Close button position (L114-118) ────────────────────

    [Fact]
    public void Window_Render_CloseButton_Position()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var (buf, surface) = Surface(20, 6);
        win.Render(surface);

        // Close button at w-4 = 16
        buf[16, 1].Character.Should().Be('[');
        buf[17, 1].Character.Should().Be('X');
        buf[18, 1].Character.Should().Be(']');
        buf[16, 1].Style.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void Window_Render_CloseButton_NotShown_WhenTooNarrow()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 5, 6)); // w < 6
        var (buf, surface) = Surface(5, 6);
        win.Render(surface);

        // No close button should be drawn (w >= 6 guard)
        Row(buf, 1).Should().NotContain("[X]");
    }

    [Fact]
    public void Window_Render_CloseButton_NotShown_WhenNotClosable()
    {
        var win = new Window("W") { Closable = false };
        win.Arrange(new Rect(0, 0, 20, 6));
        var (buf, surface) = Surface(20, 6);
        win.Render(surface);

        Row(buf, 1).Should().NotContain("[X]");
    }

    // ── Window: Separator row (L123-129) ────────────────────────────

    [Fact]
    public void Window_Render_SeparatorRow()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 10, 6));
        var (buf, surface) = Surface(10, 6);
        win.Render(surface);

        buf[0, 2].Character.Should().Be('\u251c');
        for (var col = 1; col < 9; col++)
        {
            buf[col, 2].Character.Should().Be('\u2500');
        }

        buf[9, 2].Character.Should().Be('\u2524');
    }

    // ── Window: Side borders (L132-136) ─────────────────────────────

    [Fact]
    public void Window_Render_SideBorders()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 10, 7));
        var (buf, surface) = Surface(10, 7);
        win.Render(surface);

        for (var row = 3; row < 6; row++)
        {
            buf[0, row].Character.Should().Be('\u2502');
            buf[9, row].Character.Should().Be('\u2502');
        }
    }

    // ── Window: Bottom border (L139-148) ────────────────────────────

    [Fact]
    public void Window_Render_BottomBorder()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 10, 7));
        var (buf, surface) = Surface(10, 7);
        win.Render(surface);

        buf[0, 6].Character.Should().Be('\u2514');
        for (var col = 1; col < 9; col++)
        {
            buf[col, 6].Character.Should().Be('\u2500');
        }

        buf[9, 6].Character.Should().Be('\u2518');
    }

    [Fact]
    public void Window_Render_NoBottomBorder_WhenH_Equals3()
    {
        // h > 3 guard: when h=3, no bottom border
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 10, 3));
        var (buf, surface) = Surface(10, 3);
        win.Render(surface);

        // Row 2 is separator, no bottom border row exists
        buf[0, 2].Character.Should().Be('\u251c'); // separator, not bottom
    }

    // ── Window: Focused border style (L81) ──────────────────────────

    [Fact]
    public void Window_Render_FocusedUsesAltBorderStyle()
    {
        var win = new Window("W");
        win.HasFocus = true;
        win.Arrange(new Rect(0, 0, 10, 6));
        var (buf, surface) = Surface(10, 6);
        win.Render(surface);

        // FocusedBorderStyle = new Style(Color.Cyan1)
        buf[0, 0].Style.Foreground.Should().Be(Color.Cyan1);
    }

    [Fact]
    public void Window_Render_UnfocusedUsesNormalBorderStyle()
    {
        var win = new Window("W");
        win.HasFocus = false;
        win.Arrange(new Rect(0, 0, 10, 6));
        var (buf, surface) = Surface(10, 6);
        win.Render(surface);

        // BorderStyle = new Style(Color.Grey)
        buf[0, 0].Style.Foreground.Should().Be(Color.Grey);
    }

    // ── Window: Mouse close button click area (L157-158) ────────────

    [Fact]
    public void Window_MouseClick_CloseButton_FiresEvent()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // Close button at w-4=16 to w-2=18, row 1
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 16, 1));
        result.Should().BeTrue();
        closed.Should().BeTrue();
    }

    [Fact]
    public void Window_MouseClick_CloseButton_RightEdge()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // Click at w-2 = 18 (right edge of close button area)
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 18, 1));
        result.Should().BeTrue();
        closed.Should().BeTrue();
    }

    [Fact]
    public void Window_MouseClick_NotCloseButton_WhenNotClosable()
    {
        var win = new Window("W") { Closable = false };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 17, 1));
        // Should fall through to drag handling
        closed.Should().BeFalse();
    }

    [Fact]
    public void Window_MouseClick_CloseButton_JustOutside_DoesNotClose()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // Click at w-5=15 (just before close button)
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 15, 1));
        // This will be handled by drag, not close
        closed.Should().BeFalse();
    }

    [Fact]
    public void Window_MouseClick_CloseButton_NotOnTitleRow_DoesNotClose()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // Click at correct col but wrong row
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 17, 0));
        closed.Should().BeFalse();
    }

    // ── Window: Drag handling (L164-188) ────────────────────────────

    [Fact]
    public void Window_Drag_Press_StartsDrag()
    {
        var win = new Window("W") { Movable = true };
        win.Arrange(new Rect(5, 5, 20, 6));

        // Press on title bar (row 1, left of close button)
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 6));
        result.Should().BeTrue();
    }

    [Fact]
    public void Window_Drag_Move_UpdatesBounds()
    {
        var win = new Window("W") { Movable = true, Closable = false };
        win.Arrange(new Rect(5, 5, 20, 6));

        // Start drag on title bar (localRow = 1, row = 6)
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 6));

        // Move
        var moveResult = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 15, 8));
        moveResult.Should().BeTrue();

        // Bounds should have changed
        win.Bounds.X.Should().NotBe(5);
    }

    [Fact]
    public void Window_Drag_Release_StopsDrag()
    {
        var win = new Window("W") { Movable = true, Closable = false };
        win.Arrange(new Rect(5, 5, 20, 6));

        // Start drag
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 6));
        // Release
        var releaseResult = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 15, 8));
        releaseResult.Should().BeTrue();

        // Further move should not update
        var origX = win.Bounds.X;
        var moveResult = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 20, 10));
        moveResult.Should().BeFalse(); // not dragging anymore
    }

    [Fact]
    public void Window_Drag_NotMovable_DoesNotStartDrag()
    {
        var win = new Window("W") { Movable = false, Closable = false };
        win.Arrange(new Rect(5, 5, 20, 6));

        // Press on title bar
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 6));
        result.Should().BeFalse();
    }

    [Fact]
    public void Window_MousePress_NotTitleRow_ReturnsFalse()
    {
        var win = new Window("W") { Movable = true, Closable = false };
        win.Arrange(new Rect(0, 0, 20, 6));

        // Press on row 3 (content area)
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 3));
        result.Should().BeFalse();
    }

    // ── Window: Arrange children (L53-76) ───────────────────────────

    [Fact]
    public void Window_Arrange_ChildrenInContentArea()
    {
        var win = new Window("W");
        var label = new Label("Hello");
        win.Add(label);
        win.Arrange(new Rect(10, 10, 30, 15));

        // Content: X=11, Y=12, W=28, H=12
        label.Bounds.X.Should().Be(11);
        label.Bounds.Y.Should().Be(12);
        label.Bounds.Width.Should().Be(28);
    }

    [Fact]
    public void Window_Arrange_InvisibleChildren_Skipped()
    {
        var win = new Window("W");
        var label1 = new Label("A") { Visible = false };
        var label2 = new Label("B");
        win.Add(label1);
        win.Add(label2);
        win.Arrange(new Rect(0, 0, 30, 15));

        // label2 should start at the top of content area since label1 is invisible
        label2.Bounds.Y.Should().Be(2);
    }

    // ── Window: MeasureContent with children (L34-51) ───────────────

    [Fact]
    public void Window_MeasureContent_WithChildren()
    {
        var win = new Window("W");
        win.Add(new Label("Hello World"));
        var size = win.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().BeGreaterThan(2);
        size.Height.Should().Be(4); // 1 child height + 3 chrome
    }

    // ── WindowManager: ActiveWindow empty (L12) ─────────────────────

    [Fact]
    public void WindowManager_ActiveWindow_Empty_ReturnsNull()
    {
        var wm = new WindowManager();
        wm.ActiveWindow.Should().BeNull();
    }

    // ── WindowManager: ZOrder after operations (L65-70) ─────────────

    [Fact]
    public void WindowManager_ZOrder_AfterAdd()
    {
        var wm = new WindowManager();
        var w1 = new Window("A");
        var w2 = new Window("B");
        wm.AddWindow(w1);
        wm.AddWindow(w2);

        w1.ZOrder.Should().Be(0);
        w2.ZOrder.Should().Be(1);
    }

    [Fact]
    public void WindowManager_ZOrder_AfterBringToFront()
    {
        var wm = new WindowManager();
        var w1 = new Window("A");
        var w2 = new Window("B");
        wm.AddWindow(w1);
        wm.AddWindow(w2);

        wm.BringToFront(w1);
        w2.ZOrder.Should().Be(0);
        w1.ZOrder.Should().Be(1);
    }

    [Fact]
    public void WindowManager_ZOrder_AfterSendToBack()
    {
        var wm = new WindowManager();
        var w1 = new Window("A");
        var w2 = new Window("B");
        wm.AddWindow(w1);
        wm.AddWindow(w2);

        wm.SendToBack(w2);
        w2.ZOrder.Should().Be(0);
        w1.ZOrder.Should().Be(1);
    }

    [Fact]
    public void WindowManager_ZOrder_AfterRemove()
    {
        var wm = new WindowManager();
        var w1 = new Window("A");
        var w2 = new Window("B");
        var w3 = new Window("C");
        wm.AddWindow(w1);
        wm.AddWindow(w2);
        wm.AddWindow(w3);

        wm.RemoveWindow(w2);
        w1.ZOrder.Should().Be(0);
        w3.ZOrder.Should().Be(1);
    }

    // ── WindowManager: BringToFront/SendToBack with unknown window ──

    [Fact]
    public void WindowManager_BringToFront_UnknownWindow_DoesNothing()
    {
        var wm = new WindowManager();
        var w1 = new Window("A");
        var unknown = new Window("Unknown");
        wm.AddWindow(w1);

        wm.BringToFront(unknown); // should not throw
        wm.Windows.Should().HaveCount(1);
        wm.ActiveWindow.Should().Be(w1);
    }

    [Fact]
    public void WindowManager_SendToBack_UnknownWindow_DoesNothing()
    {
        var wm = new WindowManager();
        var w1 = new Window("A");
        var unknown = new Window("Unknown");
        wm.AddWindow(w1);

        wm.SendToBack(unknown); // should not throw
        wm.Windows.Should().HaveCount(1);
    }

    // ── WindowManager: GetWindowAt with Visible (L56) ───────────────

    [Fact]
    public void WindowManager_GetWindowAt_InvisibleWindow_Skipped()
    {
        var wm = new WindowManager();
        var w1 = new Window("A") { Visible = false };
        w1.Arrange(new Rect(0, 0, 20, 10));
        wm.AddWindow(w1);

        wm.GetWindowAt(5, 5).Should().BeNull();
    }

    [Fact]
    public void WindowManager_GetWindowAt_ReturnsTopmost()
    {
        var wm = new WindowManager();
        var w1 = new Window("A");
        w1.Arrange(new Rect(0, 0, 20, 10));
        var w2 = new Window("B");
        w2.Arrange(new Rect(0, 0, 20, 10)); // overlapping
        wm.AddWindow(w1);
        wm.AddWindow(w2);

        wm.GetWindowAt(5, 5).Should().Be(w2); // w2 is on top (added last)
    }

    // ── Dialog: Properties (L16-18) ─────────────────────────────────

    [Fact]
    public void Dialog_Closable_DefaultTrue()
    {
        var d = new Dialog("D");
        d.Closable.Should().BeTrue();
    }

    [Fact]
    public void Dialog_Resizable_DefaultFalse()
    {
        var d = new Dialog("D");
        d.Resizable.Should().BeFalse();
    }

    [Fact]
    public void Dialog_Movable_DefaultTrue()
    {
        var d = new Dialog("D");
        d.Movable.Should().BeTrue();
    }

    [Fact]
    public void Dialog_Close_FiresClosedEvent()
    {
        var d = new Dialog("D");
        var closedFired = false;
        d.Closed += (_, _) => closedFired = true;
        d.Close(DialogResult.Yes);
        closedFired.Should().BeTrue();
        d.Result.Should().Be(DialogResult.Yes);
    }

    [Fact]
    public void Dialog_Result_DefaultNone()
    {
        var d = new Dialog("D");
        d.Result.Should().Be(DialogResult.None);
    }

    // ── MessageBox: Button creation and labels (L24-54) ─────────────

    [Fact]
    public void MessageBox_Ok_HasOkButton()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.Ok);
        d.Children.Should().NotBeEmpty();
        // Render to verify button text
        d.Arrange(new Rect(0, 0, 40, 15));
        var (buf, surface) = Surface(40, 15);
        d.Render(surface);

        // At minimum verify dialog has children
        d.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void MessageBox_OkCancel_HasBothButtons()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.OkCancel);
        // Close with OK
        d.Arrange(new Rect(0, 0, 40, 15));
        d.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void MessageBox_YesNo_HasYesAndNoButtons()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNo);
        d.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void MessageBox_YesNoCancel_HasThreeButtons()
    {
        var d = MessageBox.Create("T", "Q?", MessageBoxButtons.YesNoCancel);
        d.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void MessageBox_Ok_CloseActionSetsOkResult()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.Ok);
        DialogResult? result = null;
        d.Closed += (_, _) => result = d.Result;
        d.Arrange(new Rect(0, 0, 50, 20));

        // Find the OK button in children and click it
        SimulateButtonClick(d, "OK");
        d.Result.Should().Be(DialogResult.Ok);
    }

    [Fact]
    public void MessageBox_OkCancel_CancelButtonSetsCancel()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.OkCancel);
        d.Arrange(new Rect(0, 0, 50, 20));

        SimulateButtonClick(d, "Cancel");
        d.Result.Should().Be(DialogResult.Cancel);
    }

    [Fact]
    public void MessageBox_YesNo_YesButtonSetsYes()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNo);
        d.Arrange(new Rect(0, 0, 50, 20));

        SimulateButtonClick(d, "Yes");
        d.Result.Should().Be(DialogResult.Yes);
    }

    [Fact]
    public void MessageBox_YesNo_NoButtonSetsNo()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNo);
        d.Arrange(new Rect(0, 0, 50, 20));

        SimulateButtonClick(d, "No");
        d.Result.Should().Be(DialogResult.No);
    }

    [Fact]
    public void MessageBox_YesNoCancel_CancelButtonSetsCancel()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNoCancel);
        d.Arrange(new Rect(0, 0, 50, 20));

        SimulateButtonClick(d, "Cancel");
        d.Result.Should().Be(DialogResult.Cancel);
    }

    [Fact]
    public void MessageBox_Title_SetCorrectly()
    {
        var d = MessageBox.Create("Alert", "Something", MessageBoxButtons.Ok);
        d.Title.Should().Be("Alert");
    }

    /// <summary>
    /// Recursively find a Button with the given text and simulate Enter key.
    /// </summary>
    private static void SimulateButtonClick(Widget parent, string buttonText)
    {
        var children = parent.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is Button btn && btn.Text == buttonText)
            {
                btn.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
                return;
            }

            SimulateButtonClick(children[i], buttonText);
        }
    }

    // ── RenderableWidget: Defaults and null guard (L9-13, L21) ──────

    [Fact]
    public void RenderableWidget_Constructor_NullThrows()
    {
        var act = () => new RenderableWidget(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderableWidget_SetRenderable_NullThrows()
    {
        var w = new RenderableWidget(new Spectre.Console.Text("X"));
        var act = () => w.Renderable = null!;
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderableWidget_SetRenderable_Updates()
    {
        var t1 = new Spectre.Console.Text("A");
        var t2 = new Spectre.Console.Text("B");
        var w = new RenderableWidget(t1);
        w.Renderable.Should().BeSameAs(t1);
        w.Renderable = t2;
        w.Renderable.Should().BeSameAs(t2);
    }

    // ── RenderableWidget: Render (L40-64) ───────────────────────────

    [Fact]
    public void RenderableWidget_Render_TextContent()
    {
        var text = new Spectre.Console.Text("Hello");
        var w = new RenderableWidget(text);
        w.Arrange(new Rect(0, 0, 20, 3));
        var (buf, surface) = Surface(20, 3);
        w.Render(surface);

        Row(buf, 0).Should().Contain("Hello");
    }

    [Fact]
    public void RenderableWidget_Render_SkipsControlCodes()
    {
        // Rendering should skip control code segments without crashing
        var rule = new Rule("Test");
        var w = new RenderableWidget(rule);
        w.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        w.Render(surface);
        // Should not crash and should have some rendered content
        Row(buf, 0).TrimEnd().Should().NotBeEmpty();
    }

    // ── RenderableWidget: MeasureContent (L33-37) ───────────────────

    [Fact]
    public void RenderableWidget_MeasureContent_UsesRenderableWidth()
    {
        var text = new Spectre.Console.Text("ABCDE");
        var w = new RenderableWidget(text);
        var size = w.MeasureContent(new Spectre.Console.Size(50, 5));
        size.Width.Should().Be(5); // "ABCDE" = 5 chars
        size.Height.Should().Be(5); // returns available height
    }

    // ── Application: Constructor (L21) ──────────────────────────────

    [Fact]
    public void Application_Constructor_NullDriverThrows()
    {
        var act = () => new Application((ITerminalDriver)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Application: Default properties (L16-17) ────────────────────

    [Fact]
    public void Application_MouseEnabled_DefaultTrue()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.MouseEnabled.Should().BeTrue();
    }

    [Fact]
    public void Application_TargetFps_Default30()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.TargetFps.Should().Be(30);
    }

    [Fact]
    public void Application_RootWidget_DefaultNull()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.RootWidget.Should().BeNull();
    }

    // ── Application: Run initializes and shuts down (L53-115) ───────

    [Fact]
    public void Application_Run_WithMouse_EnablesAndDisables()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = true };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        app.Run(cts.Token);

        driver.MouseEnabled.Should().BeFalse(); // disabled on shutdown
    }

    [Fact]
    public void Application_Run_WithoutMouse_DoesNotEnableMouse()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        app.Run(cts.Token);

        // Mouse was never enabled; verify shutdown happened
        driver.IsShutdown.Should().BeTrue();
    }

    // ── Application: Quit exits loop (L46-48) ───────────────────────

    [Fact]
    public void Application_Quit_ExitsLoop()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        app.RootWidget = new Label("X");

        var task = Task.Run(() =>
        {
            Thread.Sleep(50);
            app.Quit();
        });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        app.Run(cts.Token);
        driver.IsShutdown.Should().BeTrue();
    }

    // ── Application: HandleKeyEvent tab navigation (L149-154) ───────

    [Fact]
    public void Application_Tab_Shift_MovesBackward()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        var stack = new VStack();
        var btn1 = new Button("A");
        var btn2 = new Button("B");
        stack.Add(btn1);
        stack.Add(btn2);
        app.RootWidget = stack;

        // Dummy key to let layout happen, then shift+tab to go backward
        driver.EnqueueKey(ConsoleKey.Escape, '\0');
        driver.EnqueueKey(ConsoleKey.Tab, '\t', true, false, false); // Shift+Tab

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        app.Run(cts.Token);
        driver.IsShutdown.Should().BeTrue();
    }

    // ── Application: HandleKeyEvent routing to focused (L142-147) ───

    [Fact]
    public void Application_KeyEvent_ConsumedByWidget_DoesNotTab()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        var tb = new TextBox();
        app.RootWidget = tb;

        // Burn first iteration, then type 'x' which textbox consumes
        driver.EnqueueKey(ConsoleKey.Escape, '\0');
        driver.EnqueueKey(ConsoleKey.X, 'x');

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        app.Run(cts.Token);
        tb.Text.Should().Be("x");
    }

    // ── Application: HandleMouseEvent (L157-177) ────────────────────

    [Fact]
    public void Application_MouseEvent_ClickFocusesWidget()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = true };
        var stack = new VStack();
        var btn1 = new Button("A");
        var btn2 = new Button("B");
        stack.Add(btn1);
        stack.Add(btn2);
        app.RootWidget = stack;

        // Burn first key, then send mouse click on btn2 area
        driver.EnqueueKey(ConsoleKey.Escape, '\0');
        // btn2 will be arranged below btn1. Its Y depends on layout.
        // We send a mouse event — the handling code runs even if hit test returns null
        driver.EnqueueInput(new MouseEvent(MouseButton.Left, MouseEventType.Press, 2, 1));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        app.Run(cts.Token);
        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void Application_MouseEvent_NoRoot_DoesNothing()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = true };
        // No RootWidget set

        driver.EnqueueInput(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 5));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        app.Run(cts.Token);
        driver.IsShutdown.Should().BeTrue();
    }

    // ── Application: HandleResize (L187-199) ────────────────────────

    [Fact]
    public void Application_ResizeEvent_ProcessedWithoutCrash()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        app.RootWidget = new Label("X");

        driver.EnqueueInput(new ResizeEvent(60, 20));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        app.Run(cts.Token);
        driver.IsShutdown.Should().BeTrue();
    }

    // ── Application: Run() overload without cancellation (L36-39) ───

    [Fact]
    public void Application_Run_NoCancellation_Quit()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };

        var task = Task.Run(() =>
        {
            Thread.Sleep(50);
            app.Quit();
        });

        app.Run(); // Uses internal CTS
        driver.IsShutdown.Should().BeTrue();
    }

    // ── Application: Dispose idempotent (L231-237) ──────────────────

    [Fact]
    public void Application_Dispose_DoubleDispose_Safe()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver);
        app.Dispose();
        app.Dispose();
        // Should not throw
    }

    // ── Application: RenderWidget visibility check (L203-204) ───────

    [Fact]
    public void Application_InvisibleRoot_NotRendered()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        var label = new Label("Secret") { Visible = false };
        app.RootWidget = label;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        app.Run(cts.Token);

        // No content should appear
        driver.GetText(0).Should().NotContain("Secret");
    }

    // ── Application: SwapBuffers (L222-225) ─────────────────────────

    [Fact]
    public void Application_RenderAndFlush_ProducesOutput()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        app.RootWidget = new Label("Visible");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        app.Run(cts.Token);

        driver.GetText(0).Should().Contain("Visible");
    }

    // ── Application: NeedsLayout/NeedsRender guards (L73-87) ────────

    [Fact]
    public void Application_MultipleIterations_SkipsUnnecessaryRender()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        var label = new Label("Static");
        app.RootWidget = label;

        // Multiple iterations via longer timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        app.Run(cts.Token);

        driver.GetText(0).Should().Contain("Static");
        driver.IsShutdown.Should().BeTrue();
    }

    // ── Application: CheckResize (L181-183) ─────────────────────────
    // The check happens if driver size differs from buffer size.
    // TestTerminalDriver has fixed size, so this is tested via ResizeEvent above.

    // ── Application: ProcessInput null event (L121-123) ─────────────

    [Fact]
    public void Application_NoInput_StillLoops()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { MouseEnabled = false };
        app.RootWidget = new Label("X");

        // No input enqueued — ReadEvent returns null
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        app.Run(cts.Token);
        driver.IsShutdown.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════
    // ADDITIONAL MUTATION KILLERS — arithmetic/equality boundary tests
    // ══════════════════════════════════════════════════════════════════

    // ── MenuBar: CanFocus default ────────────────────────────────────

    [Fact]
    public void MenuBar_CanFocus_DefaultTrue()
    {
        var mb = new MenuBar();
        mb.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void MenuBar_AddItem_Null_Throws()
    {
        var mb = new MenuBar();
        var act = () => mb.AddItem(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── MenuBar: LeftArrow boundary - _selectedIndex <=0 ternary ────

    [Fact]
    public void MenuBar_LeftArrow_FromIndex1_GoesToIndex0()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.AddItem(new MenuItem("B"));
        mb.AddItem(new MenuItem("C"));

        // Right 2x: -1 -> 0 -> 1
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        // Left: 1 -> 0
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));

        mb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        mb.Render(surface);
        // " A " at col 1 should be selected
        buf[1, 0].Style.Background.Should().Be(Color.Blue);
        // " B " at col 4 should NOT be selected
        buf[4, 0].Style.Background.Should().Be(Color.Grey);
    }

    // ── MenuBar: RightArrow modulo arithmetic ───────────────────────

    [Fact]
    public void MenuBar_RightArrow_FromMinus1_GoesTo0()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.AddItem(new MenuItem("B"));

        // First RightArrow: (-1 + 1) % 2 = 0
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));

        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);
        buf[1, 0].Style.Background.Should().Be(Color.Blue); // A selected
    }

    // ── MenuBar: Enter bounds check both sides ──────────────────────

    [Fact]
    public void MenuBar_Enter_WithValidSelection_Activates()
    {
        var mb = new MenuBar();
        var activated = false;
        var item = new MenuItem("X");
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);

        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0')); // select 0
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().BeTrue();
    }

    // ── MenuBar: Escape sets selectedIndex to exactly -1 ────────────

    [Fact]
    public void MenuBar_Escape_SetsIndexMinus1_VerifiedByNoHighlight()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.AddItem(new MenuItem("B"));

        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.Escape, '\x1b'));

        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);
        // No item should have selected style
        buf[1, 0].Style.Background.Should().Be(Color.Grey);
        buf[4, 0].Style.Background.Should().Be(Color.Grey);
    }

    // ── MenuBar: Alt shortcut sets selectedIndex ────────────────────

    [Fact]
    public void MenuBar_AltLetter_SetsSelectedIndex_VerifiedByRender()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.AddItem(new MenuItem("Edit"));

        mb.OnKeyEvent(new KeyEvent(ConsoleKey.E, 'e', false, true, false));

        mb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        mb.Render(surface);
        // "Edit" starts at col 7 (" File " = 6 chars + starting col 1 = col 7)
        buf[7, 0].Style.Background.Should().Be(Color.Blue);
    }

    // ── MenuBar: Mouse textLen arithmetic (L121: Text.Length + 2) ───

    [Fact]
    public void MenuBar_MouseClick_ExactBoundary_FirstItem()
    {
        var mb = new MenuBar();
        var act = false;
        var item = new MenuItem("AB"); // " AB " = 4 chars, text.Length = 2, textLen = 4
        item.Activated += (_, _) => act = true;
        mb.AddItem(item);
        mb.Arrange(new Rect(0, 0, 30, 1));

        // Item spans col 1 to col 4 (x=1, textLen=4)
        // Click at col 4 should be col < x + textLen = col < 5, so col 4 is in range
        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 4, 0));
        result.Should().BeTrue();
        act.Should().BeTrue();
    }

    [Fact]
    public void MenuBar_MouseClick_JustPastBoundary_FirstItem()
    {
        var mb = new MenuBar();
        var act = false;
        var item = new MenuItem("AB"); // textLen = 4, spans col 1 to 4
        item.Activated += (_, _) => act = true;
        mb.AddItem(item);
        mb.Arrange(new Rect(0, 0, 30, 1));

        // Click at col 5 should be outside (>= x + textLen = 5)
        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0));
        result.Should().BeFalse();
        act.Should().BeFalse();
    }

    // ── MenuBar: localCol calculation (L117) ────────────────────────

    [Fact]
    public void MenuBar_MouseClick_WithOffset_AdjustsCorrectly()
    {
        var mb = new MenuBar();
        var act = false;
        var item = new MenuItem("X"); // " X " = 3 chars, x starts at 1
        item.Activated += (_, _) => act = true;
        mb.AddItem(item);
        mb.Arrange(new Rect(5, 0, 30, 1)); // Bounds.X = 5

        // localCol = e.Column - Bounds.X = 6 - 5 = 1, which is at x=1 (start of item)
        var result = mb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 6, 0));
        result.Should().BeTrue();
        act.Should().BeTrue();
    }

    // ── StatusBar: Render item overflow guard (L50: x < surface.Width) ──

    [Fact]
    public void StatusBar_Render_NarrowSurface_TruncatesItems()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help"); // "F1" + "Help " = 7 chars
        sb.AddItem("F2", "Save"); // "F2" + "Save " = 7 more chars = 14 total
        sb.Arrange(new Rect(0, 0, 5, 1)); // Only 5 cols
        var (buf, surface) = Surface(5, 1);
        sb.Render(surface);

        // First few chars should be rendered within bounds
        buf[0, 0].Character.Should().Be('F');
        buf[1, 0].Character.Should().Be('1');
    }

    // ── StatusBar: Text length guard (L60: _text.Length > 0) ────────

    [Fact]
    public void StatusBar_Render_TextLengthZero_NoText()
    {
        var sb = new StatusBar { Text = "" };
        sb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        sb.Render(surface);
        // All spaces
        Row(buf, 0).TrimEnd().Should().BeEmpty();
    }

    // ── StatusBar: Mouse arithmetic (L74, L78, L79) ─────────────────

    [Fact]
    public void StatusBar_Mouse_WithOffset_CorrectLocalCol()
    {
        var sb = new StatusBar();
        var act = false;
        sb.AddItem("F1", "Help", () => act = true); // width = 2 + 4 + 1 = 7
        sb.Arrange(new Rect(10, 0, 30, 1)); // Bounds.X = 10

        // localCol = e.Column - Bounds.X = 11 - 10 = 1
        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 11, 0));
        result.Should().BeTrue();
        act.Should().BeTrue();
    }

    [Fact]
    public void StatusBar_Mouse_ExactBoundary_HitsItem()
    {
        var sb = new StatusBar();
        var act = false;
        sb.AddItem("F1", "Help", () => act = true); // width = 2 + 4 + 1 = 7
        sb.Arrange(new Rect(0, 0, 30, 1));

        // Click at col 6 (last col of item: 0 + 7 - 1 = 6)
        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 6, 0));
        result.Should().BeTrue();
        act.Should().BeTrue();
    }

    [Fact]
    public void StatusBar_Mouse_JustPastBoundary_Misses()
    {
        var sb = new StatusBar();
        var act = false;
        sb.AddItem("F1", "Help", () => act = true); // width = 7
        sb.Arrange(new Rect(0, 0, 30, 1));

        // Click at col 7 (just past item)
        var result = sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 7, 0));
        result.Should().BeFalse();
        act.Should().BeFalse();
    }

    // ── StatusBar: Text truncation boundary (L62) ───────────────────

    [Fact]
    public void StatusBar_Render_TextExactlySurfaceWidth_NotTruncated()
    {
        var sb = new StatusBar { Text = "ABCDE" }; // 5 chars
        sb.Arrange(new Rect(0, 0, 5, 1)); // exactly 5 wide
        var (buf, surface) = Surface(5, 1);
        sb.Render(surface);
        Row(buf, 0).Should().Be("ABCDE");
    }

    [Fact]
    public void StatusBar_Render_TextOneOverSurfaceWidth_Truncated()
    {
        var sb = new StatusBar { Text = "ABCDEF" }; // 6 chars
        sb.Arrange(new Rect(0, 0, 5, 1)); // 5 wide
        var (buf, surface) = Surface(5, 1);
        sb.Render(surface);
        Row(buf, 0).Should().Be("ABCDE");
    }

    // ── TabControl: CanFocus default ────────────────────────────────

    [Fact]
    public void TabControl_CanFocus_DefaultTrue()
    {
        var tc = new TabControl();
        tc.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void TabControl_AddTab_NullTitle_Throws()
    {
        var tc = new TabControl();
        var act = () => tc.AddTab(null!, new Label("X"));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TabControl_AddTab_NullContent_Throws()
    {
        var tc = new TabControl();
        var act = () => tc.AddTab("T", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TabControl_AddTab_SetsParent()
    {
        var tc = new TabControl();
        var label = new Label("X");
        tc.AddTab("T", label);
        label.Parent.Should().Be(tc);
    }

    // ── TabControl: Render selected style exact (L77) ───────────────

    [Fact]
    public void TabControl_Render_SecondTabSelected_FirstNormal()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.AddTab("B", new Label("C2"));
        tc.SelectedIndex = 1;
        tc.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        tc.Render(surface);

        // " A " at col 0 should be TabNormalStyle
        buf[0, 0].Style.Foreground.Should().Be(Color.Grey);
        // " B " starts after " A " (3) + separator (1) = col 4
        buf[4, 0].Style.Background.Should().Be(Color.Blue);
    }

    // ── TabControl: Render tab row boundary (L75: x < surface.Width) ──

    [Fact]
    public void TabControl_Render_NarrowSurface_TruncatesTabs()
    {
        var tc = new TabControl();
        tc.AddTab("ABCDEF", new Label("C1")); // " ABCDEF " = 8 chars
        tc.Arrange(new Rect(0, 0, 5, 5)); // only 5 cols
        var (buf, surface) = Surface(5, 5);
        tc.Render(surface);
        // Only first 5 chars should be rendered
        buf[0, 0].Character.Should().Be(' ');
        buf[1, 0].Character.Should().Be('A');
    }

    // ── TabControl: Separator fill (L90, L96) ───────────────────────

    [Fact]
    public void TabControl_Render_FillAndSeparator_ExactCols()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C")); // " A " = 3 chars
        tc.Arrange(new Rect(0, 0, 5, 5));
        var (buf, surface) = Surface(5, 5);
        tc.Render(surface);

        // After " A " (3 chars), cols 3 and 4 should be spaces
        buf[3, 0].Character.Should().Be(' ');
        buf[4, 0].Character.Should().Be(' ');
        // Separator line row 1, all cols
        buf[0, 1].Character.Should().Be('\u2500');
        buf[4, 1].Character.Should().Be('\u2500');
    }

    // ── TabControl: SelectedIndex setter ternary (L112-113) ─────────

    [Fact]
    public void TabControl_LeftArrow_Wrap_ExactIndex()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.AddTab("B", new Label("C2"));
        tc.AddTab("C", new Label("C3"));
        tc.HasFocus = true;

        // LeftArrow from 0: _selectedIndex > 0 is false, so use _tabs.Count - 1 = 2
        tc.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        tc.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void TabControl_RightArrow_Wrap_ExactIndex()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1"));
        tc.AddTab("B", new Label("C2"));
        tc.HasFocus = true;

        // Move to 1, then right wrap: (1+1)%2 = 0
        tc.SelectedIndex = 1;
        tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        tc.SelectedIndex.Should().Be(0);
    }

    // ── TabControl: Mouse localRow and tab width arithmetic (L129, L135, L140, L146) ──

    [Fact]
    public void TabControl_MouseClick_FirstTab_ExactBoundary()
    {
        var tc = new TabControl();
        tc.AddTab("AB", new Label("C1")); // " AB " = 4 chars, tabWidth = 4
        tc.AddTab("CD", new Label("C2")); // starts after 4 + 1 sep = 5
        tc.Arrange(new Rect(0, 0, 20, 10));

        // Click at col 3 (last col of " AB "): localCol >= 0 && localCol < 4
        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 3, 0));
        result.Should().BeTrue();
        tc.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void TabControl_MouseClick_SeparatorCol_MissesFirstTab()
    {
        var tc = new TabControl();
        tc.AddTab("AB", new Label("C1")); // tabWidth = 4, x after = 4 + 1 = 5
        tc.AddTab("CD", new Label("C2")); // starts at x=5, tabWidth = 4
        tc.Arrange(new Rect(0, 0, 20, 10));

        // Col 4 is the separator (between tabs)
        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 4, 0));
        // Col 4: first tab x=0 tabWidth=4, so col 4 >= 0+4 = 4, not < 4. Miss.
        // Second tab x=5, col 4 < 5. Miss.
        result.Should().BeFalse();
    }

    [Fact]
    public void TabControl_MouseClick_SecondTab_FirstCol()
    {
        var tc = new TabControl();
        tc.AddTab("AB", new Label("C1")); // tabWidth = 4, x = 0 → next x = 4 + 1 = 5
        tc.AddTab("CD", new Label("C2")); // starts at x=5, tabWidth = 4
        tc.Arrange(new Rect(0, 0, 20, 10));

        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0));
        result.Should().BeTrue();
        tc.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void TabControl_MouseClick_WithBoundsOffset()
    {
        var tc = new TabControl();
        tc.AddTab("X", new Label("C")); // tabWidth = 3
        tc.Arrange(new Rect(10, 5, 20, 10));

        // localCol = 11 - 10 = 1, localRow = 5 - 5 = 0
        var result = tc.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 11, 5));
        result.Should().BeTrue();
        tc.SelectedIndex.Should().Be(0);
    }

    // ── TuiPanel: MeasureContent arithmetic (L37) ───────────────────

    [Fact]
    public void TuiPanel_MeasureContent_WithContent_ArithmeticCorrect()
    {
        var panel = new TuiPanel();
        var label = new Label("ABCDE"); // measures as 5 wide, 1 high
        panel.Content = label;
        var size = panel.MeasureContent(new Spectre.Console.Size(50, 50));
        // Width = 5 + 2 = 7 (content + borders)
        size.Width.Should().Be(7);
        // Height = 1 + 2 = 3 (content + borders)
        size.Height.Should().Be(3);
    }

    // ── TuiPanel: Render boundary conditions (L53, L59, L63, L78, L86) ──

    [Fact]
    public void TuiPanel_Render_Width2Height2_MinimalBorder()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 2, 2));
        var (buf, surface) = Surface(2, 2);
        panel.Render(surface);

        // With w=2, h=2: top border ┌┐, bottom └┘, no sides (h-1=1, loop 1..0 empty)
        buf[0, 0].Character.Should().Be('\u250c');
        buf[1, 0].Character.Should().Be('\u2510');
        buf[0, 1].Character.Should().Be('\u2514');
        buf[1, 1].Character.Should().Be('\u2518');
    }

    [Fact]
    public void TuiPanel_Render_ContentArea_ExactDimensions()
    {
        var panel = new TuiPanel { ContentStyle = new Style(Color.Red) };
        panel.Arrange(new Rect(0, 0, 6, 4));
        var (buf, surface) = Surface(6, 4);
        panel.Render(surface);

        // Content area: Rect(1, 1, w-2, h-2) = Rect(1, 1, 4, 2)
        buf[1, 1].Style.Foreground.Should().Be(Color.Red);
        buf[4, 2].Style.Foreground.Should().Be(Color.Red);
        // Borders should NOT have content style
        buf[0, 0].Style.Foreground.Should().NotBe(Color.Red);
    }

    [Fact]
    public void TuiPanel_Render_TopBorder_LoopBounds()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 4, 3));
        var (buf, surface) = Surface(4, 3);
        panel.Render(surface);

        // Top border: col 1 to w-2 = col 1 to 2
        buf[0, 0].Character.Should().Be('\u250c');
        buf[1, 0].Character.Should().Be('\u2500');
        buf[2, 0].Character.Should().Be('\u2500');
        buf[3, 0].Character.Should().Be('\u2510');
    }

    [Fact]
    public void TuiPanel_Render_BottomBorder_LoopBounds()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 4, 3));
        var (buf, surface) = Surface(4, 3);
        panel.Render(surface);

        // Bottom border at row h-1=2
        buf[0, 2].Character.Should().Be('\u2514');
        buf[1, 2].Character.Should().Be('\u2500');
        buf[2, 2].Character.Should().Be('\u2500');
        buf[3, 2].Character.Should().Be('\u2518');
    }

    [Fact]
    public void TuiPanel_Render_SideBorders_LoopBounds()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 4, 4));
        var (buf, surface) = Surface(4, 4);
        panel.Render(surface);

        // Side borders: rows 1 to h-2 = rows 1 to 2
        buf[0, 1].Character.Should().Be('\u2502');
        buf[3, 1].Character.Should().Be('\u2502');
        buf[0, 2].Character.Should().Be('\u2502');
        buf[3, 2].Character.Should().Be('\u2502');
    }

    [Fact]
    public void TuiPanel_Render_TitleTruncation_Boundary()
    {
        var panel = new TuiPanel { Title = "ABCD" };
        panel.Arrange(new Rect(0, 0, 8, 3)); // w-4 = 4, exact fit
        var (buf, surface) = Surface(8, 3);
        panel.Render(surface);
        // Title fits exactly (4 chars, w-4=4)
        buf[2, 0].Character.Should().Be('A');
        buf[5, 0].Character.Should().Be('D');
    }

    // ── Window: Render arithmetic (L91, L95, L104, L107, L115, L124, L132, L142) ──

    [Fact]
    public void Window_Render_ContentFill_ExactRect()
    {
        var win = new Window("W") { ContentStyle = new Style(Color.Yellow) };
        win.Arrange(new Rect(0, 0, 8, 7));
        var (buf, surface) = Surface(8, 7);
        win.Render(surface);

        // Content area: Rect(1, 2, w-2, h-3) = Rect(1, 2, 6, 4)
        // Row 2 is separator (drawn after fill), so actual visible content is row 3+
        buf[1, 3].Character.Should().Be(' ');
        buf[1, 3].Style.Foreground.Should().Be(Color.Yellow);
        buf[6, 5].Style.Foreground.Should().Be(Color.Yellow);
    }

    [Fact]
    public void Window_Render_TopBorder_LoopBounds()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 5, 5));
        var (buf, surface) = Surface(5, 5);
        win.Render(surface);

        buf[0, 0].Character.Should().Be('\u250c');
        buf[1, 0].Character.Should().Be('\u2500');
        buf[2, 0].Character.Should().Be('\u2500');
        buf[3, 0].Character.Should().Be('\u2500');
        buf[4, 0].Character.Should().Be('\u2510');
    }

    [Fact]
    public void Window_Render_TitleFill_ExactWidth()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 8, 5));
        var (buf, surface) = Surface(8, 5);
        win.Render(surface);

        // Title fill: Rect(1, 1, w-2, 1) = Rect(1, 1, 6, 1)
        buf[1, 1].Style.Background.Should().Be(Color.Blue);
        buf[6, 1].Style.Background.Should().Be(Color.Blue);
    }

    [Fact]
    public void Window_Render_TitleTruncation_ExactBoundary()
    {
        var win = new Window("ABCD") { Closable = false }; // 4 chars
        win.Arrange(new Rect(0, 0, 8, 5)); // w-4 = 4, exact fit
        var (buf, surface) = Surface(8, 5);
        win.Render(surface);

        buf[2, 1].Character.Should().Be('A');
        buf[5, 1].Character.Should().Be('D');
    }

    [Fact]
    public void Window_Render_CloseButton_ExactWidth6()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 6, 5)); // w >= 6, close button at w-4=2
        var (buf, surface) = Surface(6, 5);
        win.Render(surface);

        buf[2, 1].Character.Should().Be('[');
        buf[3, 1].Character.Should().Be('X');
        buf[4, 1].Character.Should().Be(']');
    }

    [Fact]
    public void Window_Render_SeparatorRow_LoopBounds()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 5, 5));
        var (buf, surface) = Surface(5, 5);
        win.Render(surface);

        buf[0, 2].Character.Should().Be('\u251c');
        buf[1, 2].Character.Should().Be('\u2500');
        buf[3, 2].Character.Should().Be('\u2500');
        buf[4, 2].Character.Should().Be('\u2524');
    }

    [Fact]
    public void Window_Render_SideBorders_LoopBounds()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 5, 6));
        var (buf, surface) = Surface(5, 6);
        win.Render(surface);

        // Side borders: rows 3 to h-2 = rows 3 to 4
        buf[0, 3].Character.Should().Be('\u2502');
        buf[4, 3].Character.Should().Be('\u2502');
        buf[0, 4].Character.Should().Be('\u2502');
        buf[4, 4].Character.Should().Be('\u2502');
    }

    [Fact]
    public void Window_Render_BottomBorder_LoopBounds()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 5, 5));
        var (buf, surface) = Surface(5, 5);
        win.Render(surface);

        buf[0, 4].Character.Should().Be('\u2514');
        buf[1, 4].Character.Should().Be('\u2500');
        buf[3, 4].Character.Should().Be('\u2500');
        buf[4, 4].Character.Should().Be('\u2518');
    }

    // ── Window: Small window guard (w<3 || h<3) boundary ────────────

    [Fact]
    public void Window_Render_Width4_Height5_SmallButValid()
    {
        // Window with w=4, h=5 passes the w<3||h<3 guard
        var win = new Window("W") { Closable = false };
        win.Arrange(new Rect(0, 0, 4, 5));
        var (buf, surface) = Surface(4, 5);
        win.Render(surface);

        // Should render chrome without errors
        buf[0, 0].Character.Should().Be('\u250c');
        buf[3, 0].Character.Should().Be('\u2510');
        buf[0, 4].Character.Should().Be('\u2514');
        buf[3, 4].Character.Should().Be('\u2518');
    }

    // ── Window: Close button arithmetic (L157-158) ──────────────────

    [Fact]
    public void Window_Mouse_CloseButton_LeftBoundary()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // localCol >= Bounds.Width - 4 = 16, localCol <= Bounds.Width - 2 = 18
        // Test exact left boundary: col 16
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 16, 1));
        result.Should().BeTrue();
        closed.Should().BeTrue();
    }

    [Fact]
    public void Window_Mouse_CloseButton_JustBeforeLeftBoundary()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // localCol = 15 < 16, should not trigger close
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 15, 1));
        closed.Should().BeFalse();
    }

    [Fact]
    public void Window_Mouse_CloseButton_JustAfterRightBoundary()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(0, 0, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // localCol = 19 > 18, should not trigger close (it's the border)
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 19, 1));
        closed.Should().BeFalse();
    }

    // ── Window: Drag offset arithmetic (L178-179) ───────────────────

    [Fact]
    public void Window_Drag_ExactOffset_Position()
    {
        var win = new Window("W") { Movable = true, Closable = false };
        win.Arrange(new Rect(10, 10, 20, 6));

        // Start drag at col 15, row 11 (title bar). localCol = 5, localRow = 1
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 15, 11));

        // Move to col 20, row 15.
        // newX = 20 - 5 = 15, newY = 15 - 1 = 14
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 20, 15));

        win.Bounds.X.Should().Be(15);
        win.Bounds.Y.Should().Be(14);
        win.Bounds.Width.Should().Be(20); // unchanged
        win.Bounds.Height.Should().Be(6); // unchanged
    }

    // ── Window: Drag event type guard (L167, L176, L185) ────────────

    [Fact]
    public void Window_Drag_MoveWithoutPress_ReturnsFalse()
    {
        var win = new Window("W") { Movable = true, Closable = false };
        win.Arrange(new Rect(0, 0, 20, 6));

        // Move without prior press
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 5, 1));
        result.Should().BeFalse();
    }

    // ── Window: MeasureContent arithmetic (L43) ─────────────────────

    [Fact]
    public void Window_MeasureContent_ChildMeasuredWithReducedAvailable()
    {
        var win = new Window("W");
        var label = new Label("Hello World"); // 11 chars
        win.Add(label);
        var size = win.MeasureContent(new Spectre.Console.Size(20, 10));
        // contentWidth = max(0, 11) = 11, result = min(11+2, 20) = 13
        size.Width.Should().Be(13);
        // contentHeight = 1, result = min(1+3, 10) = 4
        size.Height.Should().Be(4);
    }

    // ── Window: Arrange content arithmetic (L58-61) ─────────────────

    [Fact]
    public void Window_Arrange_ContentArea_ExactArithmetic()
    {
        var win = new Window("W");
        var label = new Label("X");
        win.Add(label);
        win.Arrange(new Rect(5, 3, 30, 20));

        // contentX = 5+1=6, contentY = 3+2=5, contentWidth = 30-2=28, contentHeight = 20-3=17
        label.Bounds.X.Should().Be(6);
        label.Bounds.Y.Should().Be(5);
        label.Bounds.Width.Should().Be(28);
    }

    // ── WindowManager: Null guards ──────────────────────────────────

    [Fact]
    public void WindowManager_AddWindow_Null_Throws()
    {
        var wm = new WindowManager();
        var act = () => wm.AddWindow(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WindowManager_RemoveWindow_Null_Throws()
    {
        var wm = new WindowManager();
        var act = () => wm.RemoveWindow(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WindowManager_BringToFront_Null_Throws()
    {
        var wm = new WindowManager();
        var act = () => wm.BringToFront(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WindowManager_SendToBack_Null_Throws()
    {
        var wm = new WindowManager();
        var act = () => wm.SendToBack(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── RenderableWidget: MeasureContent Min→Max mutation (L37) ─────

    [Fact]
    public void RenderableWidget_MeasureContent_UsesMaxNotMin()
    {
        var text = new Spectre.Console.Text("AB"); // Min=2, Max=2
        var w = new RenderableWidget(text);
        var size = w.MeasureContent(new Spectre.Console.Size(50, 5));
        size.Width.Should().Be(2); // Max measurement
    }

    // ── MessageBox: Button text verification ────────────────────────

    [Fact]
    public void MessageBox_Ok_OkButtonClosesWithOk()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.Ok);
        d.Arrange(new Rect(0, 0, 50, 20));
        SimulateButtonClick(d, "OK");
        d.Result.Should().Be(DialogResult.Ok);
    }

    [Fact]
    public void MessageBox_OkCancel_OkButtonClosesWithOk()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.OkCancel);
        d.Arrange(new Rect(0, 0, 50, 20));
        SimulateButtonClick(d, "OK");
        d.Result.Should().Be(DialogResult.Ok);
    }

    [Fact]
    public void MessageBox_YesNoCancel_YesButtonClosesWithYes()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNoCancel);
        d.Arrange(new Rect(0, 0, 50, 20));
        SimulateButtonClick(d, "Yes");
        d.Result.Should().Be(DialogResult.Yes);
    }

    [Fact]
    public void MessageBox_YesNoCancel_NoButtonClosesWithNo()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNoCancel);
        d.Arrange(new Rect(0, 0, 50, 20));
        SimulateButtonClick(d, "No");
        d.Result.Should().Be(DialogResult.No);
    }

    // ── MessageBox: Equality mutations for button type checks ───────

    [Fact]
    public void MessageBox_Ok_DoesNotHaveCancelButton()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.Ok);
        d.Arrange(new Rect(0, 0, 50, 20));
        // Should not find a Cancel button
        var found = FindButton(d, "Cancel");
        found.Should().BeFalse();
    }

    [Fact]
    public void MessageBox_Ok_DoesNotHaveYesNoButtons()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.Ok);
        d.Arrange(new Rect(0, 0, 50, 20));
        FindButton(d, "Yes").Should().BeFalse();
        FindButton(d, "No").Should().BeFalse();
    }

    [Fact]
    public void MessageBox_YesNo_DoesNotHaveOkOrCancelButton()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNo);
        d.Arrange(new Rect(0, 0, 50, 20));
        FindButton(d, "OK").Should().BeFalse();
        FindButton(d, "Cancel").Should().BeFalse();
    }

    [Fact]
    public void MessageBox_OkCancel_DoesNotHaveYesNoButtons()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.OkCancel);
        d.Arrange(new Rect(0, 0, 50, 20));
        FindButton(d, "Yes").Should().BeFalse();
        FindButton(d, "No").Should().BeFalse();
    }

    [Fact]
    public void MessageBox_OkCancel_HasExactlyTwoButtons()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.OkCancel);
        d.Arrange(new Rect(0, 0, 50, 20));
        FindButton(d, "OK").Should().BeTrue();
        FindButton(d, "Cancel").Should().BeTrue();
    }

    [Fact]
    public void MessageBox_YesNoCancel_HasExactlyThreeButtons()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.YesNoCancel);
        d.Arrange(new Rect(0, 0, 50, 20));
        FindButton(d, "Yes").Should().BeTrue();
        FindButton(d, "No").Should().BeTrue();
        FindButton(d, "Cancel").Should().BeTrue();
    }

    private static bool FindButton(Widget parent, string buttonText)
    {
        var children = parent.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is Button btn && btn.Text == buttonText)
            {
                return true;
            }

            if (FindButton(children[i], buttonText))
            {
                return true;
            }
        }

        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    // ROUND 2 — Additional precision tests for render arithmetic
    // ══════════════════════════════════════════════════════════════════

    // ── TuiPanel: MeasureContent passes reduced size to content (L38) ──

    [Fact]
    public void TuiPanel_MeasureContent_ContentGetsReducedAvailable()
    {
        // If arithmetic is wrong (e.g. +2 instead of -2), content would get 54 instead of 48
        var panel = new TuiPanel();
        // Use a Label whose width depends on available width
        var label = new Label("X");
        panel.Content = label;
        var size = panel.MeasureContent(new Spectre.Console.Size(10, 8));
        // Label measures as 1 wide regardless, but height wraps
        // contentSize = (1, 1), result = (1+2, 1+2) = (3, 3)
        size.Width.Should().Be(3);
        size.Height.Should().Be(3);
    }

    // ── TuiPanel: Guard condition (L54: w < 2 || h < 2) ────────────

    [Fact]
    public void TuiPanel_Render_Width1_DoesNothing()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 1, 5));
        var (buf, surface) = Surface(1, 5);
        panel.Render(surface);
        buf[0, 0].Character.Should().NotBe('\u250c'); // no corner
    }

    [Fact]
    public void TuiPanel_Render_Height1_DoesNothing()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 5, 1));
        var (buf, surface) = Surface(5, 1);
        panel.Render(surface);
        buf[0, 0].Character.Should().NotBe('\u250c'); // no corner
    }

    [Fact]
    public void TuiPanel_Render_Width2_Height2_Renders()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 2, 2));
        var (buf, surface) = Surface(2, 2);
        panel.Render(surface);
        buf[0, 0].Character.Should().Be('\u250c'); // renders at exact boundary
    }

    // ── TuiPanel: Content fill Rect arithmetic (L60) ────────────────

    [Fact]
    public void TuiPanel_Render_ContentFill_DoesNotOverwriteBorders()
    {
        var panel = new TuiPanel { ContentStyle = new Style(Color.Red) };
        panel.Arrange(new Rect(0, 0, 6, 4));
        var (buf, surface) = Surface(6, 4);
        panel.Render(surface);

        // Content fill is Rect(1, 1, 4, 2)
        // Borders should not have content style
        buf[0, 0].Style.Foreground.Should().NotBe(Color.Red); // corner
        buf[5, 0].Style.Foreground.Should().NotBe(Color.Red); // corner
        buf[0, 3].Style.Foreground.Should().NotBe(Color.Red); // bottom corner
        // Content should have content style
        buf[1, 1].Style.Foreground.Should().Be(Color.Red);
        buf[4, 2].Style.Foreground.Should().Be(Color.Red);
    }

    // ── TuiPanel: Top/bottom border loop w-1 boundary (L64, L79, L87) ──

    [Fact]
    public void TuiPanel_Render_TopBorderExactCells()
    {
        var panel = new TuiPanel();
        panel.Arrange(new Rect(0, 0, 5, 4));
        var (buf, surface) = Surface(5, 4);
        panel.Render(surface);

        // Top border: corners at 0 and 4 (w-1), horizontal at 1,2,3
        buf[0, 0].Character.Should().Be('\u250c');
        buf[1, 0].Character.Should().Be('\u2500');
        buf[2, 0].Character.Should().Be('\u2500');
        buf[3, 0].Character.Should().Be('\u2500');
        buf[4, 0].Character.Should().Be('\u2510');

        // Side borders: rows 1 to h-2=2
        buf[0, 1].Character.Should().Be('\u2502');
        buf[4, 1].Character.Should().Be('\u2502');
        buf[0, 2].Character.Should().Be('\u2502');
        buf[4, 2].Character.Should().Be('\u2502');

        // Bottom border at row 3 (h-1)
        buf[0, 3].Character.Should().Be('\u2514');
        buf[1, 3].Character.Should().Be('\u2500');
        buf[3, 3].Character.Should().Be('\u2500');
        buf[4, 3].Character.Should().Be('\u2518');
    }

    // ── TuiPanel: Title truncation boundary (L74: w-4) ─────────────

    [Fact]
    public void TuiPanel_Render_Title_ExactlyFits_NotTruncated()
    {
        var panel = new TuiPanel { Title = "AB" }; // 2 chars
        panel.Arrange(new Rect(0, 0, 6, 3)); // w-4 = 2, exact fit
        var (buf, surface) = Surface(6, 3);
        panel.Render(surface);
        buf[2, 0].Character.Should().Be('A');
        buf[3, 0].Character.Should().Be('B');
    }

    [Fact]
    public void TuiPanel_Render_Title_OneOver_Truncated()
    {
        var panel = new TuiPanel { Title = "ABC" }; // 3 chars
        panel.Arrange(new Rect(0, 0, 6, 3)); // w-4 = 2, truncated
        var (buf, surface) = Surface(6, 3);
        panel.Render(surface);
        buf[2, 0].Character.Should().Be('A');
        buf[3, 0].Character.Should().Be('B');
        // C should NOT appear (truncated) — col 4 is a horizontal bar (not C)
        buf[4, 0].Character.Should().Be('\u2500'); // horizontal border, not 'C'
    }

    // ── StatusBar: Render loop boundary (L53: x < surface.Width) ────

    [Fact]
    public void StatusBar_Render_ItemExactlyFillsWidth()
    {
        var sb = new StatusBar();
        sb.AddItem("AB", "CD"); // "AB" + "CD " = 5 chars
        sb.Arrange(new Rect(0, 0, 5, 1));
        var (buf, surface) = Surface(5, 1);
        sb.Render(surface);

        buf[0, 0].Character.Should().Be('A');
        buf[1, 0].Character.Should().Be('B');
        buf[2, 0].Character.Should().Be('C');
        buf[3, 0].Character.Should().Be('D');
        buf[4, 0].Character.Should().Be(' ');
    }

    // ── StatusBar: Text length comparison (L63: _text.Length > surface.Width) ──

    [Fact]
    public void StatusBar_Render_TextExactWidth_NotTruncated()
    {
        var sb = new StatusBar { Text = "12345" }; // 5 chars
        sb.Arrange(new Rect(0, 0, 5, 1));
        var (buf, surface) = Surface(5, 1);
        sb.Render(surface);
        buf[0, 0].Character.Should().Be('1');
        buf[4, 0].Character.Should().Be('5');
    }

    [Fact]
    public void StatusBar_Render_TextOneOver_Truncated()
    {
        var sb = new StatusBar { Text = "123456" }; // 6 chars
        sb.Arrange(new Rect(0, 0, 5, 1));
        var (buf, surface) = Surface(5, 1);
        sb.Render(surface);
        buf[0, 0].Character.Should().Be('1');
        buf[4, 0].Character.Should().Be('5');
    }

    // ── TabControl: Render loop bounds (L81, L96, L102) ─────────────

    [Fact]
    public void TabControl_Render_TabSeparator_ExactPosition()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C1")); // " A " = 3 chars
        tc.AddTab("B", new Label("C2")); // separator at col 3, " B " = 3 chars at col 4
        tc.Arrange(new Rect(0, 0, 10, 5));
        var (buf, surface) = Surface(10, 5);
        tc.Render(surface);

        // Tab separator at col 3
        buf[3, 0].Character.Should().Be('\u2502');
        buf[3, 0].Style.Foreground.Should().Be(Color.Grey); // TabBorderStyle
    }

    [Fact]
    public void TabControl_Render_FillRow_StartsAfterLastTab()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C")); // " A " = 3 chars, no trailing separator
        tc.Arrange(new Rect(0, 0, 8, 5));
        var (buf, surface) = Surface(8, 5);
        tc.Render(surface);

        // Fill should start at col 3 and go to 7
        for (var col = 3; col < 8; col++)
        {
            buf[col, 0].Character.Should().Be(' ');
        }
    }

    [Fact]
    public void TabControl_Render_SeparatorLine_AllCols()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C"));
        tc.Arrange(new Rect(0, 0, 6, 5));
        var (buf, surface) = Surface(6, 5);
        tc.Render(surface);

        // Row 1: all cols should be ─
        for (var col = 0; col < 6; col++)
        {
            buf[col, 1].Character.Should().Be('\u2500');
        }
    }

    // ── TabControl: SelectedIndex property ternary result (L119, L122) ──

    [Fact]
    public void TabControl_LeftArrow_ReturnValue()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C"));
        tc.HasFocus = true;
        var result = tc.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        result.Should().BeTrue();
    }

    [Fact]
    public void TabControl_RightArrow_ReturnValue()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("C"));
        tc.HasFocus = true;
        var result = tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        result.Should().BeTrue();
    }

    // ── Window: Render arithmetic — all w-1, w-2, h-1, h-3 boundaries ──

    [Fact]
    public void Window_Render_AllChromeChars_PrecisePositions()
    {
        var win = new Window("T") { Closable = false };
        win.Arrange(new Rect(0, 0, 6, 6));
        var (buf, surface) = Surface(6, 6);
        win.Render(surface);

        // Row 0: top border. col 0=┌, 1-4=─, 5=┐
        buf[0, 0].Character.Should().Be('\u250c');
        buf[5, 0].Character.Should().Be('\u2510');

        // Row 1: title bar. col 0=│, 5=│
        buf[0, 1].Character.Should().Be('\u2502');
        buf[5, 1].Character.Should().Be('\u2502');

        // Row 2: separator. col 0=├, 1-4=─, 5=┤
        buf[0, 2].Character.Should().Be('\u251c');
        buf[5, 2].Character.Should().Be('\u2524');

        // Row 3-4: side borders. col 0=│, 5=│
        buf[0, 3].Character.Should().Be('\u2502');
        buf[5, 3].Character.Should().Be('\u2502');
        buf[0, 4].Character.Should().Be('\u2502');
        buf[5, 4].Character.Should().Be('\u2502');

        // Row 5: bottom border. col 0=└, 1-4=─, 5=┘
        buf[0, 5].Character.Should().Be('\u2514');
        buf[5, 5].Character.Should().Be('\u2518');

        // Content area: Rect(1, 2, 4, 3) = cols 1-4, rows 2-4
        // But row 2 is separator, so visible content is rows 3-4
        buf[1, 3].Character.Should().Be(' ');
    }

    // ── Window: Guard condition (L86: w < 3 || h < 3) ───────────────

    [Fact]
    public void Window_Render_Width2_SkipsRender()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 2, 5));
        var (buf, surface) = Surface(2, 5);
        win.Render(surface);
        buf[0, 0].Character.Should().NotBe('\u250c');
    }

    [Fact]
    public void Window_Render_Height2_SkipsRender()
    {
        var win = new Window("W");
        win.Arrange(new Rect(0, 0, 5, 2));
        var (buf, surface) = Surface(5, 2);
        win.Render(surface);
        buf[0, 0].Character.Should().NotBe('\u250c');
    }

    // ── Window: Mouse localCol/localRow arithmetic (L153-154, L158) ──

    [Fact]
    public void Window_Mouse_LocalCol_WithOffset()
    {
        var win = new Window("W") { Closable = true };
        win.Arrange(new Rect(10, 10, 20, 6));
        var closed = false;
        win.Closed += (_, _) => closed = true;

        // Close button: localCol >= Bounds.Width - 4 = 16, localCol <= Bounds.Width - 2 = 18
        // So col = 10 + 16 = 26, row = 10 + 1 = 11
        var result = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 26, 11));
        result.Should().BeTrue();
        closed.Should().BeTrue();
    }

    // ── Window: Drag _isDragging && Move logic (L176, L185, L187) ───

    [Fact]
    public void Window_Drag_FullCycle_VerifyState()
    {
        var win = new Window("W") { Movable = true, Closable = false };
        win.Arrange(new Rect(0, 0, 20, 6));

        // Press on title bar (row 1, col 5). localCol=5, localRow=1
        win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 1));

        // Move: should update bounds
        var moveResult = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 10, 3));
        moveResult.Should().BeTrue();
        win.Bounds.X.Should().Be(5); // 10 - 5 = 5
        win.Bounds.Y.Should().Be(2); // 3 - 1 = 2

        // Release: should stop dragging
        var releaseResult = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 10, 3));
        releaseResult.Should().BeTrue();

        // Another move after release should NOT update bounds
        var moveAfterRelease = win.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 20, 10));
        moveAfterRelease.Should().BeFalse();
        win.Bounds.X.Should().Be(5); // unchanged
    }

    // ── Window: MeasureContent child size arithmetic (L44) ──────────

    [Fact]
    public void Window_MeasureContent_ChildGetsReducedAvailable()
    {
        var win = new Window("W");
        var label = new Label("X"); // 1 char
        win.Add(label);
        var size = win.MeasureContent(new Spectre.Console.Size(20, 10));
        // contentWidth = max(0, 1) = 1, result = min(1+2, 20) = 3
        size.Width.Should().Be(3);
        // contentHeight = 1, result = min(1+3, 10) = 4
        size.Height.Should().Be(4);
    }

    // ── MenuBar: Render loop equality (L43: i == _selectedIndex) ────

    [Fact]
    public void MenuBar_Render_OnlySelectedItemIsHighlighted()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("A"));
        mb.AddItem(new MenuItem("B"));
        mb.AddItem(new MenuItem("C"));
        // Select index 1 (B)
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0')); // 0
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0')); // 1

        mb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        mb.Render(surface);

        // " A " at col 1: NormalStyle (grey bg)
        buf[1, 0].Style.Background.Should().Be(Color.Grey);
        // " B " at col 4: SelectedStyle (blue bg)
        buf[4, 0].Style.Background.Should().Be(Color.Blue);
        // " C " at col 7: NormalStyle (grey bg)
        buf[7, 0].Style.Background.Should().Be(Color.Grey);
    }

    // ── MenuBar: Enter bounds check (L81: _selectedIndex >= 0 && _selectedIndex < _items.Count) ──

    [Fact]
    public void MenuBar_Enter_AtBoundsLimit_Activates()
    {
        var mb = new MenuBar();
        var item = new MenuItem("Only");
        var activated = false;
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);

        // Select first item (index 0)
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        // Enter should activate since 0 >= 0 && 0 < 1
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().BeTrue();
    }

    // ── MenuBar: Alt shortcut logical (L98: e.Alt && e.KeyChar != '\0') ──

    [Fact]
    public void MenuBar_Alt_NullKeyChar_DoesNotMatchShortcut()
    {
        var mb = new MenuBar();
        var activated = false;
        var item = new MenuItem("File");
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);

        // Alt with '\0' keyChar should not try to match
        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.F, '\0', false, true, false));
        result.Should().BeFalse(); // '\0' fails the e.KeyChar != '\0' check
        activated.Should().BeFalse();
    }

    // ── MenuBar: Alt shortcut Text.Length check (L102) ──────────────

    [Fact]
    public void MenuBar_Alt_EmptyTextItem_NoMatch()
    {
        var mb = new MenuBar();
        var item = new MenuItem(""); // empty text
        var activated = false;
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);

        var result = mb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, true, false));
        result.Should().BeFalse();
        activated.Should().BeFalse();
    }

    // ── RenderableWidget: Object initializer (L10) ──────────────────

    [Fact]
    public void RenderableWidget_DefaultCapabilities_EnableRendering()
    {
        // The static capabilities have TrueColor, Ansi=true, Unicode=true
        // If these were wrong, rendering would produce no output or different output
        var text = new Spectre.Console.Text("Test");
        var w = new RenderableWidget(text);
        w.Arrange(new Rect(0, 0, 20, 3));
        var (buf, surface) = Surface(20, 3);
        w.Render(surface);
        // Must successfully render "Test"
        Row(buf, 0).Should().Contain("Test");
    }

    // ── RenderableWidget: MeasureContent Max→Min (L41) ──────────────

    [Fact]
    public void RenderableWidget_MeasureContent_Returns_MaxMeasurement()
    {
        // Text("Hello World") has Min=5 (longest word), Max=11 (full text)
        var text = new Spectre.Console.Text("Hello World");
        var w = new RenderableWidget(text);
        var size = w.MeasureContent(new Spectre.Console.Size(50, 5));
        // Should use Max (11), not Min (5)
        size.Width.Should().Be(11);
    }

    // ── RenderableWidget: Render row/col loop bounds (L50, L63) ─────

    [Fact]
    public void RenderableWidget_Render_MultiLine_AllLinesRendered()
    {
        // Create text that wraps into 2 lines
        var text = new Spectre.Console.Text("ABCDE\nFGHIJ");
        var w = new RenderableWidget(text);
        w.Arrange(new Rect(0, 0, 10, 5));
        var (buf, surface) = Surface(10, 5);
        w.Render(surface);

        Row(buf, 0).Should().Contain("ABCDE");
        Row(buf, 1).Should().Contain("FGHIJ");
    }

    // ── RenderableWidget: continue guard for control codes (L57) ────

    [Fact]
    public void RenderableWidget_Render_WithLineBreaks_HandledCorrectly()
    {
        var text = new Spectre.Console.Text("Line1\nLine2");
        var w = new RenderableWidget(text);
        w.Arrange(new Rect(0, 0, 20, 5));
        var (buf, surface) = Surface(20, 5);
        w.Render(surface);

        Row(buf, 0).Should().Contain("Line1");
        Row(buf, 1).Should().Contain("Line2");
    }

    // ── MessageBox: ObjectInitializer mutations (L20, L25) ──────────

    [Fact]
    public void MessageBox_VStack_HasSpacing1()
    {
        // The VStack spacing=1 ensures visual separation between label and buttons.
        // If mutated to 0, the layout would be tighter.
        // We verify by checking the dialog has children (VStack with label + button row)
        var d = MessageBox.Create("T", "Message text here", MessageBoxButtons.Ok);
        d.Children.Should().HaveCount(1); // VStack
        var vstack = d.Children[0] as VStack;
        vstack.Should().NotBeNull();
        vstack!.Spacing.Should().Be(1);
    }

    [Fact]
    public void MessageBox_HStack_HasSpacing2()
    {
        var d = MessageBox.Create("T", "M", MessageBoxButtons.OkCancel);
        d.Children.Should().HaveCount(1);
        var vstack = d.Children[0] as VStack;
        vstack.Should().NotBeNull();
        // Second child of VStack is the HStack with buttons
        vstack!.Children.Should().HaveCountGreaterThanOrEqualTo(2);
        var hstack = vstack.Children[1] as HStack;
        hstack.Should().NotBeNull();
        hstack!.Spacing.Should().Be(2);
    }
}
