using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class TuiPanelTests
{
    [Fact]
    public void Content_Should_Be_Arranged_Inside_Border()
    {
        var panel = new TuiPanel { Content = new Label("Test"), Title = "Title" };
        panel.Arrange(new Rect(0, 0, 20, 10));

        panel.Content!.Bounds.X.Should().Be(1);
        panel.Content.Bounds.Y.Should().Be(1);
        panel.Content.Bounds.Width.Should().Be(18);
    }

    [Fact]
    public void Render_Should_Draw_Border()
    {
        var driver = new TestTerminalDriver(20, 5);
        var panel = new TuiPanel { Title = "Test" };
        panel.Arrange(new Rect(0, 0, 20, 5));
        panel.Render(new BufferSurface(driver.Buffer, panel.Bounds));

        driver.GetChar(0, 0).Should().Be('\u250c'); // ┌
        driver.GetChar(19, 0).Should().Be('\u2510'); // ┐
        driver.GetChar(0, 4).Should().Be('\u2514'); // └
        driver.GetChar(19, 4).Should().Be('\u2518'); // ┘
    }

    [Fact]
    public void Render_Should_Show_Title()
    {
        var driver = new TestTerminalDriver(20, 5);
        var panel = new TuiPanel { Title = "Test" };
        panel.Arrange(new Rect(0, 0, 20, 5));
        panel.Render(new BufferSurface(driver.Buffer, panel.Bounds));
        driver.GetText(0).Should().Contain("Test");
    }

    [Fact]
    public void GetChildren_Should_Return_Content()
    {
        var label = new Label("X");
        var panel = new TuiPanel { Content = label };
        panel.GetChildren().Should().ContainSingle().Which.Should().Be(label);
    }

    [Fact]
    public void GetChildren_Without_Content_Should_Be_Empty()
    {
        var panel = new TuiPanel();
        panel.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void Setting_Content_Should_Set_Parent()
    {
        var panel = new TuiPanel();
        var label = new Label("Test");
        panel.Content = label;
        label.Parent.Should().Be(panel);
    }
}
