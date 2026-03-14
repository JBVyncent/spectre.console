using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class RadioButtonTests
{
    [Fact]
    public void Constructor_Defaults()
    {
        var rb = new RadioButton("Option A");
        rb.Text.Should().Be("Option A");
        rb.IsSelected.Should().BeFalse();
        rb.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void Select_Via_Spacebar()
    {
        var rb = new RadioButton("Option");
        rb.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' '));
        rb.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void RadioGroup_Should_Enforce_Exclusion()
    {
        var group = new RadioGroup();
        var a = new RadioButton("A");
        var b = new RadioButton("B");
        group.Add(a);
        group.Add(b);

        a.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' '));
        a.IsSelected.Should().BeTrue();
        b.IsSelected.Should().BeFalse();

        b.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' '));
        a.IsSelected.Should().BeFalse();
        b.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void RadioGroup_Selected_Should_Track()
    {
        var group = new RadioGroup();
        var a = new RadioButton("A");
        var b = new RadioButton("B");
        group.Add(a);
        group.Add(b);

        group.Selected.Should().BeNull();
        a.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        group.Selected.Should().Be(a);
    }

    [Fact]
    public void RadioGroup_SelectionChanged_Should_Fire()
    {
        var group = new RadioGroup();
        var a = new RadioButton("A");
        group.Add(a);
        RadioButton? selected = null;
        group.SelectionChanged += (_, rb) => selected = rb;
        a.OnKeyEvent(new KeyEvent(ConsoleKey.Spacebar, ' '));
        selected.Should().Be(a);
    }

    [Fact]
    public void RadioGroup_Remove_Should_Unlink()
    {
        var group = new RadioGroup();
        var a = new RadioButton("A");
        group.Add(a);
        group.Remove(a);
        group.Buttons.Should().BeEmpty();
    }
}
