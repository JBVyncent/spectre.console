using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class ProgressBarTests
{
    [Fact]
    public void Default_Value_Should_Be_Zero()
    {
        var pb = new ProgressBar();
        pb.Value.Should().Be(0);
        pb.MaxValue.Should().Be(100);
    }

    [Fact]
    public void Value_Should_Clamp_To_MaxValue()
    {
        var pb = new ProgressBar();
        pb.Value = 150;
        pb.Value.Should().Be(100);
    }

    [Fact]
    public void Value_Should_Clamp_To_Zero()
    {
        var pb = new ProgressBar();
        pb.Value = -10;
        pb.Value.Should().Be(0);
    }

    [Fact]
    public void ValueChanged_Should_Fire()
    {
        var pb = new ProgressBar();
        double? changed = null;
        pb.ValueChanged += (_, v) => changed = v;
        pb.Value = 50;
        changed.Should().Be(50);
    }

    [Fact]
    public void Render_Should_Show_Percentage()
    {
        var driver = new TestTerminalDriver(20, 1);
        var pb = new ProgressBar { Value = 50 };
        pb.Arrange(new Rect(0, 0, 20, 1));
        pb.Render(new BufferSurface(driver.Buffer, pb.Bounds));
        driver.GetText(0).Should().Contain("50%");
    }
}
