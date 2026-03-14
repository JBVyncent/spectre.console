using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class SplitterTests
{
    [Fact]
    public void Default_Split_Should_Be_50_Percent()
    {
        var splitter = new Splitter();
        splitter.SplitRatio.Should().Be(0.5);
    }

    [Fact]
    public void Vertical_Split_Should_Divide_Width()
    {
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            First = new Label("Left"),
            Second = new Label("Right"),
        };

        splitter.Arrange(new Rect(0, 0, 80, 24));

        splitter.First!.Bounds.Width.Should().Be(40);
        splitter.Second!.Bounds.X.Should().Be(41); // 40 + 1 for splitter
    }

    [Fact]
    public void Horizontal_Split_Should_Divide_Height()
    {
        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Horizontal,
            First = new Label("Top"),
            Second = new Label("Bottom"),
        };

        splitter.Arrange(new Rect(0, 0, 80, 24));

        splitter.First!.Bounds.Height.Should().Be(12);
        splitter.Second!.Bounds.Y.Should().Be(13);
    }

    [Fact]
    public void SplitRatio_Should_Clamp()
    {
        var splitter = new Splitter();
        splitter.SplitRatio = 0.0;
        splitter.SplitRatio.Should().Be(0.1);
        splitter.SplitRatio = 1.0;
        splitter.SplitRatio.Should().Be(0.9);
    }

    [Fact]
    public void GetChildren_Should_Return_Both_Panels()
    {
        var splitter = new Splitter
        {
            First = new Label("A"),
            Second = new Label("B"),
        };

        splitter.GetChildren().Should().HaveCount(2);
    }

    [Fact]
    public void GetChildren_Without_Panels_Should_Be_Empty()
    {
        var splitter = new Splitter();
        splitter.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void Setting_First_Should_Set_Parent()
    {
        var splitter = new Splitter();
        var label = new Label("Test");
        splitter.First = label;
        label.Parent.Should().Be(splitter);
    }
}
