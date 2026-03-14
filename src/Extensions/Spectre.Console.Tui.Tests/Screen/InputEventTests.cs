using FluentAssertions;
using Spectre.Console.Tui.Screen;
using Xunit;

namespace Spectre.Console.Tui.Tests.Screen;

public class InputEventTests
{
    [Fact]
    public void KeyEvent_Should_Store_Properties()
    {
        // Act
        var e = new KeyEvent(ConsoleKey.A, 'a', shift: true, alt: false, control: true);

        // Assert
        e.Key.Should().Be(ConsoleKey.A);
        e.KeyChar.Should().Be('a');
        e.Shift.Should().BeTrue();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeTrue();
    }

    [Fact]
    public void KeyEvent_From_ConsoleKeyInfo_Should_Extract_Modifiers()
    {
        // Arrange
        var keyInfo = new ConsoleKeyInfo('A', ConsoleKey.A, true, true, false);

        // Act
        var e = new KeyEvent(keyInfo);

        // Assert
        e.Key.Should().Be(ConsoleKey.A);
        e.KeyChar.Should().Be('A');
        e.Shift.Should().BeTrue();
        e.Alt.Should().BeTrue();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void MouseEvent_Should_Store_Properties()
    {
        // Act
        var e = new MouseEvent(MouseButton.Left, MouseEventType.Press, 10, 5, shift: true);

        // Assert
        e.Button.Should().Be(MouseButton.Left);
        e.EventType.Should().Be(MouseEventType.Press);
        e.Column.Should().Be(10);
        e.Row.Should().Be(5);
        e.Shift.Should().BeTrue();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void ResizeEvent_Should_Store_Dimensions()
    {
        // Act
        var e = new ResizeEvent(120, 40);

        // Assert
        e.Width.Should().Be(120);
        e.Height.Should().Be(40);
    }
}
