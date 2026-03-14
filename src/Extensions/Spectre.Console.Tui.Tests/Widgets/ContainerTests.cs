using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class ContainerTests
{
    [Fact]
    public void VStack_Should_Arrange_Children_Vertically()
    {
        var vstack = new VStack();
        var a = new Label("A");
        var b = new Label("B");
        vstack.Add(a);
        vstack.Add(b);

        vstack.Arrange(new Rect(0, 0, 80, 24));

        a.Bounds.Y.Should().Be(0);
        b.Bounds.Y.Should().Be(1);
    }

    [Fact]
    public void VStack_With_Spacing()
    {
        var vstack = new VStack { Spacing = 1 };
        var a = new Label("A");
        var b = new Label("B");
        vstack.Add(a);
        vstack.Add(b);

        vstack.Arrange(new Rect(0, 0, 80, 24));

        a.Bounds.Y.Should().Be(0);
        b.Bounds.Y.Should().Be(2); // 1 row + 1 spacing
    }

    [Fact]
    public void HStack_Should_Arrange_Children_Horizontally()
    {
        var hstack = new HStack();
        var a = new Label("AB");
        var b = new Label("CD");
        hstack.Add(a);
        hstack.Add(b);

        hstack.Arrange(new Rect(0, 0, 80, 24));

        a.Bounds.X.Should().Be(0);
        b.Bounds.X.Should().Be(2);
    }

    [Fact]
    public void HStack_With_Spacing()
    {
        var hstack = new HStack { Spacing = 2 };
        var a = new Label("AB");
        var b = new Label("CD");
        hstack.Add(a);
        hstack.Add(b);

        hstack.Arrange(new Rect(0, 0, 80, 24));

        a.Bounds.X.Should().Be(0);
        b.Bounds.X.Should().Be(4); // 2 chars + 2 spacing
    }

    [Fact]
    public void VStack_Fill_Should_Take_Remaining_Space()
    {
        var vstack = new VStack();
        var a = new Label("A");
        var b = new Label("B") { HeightConstraint = Constraint.Fill() };
        vstack.Add(a);
        vstack.Add(b);

        vstack.Arrange(new Rect(0, 0, 80, 20));

        a.Bounds.Height.Should().Be(1);
        b.Bounds.Height.Should().Be(19);
    }

    [Fact]
    public void Container_Add_Should_Set_Parent()
    {
        var vstack = new VStack();
        var label = new Label("Test");
        vstack.Add(label);
        label.Parent.Should().Be(vstack);
    }

    [Fact]
    public void Container_Remove_Should_Clear_Parent()
    {
        var vstack = new VStack();
        var label = new Label("Test");
        vstack.Add(label);
        vstack.Remove(label);
        label.Parent.Should().BeNull();
    }

    [Fact]
    public void Container_Clear_Should_Remove_All()
    {
        var vstack = new VStack();
        vstack.Add(new Label("A"));
        vstack.Add(new Label("B"));
        vstack.Clear();
        vstack.Children.Should().BeEmpty();
    }

    [Fact]
    public void VStack_Hidden_Children_Should_Not_Take_Space()
    {
        var vstack = new VStack();
        var a = new Label("A");
        var b = new Label("B") { Visible = false };
        var c = new Label("C");
        vstack.Add(a);
        vstack.Add(b);
        vstack.Add(c);

        vstack.Arrange(new Rect(0, 0, 80, 24));

        a.Bounds.Y.Should().Be(0);
        c.Bounds.Y.Should().Be(1); // b is hidden, so c comes right after a
    }

    [Fact]
    public void HStack_Fill_Should_Take_Remaining_Width()
    {
        var hstack = new HStack();
        var a = new Label("AB");
        var b = new Label("X") { WidthConstraint = Constraint.Fill() };
        hstack.Add(a);
        hstack.Add(b);

        hstack.Arrange(new Rect(0, 0, 80, 24));

        a.Bounds.Width.Should().Be(2);
        b.Bounds.Width.Should().Be(78);
    }
}
