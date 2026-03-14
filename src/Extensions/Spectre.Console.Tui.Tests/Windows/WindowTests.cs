using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Spectre.Console.Tui.Windows;
using Xunit;

namespace Spectre.Console.Tui.Tests.Windows;

public class WindowTests
{
    [Fact]
    public void Constructor_Should_Set_Title()
    {
        var window = new Window("Test Window");
        window.Title.Should().Be("Test Window");
        window.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void Render_Should_Draw_Border_And_Title()
    {
        var driver = new TestTerminalDriver(30, 10);
        var window = new Window("My Window");
        window.Arrange(new Rect(0, 0, 30, 10));
        window.Render(new BufferSurface(driver.Buffer, window.Bounds));

        // Title row should contain "My Window"
        driver.GetText(1).Should().Contain("My Window");
        // Top-left corner
        driver.GetChar(0, 0).Should().Be('\u250c');
        // Bottom-right corner
        driver.GetChar(29, 9).Should().Be('\u2518');
    }

    [Fact]
    public void Close_Button_Should_Fire_Event()
    {
        var window = new Window("Test") { Closable = true };
        window.Arrange(new Rect(0, 0, 30, 10));
        var closed = false;
        window.Closed += (_, _) => closed = true;

        // Click close button at top-right of title bar
        window.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 28, 1));
        closed.Should().BeTrue();
    }

    [Fact]
    public void Window_Should_Arrange_Children_Inside_Content_Area()
    {
        var window = new Window("Test");
        var label = new Label("Hello");
        window.Add(label);
        window.Arrange(new Rect(0, 0, 30, 10));

        // Content area starts at (1, 2) - inside border, below title
        label.Bounds.X.Should().Be(1);
        label.Bounds.Y.Should().Be(2);
    }

    [Fact]
    public void Render_Small_Window_Should_Not_Crash()
    {
        var driver = new TestTerminalDriver(2, 2);
        var window = new Window("T");
        window.Arrange(new Rect(0, 0, 2, 2));
        // Should not throw
        window.Render(new BufferSurface(driver.Buffer, window.Bounds));
    }
}

public class WindowManagerTests
{
    [Fact]
    public void AddWindow_Should_Add_To_List()
    {
        var wm = new WindowManager();
        var w = new Window("Test");
        wm.AddWindow(w);
        wm.Windows.Should().ContainSingle();
        wm.ActiveWindow.Should().Be(w);
    }

    [Fact]
    public void BringToFront_Should_Change_Order()
    {
        var wm = new WindowManager();
        var a = new Window("A");
        var b = new Window("B");
        wm.AddWindow(a);
        wm.AddWindow(b);

        wm.BringToFront(a);
        wm.ActiveWindow.Should().Be(a);
    }

    [Fact]
    public void SendToBack_Should_Move_To_Bottom()
    {
        var wm = new WindowManager();
        var a = new Window("A");
        var b = new Window("B");
        wm.AddWindow(a);
        wm.AddWindow(b);

        wm.SendToBack(b);
        wm.ActiveWindow.Should().Be(a);
    }

    [Fact]
    public void RemoveWindow_Should_Remove()
    {
        var wm = new WindowManager();
        var w = new Window("Test");
        wm.AddWindow(w);
        wm.RemoveWindow(w);
        wm.Windows.Should().BeEmpty();
    }

    [Fact]
    public void GetWindowAt_Should_Find_Topmost()
    {
        var wm = new WindowManager();
        var a = new Window("A");
        a.Arrange(new Rect(0, 0, 20, 10));
        var b = new Window("B");
        b.Arrange(new Rect(5, 5, 20, 10));

        wm.AddWindow(a);
        wm.AddWindow(b);

        // Point in overlap — should return b (topmost)
        wm.GetWindowAt(10, 7).Should().Be(b);
        // Point only in a
        wm.GetWindowAt(2, 2).Should().Be(a);
        // Point outside both
        wm.GetWindowAt(50, 50).Should().BeNull();
    }
}

public class DialogTests
{
    [Fact]
    public void Dialog_Should_Default_To_None_Result()
    {
        var dialog = new Dialog("Test");
        dialog.Result.Should().Be(DialogResult.None);
    }

    [Fact]
    public void Close_Should_Set_Result()
    {
        var dialog = new Dialog("Test");
        dialog.Close(DialogResult.Ok);
        dialog.Result.Should().Be(DialogResult.Ok);
    }
}
