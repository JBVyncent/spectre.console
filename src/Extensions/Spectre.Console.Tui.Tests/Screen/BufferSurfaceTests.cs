using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Xunit;

namespace Spectre.Console.Tui.Tests.Screen;

public class BufferSurfaceTests
{
    [Fact]
    public void Constructor_Should_Create_Full_Buffer_Surface()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);

        // Act
        var surface = new BufferSurface(buffer);

        // Assert
        surface.Width.Should().Be(80);
        surface.Height.Should().Be(24);
    }

    [Fact]
    public void Constructor_With_Bounds_Should_Clip_To_Buffer()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);

        // Act
        var surface = new BufferSurface(buffer, new Rect(10, 5, 30, 10));

        // Assert
        surface.Width.Should().Be(30);
        surface.Height.Should().Be(10);
    }

    [Fact]
    public void SetCell_Should_Offset_Coordinates()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);
        var surface = new BufferSurface(buffer, new Rect(10, 5, 30, 10));

        // Act
        surface.SetCell(0, 0, 'X', Style.Plain);

        // Assert
        buffer[10, 5].Character.Should().Be('X');
    }

    [Fact]
    public void SetCell_Should_Clip_Outside_Bounds()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);
        var surface = new BufferSurface(buffer, new Rect(10, 5, 5, 3));

        // Act — should not throw, should be clipped
        surface.SetCell(-1, 0, 'A', Style.Plain);
        surface.SetCell(5, 0, 'B', Style.Plain);

        // Assert — nothing written
        buffer[9, 5].Character.Should().Be(' ');
    }

    [Fact]
    public void SetText_Should_Write_At_Offset()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);
        var surface = new BufferSurface(buffer, new Rect(5, 3, 20, 5));

        // Act
        surface.SetText(0, 0, "Hi", Style.Plain);

        // Assert
        buffer[5, 3].Character.Should().Be('H');
        buffer[6, 3].Character.Should().Be('i');
    }

    [Fact]
    public void Fill_Should_Fill_Within_Bounds()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);
        var surface = new BufferSurface(buffer, new Rect(5, 3, 10, 5));

        // Act
        surface.Fill(new Rect(0, 0, 3, 2), '#', Style.Plain);

        // Assert
        buffer[5, 3].Character.Should().Be('#');
        buffer[6, 3].Character.Should().Be('#');
        buffer[7, 3].Character.Should().Be('#');
        buffer[5, 4].Character.Should().Be('#');
    }

    [Fact]
    public void Clear_Should_Fill_With_Spaces()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);
        var surface = new BufferSurface(buffer, new Rect(5, 3, 10, 5));
        surface.SetCell(0, 0, 'X', Style.Plain);

        // Act
        surface.Clear();

        // Assert
        buffer[5, 3].Character.Should().Be(' ');
    }

    [Fact]
    public void CreateSubSurface_Should_Nest_Offsets()
    {
        // Arrange
        var buffer = new ScreenBuffer(80, 24);
        var surface = new BufferSurface(buffer, new Rect(10, 5, 30, 10));

        // Act
        var sub = surface.CreateSubSurface(new Rect(5, 3, 10, 4));
        sub.SetCell(0, 0, 'Z', Style.Plain);

        // Assert
        buffer[15, 8].Character.Should().Be('Z'); // 10+5=15, 5+3=8
    }
}
