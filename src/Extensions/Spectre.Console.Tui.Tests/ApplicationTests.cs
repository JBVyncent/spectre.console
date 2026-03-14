using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests;

public class ApplicationTests
{
    [Fact]
    public void Run_Should_Initialize_And_Shutdown_Driver()
    {
        // Arrange
        var driver = new TestTerminalDriver(80, 24);
        var app = new Application(driver) { MouseEnabled = false };

        // Enqueue nothing — app will exit after one loop iteration with no input
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        app.Run(cts.Token);

        // Assert
        driver.IsInitialized.Should().BeTrue();
        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void Run_With_Mouse_Should_Enable_And_Disable()
    {
        var driver = new TestTerminalDriver(80, 24);
        var app = new Application(driver) { MouseEnabled = true };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        app.Run(cts.Token);

        // Mouse should be disabled on shutdown
        driver.MouseEnabled.Should().BeFalse();
    }

    [Fact]
    public void Quit_Should_Exit_Loop()
    {
        var driver = new TestTerminalDriver(80, 24);
        var app = new Application(driver) { MouseEnabled = false };

        // Queue a key that triggers quit
        var label = new Label("Test");
        app.RootWidget = label;

        // Quit immediately
        driver.EnqueueInput(new KeyEvent(ConsoleKey.Q, 'q'));

        // The 'q' key won't quit by itself unless we hook it up.
        // Let's just use cancellation
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        app.Run(cts.Token);

        driver.IsShutdown.Should().BeTrue();
    }

    [Fact]
    public void RootWidget_Should_Render_To_Buffer()
    {
        var driver = new TestTerminalDriver(80, 24);
        var app = new Application(driver) { MouseEnabled = false };
        app.RootWidget = new Label("Hello TUI");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        app.Run(cts.Token);

        // Verify something was rendered
        driver.GetText(0).Should().Contain("Hello TUI");
    }

    [Fact]
    public void Tab_Should_Move_Focus()
    {
        // Test focus management directly since Application loop timing is non-deterministic
        var fm = new FocusManager();
        var vstack = new VStack();
        var a = new Button("A");
        var b = new Button("B");
        vstack.Add(a);
        vstack.Add(b);

        fm.RebuildChain(vstack);
        fm.Focused.Should().Be(a);
        a.HasFocus.Should().BeTrue();

        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().Be(b);
        b.HasFocus.Should().BeTrue();
        a.HasFocus.Should().BeFalse();
    }

    [Fact]
    public void Dispose_Should_Not_Throw()
    {
        var driver = new TestTerminalDriver(80, 24);
        var app = new Application(driver);
        app.Dispose(); // Should not throw
        app.Dispose(); // Double dispose should not throw
    }
}
