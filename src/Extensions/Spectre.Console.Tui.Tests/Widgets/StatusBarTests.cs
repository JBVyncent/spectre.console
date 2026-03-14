using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class StatusBarTests
{
    [Fact]
    public void Text_Should_Render()
    {
        var driver = new TestTerminalDriver(40, 1);
        var sb = new StatusBar { Text = "Ready" };
        sb.Arrange(new Rect(0, 0, 40, 1));
        sb.Render(new BufferSurface(driver.Buffer, sb.Bounds));
        driver.GetText(0).Should().Contain("Ready");
    }

    [Fact]
    public void AddItem_Should_Render_Key_Label()
    {
        var driver = new TestTerminalDriver(40, 1);
        var sb = new StatusBar();
        sb.AddItem("F1", "Help");
        sb.Arrange(new Rect(0, 0, 40, 1));
        sb.Render(new BufferSurface(driver.Buffer, sb.Bounds));
        driver.GetText(0).Should().Contain("F1");
        driver.GetText(0).Should().Contain("Help");
    }

    [Fact]
    public void ClearItems_Should_Remove_All()
    {
        var sb = new StatusBar();
        sb.AddItem("F1", "Help");
        sb.ClearItems();
        sb.Items.Should().BeEmpty();
    }

    [Fact]
    public void Click_Should_Invoke_Action()
    {
        var sb = new StatusBar();
        var invoked = false;
        sb.AddItem("F1", "Help", () => invoked = true);
        sb.Arrange(new Rect(0, 0, 40, 1));

        sb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0));
        invoked.Should().BeTrue();
    }
}
