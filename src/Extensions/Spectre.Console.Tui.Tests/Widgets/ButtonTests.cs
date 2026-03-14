using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class ButtonTests
{
    [Fact]
    public void Constructor_Should_Set_Text_And_CanFocus()
    {
        var button = new Button("OK");
        button.Text.Should().Be("OK");
        button.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void MeasureContent_Should_Include_Brackets()
    {
        var button = new Button("OK");
        var size = button.MeasureContent(new Size(80, 24));
        size.Width.Should().Be(6); // "[ OK ]"
        size.Height.Should().Be(1);
    }

    [Fact]
    public void Render_Should_Show_Brackets()
    {
        // Arrange
        var driver = new TestTerminalDriver(20, 5);
        var button = new Button("OK");
        button.Arrange(new Rect(0, 0, 20, 1));

        // Act
        var surface = new BufferSurface(driver.Buffer, button.Bounds);
        button.Render(surface);

        // Assert
        driver.GetText(0).Should().StartWith("[ OK ]");
    }

    [Fact]
    public void OnKeyEvent_Enter_Should_Fire_Clicked()
    {
        // Arrange
        var button = new Button("OK");
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;

        // Act
        var handled = button.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));

        // Assert
        handled.Should().BeTrue();
        clicked.Should().BeTrue();
    }

    [Fact]
    public void OnKeyEvent_Spacebar_Should_Fire_Clicked()
    {
        // Arrange
        var button = new Button("OK");
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;

        // Act
        button.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' '));

        // Assert
        clicked.Should().BeTrue();
    }

    [Fact]
    public void OnKeyEvent_Other_Should_Not_Handle()
    {
        var button = new Button("OK");
        button.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a')).Should().BeFalse();
    }

    [Fact]
    public void OnMouseEvent_LeftClick_Should_Fire_Clicked()
    {
        // Arrange
        var button = new Button("OK");
        var clicked = false;
        button.Clicked += (_, _) => clicked = true;

        // Act
        button.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0));

        // Assert
        clicked.Should().BeTrue();
    }

    [Fact]
    public void OnMouseEvent_RightClick_Should_Not_Handle()
    {
        var button = new Button("OK");
        button.OnMouseEvent(new MouseEvent(MouseButton.Right, MouseEventType.Press, 0, 0))
            .Should().BeFalse();
    }
}
