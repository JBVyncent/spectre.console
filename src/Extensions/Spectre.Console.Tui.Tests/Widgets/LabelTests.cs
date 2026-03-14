using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class LabelTests
{
    [Fact]
    public void Constructor_Should_Set_Text()
    {
        var label = new Label("Hello");
        label.Text.Should().Be("Hello");
    }

    [Fact]
    public void MeasureContent_Should_Return_Text_Dimensions()
    {
        var label = new Label("Hello");
        var size = label.MeasureContent(new Size(80, 24));
        size.Width.Should().Be(5);
        size.Height.Should().Be(1);
    }

    [Fact]
    public void MeasureContent_Should_Handle_Multiline()
    {
        var label = new Label("Line1\nLonger Line2");
        var size = label.MeasureContent(new Size(80, 24));
        size.Width.Should().Be(12); // "Longer Line2"
        size.Height.Should().Be(2);
    }

    [Fact]
    public void Render_Should_Write_Text_To_Surface()
    {
        // Arrange
        var driver = new TestTerminalDriver(20, 5);
        var label = new Label("Test");
        label.Arrange(new Rect(0, 0, 20, 1));

        // Act
        var surface = new BufferSurface(driver.Buffer, label.Bounds);
        label.Render(surface);

        // Assert
        driver.GetText(0).Should().StartWith("Test");
    }

    [Fact]
    public void Setting_Text_Should_Invalidate()
    {
        var label = new Label("Old");
        label.Arrange(new Rect(0, 0, 20, 1));
        label.Text = "New";
        label.NeedsRender.Should().BeTrue();
    }

    [Fact]
    public void CanFocus_Should_Default_False()
    {
        var label = new Label("test");
        label.CanFocus.Should().BeFalse();
    }
}
