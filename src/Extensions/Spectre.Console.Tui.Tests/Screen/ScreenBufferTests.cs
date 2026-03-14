using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Xunit;

namespace Spectre.Console.Tui.Tests.Screen;

public class ScreenBufferTests
{
    [Fact]
    public void Constructor_Should_Create_Buffer_With_Correct_Dimensions()
    {
        // Arrange & Act
        var buffer = new ScreenBuffer(80, 24);

        // Assert
        buffer.Width.Should().Be(80);
        buffer.Height.Should().Be(24);
    }

    [Fact]
    public void Constructor_Should_Throw_For_Zero_Width()
    {
        // Act & Assert
        var act = () => new ScreenBuffer(0, 24);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_Should_Throw_For_Zero_Height()
    {
        // Act & Assert
        var act = () => new ScreenBuffer(80, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Clear_Should_Fill_With_Space_And_Plain_Style()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        buffer.SetCell(0, 0, 'X', new Style(Color.Red));

        // Act
        buffer.Clear();

        // Assert
        buffer[0, 0].Character.Should().Be(' ');
        buffer[0, 0].Style.Should().Be(Style.Plain);
        buffer[0, 0].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SetCell_Should_Update_Cell_And_Mark_Dirty()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        buffer.ClearDirtyFlags();
        var style = new Style(Color.Red);

        // Act
        buffer.SetCell(3, 2, 'A', style);

        // Assert
        buffer[3, 2].Character.Should().Be('A');
        buffer[3, 2].Style.Should().Be(style);
        buffer[3, 2].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SetCell_Should_Ignore_Out_Of_Bounds()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);

        // Act — should not throw
        buffer.SetCell(-1, 0, 'X', Style.Plain);
        buffer.SetCell(0, -1, 'X', Style.Plain);
        buffer.SetCell(10, 0, 'X', Style.Plain);
        buffer.SetCell(0, 5, 'X', Style.Plain);
    }

    [Fact]
    public void SetCell_Should_Not_Mark_Dirty_If_Same_Value()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        buffer.SetCell(0, 0, 'A', Style.Plain);
        buffer.ClearDirtyFlags();

        // Act
        buffer.SetCell(0, 0, 'A', Style.Plain);

        // Assert
        buffer[0, 0].IsDirty.Should().BeFalse();
    }

    [Fact]
    public void SetText_Should_Write_String_At_Position()
    {
        // Arrange
        var buffer = new ScreenBuffer(20, 5);
        var style = new Style(Color.Green);

        // Act
        buffer.SetText(2, 1, "Hello", style);

        // Assert
        buffer[2, 1].Character.Should().Be('H');
        buffer[3, 1].Character.Should().Be('e');
        buffer[4, 1].Character.Should().Be('l');
        buffer[5, 1].Character.Should().Be('l');
        buffer[6, 1].Character.Should().Be('o');
        buffer[2, 1].Style.Should().Be(style);
    }

    [Fact]
    public void SetText_Should_Clip_At_Width_Boundary()
    {
        // Arrange
        var buffer = new ScreenBuffer(5, 1);

        // Act
        buffer.SetText(3, 0, "Hello", Style.Plain);

        // Assert
        buffer[3, 0].Character.Should().Be('H');
        buffer[4, 0].Character.Should().Be('e');
    }

    [Fact]
    public void Fill_Should_Fill_Rect_Area()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        var style = new Style(Color.Blue);

        // Act
        buffer.Fill(new Rect(1, 1, 3, 2), '#', style);

        // Assert
        buffer[1, 1].Character.Should().Be('#');
        buffer[2, 1].Character.Should().Be('#');
        buffer[3, 1].Character.Should().Be('#');
        buffer[1, 2].Character.Should().Be('#');
        buffer[0, 0].Character.Should().Be(' '); // Not filled
        buffer[1, 1].Style.Should().Be(style);
    }

    [Fact]
    public void Resize_Should_Preserve_Existing_Content()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        buffer.SetCell(0, 0, 'A', Style.Plain);
        buffer.SetCell(4, 2, 'B', Style.Plain);

        // Act
        buffer.Resize(20, 10);

        // Assert
        buffer.Width.Should().Be(20);
        buffer.Height.Should().Be(10);
        buffer[0, 0].Character.Should().Be('A');
        buffer[4, 2].Character.Should().Be('B');
    }

    [Fact]
    public void Resize_Should_Shrink_Content()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        buffer.SetCell(9, 4, 'Z', Style.Plain);

        // Act
        buffer.Resize(5, 3);

        // Assert
        buffer.Width.Should().Be(5);
        buffer.Height.Should().Be(3);
    }

    [Fact]
    public void Resize_Same_Dimensions_Should_Be_Noop()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);
        buffer.SetCell(0, 0, 'X', Style.Plain);
        buffer.ClearDirtyFlags();

        // Act
        buffer.Resize(10, 5);

        // Assert
        buffer[0, 0].Character.Should().Be('X');
        buffer[0, 0].IsDirty.Should().BeFalse();
    }

    [Fact]
    public void ClearDirtyFlags_Should_Reset_All_Dirty_Flags()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);

        // Act
        buffer.ClearDirtyFlags();

        // Assert
        buffer[0, 0].IsDirty.Should().BeFalse();
        buffer[9, 4].IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Indexer_Should_Throw_For_Negative_Col()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);

        // Act & Assert
        var act = () => { var _ = buffer[-1, 0]; };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Indexer_Should_Throw_For_Col_Beyond_Width()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);

        // Act & Assert
        var act = () => { var _ = buffer[10, 0]; };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Resize_Should_Throw_For_Zero_Width()
    {
        // Arrange
        var buffer = new ScreenBuffer(10, 5);

        // Act & Assert
        var act = () => buffer.Resize(0, 5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
