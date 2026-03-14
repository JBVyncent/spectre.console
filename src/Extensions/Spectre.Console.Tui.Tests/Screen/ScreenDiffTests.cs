using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Xunit;

namespace Spectre.Console.Tui.Tests.Screen;

public class ScreenDiffTests
{
    [Fact]
    public void ComputeChanges_Should_Return_Empty_For_Identical_Buffers()
    {
        // Arrange
        var current = new ScreenBuffer(10, 5);
        var previous = new ScreenBuffer(10, 5);

        // Act
        var changes = ScreenDiff.ComputeChanges(current, previous);

        // Assert
        changes.Should().BeEmpty();
    }

    [Fact]
    public void ComputeChanges_Should_Detect_Character_Change()
    {
        // Arrange
        var current = new ScreenBuffer(10, 5);
        var previous = new ScreenBuffer(10, 5);
        current.SetCell(3, 2, 'X', Style.Plain);

        // Act
        var changes = ScreenDiff.ComputeChanges(current, previous);

        // Assert
        changes.Should().ContainSingle();
        changes[0].Column.Should().Be(3);
        changes[0].Row.Should().Be(2);
        changes[0].Character.Should().Be('X');
    }

    [Fact]
    public void ComputeChanges_Should_Detect_Style_Change()
    {
        // Arrange
        var current = new ScreenBuffer(10, 5);
        var previous = new ScreenBuffer(10, 5);
        current.SetCell(0, 0, ' ', new Style(Color.Red));

        // Act
        var changes = ScreenDiff.ComputeChanges(current, previous);

        // Assert
        changes.Should().ContainSingle();
        changes[0].Style.Should().Be(new Style(Color.Red));
    }

    [Fact]
    public void ComputeChanges_Should_Handle_Size_Increase()
    {
        // Arrange
        var current = new ScreenBuffer(12, 6);
        var previous = new ScreenBuffer(10, 5);
        current.SetCell(11, 5, 'Z', Style.Plain);

        // Act
        var changes = ScreenDiff.ComputeChanges(current, previous);

        // Assert
        changes.Should().NotBeEmpty();
        changes.Should().Contain(c => c.Column == 11 && c.Row == 5 && c.Character == 'Z');
    }

    [Fact]
    public void GetDirtyChanges_Should_Return_Only_Dirty_Cells()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        buffer.ClearDirtyFlags();
        buffer.SetCell(5, 3, 'A', Style.Plain);

        // Act
        var changes = ScreenDiff.GetDirtyChanges(buffer);

        // Assert
        changes.Should().ContainSingle();
        changes[0].Column.Should().Be(5);
        changes[0].Row.Should().Be(3);
        changes[0].Character.Should().Be('A');
    }

    [Fact]
    public void GetDirtyChanges_Should_Return_All_After_Clear()
    {
        // Arrange
        var buffer = new ScreenBuffer(3, 2);

        // Act
        var changes = ScreenDiff.GetDirtyChanges(buffer);

        // Assert
        changes.Should().HaveCount(6); // 3x2
    }
}
