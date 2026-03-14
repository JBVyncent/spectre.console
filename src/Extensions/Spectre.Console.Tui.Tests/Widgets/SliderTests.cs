using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class SliderTests
{
    [Fact]
    public void Default_Range()
    {
        var s = new Slider();
        s.Value.Should().Be(0);
        s.Minimum.Should().Be(0);
        s.Maximum.Should().Be(100);
    }

    [Fact]
    public void Value_Should_Clamp()
    {
        var s = new Slider();
        s.Value = 150;
        s.Value.Should().Be(100);
        s.Value = -10;
        s.Value.Should().Be(0);
    }

    [Fact]
    public void LeftArrow_Should_Decrease()
    {
        var s = new Slider { Value = 50 };
        s.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        s.Value.Should().Be(49);
    }

    [Fact]
    public void RightArrow_Should_Increase()
    {
        var s = new Slider { Value = 50 };
        s.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        s.Value.Should().Be(51);
    }

    [Fact]
    public void Home_Should_Go_To_Min()
    {
        var s = new Slider { Value = 50 };
        s.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        s.Value.Should().Be(0);
    }

    [Fact]
    public void End_Should_Go_To_Max()
    {
        var s = new Slider { Value = 50 };
        s.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        s.Value.Should().Be(100);
    }

    [Fact]
    public void ValueChanged_Should_Fire()
    {
        var s = new Slider();
        var fired = false;
        s.ValueChanged += (_, _) => fired = true;
        s.Value = 50;
        fired.Should().BeTrue();
    }
}
