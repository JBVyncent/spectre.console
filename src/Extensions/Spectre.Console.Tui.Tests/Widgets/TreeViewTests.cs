using FluentAssertions;
using Xunit;
using TuiTreeNode = Spectre.Console.Tui.Widgets.Controls.TreeNode;

namespace Spectre.Console.Tui.Tests.Widgets;

public class TreeViewTests
{
    [Fact]
    public void Constructor_Should_Create_Root()
    {
        var tv = new TreeView("Root");
        tv.Root.Text.Should().Be("Root");
        tv.Root.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void AddChild_Should_Add_To_Root()
    {
        var tv = new TreeView("Root");
        tv.Root.AddChild("Child");
        tv.Root.Children.Should().ContainSingle();
    }

    [Fact]
    public void Arrow_Down_Should_Navigate()
    {
        var tv = new TreeView("Root");
        tv.Root.AddChild("A");
        tv.Root.AddChild("B");

        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        // Should move from Root to A
    }

    [Fact]
    public void RightArrow_Should_Expand()
    {
        var tv = new TreeView("Root");
        var child = tv.Root.AddChild("Child");
        child.AddChild("Grandchild");
        child.IsExpanded = false;

        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));

        child.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void LeftArrow_Should_Collapse()
    {
        var tv = new TreeView("Root");
        var child = tv.Root.AddChild("Child");
        child.AddChild("Grandchild");
        child.IsExpanded = true;

        tv.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        tv.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));

        child.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void Enter_Should_Fire_NodeActivated()
    {
        var tv = new TreeView("Root");
        TuiTreeNode? activated = null;
        tv.NodeActivated += (_, n) => activated = n;

        tv.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().NotBeNull();
        activated!.Text.Should().Be("Root");
    }

    [Fact]
    public void Tag_Should_Store_Custom_Data()
    {
        var node = new TuiTreeNode("Test") { Tag = 42 };
        node.Tag.Should().Be(42);
    }
}
