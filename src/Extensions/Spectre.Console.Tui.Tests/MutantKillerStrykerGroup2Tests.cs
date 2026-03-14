using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Controls;
using Spectre.Console.Tui.Widgets.Containers;
using Xunit;

namespace Spectre.Console.Tui.Tests;

/// <summary>
/// Mutation-killing tests for GROUP 2: Simple widgets + Containers.
/// Targets specific surviving/NoCoverage mutants in Label, Button, CheckBox,
/// RadioButton, RadioGroup, ProgressBar, Slider, VStack, HStack, ContainerWidget, Splitter.
/// </summary>
public sealed class MutantKillerStrykerGroup2Tests
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

    // ═══════════════════════════════════════════════════════════════════
    // Label — NoCov L16 (string), L33 (string); Survived L17 (statement),
    //   L27 (statement), L34 (null coalescing), L52 (equality)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Label_Constructor_NullText_BecomesEmpty()
    {
        // L33: _text = text ?? string.Empty — kills null coalescing mutant
        var label = new Label(null!);
        label.Text.Should().Be(string.Empty, "null constructor arg should coalesce to empty string");
    }

    [Fact]
    public void Label_Constructor_NullStyle_BecomesPlain()
    {
        // L34: _style = style ?? Style.Plain — kills null coalescing mutant
        var label = new Label("Hi");
        // Default style param is null, so it should become Style.Plain
        label.LabelStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void Label_Constructor_ExplicitStyle_UsesIt()
    {
        // L34: Verify that passing a non-null style keeps it (not replaced by Plain)
        var style = new Style(Color.Red);
        var label = new Label("Hi", style);
        label.LabelStyle.Should().Be(style);
    }

    [Fact]
    public void Label_TextSetter_NullValue_CoalescesToEmpty()
    {
        // L16: _text = value ?? string.Empty — kills NoCov on string
        var label = new Label("Hello");
        label.Text = null!;
        label.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void Label_TextSetter_Invalidates()
    {
        // L17: Invalidate() — kills statement removal mutant
        var label = new Label("A");
        label.MarkRendered();
        label.NeedsRender.Should().BeFalse();
        label.Text = "B";
        label.NeedsRender.Should().BeTrue("setting Text should call Invalidate()");
    }

    [Fact]
    public void Label_StyleSetter_Invalidates()
    {
        // L27: Invalidate() — kills statement removal mutant
        var label = new Label("X");
        label.MarkRendered();
        label.NeedsRender.Should().BeFalse();
        label.LabelStyle = new Style(Color.Green);
        label.NeedsRender.Should().BeTrue("setting LabelStyle should call Invalidate()");
    }

    [Fact]
    public void Label_Render_RowBoundaryCheck()
    {
        // L52: row < surface.Height — kills equality mutant (< vs <=)
        // Use surface height exactly equal to line count to ensure boundary is correct
        var label = new Label("Line1\nLine2");
        label.Arrange(new Rect(0, 0, 10, 2));
        var (buf, surface) = Surface(10, 2);
        label.Render(surface);
        Row(buf, 0).Should().StartWith("Line1");
        Row(buf, 1).Should().StartWith("Line2");
    }

    [Fact]
    public void Label_Render_ExactHeightMatch_NoOverflow()
    {
        // L52: row < surface.Height — text has 3 lines, surface has 2 rows
        // Only first 2 lines should render, not overflow
        var label = new Label("A\nB\nC");
        label.Arrange(new Rect(0, 0, 5, 2));
        var (buf, surface) = Surface(5, 2);
        label.Render(surface);
        Row(buf, 0).Should().StartWith("A");
        Row(buf, 1).Should().StartWith("B");
        // Line C should not appear — only 2 rows available
    }

    // ═══════════════════════════════════════════════════════════════════
    // Button — NoCov L15-16 (constructor defaults), L28 (render string),
    //   L45 (block); Survived L41 (conditional), L57 (statement), L69-70 (boolean)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Button_Constructor_NullText_CoalescesToEmpty()
    {
        // L28: _text = text ?? string.Empty — NoCov on string
        var button = new Button(null!);
        button.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void Button_TextSetter_NullValue_CoalescesToEmpty()
    {
        // L15: _text = value ?? string.Empty — NoCov
        var button = new Button("OK");
        button.Text = null!;
        button.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void Button_TextSetter_Invalidates()
    {
        // L16: Invalidate() — NoCov
        var button = new Button("A");
        button.MarkRendered();
        button.NeedsRender.Should().BeFalse();
        button.Text = "B";
        button.NeedsRender.Should().BeTrue("setting Text should call Invalidate()");
    }

    [Fact]
    public void Button_Render_Unfocused_UsesNormalStyle()
    {
        // L41: HasFocus ? FocusedStyle : NormalStyle — kills conditional mutant
        var button = new Button("OK");
        button.HasFocus = false;
        var normalStyle = new Style(Color.Yellow, Color.Black);
        button.NormalStyle = normalStyle;
        button.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        button.Render(surface);
        // The [ character should have normal style
        buf[0, 0].Style.Foreground.Should().Be(Color.Yellow, "unfocused button should use NormalStyle");
    }

    [Fact]
    public void Button_Render_Focused_UsesFocusedStyle()
    {
        // L41: HasFocus ? FocusedStyle : NormalStyle — verify focused path
        var button = new Button("OK");
        button.HasFocus = true;
        var focusedStyle = new Style(Color.Magenta1, Color.Black);
        button.FocusedStyle = focusedStyle;
        button.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        button.Render(surface);
        buf[0, 0].Style.Foreground.Should().Be(Color.Magenta1, "focused button should use FocusedStyle");
    }

    [Fact]
    public void Button_Render_TruncatesLongText()
    {
        // L45: if (display.Length > surface.Width) — kills block removal
        var button = new Button("VeryLongButtonText");
        button.Arrange(new Rect(0, 0, 6, 1));
        var (buf, surface) = Surface(6, 1);
        button.Render(surface);
        // "[ VeryLongButtonText ]" is 24 chars, truncated to 6
        var row = Row(buf, 0);
        row.Length.Should().Be(6);
        row.Should().Be("[ Very");
    }

    [Fact]
    public void Button_OnKeyEvent_Enter_Invalidates()
    {
        // L57: Invalidate() — kills statement removal
        var button = new Button("Go");
        button.MarkRendered();
        button.NeedsRender.Should().BeFalse();
        button.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        button.NeedsRender.Should().BeTrue("Enter key should invalidate the button");
    }

    [Fact]
    public void Button_OnKeyEvent_Enter_ReturnsTrue()
    {
        // L58: return true — kills boolean mutant
        var button = new Button("Go");
        var result = button.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        result.Should().BeTrue("Enter key should return true (handled)");
    }

    [Fact]
    public void Button_OnKeyEvent_Other_ReturnsFalse()
    {
        // L61: return false — kills boolean mutant
        var button = new Button("Go");
        var result = button.OnKeyEvent(new KeyEvent(ConsoleKey.Tab, '\t', false, false, false));
        result.Should().BeFalse("unhandled key should return false");
    }

    [Fact]
    public void Button_OnMouseEvent_LeftPress_Invalidates()
    {
        // L69: Invalidate() — kills statement removal (survived)
        var button = new Button("Go");
        button.MarkRendered();
        button.NeedsRender.Should().BeFalse();
        button.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        button.NeedsRender.Should().BeTrue("left click should invalidate the button");
    }

    [Fact]
    public void Button_OnMouseEvent_LeftPress_ReturnsTrue()
    {
        // L70: return true — kills boolean mutant (survived)
        var button = new Button("Go");
        var result = button.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        result.Should().BeTrue("left press should return true (handled)");
    }

    [Fact]
    public void Button_OnMouseEvent_Release_ReturnsFalse()
    {
        // L73: return false — kills boolean mutant
        var button = new Button("Go");
        var result = button.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 0, 0, false, false, false));
        result.Should().BeFalse("release event should return false");
    }

    [Fact]
    public void Button_OnMouseEvent_RightPress_ReturnsFalse()
    {
        // Check the && condition: right button does not fire
        var button = new Button("Go");
        var result = button.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 0, 0, false, false, false));
        result.Should().BeFalse("right press should return false");
    }

    // ═══════════════════════════════════════════════════════════════════
    // CheckBox — NoCov L16-17, L42, L60; Survived L29, L55, L59, L72-86
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void CheckBox_Constructor_NullText_CoalescesToEmpty()
    {
        // L42: _text = text ?? string.Empty — NoCov
        var cb = new CheckBox(null!);
        cb.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void CheckBox_Constructor_WithChecked_SetsValue()
    {
        // L16-17: constructor defaults — NoCov
        var cb = new CheckBox("Test", true);
        cb.IsChecked.Should().BeTrue();
        cb.Text.Should().Be("Test");
    }

    [Fact]
    public void CheckBox_TextSetter_NullValue_CoalescesToEmpty()
    {
        // L16: _text = value ?? string.Empty — NoCov
        var cb = new CheckBox("Test");
        cb.Text = null!;
        cb.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void CheckBox_TextSetter_Invalidates()
    {
        // L17: Invalidate() — NoCov
        var cb = new CheckBox("A");
        cb.MarkRendered();
        cb.NeedsRender.Should().BeFalse();
        cb.Text = "B";
        cb.NeedsRender.Should().BeTrue();
    }

    [Fact]
    public void CheckBox_IsCheckedSetter_Invalidates()
    {
        // L29: Invalidate() — Survived statement removal
        var cb = new CheckBox("Test");
        cb.MarkRendered();
        cb.NeedsRender.Should().BeFalse();
        cb.IsChecked = true;
        cb.NeedsRender.Should().BeTrue("IsChecked setter should invalidate");
    }

    [Fact]
    public void CheckBox_IsCheckedSetter_SameValue_NoEvent()
    {
        // L26: if (_isChecked != value) — the guard condition
        var cb = new CheckBox("Test", true);
        var eventFired = false;
        cb.CheckedChanged += (_, _) => eventFired = true;
        cb.IsChecked = true; // same value
        eventFired.Should().BeFalse("setting same value should not fire event");
    }

    [Fact]
    public void CheckBox_Render_FocusedUsedFocusedStyle()
    {
        // L55: HasFocus ? FocusedStyle : NormalStyle — Survived conditional
        var cb = new CheckBox("Test");
        cb.HasFocus = true;
        var focusedStyle = new Style(Color.Red);
        cb.FocusedStyle = focusedStyle;
        cb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        cb.Render(surface);
        buf[0, 0].Style.Foreground.Should().Be(Color.Red, "focused checkbox should use FocusedStyle");
    }

    [Fact]
    public void CheckBox_Render_UnfocusedUsesNormalStyle()
    {
        // L55: the else branch
        var cb = new CheckBox("Test");
        cb.HasFocus = false;
        var normalStyle = new Style(Color.Green);
        cb.NormalStyle = normalStyle;
        cb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        cb.Render(surface);
        buf[0, 0].Style.Foreground.Should().Be(Color.Green, "unfocused checkbox should use NormalStyle");
    }

    [Fact]
    public void CheckBox_Render_Truncates()
    {
        // L59-60: if (display.Length > surface.Width) — Survived equality + block
        var cb = new CheckBox("VeryLongCheckboxLabel");
        cb.Arrange(new Rect(0, 0, 8, 1));
        var (buf, surface) = Surface(8, 1);
        cb.Render(surface);
        var row = Row(buf, 0);
        row.Length.Should().Be(8);
        row.Should().Be("[ ] Very");
    }

    [Fact]
    public void CheckBox_OnKeyEvent_Space_ReturnsTrue()
    {
        // L72: return true — Survived boolean
        var cb = new CheckBox("Test");
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        result.Should().BeTrue();
    }

    [Fact]
    public void CheckBox_OnKeyEvent_Enter_ReturnsTrue()
    {
        // L72: return true — covers Enter branch
        var cb = new CheckBox("Test");
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        result.Should().BeTrue();
    }

    [Fact]
    public void CheckBox_OnKeyEvent_Other_ReturnsFalse()
    {
        // L75: return false — Survived boolean
        var cb = new CheckBox("Test");
        var result = cb.OnKeyEvent(new KeyEvent(ConsoleKey.Tab, '\t', false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void CheckBox_OnMouseEvent_LeftPress_ReturnsTrue()
    {
        // L83: return true — Survived boolean
        var cb = new CheckBox("Test");
        var result = cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        result.Should().BeTrue();
    }

    [Fact]
    public void CheckBox_OnMouseEvent_Release_ReturnsFalse()
    {
        // L86: return false — Survived boolean
        var cb = new CheckBox("Test");
        var result = cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 0, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void CheckBox_OnMouseEvent_RightPress_ReturnsFalse()
    {
        // Tests the && condition: right button should not toggle
        var cb = new CheckBox("Test");
        var result = cb.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 0, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void CheckBox_CheckedChanged_EventValue_MatchesNewState()
    {
        // L30: CheckedChanged?.Invoke(this, _isChecked) — verify the event arg
        var cb = new CheckBox("Test");
        bool? eventValue = null;
        cb.CheckedChanged += (_, val) => eventValue = val;
        cb.IsChecked = true;
        eventValue.Should().Be(true);
        cb.IsChecked = false;
        eventValue.Should().Be(false);
    }

    // ═══════════════════════════════════════════════════════════════════
    // RadioButton — NoCov L16-17, L43, L61, L87; Survived L29, L56, L60, L73-84
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void RadioButton_Constructor_NullText_CoalescesToEmpty()
    {
        // L43: _text = text ?? string.Empty — NoCov
        var rb = new RadioButton(null!);
        rb.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void RadioButton_Constructor_WithSelected_SetsValue()
    {
        // L16-17: constructor defaults — NoCov
        var rb = new RadioButton("Test", true);
        rb.IsSelected.Should().BeTrue();
        rb.Text.Should().Be("Test");
    }

    [Fact]
    public void RadioButton_TextSetter_NullValue_CoalescesToEmpty()
    {
        // L16: _text = value ?? string.Empty — NoCov
        var rb = new RadioButton("Test");
        rb.Text = null!;
        rb.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void RadioButton_TextSetter_Invalidates()
    {
        // L17: Invalidate() — NoCov
        var rb = new RadioButton("A");
        rb.MarkRendered();
        rb.NeedsRender.Should().BeFalse();
        rb.Text = "B";
        rb.NeedsRender.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_IsSelectedSetter_Invalidates()
    {
        // L29: Invalidate() — Survived statement removal
        var rb = new RadioButton("Test");
        rb.MarkRendered();
        rb.NeedsRender.Should().BeFalse();
        rb.IsSelected = true;
        rb.NeedsRender.Should().BeTrue("IsSelected setter should invalidate");
    }

    [Fact]
    public void RadioButton_IsSelectedSetter_SameValue_NoEvent()
    {
        // L26: if (_isSelected != value) — guard condition
        var rb = new RadioButton("Test", true);
        var eventFired = false;
        rb.SelectionChanged += (_, _) => eventFired = true;
        rb.IsSelected = true; // same value
        eventFired.Should().BeFalse("setting same value should not fire event");
    }

    [Fact]
    public void RadioButton_Render_FocusedUsesFocusedStyle()
    {
        // L56: HasFocus ? FocusedStyle : NormalStyle — Survived conditional
        var rb = new RadioButton("Test");
        rb.HasFocus = true;
        var focusedStyle = new Style(Color.Red);
        rb.FocusedStyle = focusedStyle;
        rb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        rb.Render(surface);
        buf[0, 0].Style.Foreground.Should().Be(Color.Red, "focused radio should use FocusedStyle");
    }

    [Fact]
    public void RadioButton_Render_UnfocusedUsesNormalStyle()
    {
        // L56: the else branch
        var rb = new RadioButton("Test");
        rb.HasFocus = false;
        var normalStyle = new Style(Color.Green);
        rb.NormalStyle = normalStyle;
        rb.Arrange(new Rect(0, 0, 15, 1));
        var (buf, surface) = Surface(15, 1);
        rb.Render(surface);
        buf[0, 0].Style.Foreground.Should().Be(Color.Green, "unfocused radio should use NormalStyle");
    }

    [Fact]
    public void RadioButton_Render_Truncates()
    {
        // L60-61: if (display.Length > surface.Width) — Survived equality + block
        var rb = new RadioButton("VeryLongRadioLabel");
        rb.Arrange(new Rect(0, 0, 8, 1));
        var (buf, surface) = Surface(8, 1);
        rb.Render(surface);
        var row = Row(buf, 0);
        row.Length.Should().Be(8);
        row.Should().Be("( ) Very");
    }

    [Fact]
    public void RadioButton_OnKeyEvent_Space_ReturnsTrue()
    {
        // L73: return true — Survived boolean
        var rb = new RadioButton("Test");
        var result = rb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        result.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_OnKeyEvent_Enter_ReturnsTrue()
    {
        var rb = new RadioButton("Test");
        var result = rb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r', false, false, false));
        result.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_OnKeyEvent_Other_ReturnsFalse()
    {
        // L76: return false — Survived boolean
        var rb = new RadioButton("Test");
        var result = rb.OnKeyEvent(new KeyEvent(ConsoleKey.Tab, '\t', false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void RadioButton_OnMouseEvent_LeftPress_ReturnsTrue()
    {
        // L84: return true — Survived boolean
        var rb = new RadioButton("Test");
        var result = rb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        result.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_OnMouseEvent_Release_ReturnsFalse()
    {
        // L87: return false — NoCov
        var rb = new RadioButton("Test");
        var result = rb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 0, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void RadioButton_OnMouseEvent_RightPress_ReturnsFalse()
    {
        var rb = new RadioButton("Test");
        var result = rb.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 0, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void RadioButton_SelectionChanged_EventValue_MatchesNewState()
    {
        // L30: SelectionChanged?.Invoke(this, _isSelected) — verify event arg
        var rb = new RadioButton("Test");
        bool? eventValue = null;
        rb.SelectionChanged += (_, val) => eventValue = val;
        rb.IsSelected = true;
        eventValue.Should().Be(true);
    }

    [Fact]
    public void RadioButton_WithGroup_SelectDelegatesToGroup()
    {
        // L92-98: Select() delegates to Group.Select when Group != null
        var group = new RadioGroup();
        var rb1 = new RadioButton("A");
        var rb2 = new RadioButton("B");
        group.Add(rb1);
        group.Add(rb2);

        // Select rb1 via key
        rb1.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb1.IsSelected.Should().BeTrue();
        rb2.IsSelected.Should().BeFalse();

        // Select rb2 via key - mutual exclusion via group
        rb2.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb2.IsSelected.Should().BeTrue();
        rb1.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void RadioButton_WithoutGroup_SelectSetsDirectly()
    {
        // L96-98: else { IsSelected = true; } — no group path
        var rb = new RadioButton("Solo");
        rb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb.IsSelected.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // RadioGroup — Survived L18 (statement), L26 (statement)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void RadioGroup_Add_SetsGroupReference()
    {
        // L18: ArgumentNullException.ThrowIfNull(button) — statement
        // L20: button.Group = this — statement
        var group = new RadioGroup();
        var rb = new RadioButton("A");
        group.Add(rb);
        group.Buttons.Should().Contain(rb);
        // After adding, selecting via key should go through group
        rb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb.IsSelected.Should().BeTrue();
        group.Selected.Should().Be(rb);
    }

    [Fact]
    public void RadioGroup_Add_NullThrows()
    {
        // L18: ArgumentNullException.ThrowIfNull(button) — kills statement removal
        var group = new RadioGroup();
        var act = () => group.Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RadioGroup_Remove_NullThrows()
    {
        // L26: ArgumentNullException.ThrowIfNull(button) — kills statement removal
        var group = new RadioGroup();
        var act = () => group.Remove(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RadioGroup_Remove_ClearsGroupAndRemovesFromList()
    {
        // L28-29: button.Group = null; _buttons.Remove(button);
        var group = new RadioGroup();
        var rb = new RadioButton("A");
        group.Add(rb);
        group.Buttons.Should().HaveCount(1);
        group.Remove(rb);
        group.Buttons.Should().BeEmpty();
        // After removal, the button should select without group
        rb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb.IsSelected.Should().BeTrue();
        group.Selected.Should().BeNull("removed button should not affect group");
    }

    [Fact]
    public void RadioGroup_Select_DeselectsOthers()
    {
        // L34-40: loop deselects non-target buttons
        var group = new RadioGroup();
        var rb1 = new RadioButton("A");
        var rb2 = new RadioButton("B");
        var rb3 = new RadioButton("C");
        group.Add(rb1);
        group.Add(rb2);
        group.Add(rb3);

        rb1.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb1.IsSelected.Should().BeTrue();
        rb2.IsSelected.Should().BeFalse();
        rb3.IsSelected.Should().BeFalse();

        rb3.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        rb3.IsSelected.Should().BeTrue();
        rb1.IsSelected.Should().BeFalse();
        rb2.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void RadioGroup_SelectionChanged_FiresWithSelectedButton()
    {
        // L43: SelectionChanged?.Invoke(this, button) — verify the event arg
        var group = new RadioGroup();
        var rb1 = new RadioButton("A");
        var rb2 = new RadioButton("B");
        group.Add(rb1);
        group.Add(rb2);
        RadioButton? eventArg = null;
        group.SelectionChanged += (_, btn) => eventArg = btn;
        rb2.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' ', false, false, false));
        eventArg.Should().Be(rb2);
    }

    // ═══════════════════════════════════════════════════════════════════
    // ProgressBar — NoCov L47; Survived L16, L19, L36, L42, L45, L53, L55
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ProgressBar_Value_Clamped_UsesMinMax()
    {
        // L15-16: Math.Clamp(value, 0.0, MaxValue) and equality check
        var pb = new ProgressBar { MaxValue = 50 };
        pb.Value = 60;
        pb.Value.Should().Be(50.0, "value should clamp to MaxValue");
        pb.Value = -5;
        pb.Value.Should().Be(0.0, "value should clamp to 0");
    }

    [Fact]
    public void ProgressBar_ValueSetter_Invalidates()
    {
        // L19: Invalidate() — Survived statement
        var pb = new ProgressBar();
        pb.MarkRendered();
        pb.NeedsRender.Should().BeFalse();
        pb.Value = 25;
        pb.NeedsRender.Should().BeTrue("setting Value should invalidate");
    }

    [Fact]
    public void ProgressBar_ValueSetter_FiresEventWithExactValue()
    {
        // L20: ValueChanged?.Invoke(this, _value) — verify exact arg
        var pb = new ProgressBar();
        double? eventValue = null;
        pb.ValueChanged += (_, v) => eventValue = v;
        pb.Value = 42;
        eventValue.Should().Be(42.0);
    }

    [Fact]
    public void ProgressBar_MeasureContent_UsesMin()
    {
        // L36: Math.Min(20, available.Width) — kills Min/Max mutation
        var pb = new ProgressBar();
        var size = pb.MeasureContent(new Spectre.Console.Size(10, 5));
        size.Width.Should().Be(10, "should use Min(20, 10) = 10");
        var size2 = pb.MeasureContent(new Spectre.Console.Size(30, 5));
        size2.Width.Should().Be(20, "should use Min(20, 30) = 20");
    }

    [Fact]
    public void ProgressBar_Render_PercentageText_CorrectValue()
    {
        // L42: (int)(Value / MaxValue * 100) — verify exact percentage
        var pb = new ProgressBar { Value = 25, MaxValue = 100, ShowPercentage = true };
        pb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        pb.Render(surface);
        Row(buf, 0).Should().Contain("25%");
    }

    [Fact]
    public void ProgressBar_Render_Conditional_ShowPercentageTrue()
    {
        // L42: ShowPercentage ? ... : string.Empty — kills conditional
        var pb = new ProgressBar { Value = 50, ShowPercentage = true };
        pb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        pb.Render(surface);
        Row(buf, 0).Should().Contain("50%");
    }

    [Fact]
    public void ProgressBar_Render_Conditional_ShowPercentageFalse()
    {
        var pb = new ProgressBar { Value = 50, ShowPercentage = false };
        pb.Arrange(new Rect(0, 0, 30, 1));
        var (buf, surface) = Surface(30, 1);
        pb.Render(surface);
        Row(buf, 0).Should().NotContain("%");
    }

    [Fact]
    public void ProgressBar_Render_ZeroBarWidth_Returns()
    {
        // L45: if (barWidth <= 0) return — NoCov L47
        var pb = new ProgressBar { Value = 50, ShowPercentage = true };
        pb.Arrange(new Rect(0, 0, 3, 1));
        var (buf, surface) = Surface(3, 1);
        // " 50%" is 4 chars, barWidth = 3 - 4 = -1, so should early return
        pb.Render(surface);
        // No crash and buffer should be empty
        Row(buf, 0).Should().Be("   ");
    }

    [Fact]
    public void ProgressBar_Render_FilledWidth_ExactCount()
    {
        // L53-55: col < filledWidth — kills equality mutant
        // At 50%, half the bar should be filled
        var pb = new ProgressBar { Value = 50, MaxValue = 100, ShowPercentage = false };
        pb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        pb.Render(surface);
        var row = Row(buf, 0);
        var filledCount = 0;
        var emptyCount = 0;
        for (int i = 0; i < 10; i++)
        {
            if (buf[i, 0].Character == '\u2588')
            {
                filledCount++;
            }
            else if (buf[i, 0].Character == '\u2591')
            {
                emptyCount++;
            }
        }

        filledCount.Should().Be(5, "50% of 10 = 5 filled");
        emptyCount.Should().Be(5, "remaining 5 should be empty");
    }

    [Fact]
    public void ProgressBar_Render_FilledUsesFilledStyle()
    {
        // Verify filled chars use FilledStyle, empty use EmptyStyle
        var filledStyle = new Style(Color.Red);
        var emptyStyle = new Style(Color.Blue);
        var pb = new ProgressBar
        {
            Value = 50,
            MaxValue = 100,
            ShowPercentage = false,
            FilledStyle = filledStyle,
            EmptyStyle = emptyStyle,
        };
        pb.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        pb.Render(surface);
        buf[0, 0].Style.Foreground.Should().Be(Color.Red, "filled region should use FilledStyle");
        buf[9, 0].Style.Foreground.Should().Be(Color.Blue, "empty region should use EmptyStyle");
    }

    [Fact]
    public void ProgressBar_EqualityThreshold_SmallChange_NoEvent()
    {
        // L16: Math.Abs(_value - clamped) > 0.001 — equality threshold
        var pb = new ProgressBar { Value = 50 };
        var eventFired = false;
        pb.ValueChanged += (_, _) => eventFired = true;
        pb.Value = 50.0005; // within threshold
        eventFired.Should().BeFalse("tiny change within 0.001 threshold should not fire");
    }

    [Fact]
    public void ProgressBar_EqualityThreshold_LargeChange_FiresEvent()
    {
        // L16: just above threshold
        var pb = new ProgressBar { Value = 50 };
        var eventFired = false;
        pb.ValueChanged += (_, _) => eventFired = true;
        pb.Value = 50.002;
        eventFired.Should().BeTrue("change above 0.001 threshold should fire");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Slider — Many survivors: L19, L32,38, L43, L49, L52-65, L86-97, L103-118
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Slider_ValueSetter_Invalidates()
    {
        // L19: Invalidate() — Survived statement
        var slider = new Slider();
        slider.MarkRendered();
        slider.NeedsRender.Should().BeFalse();
        slider.Value = 42;
        slider.NeedsRender.Should().BeTrue("setting Value should invalidate");
    }

    [Fact]
    public void Slider_ValueSetter_SameValue_NoEvent()
    {
        // L16: if (_value != clamped) — guard
        var slider = new Slider { Value = 50 };
        var eventFired = false;
        slider.ValueChanged += (_, _) => eventFired = true;
        slider.Value = 50;
        eventFired.Should().BeFalse("setting same value should not fire event");
    }

    [Fact]
    public void Slider_ShowValue_True()
    {
        // L32: ShowValue = true — default
        var slider = new Slider();
        slider.ShowValue.Should().BeTrue("default ShowValue should be true");
    }

    [Fact]
    public void Slider_CanFocus_True()
    {
        // L38: CanFocus = true — constructor sets it
        var slider = new Slider();
        slider.CanFocus.Should().BeTrue("Slider constructor should set CanFocus = true");
    }

    [Fact]
    public void Slider_MeasureContent_UsesMin()
    {
        // L43: Math.Min(20, available.Width) — kills Min/Max mutation
        var slider = new Slider();
        var size = slider.MeasureContent(new Spectre.Console.Size(10, 5));
        size.Width.Should().Be(10, "should use Min(20, 10) = 10");
        var size2 = slider.MeasureContent(new Spectre.Console.Size(30, 5));
        size2.Width.Should().Be(20, "should use Min(20, 30) = 20");
    }

    [Fact]
    public void Slider_Render_TrackWidthZero_Returns()
    {
        // L49/L52: if (trackWidth <= 0) return — kills conditional
        var slider = new Slider { Value = 50, ShowValue = true };
        // " 50" is 3 chars, surface width 2 => trackWidth = 2 - 3 = -1
        slider.Arrange(new Rect(0, 0, 2, 1));
        var (buf, surface) = Surface(2, 1);
        slider.Render(surface); // should not crash
        Row(buf, 0).Should().Be("  "); // nothing rendered
    }

    [Fact]
    public void Slider_Render_ThumbPosition_ExactCalculation()
    {
        // L57-58: range and thumbPos arithmetic — kills mutations
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 0, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 11, 1));
        var (buf, surface) = Surface(11, 1);
        slider.Render(surface);
        // Value=0 => thumbPos=0 (leftmost)
        buf[0, 0].Character.Should().Be('\u2588', "thumb at position 0 for value=0");
        buf[1, 0].Character.Should().Be('\u2500', "track at position 1");
    }

    [Fact]
    public void Slider_Render_ThumbAtEnd()
    {
        // L58: thumbPos = (trackWidth - 1) when value = max
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 100, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 11, 1));
        var (buf, surface) = Surface(11, 1);
        slider.Render(surface);
        buf[10, 0].Character.Should().Be('\u2588', "thumb at rightmost for value=max");
        buf[9, 0].Character.Should().Be('\u2500', "track before thumb");
    }

    [Fact]
    public void Slider_Render_ThumbAtMiddle()
    {
        // L63: col == thumbPos — kills equality mutant
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 11, 1));
        var (buf, surface) = Surface(11, 1);
        slider.Render(surface);
        // thumbPos = 50/100 * 10 = 5
        buf[5, 0].Character.Should().Be('\u2588', "thumb at position 5 for value=50");
    }

    [Fact]
    public void Slider_Render_FocusedThumbStyle()
    {
        // L65: HasFocus ? FocusedThumbStyle : ThumbStyle
        var slider = new Slider { Value = 50, ShowValue = false };
        slider.HasFocus = true;
        slider.FocusedThumbStyle = new Style(Color.Red, Color.Green);
        slider.Arrange(new Rect(0, 0, 11, 1));
        var (buf, surface) = Surface(11, 1);
        slider.Render(surface);
        var thumbCol = 5; // 50% of 10
        buf[thumbCol, 0].Style.Foreground.Should().Be(Color.Red, "focused slider uses FocusedThumbStyle");
    }

    [Fact]
    public void Slider_Render_UnfocusedThumbStyle()
    {
        var slider = new Slider { Value = 50, ShowValue = false };
        slider.HasFocus = false;
        slider.ThumbStyle = new Style(Color.Yellow, Color.Blue);
        slider.Arrange(new Rect(0, 0, 11, 1));
        var (buf, surface) = Surface(11, 1);
        slider.Render(surface);
        var thumbCol = 5;
        buf[thumbCol, 0].Style.Foreground.Should().Be(Color.Yellow, "unfocused slider uses ThumbStyle");
    }

    [Fact]
    public void Slider_Render_ShowValueTrue_Appends()
    {
        // L74: if (ShowValue) surface.SetText — kills conditional
        var slider = new Slider { Value = 42, ShowValue = true };
        slider.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        slider.Render(surface);
        Row(buf, 0).Should().Contain("42");
    }

    [Fact]
    public void Slider_Render_ShowValueFalse_NoText()
    {
        var slider = new Slider { Value = 42, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 20, 1));
        var (buf, surface) = Surface(20, 1);
        slider.Render(surface);
        Row(buf, 0).Should().NotContain("42");
    }

    [Fact]
    public void Slider_Render_RangeZero_ThumbAtZero()
    {
        // L58: range > 0 ? ... : 0 — when Minimum == Maximum
        var slider = new Slider { Minimum = 50, Maximum = 50, Value = 50, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 10, 1));
        var (buf, surface) = Surface(10, 1);
        slider.Render(surface);
        buf[0, 0].Character.Should().Be('\u2588', "range=0 => thumbPos=0");
    }

    [Fact]
    public void Slider_OnKeyEvent_Left_DecreasesAndReturnsTrue()
    {
        // L85-86: Value -= Step; return true — kills boolean mutants
        var slider = new Slider { Value = 50, Step = 10 };
        var result = slider.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0', false, false, false));
        slider.Value.Should().Be(40);
        result.Should().BeTrue();
    }

    [Fact]
    public void Slider_OnKeyEvent_Right_IncreasesAndReturnsTrue()
    {
        // L88-89: Value += Step; return true
        var slider = new Slider { Value = 50, Step = 10 };
        var result = slider.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0', false, false, false));
        slider.Value.Should().Be(60);
        result.Should().BeTrue();
    }

    [Fact]
    public void Slider_OnKeyEvent_Home_SetsMinAndReturnsTrue()
    {
        // L91-92: Value = Minimum; return true
        var slider = new Slider { Value = 50, Minimum = 10 };
        var result = slider.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0', false, false, false));
        slider.Value.Should().Be(10);
        result.Should().BeTrue();
    }

    [Fact]
    public void Slider_OnKeyEvent_End_SetsMaxAndReturnsTrue()
    {
        // L94-95: Value = Maximum; return true
        var slider = new Slider { Value = 50, Maximum = 90 };
        var result = slider.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0', false, false, false));
        slider.Value.Should().Be(90);
        result.Should().BeTrue();
    }

    [Fact]
    public void Slider_OnKeyEvent_Default_ReturnsFalse()
    {
        // L97: return false
        var slider = new Slider { Value = 50 };
        var result = slider.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a', false, false, false));
        result.Should().BeFalse();
        slider.Value.Should().Be(50);
    }

    [Fact]
    public void Slider_OnMouseEvent_LeftPress_SetsValueAndReturnsTrue()
    {
        // L103-115: mouse press handling — kills boolean/conditional mutants
        var slider = new Slider { Minimum = 0, Maximum = 100, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 11, 1));
        // Click at column 5, trackWidth = 11
        // Value = 0 + (int)(5.0 / 10 * 100) = 50
        var result = slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 0, false, false, false));
        result.Should().BeTrue();
        slider.Value.Should().Be(50);
    }

    [Fact]
    public void Slider_OnMouseEvent_LeftPress_AtStart()
    {
        // Column 0 => value = Minimum
        var slider = new Slider { Minimum = 0, Maximum = 100, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 11, 1));
        slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        slider.Value.Should().Be(0);
    }

    [Fact]
    public void Slider_OnMouseEvent_LeftPress_AtEnd()
    {
        // Column = trackWidth-1 => value = Maximum
        var slider = new Slider { Minimum = 0, Maximum = 100, ShowValue = false };
        slider.Arrange(new Rect(0, 0, 11, 1));
        slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 0, false, false, false));
        slider.Value.Should().Be(100);
    }

    [Fact]
    public void Slider_OnMouseEvent_LeftPress_BeyondTrack_NoTrackSet()
    {
        // L109: localCol < trackWidth — click beyond track area (in value text area)
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50, ShowValue = true };
        slider.Arrange(new Rect(0, 0, 20, 1));
        // " 50" = 3 chars, trackWidth = 17. Click at col 18 (in text area)
        var result = slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 18, 0, false, false, false));
        result.Should().BeTrue("press always returns true");
        slider.Value.Should().Be(50, "value should not change when click is beyond track");
    }

    [Fact]
    public void Slider_OnMouseEvent_Release_ReturnsFalse()
    {
        // L118: return false
        var slider = new Slider();
        var result = slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 0, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void Slider_OnMouseEvent_RightPress_ReturnsFalse()
    {
        // Tests the && condition: right button not handled
        var slider = new Slider();
        slider.Arrange(new Rect(0, 0, 20, 1));
        var result = slider.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 5, 0, false, false, false));
        // Right press doesn't match left press, goes to return false
        result.Should().BeFalse();
    }

    [Fact]
    public void Slider_OnMouseEvent_Move_WithoutPriorPress_ReturnsFalse()
    {
        // Move without drag shouldn't do anything
        var slider = new Slider();
        slider.Arrange(new Rect(0, 0, 20, 1));
        var result = slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 5, 0, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void Slider_ValueChanged_ReportsExactValue()
    {
        // L20: ValueChanged?.Invoke(this, _value) — verify exact arg
        var slider = new Slider();
        int? eventValue = null;
        slider.ValueChanged += (_, v) => eventValue = v;
        slider.Value = 77;
        eventValue.Should().Be(77);
    }

    [Fact]
    public void Slider_Step_DefaultIsOne()
    {
        var slider = new Slider();
        slider.Step.Should().Be(1);
    }

    [Fact]
    public void Slider_OnMouseEvent_WithOffset_CalculatesLocalCol()
    {
        // L105: var localCol = e.Column - Bounds.X — kills arithmetic mutant
        var slider = new Slider { Minimum = 0, Maximum = 100, ShowValue = false };
        slider.Arrange(new Rect(5, 0, 11, 1)); // offset X = 5
        // Click at absolute column 10, localCol = 10 - 5 = 5
        // Value = 0 + (int)(5.0 / 10 * 100) = 50
        slider.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 0, false, false, false));
        slider.Value.Should().Be(50);
    }

    // ═══════════════════════════════════════════════════════════════════
    // VStack — NoCov L20; Survived L23 (arithmetic), L27 (negate)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void VStack_MeasureContent_InvisibleSkipped()
    {
        // L18-20: if (!children[i].Visible) continue — NoCov L20
        var stack = new VStack();
        var a = new Label("AAAA"); // 4 wide
        var b = new Label("BB") { Visible = false }; // invisible
        var c = new Label("CCC"); // 3 wide
        stack.Add(a);
        stack.Add(b);
        stack.Add(c);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(4, "invisible child should not affect width");
        size.Height.Should().Be(2, "invisible child should not count toward height");
    }

    [Fact]
    public void VStack_MeasureContent_HeightAccumulation()
    {
        // L23: height += childSize.Height — kills arithmetic mutant (+ vs -)
        var stack = new VStack { Spacing = 0 };
        var a = new Label("A\nB"); // 2 lines
        var b = new Label("C"); // 1 line
        stack.Add(a);
        stack.Add(b);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Height.Should().Be(3, "should be 2 + 1 = 3");
    }

    [Fact]
    public void VStack_MeasureContent_SpacingOnlyBetweenVisible()
    {
        // L27: if (i < children.Count - 1) height += Spacing — kills negate mutant
        var stack = new VStack { Spacing = 5 };
        var a = new Label("A"); // 1 line
        var b = new Label("B"); // 1 line
        stack.Add(a);
        stack.Add(b);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        // 1 + 5 (spacing before last) + 1 = 7
        size.Height.Should().Be(7, "spacing 5 between 2 items = 1+5+1=7");
    }

    [Fact]
    public void VStack_MeasureContent_NoSpacingAfterLastChild()
    {
        // L27: spacing NOT added after the last child
        var stack = new VStack { Spacing = 10 };
        var a = new Label("A");
        stack.Add(a);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Height.Should().Be(1, "single child should have no spacing added");
    }

    [Fact]
    public void VStack_Arrange_SpacingBetweenItems()
    {
        // Verify spacing accumulation in arrange pass
        var stack = new VStack { Spacing = 3 };
        var a = new Label("A");
        a.HeightConstraint = Constraint.Fixed(2);
        var b = new Label("B");
        b.HeightConstraint = Constraint.Fixed(2);
        var c = new Label("C");
        c.HeightConstraint = Constraint.Fixed(2);
        stack.Add(a);
        stack.Add(b);
        stack.Add(c);
        stack.Arrange(new Rect(0, 0, 20, 20));
        a.Bounds.Y.Should().Be(0);
        b.Bounds.Y.Should().Be(5); // 2 + 3
        c.Bounds.Y.Should().Be(10); // 5 + 2 + 3
    }

    // ═══════════════════════════════════════════════════════════════════
    // HStack — NoCov L20; Survived L23 (arithmetic), L27 (negate)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void HStack_MeasureContent_InvisibleSkipped()
    {
        // L18-20: if (!children[i].Visible) continue — NoCov L20
        var stack = new HStack();
        var a = new Label("AAAA"); // 4 wide
        var b = new Label("BB") { Visible = false }; // invisible
        var c = new Label("CCC"); // 3 wide
        stack.Add(a);
        stack.Add(b);
        stack.Add(c);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(7, "invisible child should not affect width: 4 + 3 = 7");
        size.Height.Should().Be(1);
    }

    [Fact]
    public void HStack_MeasureContent_WidthAccumulation()
    {
        // L23: width += childSize.Width — kills arithmetic mutant (+ vs -)
        var stack = new HStack { Spacing = 0 };
        var a = new Label("AAAA"); // 4 wide
        var b = new Label("BB"); // 2 wide
        stack.Add(a);
        stack.Add(b);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(6, "should be 4 + 2 = 6");
    }

    [Fact]
    public void HStack_MeasureContent_SpacingOnlyBetweenVisible()
    {
        // L27: if (i < children.Count - 1) width += Spacing — kills negate mutant
        var stack = new HStack { Spacing = 5 };
        var a = new Label("AA"); // 2 wide
        var b = new Label("BB"); // 2 wide
        stack.Add(a);
        stack.Add(b);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(9, "spacing 5 between 2 items = 2+5+2=9");
    }

    [Fact]
    public void HStack_MeasureContent_NoSpacingAfterLastChild()
    {
        var stack = new HStack { Spacing = 10 };
        var a = new Label("AA");
        stack.Add(a);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Width.Should().Be(2, "single child should have no spacing");
    }

    [Fact]
    public void HStack_Arrange_SpacingBetweenItems()
    {
        var stack = new HStack { Spacing = 3 };
        var a = new Label("A");
        a.WidthConstraint = Constraint.Fixed(2);
        var b = new Label("B");
        b.WidthConstraint = Constraint.Fixed(2);
        var c = new Label("C");
        c.WidthConstraint = Constraint.Fixed(2);
        stack.Add(a);
        stack.Add(b);
        stack.Add(c);
        stack.Arrange(new Rect(0, 0, 30, 5));
        a.Bounds.X.Should().Be(0);
        b.Bounds.X.Should().Be(5); // 2 + 3
        c.Bounds.X.Should().Be(10); // 5 + 2 + 3
    }

    [Fact]
    public void HStack_MeasureContent_HeightIsMax()
    {
        // L25: height = Math.Max(height, childSize.Height) — verify max behavior
        var stack = new HStack();
        var a = new Label("A"); // 1 line
        var b = new Label("B\nC\nD"); // 3 lines
        stack.Add(a);
        stack.Add(b);
        var size = stack.MeasureContent(new Spectre.Console.Size(50, 50));
        size.Height.Should().Be(3, "height should be max of children heights");
    }

    // ═══════════════════════════════════════════════════════════════════
    // ContainerWidget — Survived L at many (Add/Remove/Clear statements)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ContainerWidget_Add_NullThrows()
    {
        // L14: ArgumentNullException.ThrowIfNull(child)
        var stack = new VStack();
        var act = () => stack.Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ContainerWidget_Remove_NullThrows()
    {
        // L24: ArgumentNullException.ThrowIfNull(child)
        var stack = new VStack();
        var act = () => stack.Remove(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ContainerWidget_Add_SetsParentAndInvalidates()
    {
        // L16: child.Parent = this; L19: Invalidate()
        var stack = new VStack();
        stack.MarkRendered();
        stack.NeedsRender.Should().BeFalse();
        var child = new Label("X");
        stack.Add(child);
        child.Parent.Should().Be(stack, "Add should set child's Parent");
        stack.NeedsRender.Should().BeTrue("Add should invalidate container");
    }

    [Fact]
    public void ContainerWidget_Add_CallsOnMount()
    {
        // L18: child.OnMount() — statement
        // OnMount is virtual, so we verify it doesn't throw and child is in list
        var stack = new VStack();
        var label = new Label("X");
        stack.Add(label);
        stack.Children.Should().Contain(label);
    }

    [Fact]
    public void ContainerWidget_Remove_ClearsParentAndInvalidates()
    {
        // L28: child.OnUnmount(); L29: child.Parent = null; L30: Invalidate()
        var stack = new VStack();
        var child = new Label("X");
        stack.Add(child);
        stack.MarkRendered();
        stack.NeedsRender.Should().BeFalse();
        stack.Remove(child);
        child.Parent.Should().BeNull("Remove should clear child's Parent");
        stack.NeedsRender.Should().BeTrue("Remove should invalidate container");
        stack.Children.Should().NotContain(child);
    }

    [Fact]
    public void ContainerWidget_Remove_NonExistentChild_NoEffect()
    {
        // L26: if (_children.Remove(child)) — the Remove returns false, no side effects
        var stack = new VStack();
        var child = new Label("X");
        stack.MarkRendered();
        stack.NeedsRender.Should().BeFalse();
        stack.Remove(child);
        // If child wasn't in the list, container should NOT be invalidated
        stack.NeedsRender.Should().BeFalse("removing non-existent child should not invalidate");
    }

    [Fact]
    public void ContainerWidget_Clear_ClearsParentForAllAndInvalidates()
    {
        // L36-43: iterates backward, clears parent, then clears list and invalidates
        var stack = new VStack();
        var a = new Label("A");
        var b = new Label("B");
        var c = new Label("C");
        stack.Add(a);
        stack.Add(b);
        stack.Add(c);
        stack.MarkRendered();
        stack.NeedsRender.Should().BeFalse();
        stack.Clear();
        a.Parent.Should().BeNull();
        b.Parent.Should().BeNull();
        c.Parent.Should().BeNull();
        stack.Children.Should().BeEmpty();
        stack.NeedsRender.Should().BeTrue("Clear should invalidate container");
    }

    [Fact]
    public void ContainerWidget_Clear_OnEmpty_NoThrow()
    {
        var stack = new VStack();
        stack.MarkRendered();
        var act = () => stack.Clear();
        act.Should().NotThrow();
        stack.NeedsRender.Should().BeTrue("Clear should invalidate even when empty");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Splitter — NoCov L19,39,67 (block removal), L128,134-153 (mouse drag);
    //   Survived L29,49,59 (statement), L79,85 (arithmetic),
    //   L94,102 (equality), L111-131 (mouse handling)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Splitter_First_Setter_ClearsPreviousParent()
    {
        // L18-21: if (_first != null) { _first.Parent = null; } — NoCov block
        var splitter = new Splitter();
        var a = new Label("A");
        var b = new Label("B");
        splitter.First = a;
        a.Parent.Should().Be(splitter);
        splitter.First = b; // should clear a's parent
        a.Parent.Should().BeNull("previous First should have Parent cleared");
        b.Parent.Should().Be(splitter);
    }

    [Fact]
    public void Splitter_Second_Setter_ClearsPreviousParent()
    {
        // L38-41: if (_second != null) { _second.Parent = null; }
        var splitter = new Splitter();
        var a = new Label("A");
        var b = new Label("B");
        splitter.Second = a;
        a.Parent.Should().Be(splitter);
        splitter.Second = b;
        a.Parent.Should().BeNull("previous Second should have Parent cleared");
        b.Parent.Should().Be(splitter);
    }

    [Fact]
    public void Splitter_First_Setter_Invalidates()
    {
        // L29: Invalidate() — Survived statement
        var splitter = new Splitter();
        splitter.MarkRendered();
        splitter.NeedsRender.Should().BeFalse();
        splitter.First = new Label("A");
        splitter.NeedsRender.Should().BeTrue("setting First should invalidate");
    }

    [Fact]
    public void Splitter_Second_Setter_Invalidates()
    {
        // L49: Invalidate() — Survived statement
        var splitter = new Splitter();
        splitter.MarkRendered();
        splitter.NeedsRender.Should().BeFalse();
        splitter.Second = new Label("B");
        splitter.NeedsRender.Should().BeTrue("setting Second should invalidate");
    }

    [Fact]
    public void Splitter_SplitRatio_Setter_Invalidates()
    {
        // L59: Invalidate() — Survived statement
        var splitter = new Splitter();
        splitter.MarkRendered();
        splitter.NeedsRender.Should().BeFalse();
        splitter.SplitRatio = 0.3;
        splitter.NeedsRender.Should().BeTrue("setting SplitRatio should invalidate");
    }

    [Fact]
    public void Splitter_MeasureContent_ReturnsAvailableSize()
    {
        // L67: return new Size(available.Width, available.Height) — NoCov block
        var splitter = new Splitter();
        var size = splitter.MeasureContent(new Spectre.Console.Size(40, 20));
        size.Width.Should().Be(40);
        size.Height.Should().Be(20);
    }

    [Fact]
    public void Splitter_Arrange_Vertical_FirstAndSecondBounds()
    {
        // L77-79: arithmetic for vertical split — kills arithmetic mutants
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        var first = new Label("L");
        var second = new Label("R");
        splitter.First = first;
        splitter.Second = second;
        splitter.Arrange(new Rect(0, 0, 20, 10));

        var splitCol = (int)(20 * 0.5); // 10
        first.Bounds.X.Should().Be(0);
        first.Bounds.Width.Should().Be(splitCol, "first panel width = splitCol");
        second.Bounds.X.Should().Be(splitCol + 1, "second starts after divider");
        second.Bounds.Width.Should().Be(20 - splitCol - 1, "second width = total - splitCol - 1 (divider)");
    }

    [Fact]
    public void Splitter_Arrange_Horizontal_FirstAndSecondBounds()
    {
        // L83-85: arithmetic for horizontal split
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            SplitRatio = 0.5,
        };
        var first = new Label("T");
        var second = new Label("B");
        splitter.First = first;
        splitter.Second = second;
        splitter.Arrange(new Rect(0, 0, 20, 10));

        var splitRow = (int)(10 * 0.5); // 5
        first.Bounds.Y.Should().Be(0);
        first.Bounds.Height.Should().Be(splitRow);
        second.Bounds.Y.Should().Be(splitRow + 1);
        second.Bounds.Height.Should().Be(10 - splitRow - 1);
    }

    [Fact]
    public void Splitter_Render_Vertical_DividerAtCorrectCol()
    {
        // L93-97: render loop for vertical divider — kills equality mutant L94
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
            SplitterStyle = new Style(Color.Red),
        };
        splitter.Arrange(new Rect(0, 0, 20, 3));
        var (buf, surface) = Surface(20, 3);
        splitter.Render(surface);
        var splitCol = (int)(20 * 0.5); // 10
        for (int row = 0; row < 3; row++)
        {
            buf[splitCol, row].Character.Should().Be('\u2502', $"divider at col {splitCol}, row {row}");
            buf[splitCol, row].Style.Foreground.Should().Be(Color.Red);
        }
    }

    [Fact]
    public void Splitter_Render_Horizontal_DividerAtCorrectRow()
    {
        // L101-105: render loop for horizontal divider — kills equality mutant L102
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            SplitRatio = 0.5,
            SplitterStyle = new Style(Color.Blue),
        };
        splitter.Arrange(new Rect(0, 0, 5, 10));
        var (buf, surface) = Surface(5, 10);
        splitter.Render(surface);
        var splitRow = (int)(10 * 0.5); // 5
        for (int col = 0; col < 5; col++)
        {
            buf[col, splitRow].Character.Should().Be('\u2500', $"divider at row {splitRow}, col {col}");
            buf[col, splitRow].Style.Foreground.Should().Be(Color.Blue);
        }
    }

    [Fact]
    public void Splitter_Mouse_PressOnDivider_StartsDrag()
    {
        // L111-117: press on splitter line starts drag
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var splitCol = 0 + (int)(20 * 0.5); // 10
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, splitCol, 2, false, false, false));
        result.Should().BeTrue("pressing on divider should start drag");
    }

    [Fact]
    public void Splitter_Mouse_PressOffDivider_DoesNotStartDrag()
    {
        // L113: IsOnSplitter returns false
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0, false, false, false));
        result.Should().BeFalse("pressing off divider should not start drag");
    }

    [Fact]
    public void Splitter_Mouse_DragVertical_ChangesRatio()
    {
        // L122-124: vertical drag updates ratio
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var splitCol = (int)(20 * 0.5); // 10

        // Start drag
        splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, splitCol, 2, false, false, false));
        // Drag to col 15
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 15, 2, false, false, false));
        result.Should().BeTrue();
        // Ratio = (15 - 0) / 20 = 0.75
        splitter.SplitRatio.Should().BeApproximately(0.75, 0.01);
    }

    [Fact]
    public void Splitter_Mouse_DragHorizontal_ChangesRatio()
    {
        // L126-128: horizontal drag updates ratio
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 10));
        var splitRow = (int)(10 * 0.5); // 5

        // Start drag on horizontal divider
        splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, splitRow, false, false, false));
        // Drag to row 7
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 5, 7, false, false, false));
        result.Should().BeTrue();
        // Ratio = (7 - 0) / 10 = 0.7
        splitter.SplitRatio.Should().BeApproximately(0.7, 0.01);
    }

    [Fact]
    public void Splitter_Mouse_Release_StopsDrag()
    {
        // L134-137: release stops drag
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var splitCol = (int)(20 * 0.5);

        // Start drag
        splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, splitCol, 2, false, false, false));
        // Release
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, splitCol, 2, false, false, false));
        result.Should().BeTrue("release during drag should return true");

        // Now move should NOT drag (no longer dragging)
        var savedRatio = splitter.SplitRatio;
        var result2 = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 15, 2, false, false, false));
        result2.Should().BeFalse("move after release should not drag");
        splitter.SplitRatio.Should().Be(savedRatio);
    }

    [Fact]
    public void Splitter_Mouse_MoveWithoutDrag_ReturnsFalse()
    {
        // L140: return false — final fallthrough
        var splitter = new Splitter();
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Move, 5, 2, false, false, false));
        result.Should().BeFalse();
    }

    [Fact]
    public void Splitter_IsOnSplitter_Vertical_ChecksExactColumn()
    {
        // L147-148: col == splitCol && row >= Bounds.Y && row < Bounds.Bottom
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var splitCol = (int)(20 * 0.5); // 10

        // Press exactly on divider at valid row: should start drag
        var result1 = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, splitCol, 0, false, false, false));
        result1.Should().BeTrue("press on divider at top row should hit");

        // Release to stop drag
        splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, splitCol, 0, false, false, false));

        // Press on col adjacent to divider: should not hit
        var result2 = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, splitCol + 1, 0, false, false, false));
        result2.Should().BeFalse("press one col off divider should not hit");
    }

    [Fact]
    public void Splitter_IsOnSplitter_Vertical_ChecksRowBounds()
    {
        // row < Bounds.Bottom — boundary check
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var splitCol = (int)(20 * 0.5);

        // Press at row 5 (= Bounds.Bottom, should be out of bounds)
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, splitCol, 5, false, false, false));
        result.Should().BeFalse("row == Bounds.Bottom should be out of bounds");
    }

    [Fact]
    public void Splitter_IsOnSplitter_Horizontal_ChecksExactRow()
    {
        // L152-153: row == splitRow && col >= Bounds.X && col < Bounds.Right
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 10));
        var splitRow = (int)(10 * 0.5); // 5

        // Press on divider row
        var result1 = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, splitRow, false, false, false));
        result1.Should().BeTrue();

        // Release
        splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 0, splitRow, false, false, false));

        // Press one row off
        var result2 = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, splitRow + 1, false, false, false));
        result2.Should().BeFalse();
    }

    [Fact]
    public void Splitter_IsOnSplitter_Horizontal_ChecksColBounds()
    {
        // col < Bounds.Right — boundary check
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            SplitRatio = 0.5,
        };
        splitter.Arrange(new Rect(0, 0, 20, 10));
        var splitRow = (int)(10 * 0.5);

        // Press at col 20 (= Bounds.Right, out of bounds)
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 20, splitRow, false, false, false));
        result.Should().BeFalse("col == Bounds.Right should be out of bounds");
    }

    [Fact]
    public void Splitter_First_SetNull_ClearsChild()
    {
        // Setting First to null should work (no child)
        var splitter = new Splitter();
        var label = new Label("A");
        splitter.First = label;
        label.Parent.Should().Be(splitter);
        splitter.First = null;
        label.Parent.Should().BeNull();
        splitter.First.Should().BeNull();
        splitter.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void Splitter_Second_SetNull_ClearsChild()
    {
        var splitter = new Splitter();
        var label = new Label("A");
        splitter.Second = label;
        label.Parent.Should().Be(splitter);
        splitter.Second = null;
        label.Parent.Should().BeNull();
        splitter.Second.Should().BeNull();
    }

    [Fact]
    public void Splitter_Arrange_WithOffset_CorrectBounds()
    {
        // L77-79: bounds.X offset in arithmetic
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            SplitRatio = 0.5,
        };
        var first = new Label("L");
        var second = new Label("R");
        splitter.First = first;
        splitter.Second = second;
        splitter.Arrange(new Rect(10, 5, 20, 10));

        var splitCol = (int)(20 * 0.5); // 10
        first.Bounds.X.Should().Be(10);
        first.Bounds.Y.Should().Be(5);
        first.Bounds.Width.Should().Be(splitCol);
        second.Bounds.X.Should().Be(10 + splitCol + 1);
    }

    [Fact]
    public void Splitter_Arrange_Horizontal_WithOffset_CorrectBounds()
    {
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            SplitRatio = 0.5,
        };
        var first = new Label("T");
        var second = new Label("B");
        splitter.First = first;
        splitter.Second = second;
        splitter.Arrange(new Rect(10, 5, 20, 10));

        var splitRow = (int)(10 * 0.5); // 5
        first.Bounds.Y.Should().Be(5);
        first.Bounds.Height.Should().Be(splitRow);
        second.Bounds.Y.Should().Be(5 + splitRow + 1);
    }

    [Fact]
    public void Splitter_Mouse_ReleaseWithoutDrag_ReturnsFalse()
    {
        // L134: _isDragging check — release without prior drag
        var splitter = new Splitter();
        splitter.Arrange(new Rect(0, 0, 20, 5));
        var result = splitter.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Release, 5, 2, false, false, false));
        result.Should().BeFalse("release without drag should return false");
    }
}
