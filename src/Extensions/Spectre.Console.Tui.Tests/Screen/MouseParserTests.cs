using FluentAssertions;
using Spectre.Console.Tui.Screen;
using Xunit;

namespace Spectre.Console.Tui.Tests.Screen;

public class MouseParserTests
{
    [Fact]
    public void TryParse_Should_Parse_Left_Button_Press()
    {
        // Act
        var result = MouseParser.TryParse("0;15;8M");

        // Assert
        result.Should().NotBeNull();
        result!.Button.Should().Be(MouseButton.Left);
        result.EventType.Should().Be(MouseEventType.Press);
        result.Column.Should().Be(14); // 0-indexed
        result.Row.Should().Be(7);
    }

    [Fact]
    public void TryParse_Should_Parse_Left_Button_Release()
    {
        // Act
        var result = MouseParser.TryParse("0;15;8m");

        // Assert
        result.Should().NotBeNull();
        result!.Button.Should().Be(MouseButton.Left);
        result.EventType.Should().Be(MouseEventType.Release);
    }

    [Fact]
    public void TryParse_Should_Parse_Right_Button()
    {
        // Act
        var result = MouseParser.TryParse("2;5;3M");

        // Assert
        result.Should().NotBeNull();
        result!.Button.Should().Be(MouseButton.Right);
        result.EventType.Should().Be(MouseEventType.Press);
    }

    [Fact]
    public void TryParse_Should_Parse_Middle_Button()
    {
        // Act
        var result = MouseParser.TryParse("1;5;3M");

        // Assert
        result.Should().NotBeNull();
        result!.Button.Should().Be(MouseButton.Middle);
    }

    [Fact]
    public void TryParse_Should_Parse_ScrollUp()
    {
        // Act — 64 = scroll bit
        var result = MouseParser.TryParse("64;10;5M");

        // Assert
        result.Should().NotBeNull();
        result!.EventType.Should().Be(MouseEventType.ScrollUp);
        result.Button.Should().Be(MouseButton.None);
    }

    [Fact]
    public void TryParse_Should_Parse_ScrollDown()
    {
        // Act — 65 = scroll bit + 1
        var result = MouseParser.TryParse("65;10;5M");

        // Assert
        result.Should().NotBeNull();
        result!.EventType.Should().Be(MouseEventType.ScrollDown);
    }

    [Fact]
    public void TryParse_Should_Parse_Motion()
    {
        // Act — 35 = motion bit (32) + button 3 (no button)
        var result = MouseParser.TryParse("35;10;5M");

        // Assert
        result.Should().NotBeNull();
        result!.EventType.Should().Be(MouseEventType.Move);
        result.Button.Should().Be(MouseButton.None);
    }

    [Fact]
    public void TryParse_Should_Parse_Modifiers()
    {
        // Act — 4 = shift, 8 = alt, 16 = ctrl. 0 + 4 + 8 = 12
        var result = MouseParser.TryParse("12;5;3M");

        // Assert
        result.Should().NotBeNull();
        result!.Shift.Should().BeTrue();
        result.Alt.Should().BeTrue();
        result.Control.Should().BeFalse();
    }

    [Fact]
    public void TryParse_Should_Parse_Ctrl_Click()
    {
        // Act — 16 = ctrl
        var result = MouseParser.TryParse("16;5;3M");

        // Assert
        result.Should().NotBeNull();
        result!.Control.Should().BeTrue();
    }

    [Fact]
    public void TryParse_Should_Return_Null_For_Empty()
    {
        // Act & Assert
        MouseParser.TryParse("").Should().BeNull();
        MouseParser.TryParse(null!).Should().BeNull();
    }

    [Fact]
    public void TryParse_Should_Return_Null_For_Too_Short()
    {
        MouseParser.TryParse("0M").Should().BeNull();
    }

    [Fact]
    public void TryParse_Should_Return_Null_For_Invalid_Terminator()
    {
        MouseParser.TryParse("0;5;3X").Should().BeNull();
    }

    [Fact]
    public void TryParse_Should_Return_Null_For_Wrong_Part_Count()
    {
        MouseParser.TryParse("0;5M").Should().BeNull();
    }

    [Fact]
    public void TryParse_Should_Return_Null_For_NonNumeric_Parts()
    {
        MouseParser.TryParse("a;5;3M").Should().BeNull();
    }
}
