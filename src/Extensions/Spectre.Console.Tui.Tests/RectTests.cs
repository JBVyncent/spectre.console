using FluentAssertions;
using Spectre.Console.Tui;
using Xunit;

namespace Spectre.Console.Tui.Tests;

public class RectTests
{
    [Fact]
    public void Constructor_Should_Store_Values()
    {
        var rect = new Rect(1, 2, 10, 20);
        rect.X.Should().Be(1);
        rect.Y.Should().Be(2);
        rect.Width.Should().Be(10);
        rect.Height.Should().Be(20);
    }

    [Fact]
    public void Constructor_Should_Clamp_Negative_Dimensions()
    {
        var rect = new Rect(0, 0, -5, -3);
        rect.Width.Should().Be(0);
        rect.Height.Should().Be(0);
    }

    [Fact]
    public void Right_And_Bottom_Should_Compute_Correctly()
    {
        var rect = new Rect(5, 10, 20, 15);
        rect.Right.Should().Be(25);
        rect.Bottom.Should().Be(25);
    }

    [Fact]
    public void Contains_Should_Return_True_For_Interior_Point()
    {
        var rect = new Rect(5, 5, 10, 10);
        rect.Contains(10, 10).Should().BeTrue();
    }

    [Fact]
    public void Contains_Should_Return_True_For_TopLeft_Corner()
    {
        var rect = new Rect(5, 5, 10, 10);
        rect.Contains(5, 5).Should().BeTrue();
    }

    [Fact]
    public void Contains_Should_Return_False_For_Right_Edge()
    {
        var rect = new Rect(5, 5, 10, 10);
        rect.Contains(15, 5).Should().BeFalse(); // Right edge is exclusive
    }

    [Fact]
    public void Contains_Should_Return_False_For_Outside_Point()
    {
        var rect = new Rect(5, 5, 10, 10);
        rect.Contains(0, 0).Should().BeFalse();
    }

    [Fact]
    public void Intersect_Should_Return_Overlap()
    {
        var a = new Rect(0, 0, 10, 10);
        var b = new Rect(5, 5, 10, 10);
        var result = a.Intersect(b);

        result.X.Should().Be(5);
        result.Y.Should().Be(5);
        result.Width.Should().Be(5);
        result.Height.Should().Be(5);
    }

    [Fact]
    public void Intersect_Should_Return_Empty_For_No_Overlap()
    {
        var a = new Rect(0, 0, 5, 5);
        var b = new Rect(10, 10, 5, 5);
        var result = a.Intersect(b);

        result.Width.Should().Be(0);
        result.Height.Should().Be(0);
    }

    [Fact]
    public void Equality_Should_Work()
    {
        var a = new Rect(1, 2, 3, 4);
        var b = new Rect(1, 2, 3, 4);
        var c = new Rect(0, 0, 3, 4);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_Be_Equal_For_Equal_Rects()
    {
        var a = new Rect(1, 2, 3, 4);
        var b = new Rect(1, 2, 3, 4);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_Should_Show_Dimensions()
    {
        var rect = new Rect(1, 2, 10, 20);
        rect.ToString().Should().Be("(1, 2, 10x20)");
    }

    [Fact]
    public void Empty_Should_Have_Zero_Dimensions()
    {
        var empty = Rect.Empty;
        empty.X.Should().Be(0);
        empty.Y.Should().Be(0);
        empty.Width.Should().Be(0);
        empty.Height.Should().Be(0);
    }
}
