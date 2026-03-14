using FluentAssertions;
using Spectre.Console.Tui;
using Xunit;

namespace Spectre.Console.Tui.Tests;

public class ConstraintTests
{
    [Fact]
    public void Fixed_Should_Resolve_To_Value()
    {
        var c = Constraint.Fixed(10);
        c.Kind.Should().Be(ConstraintKind.Fixed);
        c.Value.Should().Be(10);
        c.Resolve(100).Should().Be(10);
    }

    [Fact]
    public void Fixed_Should_Not_Exceed_Available()
    {
        var c = Constraint.Fixed(50);
        c.Resolve(30).Should().Be(30);
    }

    [Fact]
    public void Min_Should_Return_At_Least_Value()
    {
        var c = Constraint.Min(10);
        c.Kind.Should().Be(ConstraintKind.Min);
        c.Resolve(100).Should().Be(10);
    }

    [Fact]
    public void Max_Should_Cap_At_Value()
    {
        var c = Constraint.Max(50);
        c.Kind.Should().Be(ConstraintKind.Max);
        c.Resolve(100).Should().Be(50);
        c.Resolve(30).Should().Be(30);
    }

    [Fact]
    public void Percentage_Should_Compute_Correctly()
    {
        var c = Constraint.Percentage(50);
        c.Kind.Should().Be(ConstraintKind.Percentage);
        c.Resolve(100).Should().Be(50);
        c.Resolve(80).Should().Be(40);
    }

    [Fact]
    public void Fill_Should_Take_All_Available()
    {
        var c = Constraint.Fill();
        c.Kind.Should().Be(ConstraintKind.Fill);
        c.Resolve(100).Should().Be(100);
    }

    [Fact]
    public void Negative_Values_Should_Be_Clamped()
    {
        Constraint.Fixed(-5).Value.Should().Be(0);
        Constraint.Min(-5).Value.Should().Be(0);
    }

    [Fact]
    public void Percentage_Should_Clamp_To_0_100()
    {
        Constraint.Percentage(-10).Value.Should().Be(0);
        Constraint.Percentage(150).Value.Should().Be(100);
    }

    [Fact]
    public void Fill_Weight_Should_Be_At_Least_1()
    {
        Constraint.Fill(0).Value.Should().Be(1);
        Constraint.Fill(-1).Value.Should().Be(1);
    }

    [Fact]
    public void Equality_Should_Work()
    {
        var a = Constraint.Fixed(10);
        var b = Constraint.Fixed(10);
        var c = Constraint.Min(10);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
