using FluentAssertions;
using Spectre.Console;
using Xunit;
using TuiTreeNode = Spectre.Console.Tui.Widgets.Controls.TreeNode;

namespace Spectre.Console.Tui.Tests;

/// <summary>
/// Mutation-killing tests for widgets — rendering, events, and edge cases.
/// </summary>
public sealed class MutantKillerWidgetTests
{
    // ── Helpers ──────────────────────────────────────────────────────

    private static BufferSurface Arrange(Widget widget, int w, int h)
    {
        widget.Arrange(new Rect(0, 0, w, h));
        var buf = new ScreenBuffer(w, h);
        return new BufferSurface(buf);
    }

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

    // ── Label ───────────────────────────────────────────────────────

    [Fact]
    public void Label_Render_SingleLine()
    {
        var label = new Label("Hello");
        label.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        label.Render(surface);
        Row(buf, 0).Should().StartWith("Hello");
    }

    [Fact]
    public void Label_Render_MultiLine()
    {
        var label = new Label("A\nB\nC");
        label.Arrange(new Rect(0, 0, 10, 3));
        var (buf, surface) = Surface(10, 3);
        label.Render(surface);
        Row(buf, 0).Should().StartWith("A");
        Row(buf, 1).Should().StartWith("B");
        Row(buf, 2).Should().StartWith("C");
    }

    [Fact]
    public void Label_MeasureContent_WidthIsMaxLine()
    {
        var label = new Label("Short\nLonger line");
        var size = label.MeasureContent(new Spectre.Console.Size(100, 100));
        size.Width.Should().Be(11); // "Longer line"
        size.Height.Should().Be(2);
    }

    [Fact]
    public void Label_Render_WithStyle()
    {
        var style = new Style(Color.Red);
        var label = new Label("X") { LabelStyle = style };
        label.Arrange(new Rect(0, 0, 5, 1));
        var (buf, surface) = Surface(5, 1);
        label.Render(surface);
        buf[0, 0].Character.Should().Be('X');
        buf[0, 0].Style.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void Label_Text_Setter()
    {
        var label = new Label("old");
        label.Text.Should().Be("old");
        label.Text = "new";
        label.Text.Should().Be("new");
    }

    // ── Button ──────────────────────────────────────────────────────

    [Fact]
    public void Button_Render_ShowsBrackets()
    {
        var button = new Button("OK");
        button.CanFocus = true;
        button.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        button.Render(surface);
        var row = Row(buf, 0);
        row.Should().Contain("[");
        row.Should().Contain("OK");
        row.Should().Contain("]");
    }

    [Fact]
    public void Button_Enter_FiresClicked()
    {
        var button = new Button("Go");
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;
        button.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        clicked.Should().BeTrue();
    }

    [Fact]
    public void Button_Space_FiresClicked()
    {
        var button = new Button("Go");
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;
        button.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        clicked.Should().BeTrue();
    }

    [Fact]
    public void Button_LeftClick_FiresClicked()
    {
        var button = new Button("Go");
        button.Arrange(new Rect(0, 0, 10, 1));
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;
        button.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 2, 0, false, false, false));
        clicked.Should().BeTrue();
    }

    [Fact]
    public void Button_RightClick_DoesNotFire()
    {
        var button = new Button("Go");
        button.Arrange(new Rect(0, 0, 10, 1));
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;
        button.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 2, 0, false, false, false));
        clicked.Should().BeFalse();
    }

    [Fact]
    public void Button_OtherKey_DoesNotFire()
    {
        var button = new Button("Go");
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;
        button.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, false, false));
        clicked.Should().BeFalse();
    }

    [Fact]
    public void Button_MeasureContent()
    {
        var button = new Button("Test");
        var size = button.MeasureContent(new Spectre.Console.Size(50, 5));
        // "[ Test ]" = 8 chars width
        size.Width.Should().Be(8);
        size.Height.Should().Be(1);
    }

    [Fact]
    public void Button_Render_FocusedHighlight()
    {
        var button = new Button("Hi");
        button.CanFocus = true;
        button.OnFocusGained();
        button.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        button.Render(surface);
        // When focused, uses FocusedStyle
        Row(buf, 0).Should().Contain("Hi");
    }

    // ── CheckBox ────────────────────────────────────────────────────

    [Fact]
    public void CheckBox_Render_Unchecked()
    {
        var cb = new CheckBox("Agree");
        cb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        cb.Render(surface);
        Row(buf, 0).Should().Contain("[ ]").And.Contain("Agree");
    }

    [Fact]
    public void CheckBox_Render_Checked()
    {
        var cb = new CheckBox("Agree") { IsChecked = true };
        cb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        cb.Render(surface);
        Row(buf, 0).Should().Contain("[x]").And.Contain("Agree");
    }

    [Fact]
    public void CheckBox_Space_Toggles()
    {
        var cb = new CheckBox("Test");
        cb.IsChecked.Should().BeFalse();
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        cb.IsChecked.Should().BeTrue();
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        cb.IsChecked.Should().BeFalse();
    }

    [Fact]
    public void CheckBox_Enter_Toggles()
    {
        var cb = new CheckBox("Test");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        cb.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void CheckBox_LeftClick_Toggles()
    {
        var cb = new CheckBox("Test");
        cb.Arrange(new Rect(0, 0, 15, 1));
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        cb.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void CheckBox_CheckedChanged_Event()
    {
        var cb = new CheckBox("Test");
        var eventFired = false;
        cb.CheckedChanged += (_, _) => eventFired = true;
        cb.IsChecked = true;
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void CheckBox_MeasureContent()
    {
        var cb = new CheckBox("ABC");
        var size = cb.MeasureContent(new Spectre.Console.Size(50, 5));
        // "[x] ABC" = 7 chars
        size.Width.Should().Be(7);
        size.Height.Should().Be(1);
    }

    [Fact]
    public void CheckBox_OtherKey_NoToggle()
    {
        var cb = new CheckBox("Test");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, false, false));
        cb.IsChecked.Should().BeFalse();
    }

    [Fact]
    public void CheckBox_RightClick_NoToggle()
    {
        var cb = new CheckBox("Test");
        cb.Arrange(new Rect(0, 0, 15, 1));
        cb.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 0, 0, false, false, false));
        cb.IsChecked.Should().BeFalse();
    }

    // ── RadioButton ─────────────────────────────────────────────────

    [Fact]
    public void RadioButton_Render_Unselected()
    {
        var rb = new RadioButton("Option");
        rb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        rb.Render(surface);
        Row(buf, 0).Should().Contain("( )").And.Contain("Option");
    }

    [Fact]
    public void RadioButton_Render_Selected()
    {
        var rb = new RadioButton("Option") { IsSelected = true };
        rb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        rb.Render(surface);
        Row(buf, 0).Should().Contain("(o)").And.Contain("Option");
    }

    [Fact]
    public void RadioButton_Space_Selects()
    {
        var rb = new RadioButton("Test");
        rb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_Enter_Selects()
    {
        var rb = new RadioButton("Test");
        rb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        rb.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_LeftClick_Selects()
    {
        var rb = new RadioButton("Test");
        rb.Arrange(new Rect(0, 0, 15, 1));
        rb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        rb.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_SelectionChanged_Event()
    {
        var rb = new RadioButton("Test");
        var eventFired = false;
        rb.SelectionChanged += (_, _) => eventFired = true;
        rb.IsSelected = true;
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_MeasureContent()
    {
        var rb = new RadioButton("XY");
        var size = rb.MeasureContent(new Spectre.Console.Size(50, 5));
        // "(o) XY" = 6 chars
        size.Width.Should().Be(6);
        size.Height.Should().Be(1);
    }

    [Fact]
    public void RadioButton_OtherKey_NoSelect()
    {
        var rb = new RadioButton("Test");
        rb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, false, false));
        rb.IsSelected.Should().BeFalse();
    }

    // ── RadioGroup ──────────────────────────────────────────────────

    [Fact]
    public void RadioGroup_MutualExclusion()
    {
        var group = new RadioGroup();
        var rb1 = new RadioButton("A");
        var rb2 = new RadioButton("B");
        group.Add(rb1);
        group.Add(rb2);

        rb1.IsSelected = true;
        rb1.IsSelected.Should().BeTrue();
        rb2.IsSelected.Should().BeFalse();

        rb2.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb2.IsSelected.Should().BeTrue();
        rb1.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void RadioGroup_Selected_ReturnsActiveButton()
    {
        var group = new RadioGroup();
        var rb1 = new RadioButton("A");
        var rb2 = new RadioButton("B");
        group.Add(rb1);
        group.Add(rb2);
        group.Selected.Should().BeNull();
        rb1.IsSelected = true;
        group.Selected.Should().Be(rb1);
    }

    [Fact]
    public void RadioGroup_Remove_ClearsGroupReference()
    {
        var group = new RadioGroup();
        var rb = new RadioButton("A");
        group.Add(rb);
        group.Remove(rb);
        // After removal, selecting should not affect group
        rb.IsSelected = true;
        group.Selected.Should().BeNull();
    }

    [Fact]
    public void RadioGroup_SelectionChanged_Event()
    {
        var group = new RadioGroup();
        var rb1 = new RadioButton("A");
        var rb2 = new RadioButton("B");
        group.Add(rb1);
        group.Add(rb2);
        var eventFired = false;
        group.SelectionChanged += (_, _) => eventFired = true;
        rb1.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        eventFired.Should().BeTrue();
    }

    // ── ProgressBar ─────────────────────────────────────────────────

    [Fact]
    public void ProgressBar_Render_ShowsFilledAndEmpty()
    {
        var pb = new ProgressBar { Value = 50, MaxValue = 100 };
        pb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        pb.Render(surface);
        var row = Row(buf, 0);
        // Should contain filled and empty chars
        row.Should().Contain("█");
        row.Should().Contain("░");
    }

    [Fact]
    public void ProgressBar_Render_ShowsPercentage()
    {
        var pb = new ProgressBar { Value = 50, ShowPercentage = true };
        pb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        pb.Render(surface);
        Row(buf, 0).Should().Contain("50%");
    }

    [Fact]
    public void ProgressBar_Render_HidesPercentage()
    {
        var pb = new ProgressBar { Value = 50, ShowPercentage = false };
        pb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        pb.Render(surface);
        Row(buf, 0).Should().NotContain("50%");
    }

    [Fact]
    public void ProgressBar_Value_ClampedToRange()
    {
        var pb = new ProgressBar { MaxValue = 100 };
        pb.Value = -10;
        pb.Value.Should().Be(0);
        pb.Value = 200;
        pb.Value.Should().Be(100);
    }

    [Fact]
    public void ProgressBar_ValueChanged_Event()
    {
        var pb = new ProgressBar();
        var eventFired = false;
        pb.ValueChanged += (_, _) => eventFired = true;
        pb.Value = 50;
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ProgressBar_ValueChanged_NoFireOnSameValue()
    {
        var pb = new ProgressBar { Value = 50 };
        var eventFired = false;
        pb.ValueChanged += (_, _) => eventFired = true;
        pb.Value = 50;
        eventFired.Should().BeFalse();
    }

    [Fact]
    public void ProgressBar_MeasureContent()
    {
        var pb = new ProgressBar();
        var size = pb.MeasureContent(new Spectre.Console.Size(100, 5));
        size.Height.Should().Be(1);
        size.Width.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProgressBar_Render_ZeroValue()
    {
        var pb = new ProgressBar { Value = 0 };
        pb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        pb.Render(surface);
        Row(buf, 0).Should().Contain("0%");
    }

    [Fact]
    public void ProgressBar_Render_FullValue()
    {
        var pb = new ProgressBar { Value = 100 };
        pb.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        pb.Render(surface);
        Row(buf, 0).Should().Contain("100%");
    }

    // ── Slider ──────────────────────────────────────────────────────

    [Fact]
    public void Slider_Render_ShowsTrackAndThumb()
    {
        var slider = new Slider { Value = 50, Minimum = 0, Maximum = 100 };
        slider.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        slider.Render(surface);
        var row = Row(buf, 0);
        row.Should().Contain("\u2500"); // ─ track
        row.Should().Contain("\u2588"); // █ thumb
    }

    [Fact]
    public void Slider_Render_ShowsValue()
    {
        var slider = new Slider { Value = 75, ShowValue = true };
        slider.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        slider.Render(surface);
        Row(buf, 0).Should().Contain("75");
    }

    [Fact]
    public void Slider_Render_HidesValue()
    {
        var slider = new Slider { Value = 75, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        slider.Render(surface);
        Row(buf, 0).Should().NotContain("75");
    }

    [Fact]
    public void Slider_Left_DecreasesValue()
    {
        var slider = new Slider { Value = 50, Step = 5 };
        slider.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        slider.Value.Should().Be(45);
    }

    [Fact]
    public void Slider_Right_IncreasesValue()
    {
        var slider = new Slider { Value = 50, Step = 5 };
        slider.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        slider.Value.Should().Be(55);
    }

    [Fact]
    public void Slider_Home_GoesToMin()
    {
        var slider = new Slider { Value = 50, Minimum = 10 };
        slider.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0', false, false, false));
        slider.Value.Should().Be(10);
    }

    [Fact]
    public void Slider_End_GoesToMax()
    {
        var slider = new Slider { Value = 50, Maximum = 90 };
        slider.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0', false, false, false));
        slider.Value.Should().Be(90);
    }

    [Fact]
    public void Slider_Value_Clamped()
    {
        var slider = new Slider { Minimum = 0, Maximum = 100 };
        slider.Value = -10;
        slider.Value.Should().Be(0);
        slider.Value = 200;
        slider.Value.Should().Be(100);
    }

    [Fact]
    public void Slider_ValueChanged_Event()
    {
        var slider = new Slider();
        var eventFired = false;
        slider.ValueChanged += (_, _) => eventFired = true;
        slider.Value = 42;
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void Slider_MouseClick_SetsValue()
    {
        var slider = new Slider { Minimum = 0, Maximum = 100 };
        slider.Arrange(new Rect(0, 0, 20, 1));
        slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 0, false, false, false));
        slider.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Slider_MeasureContent()
    {
        var slider = new Slider();
        var size = slider.MeasureContent(new Spectre.Console.Size(100, 5));
        size.Height.Should().Be(1);
    }

    [Fact]
    public void Slider_OtherKey_NoChange()
    {
        var slider = new Slider { Value = 50 };
        slider.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, false, false));
        slider.Value.Should().Be(50);
    }

    // ── Widget base class ───────────────────────────────────────────

    [Fact]
    public void Widget_Visible_Default()
    {
        var label = new Label("X");
        label.Visible.Should().BeTrue();
        label.CanFocus.Should().BeFalse();
        label.HasFocus.Should().BeFalse();
        label.TabIndex.Should().Be(0);
        label.Parent.Should().BeNull();
    }

    [Fact]
    public void Widget_Invalidate_SetsFlags()
    {
        var label = new Label("X");
        label.MarkRendered();
        label.Invalidate();
        label.NeedsRender.Should().BeTrue();
        label.NeedsLayout.Should().BeTrue();
    }

    [Fact]
    public void Widget_Constraints()
    {
        var label = new Label("X");
        label.WidthConstraint.Should().BeNull();
        label.HeightConstraint.Should().BeNull();
        label.WidthConstraint = Constraint.Fixed(10);
        label.WidthConstraint.Should().NotBeNull();
        label.WidthConstraint!.Value.Resolve(100).Should().Be(10);
    }

    [Fact]
    public void Widget_Margin_Padding()
    {
        var label = new Label("X");
        label.Margin.Should().Be(Margin.None);
        label.Padding.Should().Be(Margin.None);
        label.Margin = new Margin(2);
        label.Margin.Left.Should().Be(2);
    }

    [Fact]
    public void Widget_GetChildren_EmptyForLeaf()
    {
        var label = new Label("X");
        label.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void Widget_FocusCallbacks()
    {
        // OnFocusGained/OnFocusLost are virtual callbacks - HasFocus is set externally
        var button = new Button("X");
        button.CanFocus = true;
        button.HasFocus.Should().BeFalse();
        // These callbacks are just hooks — they don't set HasFocus directly
        button.OnFocusGained(); // should not throw
        button.OnFocusLost();   // should not throw
        button.HasFocus.Should().BeFalse(); // unchanged
    }

    // ── HitTester ───────────────────────────────────────────────────

    [Fact]
    public void HitTester_ReturnsDeepestWidget()
    {
        var container = new VStack();
        var btn = new Button("X");
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));
        btn.Arrange(new Rect(0, 0, 20, 1));

        var hit = HitTester.HitTest(container, 5, 0);
        hit.Should().Be(btn);
    }

    [Fact]
    public void HitTester_MissesInvisibleWidgets()
    {
        var container = new VStack();
        var btn = new Button("X") { Visible = false };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));
        btn.Arrange(new Rect(0, 0, 20, 1));

        var hit = HitTester.HitTest(container, 5, 0);
        hit.Should().NotBe(btn);
    }

    [Fact]
    public void HitTester_ReturnsNull_OutOfBounds()
    {
        var label = new Label("X");
        label.Arrange(new Rect(0, 0, 5, 1));
        var hit = HitTester.HitTest(label, 50, 50);
        hit.Should().BeNull();
    }

    // ── FocusManager ────────────────────────────────────────────────

    [Fact]
    public void FocusManager_RebuildChain_CollectsFocusable()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 1 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 0 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().Be(btn2); // lower TabIndex first
    }

    [Fact]
    public void FocusManager_MoveFocus_Forward()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().Be(btn1);
        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().Be(btn2);
    }

    [Fact]
    public void FocusManager_MoveFocus_Backward()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.MoveFocus(FocusDirection.Backward);
        fm.Focused.Should().Be(btn2); // wraps to end
    }

    [Fact]
    public void FocusManager_SetFocus()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true };
        var btn2 = new Button("B") { CanFocus = true };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.SetFocus(btn2);
        fm.Focused.Should().Be(btn2);
        btn2.HasFocus.Should().BeTrue();
        btn1.HasFocus.Should().BeFalse();
    }

    [Fact]
    public void FocusManager_RemoveFromChain()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true };
        var btn2 = new Button("B") { CanFocus = true };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.SetFocus(btn1);
        fm.RemoveFromChain(btn1);
        fm.Focused.Should().Be(btn2);
    }

    [Fact]
    public void FocusManager_MoveFocus_Wraps()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("Only") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().Be(btn); // single item wraps to itself
    }

    [Fact]
    public void FocusManager_EmptyChain_NoFocused()
    {
        var fm = new FocusManager();
        var container = new VStack();
        container.Arrange(new Rect(0, 0, 20, 5));
        fm.RebuildChain(container);
        fm.Focused.Should().BeNull();
    }

    [Fact]
    public void FocusManager_InvisibleWidget_SkippedInChain()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, Visible = false };
        var btn2 = new Button("B") { CanFocus = true };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().Be(btn2);
    }
}
