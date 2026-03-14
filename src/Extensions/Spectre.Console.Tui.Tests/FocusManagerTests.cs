using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests;

public class FocusManagerTests
{
    [Fact]
    public void RebuildChain_Should_Find_Focusable_Widgets()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        var btn = new Button("A");
        var lbl = new Label("B"); // CanFocus = false
        vstack.Add(btn);
        vstack.Add(lbl);

        fm.RebuildChain(vstack);

        fm.Focused.Should().Be(btn);
    }

    [Fact]
    public void MoveFocus_Forward_Should_Cycle()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        var a = new Button("A");
        var b = new Button("B");
        vstack.Add(a);
        vstack.Add(b);

        fm.RebuildChain(vstack);
        fm.Focused.Should().Be(a);

        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().Be(b);

        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().Be(a); // wraps
    }

    [Fact]
    public void MoveFocus_Backward_Should_Cycle()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        var a = new Button("A");
        var b = new Button("B");
        vstack.Add(a);
        vstack.Add(b);

        fm.RebuildChain(vstack);
        fm.MoveFocus(FocusDirection.Backward);
        fm.Focused.Should().Be(b); // wraps to last
    }

    [Fact]
    public void SetFocus_Should_Switch()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        var a = new Button("A");
        var b = new Button("B");
        vstack.Add(a);
        vstack.Add(b);

        fm.RebuildChain(vstack);
        fm.SetFocus(b);
        fm.Focused.Should().Be(b);
        b.HasFocus.Should().BeTrue();
        a.HasFocus.Should().BeFalse();
    }

    [Fact]
    public void RemoveFromChain_Should_Update_Focus()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        var a = new Button("A");
        var b = new Button("B");
        vstack.Add(a);
        vstack.Add(b);

        fm.RebuildChain(vstack);
        fm.RemoveFromChain(a);
        fm.Focused.Should().Be(b);
    }

    [Fact]
    public void Hidden_Widget_Should_Not_Be_In_Chain()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        var a = new Button("A") { Visible = false };
        var b = new Button("B");
        vstack.Add(a);
        vstack.Add(b);

        fm.RebuildChain(vstack);
        fm.Focused.Should().Be(b);
    }

    [Fact]
    public void Empty_Chain_Should_Have_Null_Focus()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        vstack.Add(new Label("text")); // not focusable

        fm.RebuildChain(vstack);
        fm.Focused.Should().BeNull();
    }

    [Fact]
    public void MoveFocus_On_Empty_Chain_Should_Return_False()
    {
        var fm = new FocusManager();
        var vstack = new VStack();
        fm.RebuildChain(vstack);
        fm.MoveFocus(FocusDirection.Forward).Should().BeFalse();
    }
}
