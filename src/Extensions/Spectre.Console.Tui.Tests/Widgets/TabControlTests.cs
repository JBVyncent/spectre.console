using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Chrome;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class TabControlTests
{
    [Fact]
    public void AddTab_Should_Add_Page()
    {
        var tc = new TabControl();
        tc.AddTab("Tab1", new Label("Content"));
        tc.Tabs.Should().ContainSingle();
        tc.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void SelectedTab_Should_Return_Current()
    {
        var tc = new TabControl();
        tc.AddTab("Tab1", new Label("A"));
        tc.AddTab("Tab2", new Label("B"));
        tc.SelectedTab!.Title.Should().Be("Tab1");
        tc.SelectedIndex = 1;
        tc.SelectedTab!.Title.Should().Be("Tab2");
    }

    [Fact]
    public void GetChildren_Should_Return_Only_Active_Tab()
    {
        var tc = new TabControl();
        var a = new Label("A");
        var b = new Label("B");
        tc.AddTab("Tab1", a);
        tc.AddTab("Tab2", b);

        tc.GetChildren().Should().ContainSingle().Which.Should().Be(a);
        tc.SelectedIndex = 1;
        tc.GetChildren().Should().ContainSingle().Which.Should().Be(b);
    }

    [Fact]
    public void Keyboard_Navigation()
    {
        var tc = new TabControl();
        tc.HasFocus = true;
        tc.AddTab("A", new Label("1"));
        tc.AddTab("B", new Label("2"));

        tc.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        tc.SelectedIndex.Should().Be(1);

        tc.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        tc.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void SelectedTabChanged_Should_Fire()
    {
        var tc = new TabControl();
        tc.AddTab("A", new Label("1"));
        tc.AddTab("B", new Label("2"));
        var changed = -1;
        tc.SelectedTabChanged += (_, i) => changed = i;
        tc.SelectedIndex = 1;
        changed.Should().Be(1);
    }
}
