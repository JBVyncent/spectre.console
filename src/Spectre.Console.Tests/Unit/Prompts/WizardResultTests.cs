namespace Spectre.Console.Tests.Unit.Prompts;

public sealed class WizardResultTests
{
    [Fact]
    public void Get_Returns_Value_When_Set()
    {
        var result = new WizardResult();
        result.Set("name", "Alice");

        result.Get<string>("name").Should().Be("Alice");
    }

    [Fact]
    public void Get_Throws_KeyNotFoundException_When_Missing()
    {
        var result = new WizardResult();

        var act = () => result.Get<string>("missing");

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*'missing'*");
    }

    [Fact]
    public void Get_Throws_InvalidCastException_When_Wrong_Type()
    {
        var result = new WizardResult();
        result.Set("age", 42);

        var act = () => result.Get<string>("age");

        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void TryGet_Returns_True_And_Value_When_Found()
    {
        var result = new WizardResult();
        result.Set("name", "Bob");

        result.TryGet<string>("name", out var value).Should().BeTrue();
        value.Should().Be("Bob");
    }

    [Fact]
    public void TryGet_Returns_False_When_Missing()
    {
        var result = new WizardResult();

        result.TryGet<string>("missing", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGet_Returns_False_When_Wrong_Type()
    {
        var result = new WizardResult();
        result.Set("age", 42);

        result.TryGet<string>("age", out _).Should().BeFalse();
    }

    [Fact]
    public void Contains_Returns_True_When_Set()
    {
        var result = new WizardResult();
        result.Set("key", "val");

        result.Contains("key").Should().BeTrue();
    }

    [Fact]
    public void Contains_Returns_False_When_Not_Set()
    {
        var result = new WizardResult();

        result.Contains("key").Should().BeFalse();
    }

    [Fact]
    public void Remove_Removes_Entry()
    {
        var result = new WizardResult();
        result.Set("key", "val");
        result.Remove("key");

        result.Contains("key").Should().BeFalse();
    }

    [Fact]
    public void Keys_Returns_All_Keys()
    {
        var result = new WizardResult();
        result.Set("a", 1);
        result.Set("b", 2);

        result.Keys.Should().BeEquivalentTo("a", "b");
    }

    [Fact]
    public void IsCancelled_Defaults_False()
    {
        var result = new WizardResult();

        result.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void Set_Overwrites_Existing_Value()
    {
        var result = new WizardResult();
        result.Set("key", "old");
        result.Set("key", "new");

        result.Get<string>("key").Should().Be("new");
    }

    [Fact]
    public void Get_Throws_ArgumentNullException_On_Null_Key()
    {
        var result = new WizardResult();

        var act = () => result.Get<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGet_Throws_ArgumentNullException_On_Null_Key()
    {
        var result = new WizardResult();

        var act = () => result.TryGet<string>(null!, out _);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Contains_Throws_ArgumentNullException_On_Null_Key()
    {
        var result = new WizardResult();

        var act = () => result.Contains(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
