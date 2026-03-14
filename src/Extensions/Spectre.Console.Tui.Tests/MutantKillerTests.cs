using FluentAssertions;
using Spectre.Console;
using Xunit;
using TuiTreeNode = Spectre.Console.Tui.Widgets.Controls.TreeNode;

namespace Spectre.Console.Tui.Tests;

/// <summary>
/// Comprehensive mutation-killing tests for 100% Stryker score.
/// Organized by source file to systematically cover all mutants.
/// </summary>
public sealed class MutantKillerTests
{
    // ── Helpers ──────────────────────────────────────────────────────

    private static (ScreenBuffer buffer, BufferSurface surface) CreateSurface(int w, int h)
    {
        var buffer = new ScreenBuffer(w, h);
        var surface = new BufferSurface(buffer);
        return (buffer, surface);
    }

    private static void ArrangeWidget(Widget widget, int w, int h)
    {
        widget.Arrange(new Rect(0, 0, w, h));
    }

    private static void RenderWidget(Widget widget, ScreenBuffer buffer, BufferSurface surface)
    {
        widget.Render(surface);
    }

    private static string GetRow(ScreenBuffer buffer, int row)
    {
        var chars = new char[buffer.Width];
        for (var i = 0; i < buffer.Width; i++)
        {
            chars[i] = buffer[i, row].Character;
        }
        return new string(chars);
    }

    // ── BufferCell ──────────────────────────────────────────────────

    [Fact]
    public void BufferCell_Empty_HasSpaceCharacter()
    {
        var cell = BufferCell.Empty;
        cell.Character.Should().Be(' ');
        cell.Style.Should().Be(Style.Plain);
        cell.IsDirty.Should().BeTrue(); // constructor sets IsDirty=true
    }

    [Fact]
    public void BufferCell_Equality_IgnoresDirtyFlag()
    {
        var a = new BufferCell { Character = 'A', Style = Style.Plain, IsDirty = true };
        var b = new BufferCell { Character = 'A', Style = Style.Plain, IsDirty = false };
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
    }

    [Fact]
    public void BufferCell_Inequality_DifferentCharacter()
    {
        var a = new BufferCell { Character = 'A', Style = Style.Plain };
        var b = new BufferCell { Character = 'B', Style = Style.Plain };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void BufferCell_Inequality_DifferentStyle()
    {
        var a = new BufferCell { Character = 'A', Style = Style.Plain };
        var b = new BufferCell { Character = 'A', Style = new Style(Color.Red) };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void BufferCell_GetHashCode_DiffersForDifferentValues()
    {
        var a = new BufferCell { Character = 'A', Style = Style.Plain };
        var b = new BufferCell { Character = 'B', Style = Style.Plain };
        var c = new BufferCell { Character = 'A', Style = new Style(Color.Red) };
        a.GetHashCode().Should().NotBe(b.GetHashCode());
        // Style component matters
        a.GetHashCode().Should().NotBe(c.GetHashCode());
    }

    [Fact]
    public void BufferCell_Equals_NullReturnsFalse()
    {
        var cell = new BufferCell { Character = 'X' };
        cell.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void BufferCell_Equals_WrongTypeReturnsFalse()
    {
        var cell = new BufferCell { Character = 'X' };
        cell.Equals("string").Should().BeFalse();
    }

    // ── Rect ────────────────────────────────────────────────────────

    [Fact]
    public void Rect_Contains_ExactEdges()
    {
        var r = new Rect(5, 10, 20, 15);
        // Top-left corner
        r.Contains(5, 10).Should().BeTrue();
        // Bottom-right edge (exclusive)
        r.Contains(24, 24).Should().BeTrue();
        r.Contains(25, 25).Should().BeFalse();
        // Just outside each edge
        r.Contains(4, 10).Should().BeFalse();
        r.Contains(5, 9).Should().BeFalse();
        r.Contains(25, 10).Should().BeFalse();
        r.Contains(5, 25).Should().BeFalse();
    }

    [Fact]
    public void Rect_Intersect_PartialOverlap()
    {
        var a = new Rect(0, 0, 10, 10);
        var b = new Rect(5, 5, 10, 10);
        var i = a.Intersect(b);
        i.X.Should().Be(5);
        i.Y.Should().Be(5);
        i.Width.Should().Be(5);
        i.Height.Should().Be(5);
    }

    [Fact]
    public void Rect_Intersect_NoOverlap()
    {
        var a = new Rect(0, 0, 5, 5);
        var b = new Rect(10, 10, 5, 5);
        var i = a.Intersect(b);
        i.Width.Should().Be(0);
        i.Height.Should().Be(0);
    }

    [Fact]
    public void Rect_Intersect_CompleteOverlap()
    {
        var a = new Rect(0, 0, 20, 20);
        var b = new Rect(5, 5, 5, 5);
        var i = a.Intersect(b);
        i.Should().Be(b);
    }

    [Fact]
    public void Rect_Equality_AllFieldsMatter()
    {
        var r = new Rect(1, 2, 3, 4);
        r.Should().NotBe(new Rect(0, 2, 3, 4)); // X differs
        r.Should().NotBe(new Rect(1, 0, 3, 4)); // Y differs
        r.Should().NotBe(new Rect(1, 2, 0, 4)); // Width differs
        r.Should().NotBe(new Rect(1, 2, 3, 0)); // Height differs
        r.Should().Be(new Rect(1, 2, 3, 4));
    }

    [Fact]
    public void Rect_GetHashCode_AllFieldsContribute()
    {
        var a = new Rect(1, 2, 3, 4);
        var b = new Rect(4, 3, 2, 1);
        var c = new Rect(1, 2, 3, 5);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
        a.GetHashCode().Should().NotBe(c.GetHashCode());
    }

    [Fact]
    public void Rect_Operators_WorkCorrectly()
    {
        var a = new Rect(1, 2, 3, 4);
        var b = new Rect(1, 2, 3, 4);
        var c = new Rect(5, 6, 7, 8);
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        (a == c).Should().BeFalse();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Rect_Right_Bottom_Computed()
    {
        var r = new Rect(3, 7, 10, 20);
        r.Right.Should().Be(13);
        r.Bottom.Should().Be(27);
    }

    [Fact]
    public void Rect_ToString_IncludesAllFields()
    {
        var r = new Rect(1, 2, 3, 4);
        var s = r.ToString();
        s.Should().Contain("1").And.Contain("2").And.Contain("3").And.Contain("4");
    }

    [Fact]
    public void Rect_Equals_BoxedNull()
    {
        var r = new Rect(1, 1, 1, 1);
        r.Equals(null).Should().BeFalse();
        r.Equals((object)new Rect(1, 1, 1, 1)).Should().BeTrue();
        r.Equals("not a rect").Should().BeFalse();
    }

    // ── Margin ──────────────────────────────────────────────────────

    [Fact]
    public void Margin_AllConstructors()
    {
        var uniform = new Margin(5);
        uniform.Left.Should().Be(5);
        uniform.Top.Should().Be(5);
        uniform.Right.Should().Be(5);
        uniform.Bottom.Should().Be(5);

        var hv = new Margin(3, 7);
        hv.Left.Should().Be(3);
        hv.Right.Should().Be(3);
        hv.Top.Should().Be(7);
        hv.Bottom.Should().Be(7);

        var full = new Margin(1, 2, 3, 4);
        full.Left.Should().Be(1);
        full.Top.Should().Be(2);
        full.Right.Should().Be(3);
        full.Bottom.Should().Be(4);
    }

    [Fact]
    public void Margin_Computed_HorizontalVertical()
    {
        var m = new Margin(2, 3, 4, 5);
        m.Horizontal.Should().Be(6);
        m.Vertical.Should().Be(8);
    }

    [Fact]
    public void Margin_Equality_AllFieldsMatter()
    {
        var m = new Margin(1, 2, 3, 4);
        m.Should().NotBe(new Margin(0, 2, 3, 4));
        m.Should().NotBe(new Margin(1, 0, 3, 4));
        m.Should().NotBe(new Margin(1, 2, 0, 4));
        m.Should().NotBe(new Margin(1, 2, 3, 0));
        (m == new Margin(1, 2, 3, 4)).Should().BeTrue();
        (m != new Margin(0, 0, 0, 0)).Should().BeTrue();
    }

    [Fact]
    public void Margin_GetHashCode_AllFieldsContribute()
    {
        var a = new Margin(1, 2, 3, 4);
        var b = new Margin(4, 3, 2, 1);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Margin_Equals_BoxedNull()
    {
        var m = new Margin(1);
        m.Equals(null).Should().BeFalse();
        m.Equals((object)new Margin(1)).Should().BeTrue();
        m.Equals("not a margin").Should().BeFalse();
    }

    [Fact]
    public void Margin_NegativeClamped()
    {
        var m = new Margin(-5, -3, -1, -2);
        m.Left.Should().Be(0);
        m.Top.Should().Be(0);
        m.Right.Should().Be(0);
        m.Bottom.Should().Be(0);
    }

    // ── Constraint ──────────────────────────────────────────────────

    [Fact]
    public void Constraint_Resolve_AllKinds()
    {
        Constraint.Fixed(50).Resolve(100).Should().Be(50);
        Constraint.Fixed(200).Resolve(100).Should().Be(100); // capped
        Constraint.Min(30).Resolve(100).Should().Be(30);
        Constraint.Max(80).Resolve(100).Should().Be(80);
        Constraint.Max(200).Resolve(100).Should().Be(100); // capped
        Constraint.Percentage(50).Resolve(100).Should().Be(50);
        Constraint.Fill().Resolve(100).Should().Be(100);
    }

    [Fact]
    public void Constraint_Equality()
    {
        var a = Constraint.Fixed(10);
        var b = Constraint.Fixed(10);
        var c = Constraint.Fixed(20);
        var d = Constraint.Min(10);
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        (a != d).Should().BeTrue(); // different kind
        a.Equals((object)b).Should().BeTrue();
        a.Equals(null).Should().BeFalse();
        a.Equals("x").Should().BeFalse();
    }

    [Fact]
    public void Constraint_GetHashCode_KindAndValueMatter()
    {
        var a = Constraint.Fixed(10);
        var b = Constraint.Min(10);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    // ── ScreenBuffer ────────────────────────────────────────────────

    [Fact]
    public void ScreenBuffer_SetCell_OutOfBounds_NoThrow()
    {
        var buf = new ScreenBuffer(5, 5);
        // These should silently return without throwing
        buf.SetCell(-1, 0, 'X', Style.Plain);
        buf.SetCell(0, -1, 'X', Style.Plain);
        buf.SetCell(5, 0, 'X', Style.Plain);
        buf.SetCell(0, 5, 'X', Style.Plain);
        // No exception = pass
    }

    [Fact]
    public void ScreenBuffer_SetCell_MarksDirty_OnlyOnChange()
    {
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf[0, 0].IsDirty.Should().BeTrue();
        buf.ClearDirtyFlags();
        buf[0, 0].IsDirty.Should().BeFalse();
        // Same value should not mark dirty
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf[0, 0].IsDirty.Should().BeFalse();
        // Different value should mark dirty
        buf.SetCell(0, 0, 'B', Style.Plain);
        buf[0, 0].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ScreenBuffer_SetText_WritesSequence()
    {
        var buf = new ScreenBuffer(10, 1);
        buf.SetText(2, 0, "Hello", Style.Plain);
        GetRow(buf, 0).Should().Be("  Hello   ");
    }

    [Fact]
    public void ScreenBuffer_Fill_IntersectsWithBounds()
    {
        var buf = new ScreenBuffer(5, 5);
        var style = new Style(Color.Red);
        // Fill extends beyond buffer - should be clipped
        buf.Fill(new Rect(-2, -2, 10, 10), '#', style);
        buf[0, 0].Character.Should().Be('#');
        buf[4, 4].Character.Should().Be('#');
        buf[0, 0].Style.Should().Be(style);
    }

    [Fact]
    public void ScreenBuffer_Clear_ResetsAllCells()
    {
        var buf = new ScreenBuffer(3, 3);
        buf.SetCell(1, 1, 'X', Style.Plain);
        buf.ClearDirtyFlags();
        buf.Clear();
        buf[1, 1].Character.Should().Be(' ');
        buf[1, 1].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ScreenBuffer_Resize_PreservesData()
    {
        var buf = new ScreenBuffer(3, 3);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.SetCell(2, 2, 'B', Style.Plain);
        buf.Resize(5, 5);
        buf.Width.Should().Be(5);
        buf.Height.Should().Be(5);
        buf[0, 0].Character.Should().Be('A');
        buf[2, 2].Character.Should().Be('B');
    }

    [Fact]
    public void ScreenBuffer_Resize_ShrinkLosesData()
    {
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(4, 4, 'X', Style.Plain);
        buf.Resize(3, 3);
        buf.Width.Should().Be(3);
        buf.Height.Should().Be(3);
        // Cell at 4,4 is gone
    }

    // ── ScreenDiff ──────────────────────────────────────────────────

    [Fact]
    public void ScreenDiff_ComputeChanges_DetectsCharDifference()
    {
        var current = new ScreenBuffer(3, 1);
        var previous = new ScreenBuffer(3, 1);
        current.SetCell(1, 0, 'X', Style.Plain);
        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().HaveCount(1);
        changes[0].Column.Should().Be(1);
        changes[0].Row.Should().Be(0);
        changes[0].Character.Should().Be('X');
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_DetectsStyleDifference()
    {
        var current = new ScreenBuffer(3, 1);
        var previous = new ScreenBuffer(3, 1);
        var red = new Style(Color.Red);
        current.SetCell(0, 0, ' ', red);
        previous.SetCell(0, 0, ' ', Style.Plain);
        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().HaveCount(1);
        changes[0].Style.Should().Be(red);
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_SizeMismatch_HandlesLargerCurrent()
    {
        var current = new ScreenBuffer(5, 5);
        var previous = new ScreenBuffer(3, 3);
        current.SetCell(4, 4, 'X', Style.Plain);
        var changes = ScreenDiff.ComputeChanges(current, previous);
        // Should include cells beyond previous bounds
        changes.Should().Contain(c => c.Column == 4 && c.Row == 4);
    }

    [Fact]
    public void ScreenDiff_GetDirtyChanges_ReturnsDirtyCells()
    {
        var buf = new ScreenBuffer(3, 3);
        buf.ClearDirtyFlags(); // Clear initial dirty state
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.SetCell(2, 2, 'B', Style.Plain);
        var changes = ScreenDiff.GetDirtyChanges(buf);
        changes.Should().HaveCount(2);
        changes.Should().Contain(c => c.Character == 'A');
        changes.Should().Contain(c => c.Character == 'B');
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_NoDifference_Empty()
    {
        var a = new ScreenBuffer(3, 3);
        var b = new ScreenBuffer(3, 3);
        ScreenDiff.ComputeChanges(a, b).Should().BeEmpty();
    }

    // ── BufferSurface ───────────────────────────────────────────────

    [Fact]
    public void BufferSurface_SetCell_ClipsToRect()
    {
        var buf = new ScreenBuffer(10, 10);
        var surface = new BufferSurface(buf, new Rect(2, 3, 5, 5));
        surface.SetCell(0, 0, 'A', Style.Plain);
        buf[2, 3].Character.Should().Be('A');
        // Out of clip area
        surface.SetCell(-1, 0, 'X', Style.Plain);
        surface.SetCell(5, 0, 'X', Style.Plain);
        surface.SetCell(0, -1, 'X', Style.Plain);
        surface.SetCell(0, 5, 'X', Style.Plain);
    }

    [Fact]
    public void BufferSurface_SetText_WrapsToSetCell()
    {
        var buf = new ScreenBuffer(10, 1);
        var surface = new BufferSurface(buf);
        surface.SetText(1, 0, "Hi", Style.Plain);
        buf[1, 0].Character.Should().Be('H');
        buf[2, 0].Character.Should().Be('i');
    }

    [Fact]
    public void BufferSurface_Fill_FillsArea()
    {
        var buf = new ScreenBuffer(5, 5);
        var surface = new BufferSurface(buf);
        surface.Fill(new Rect(1, 1, 3, 3), '#', Style.Plain);
        buf[1, 1].Character.Should().Be('#');
        buf[3, 3].Character.Should().Be('#');
        buf[0, 0].Character.Should().Be(' ');
    }

    [Fact]
    public void BufferSurface_Clear_FillsWithSpaces()
    {
        var buf = new ScreenBuffer(5, 5);
        var surface = new BufferSurface(buf);
        surface.SetCell(2, 2, 'X', Style.Plain);
        surface.Clear();
        buf[2, 2].Character.Should().Be(' ');
    }

    [Fact]
    public void BufferSurface_CreateSubSurface_OffsetsCorrectly()
    {
        var buf = new ScreenBuffer(20, 20);
        var surface = new BufferSurface(buf, new Rect(5, 5, 10, 10));
        var sub = surface.CreateSubSurface(new Rect(2, 3, 4, 4));
        sub.SetCell(0, 0, 'Z', Style.Plain);
        buf[7, 8].Character.Should().Be('Z'); // 5+2, 5+3
    }

    [Fact]
    public void BufferSurface_WidthHeight()
    {
        var buf = new ScreenBuffer(20, 20);
        var surface = new BufferSurface(buf, new Rect(0, 0, 8, 6));
        surface.Width.Should().Be(8);
        surface.Height.Should().Be(6);
    }

    // ── TestTerminalDriver ──────────────────────────────────────────

    [Fact]
    public void TestTerminalDriver_Initialize_Shutdown_State()
    {
        var driver = new TestTerminalDriver(40, 10);
        driver.IsInitialized.Should().BeFalse();
        driver.IsShutdown.Should().BeFalse();
        driver.Initialize();
        driver.IsInitialized.Should().BeTrue();
        driver.Shutdown();
        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void TestTerminalDriver_MouseEnable_Disable()
    {
        var driver = new TestTerminalDriver(40, 10);
        driver.MouseEnabled.Should().BeFalse();
        driver.EnableMouse();
        driver.MouseEnabled.Should().BeTrue();
        driver.DisableMouse();
        driver.MouseEnabled.Should().BeFalse();
    }

    [Fact]
    public void TestTerminalDriver_CursorVisibility()
    {
        var driver = new TestTerminalDriver(40, 10);
        driver.CursorVisible.Should().BeTrue();
        driver.HideCursor();
        driver.CursorVisible.Should().BeFalse();
        driver.ShowCursor();
        driver.CursorVisible.Should().BeTrue();
    }

    [Fact]
    public void TestTerminalDriver_Flush_WritesToBuffer()
    {
        var driver = new TestTerminalDriver(10, 5);
        driver.Initialize();
        var changes = new List<CellChange>
        {
            new CellChange(0, 0, 'H', Style.Plain),
            new CellChange(1, 0, 'i', Style.Plain),
        };
        driver.Flush(changes);
        driver.GetChar(0, 0).Should().Be('H');
        driver.GetChar(1, 0).Should().Be('i');
        driver.GetText(0).Should().StartWith("Hi");
    }

    [Fact]
    public void TestTerminalDriver_Clear_ClearsBuffer()
    {
        var driver = new TestTerminalDriver(5, 5);
        driver.Initialize();
        driver.Flush(new List<CellChange> { new CellChange(0, 0, 'X', Style.Plain) });
        driver.Clear();
        driver.GetChar(0, 0).Should().Be(' ');
    }

    [Fact]
    public void TestTerminalDriver_EnqueueAndReadEvents()
    {
        var driver = new TestTerminalDriver(10, 5);
        var key = new KeyEvent(ConsoleKey.A, 'a', false, false, false);
        driver.EnqueueInput(key);
        var read = driver.ReadEvent(CancellationToken.None);
        read.Should().Be(key);
    }

    [Fact]
    public void TestTerminalDriver_ReadEvent_EmptyQueue_ReturnsNull()
    {
        var driver = new TestTerminalDriver(10, 5);
        var result = driver.ReadEvent(CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public void TestTerminalDriver_GetStyle()
    {
        var driver = new TestTerminalDriver(10, 5);
        driver.Initialize();
        var red = new Style(Color.Red);
        driver.Flush(new List<CellChange> { new CellChange(0, 0, 'X', red) });
        driver.GetStyle(0, 0).Should().Be(red);
    }

    // ── MouseParser ─────────────────────────────────────────────────

    [Fact]
    public void MouseParser_LeftClick()
    {
        var e = MouseParser.TryParse("0;10;5M");
        e.Should().NotBeNull();
        e!.Button.Should().Be(MouseButton.Left);
        e.EventType.Should().Be(MouseEventType.Press);
        e.Column.Should().Be(9);  // 0-indexed
        e.Row.Should().Be(4);
    }

    [Fact]
    public void MouseParser_RightClick()
    {
        var e = MouseParser.TryParse("2;5;3M");
        e.Should().NotBeNull();
        e!.Button.Should().Be(MouseButton.Right);
    }

    [Fact]
    public void MouseParser_MiddleClick()
    {
        var e = MouseParser.TryParse("1;5;3M");
        e.Should().NotBeNull();
        e!.Button.Should().Be(MouseButton.Middle);
    }

    [Fact]
    public void MouseParser_Release()
    {
        var e = MouseParser.TryParse("0;5;3m");
        e.Should().NotBeNull();
        e!.EventType.Should().Be(MouseEventType.Release);
    }

    [Fact]
    public void MouseParser_ScrollUp()
    {
        var e = MouseParser.TryParse("64;5;3M");
        e.Should().NotBeNull();
        e!.EventType.Should().Be(MouseEventType.ScrollUp);
    }

    [Fact]
    public void MouseParser_ScrollDown()
    {
        var e = MouseParser.TryParse("65;5;3M");
        e.Should().NotBeNull();
        e!.EventType.Should().Be(MouseEventType.ScrollDown);
    }

    [Fact]
    public void MouseParser_Motion()
    {
        // Motion with no button = 32 + 3 (baseButton=3 triggers Move)
        var e = MouseParser.TryParse("35;5;3M");
        e.Should().NotBeNull();
        e!.EventType.Should().Be(MouseEventType.Move);
        e.Button.Should().Be(MouseButton.None);
    }

    [Fact]
    public void MouseParser_MotionWithButton()
    {
        // Motion bit + left button (32 + 0 = 32) — treated as press with left
        var e = MouseParser.TryParse("32;5;3M");
        e.Should().NotBeNull();
        e!.Button.Should().Be(MouseButton.Left);
        e.EventType.Should().Be(MouseEventType.Press);
    }

    [Fact]
    public void MouseParser_WithModifiers()
    {
        // Shift (4) + Left (0) = 4
        var e = MouseParser.TryParse("4;5;3M");
        e.Should().NotBeNull();
        e!.Shift.Should().BeTrue();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeFalse();

        // Alt (8) + Left = 8
        var e2 = MouseParser.TryParse("8;5;3M");
        e2.Should().NotBeNull();
        e2!.Alt.Should().BeTrue();

        // Control (16) + Left = 16
        var e3 = MouseParser.TryParse("16;5;3M");
        e3.Should().NotBeNull();
        e3!.Control.Should().BeTrue();
    }

    [Fact]
    public void MouseParser_InvalidFormat_ReturnsNull()
    {
        MouseParser.TryParse("").Should().BeNull();
        MouseParser.TryParse("invalid").Should().BeNull();
        MouseParser.TryParse("0;5").Should().BeNull();
        MouseParser.TryParse("abc;5;3M").Should().BeNull();
    }

    // ── InputEvent ──────────────────────────────────────────────────

    [Fact]
    public void KeyEvent_Properties()
    {
        var e = new KeyEvent(ConsoleKey.Enter, '\r', true, true, true);
        e.Key.Should().Be(ConsoleKey.Enter);
        e.KeyChar.Should().Be('\r');
        e.Shift.Should().BeTrue();
        e.Alt.Should().BeTrue();
        e.Control.Should().BeTrue();
    }

    [Fact]
    public void KeyEvent_FromConsoleKeyInfo()
    {
        var info = new ConsoleKeyInfo('x', ConsoleKey.X, false, true, false);
        var e = new KeyEvent(info);
        e.Key.Should().Be(ConsoleKey.X);
        e.KeyChar.Should().Be('x');
        e.Alt.Should().BeTrue();
        e.Shift.Should().BeFalse();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void MouseEvent_Properties()
    {
        var e = new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 10, true, false, true);
        e.Button.Should().Be(MouseButton.Left);
        e.EventType.Should().Be(MouseEventType.Press);
        e.Column.Should().Be(5);
        e.Row.Should().Be(10);
        e.Shift.Should().BeTrue();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeTrue();
    }

    [Fact]
    public void ResizeEvent_Properties()
    {
        var e = new ResizeEvent(120, 40);
        e.Width.Should().Be(120);
        e.Height.Should().Be(40);
    }

    // ── CellChange ──────────────────────────────────────────────────

    [Fact]
    public void CellChange_Properties()
    {
        var c = new CellChange(5, 10, 'Z', new Style(Color.Red));
        c.Column.Should().Be(5);
        c.Row.Should().Be(10);
        c.Character.Should().Be('Z');
        c.Style.Foreground.Should().Be(Color.Red);
    }
}
