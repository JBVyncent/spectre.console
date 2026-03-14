using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class ListBoxTests
{
    [Fact]
    public void AddItem_Should_Auto_Select_First()
    {
        var lb = new ListBox();
        lb.AddItem("First");
        lb.SelectedIndex.Should().Be(0);
        lb.SelectedItem.Should().Be("First");
    }

    [Fact]
    public void AddItems_Should_Add_Multiple()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.Items.Should().HaveCount(3);
    }

    [Fact]
    public void Arrow_Down_Should_Move_Selection()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void Arrow_Up_Should_Move_Selection()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.SelectedIndex = 2;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void Arrow_Down_At_End_Should_Stay()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B" });
        lb.SelectedIndex = 1;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void Home_Should_Go_To_First()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.SelectedIndex = 2;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void End_Should_Go_To_Last()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        lb.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void Enter_Should_Fire_ItemActivated()
    {
        var lb = new ListBox();
        lb.AddItem("A");
        var activated = -1;
        lb.ItemActivated += (_, i) => activated = i;
        lb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().Be(0);
    }

    [Fact]
    public void ClearItems_Should_Reset()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B" });
        lb.ClearItems();
        lb.Items.Should().BeEmpty();
        lb.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void RemoveItem_Should_Adjust_Selection()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B" });
        lb.SelectedIndex = 1;
        lb.RemoveItem(1);
        lb.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void Mouse_Click_Should_Select()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.Arrange(new Rect(0, 0, 20, 3));
        lb.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 5, 1));
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void ScrollUp_Should_Move_Selection()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B", "C" });
        lb.SelectedIndex = 2;
        lb.OnMouseEvent(new MouseEvent(MouseButton.None, MouseEventType.ScrollUp, 0, 0));
        lb.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void SelectionChanged_Should_Fire()
    {
        var lb = new ListBox();
        lb.AddItems(new[] { "A", "B" });
        var changed = -1;
        lb.SelectionChanged += (_, i) => changed = i;
        lb.SelectedIndex = 1;
        changed.Should().Be(1);
    }
}
