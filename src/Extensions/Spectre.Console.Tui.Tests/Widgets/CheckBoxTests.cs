using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class CheckBoxTests
{
    [Fact]
    public void Constructor_Should_Set_Defaults()
    {
        var cb = new CheckBox("Option");
        cb.Text.Should().Be("Option");
        cb.IsChecked.Should().BeFalse();
        cb.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void Constructor_With_Checked_Should_Set_Checked()
    {
        var cb = new CheckBox("Option", true);
        cb.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void Toggle_Via_Spacebar()
    {
        var cb = new CheckBox("Option");
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' '));
        cb.IsChecked.Should().BeTrue();
        cb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' '));
        cb.IsChecked.Should().BeFalse();
    }

    [Fact]
    public void Toggle_Via_Mouse()
    {
        var cb = new CheckBox("Option");
        cb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0));
        cb.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void CheckedChanged_Should_Fire()
    {
        var cb = new CheckBox("Option");
        var fired = false;
        cb.CheckedChanged += (_, v) => fired = true;
        cb.IsChecked = true;
        fired.Should().BeTrue();
    }

    [Fact]
    public void CheckedChanged_Should_Not_Fire_If_Same_Value()
    {
        var cb = new CheckBox("Option", true);
        var fired = false;
        cb.CheckedChanged += (_, _) => fired = true;
        cb.IsChecked = true;
        fired.Should().BeFalse();
    }

    [Fact]
    public void Render_Should_Show_Unchecked()
    {
        var driver = new TestTerminalDriver(20, 1);
        var cb = new CheckBox("Test");
        cb.Arrange(new Rect(0, 0, 20, 1));
        cb.Render(new BufferSurface(driver.Buffer, cb.Bounds));
        driver.GetText(0).Should().StartWith("[ ] Test");
    }

    [Fact]
    public void Render_Should_Show_Checked()
    {
        var driver = new TestTerminalDriver(20, 1);
        var cb = new CheckBox("Test", true);
        cb.Arrange(new Rect(0, 0, 20, 1));
        cb.Render(new BufferSurface(driver.Buffer, cb.Bounds));
        driver.GetText(0).Should().StartWith("[x] Test");
    }
}
