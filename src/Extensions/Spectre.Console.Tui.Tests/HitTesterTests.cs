using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests;

public class HitTesterTests
{
    [Fact]
    public void HitTest_Should_Return_Root_If_No_Children()
    {
        var label = new Label("Test");
        label.Arrange(new Rect(0, 0, 10, 1));

        var result = HitTester.HitTest(label, 5, 0);
        result.Should().Be(label);
    }

    [Fact]
    public void HitTest_Should_Return_Null_Outside_Bounds()
    {
        var label = new Label("Test");
        label.Arrange(new Rect(0, 0, 10, 1));

        HitTester.HitTest(label, 15, 0).Should().BeNull();
    }

    [Fact]
    public void HitTest_Should_Return_Deepest_Child()
    {
        var vstack = new VStack();
        var button = new Button("Click");
        var label = new Label("Text");
        vstack.Add(button);
        vstack.Add(label);

        vstack.Arrange(new Rect(0, 0, 80, 24));

        var result = HitTester.HitTest(vstack, 5, 0);
        result.Should().Be(button);
    }

    [Fact]
    public void HitTest_Should_Skip_Invisible_Widgets()
    {
        var vstack = new VStack();
        var invisible = new Button("Hidden") { Visible = false };
        var visible = new Label("Visible");
        vstack.Add(invisible);
        vstack.Add(visible);

        vstack.Arrange(new Rect(0, 0, 80, 24));

        var result = HitTester.HitTest(vstack, 5, 0);
        result.Should().Be(visible);
    }
}
