using FluentAssertions;
using Spectre.Console.Tui.Screen;
using Xunit;

namespace Spectre.Console.Tui.Tests.Screen;

public class TestTerminalDriverTests
{
    [Fact]
    public void Constructor_Should_Create_With_Defaults()
    {
        // Act
        var driver = new TestTerminalDriver();

        // Assert
        driver.Width.Should().Be(80);
        driver.Height.Should().Be(24);
        driver.CursorVisible.Should().BeTrue();
        driver.MouseEnabled.Should().BeFalse();
        driver.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Initialize_Should_Set_Initialized()
    {
        // Arrange
        var driver = new TestTerminalDriver();

        // Act
        driver.Initialize();

        // Assert
        driver.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Shutdown_Should_Set_Shutdown()
    {
        // Arrange
        var driver = new TestTerminalDriver();

        // Act
        driver.Shutdown();

        // Assert
        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void EnableMouse_Should_Set_Flag()
    {
        // Arrange
        var driver = new TestTerminalDriver();

        // Act
        driver.EnableMouse();

        // Assert
        driver.MouseEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableMouse_Should_Clear_Flag()
    {
        // Arrange
        var driver = new TestTerminalDriver();
        driver.EnableMouse();

        // Act
        driver.DisableMouse();

        // Assert
        driver.MouseEnabled.Should().BeFalse();
    }

    [Fact]
    public void HideCursor_Should_Set_Flag()
    {
        // Arrange
        var driver = new TestTerminalDriver();

        // Act
        driver.HideCursor();

        // Assert
        driver.CursorVisible.Should().BeFalse();
    }

    [Fact]
    public void Flush_Should_Write_To_Buffer()
    {
        // Arrange
        var driver = new TestTerminalDriver(10, 5);
        var changes = new List<CellChange>
        {
            new CellChange(0, 0, 'H', Style.Plain),
            new CellChange(1, 0, 'i', Style.Plain),
        };

        // Act
        driver.Flush(changes);

        // Assert
        driver.GetChar(0, 0).Should().Be('H');
        driver.GetChar(1, 0).Should().Be('i');
    }

    [Fact]
    public void EnqueueKey_And_ReadEvent_Should_Round_Trip()
    {
        // Arrange
        var driver = new TestTerminalDriver();
        driver.EnqueueKey(ConsoleKey.A, 'a');

        // Act
        var evt = driver.ReadEvent(CancellationToken.None);

        // Assert
        evt.Should().BeOfType<KeyEvent>();
        var keyEvt = (KeyEvent)evt!;
        keyEvt.Key.Should().Be(ConsoleKey.A);
        keyEvt.KeyChar.Should().Be('a');
    }

    [Fact]
    public void ReadEvent_Should_Return_Null_When_Empty()
    {
        // Arrange
        var driver = new TestTerminalDriver();

        // Act
        var evt = driver.ReadEvent(CancellationToken.None);

        // Assert
        evt.Should().BeNull();
    }

    [Fact]
    public void ReadEvent_Should_Return_Null_When_Cancelled()
    {
        // Arrange
        var driver = new TestTerminalDriver();
        driver.EnqueueKey(ConsoleKey.A, 'a');
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var evt = driver.ReadEvent(cts.Token);

        // Assert
        evt.Should().BeNull();
    }

    [Fact]
    public void GetText_Should_Return_Row_Content()
    {
        // Arrange
        var driver = new TestTerminalDriver(20, 5);
        driver.Flush(new List<CellChange>
        {
            new CellChange(0, 0, 'H', Style.Plain),
            new CellChange(1, 0, 'e', Style.Plain),
            new CellChange(2, 0, 'l', Style.Plain),
            new CellChange(3, 0, 'l', Style.Plain),
            new CellChange(4, 0, 'o', Style.Plain),
        });

        // Act
        var text = driver.GetText(0);

        // Assert
        text.Should().Be("Hello");
    }

    [Fact]
    public void GetStyle_Should_Return_Cell_Style()
    {
        // Arrange
        var driver = new TestTerminalDriver(10, 5);
        var style = new Style(Color.Red);
        driver.Flush(new List<CellChange>
        {
            new CellChange(0, 0, 'X', style),
        });

        // Act
        var result = driver.GetStyle(0, 0);

        // Assert
        result.Should().Be(style);
    }
}
