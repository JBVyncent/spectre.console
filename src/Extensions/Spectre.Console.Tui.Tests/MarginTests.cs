using FluentAssertions;
using Spectre.Console.Tui;
using Xunit;

namespace Spectre.Console.Tui.Tests;

public class MarginTests
{
    [Fact]
    public void Uniform_Constructor_Should_Set_All_Sides()
    {
        var m = new Margin(5);
        m.Left.Should().Be(5);
        m.Top.Should().Be(5);
        m.Right.Should().Be(5);
        m.Bottom.Should().Be(5);
    }

    [Fact]
    public void HV_Constructor_Should_Set_Sides()
    {
        var m = new Margin(3, 5);
        m.Left.Should().Be(3);
        m.Right.Should().Be(3);
        m.Top.Should().Be(5);
        m.Bottom.Should().Be(5);
    }

    [Fact]
    public void Full_Constructor_Should_Set_All()
    {
        var m = new Margin(1, 2, 3, 4);
        m.Left.Should().Be(1);
        m.Top.Should().Be(2);
        m.Right.Should().Be(3);
        m.Bottom.Should().Be(4);
    }

    [Fact]
    public void Negative_Values_Should_Be_Clamped()
    {
        var m = new Margin(-1, -2, -3, -4);
        m.Left.Should().Be(0);
        m.Top.Should().Be(0);
        m.Right.Should().Be(0);
        m.Bottom.Should().Be(0);
    }

    [Fact]
    public void Horizontal_And_Vertical_Should_Compute()
    {
        var m = new Margin(2, 3, 4, 5);
        m.Horizontal.Should().Be(6); // 2+4
        m.Vertical.Should().Be(8); // 3+5
    }

    [Fact]
    public void None_Should_Be_All_Zeros()
    {
        var m = Margin.None;
        m.Left.Should().Be(0);
        m.Top.Should().Be(0);
        m.Right.Should().Be(0);
        m.Bottom.Should().Be(0);
    }

    [Fact]
    public void Equality_Should_Work()
    {
        var a = new Margin(1, 2, 3, 4);
        var b = new Margin(1, 2, 3, 4);
        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
