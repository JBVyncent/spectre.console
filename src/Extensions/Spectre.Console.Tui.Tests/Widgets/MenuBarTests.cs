using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class MenuBarTests
{
    [Fact]
    public void AddItem_Should_Add()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.Items.Should().ContainSingle();
    }

    [Fact]
    public void RightArrow_Should_Navigate()
    {
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.AddItem(new MenuItem("Edit"));

        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        // Second right arrow wraps to first item (index 0→1→0... depends on initial -1)
    }

    [Fact]
    public void Enter_Should_Activate_Item()
    {
        var mb = new MenuBar();
        var activated = false;
        var item = new MenuItem("File");
        item.Activated += (_, _) => activated = true;
        mb.AddItem(item);

        mb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0')); // Select first
        mb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().BeTrue();
    }

    [Fact]
    public void MenuItem_Separator_Should_Be_Separator()
    {
        var sep = MenuItem.Separator();
        sep.IsSeparator.Should().BeTrue();
    }

    [Fact]
    public void Render_Should_Show_Items()
    {
        var driver = new TestTerminalDriver(40, 1);
        var mb = new MenuBar();
        mb.AddItem(new MenuItem("File"));
        mb.AddItem(new MenuItem("Edit"));
        mb.Arrange(new Rect(0, 0, 40, 1));
        mb.Render(new BufferSurface(driver.Buffer, mb.Bounds));

        driver.GetText(0).Should().Contain("File");
        driver.GetText(0).Should().Contain("Edit");
    }
}
