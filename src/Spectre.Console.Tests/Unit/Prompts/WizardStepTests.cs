namespace Spectre.Console.Tests.Unit.Prompts;

public sealed class WizardStepTests
{
    [Fact]
    public void Constructor_Sets_Key_And_Title()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var step = new WizardStep<string>("name", "Your Name", prompt);

        step.Key.Should().Be("name");
        step.Title.Should().Be("Your Name");
        step.Condition.Should().BeNull();
    }

    [Fact]
    public void Constructor_Throws_On_Null_Key()
    {
        var act = () => new WizardStep<string>(null!, "title", new TextPrompt<string>("x"));

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("key");
    }

    [Fact]
    public void Constructor_Throws_On_Null_Title()
    {
        var act = () => new WizardStep<string>("key", null!, new TextPrompt<string>("x"));

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("title");
    }

    [Fact]
    public void Constructor_Throws_On_Null_Prompt()
    {
        var act = () => new WizardStep<string>("key", "title", null!);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("prompt");
    }

    [Fact]
    public void Show_Delegates_To_Inner_Prompt()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Hello");
        var prompt = new TextPrompt<string>("Enter:");
        var step = new WizardStep<string>("key", "title", prompt);

        var result = step.Show(console);

        result.Should().Be("Hello");
    }

    [Fact]
    public async Task ShowAsync_Delegates_To_Inner_Prompt()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("World");
        var prompt = new TextPrompt<string>("Enter:");
        var step = new WizardStep<string>("key", "title", prompt);

        var result = await step.ShowAsync(console, System.Threading.CancellationToken.None);

        result.Should().Be("World");
    }

    [Fact]
    public void FormatResult_Uses_Custom_Formatter()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var step = new WizardStep<string>("key", "title", prompt,
            formatter: v => $"<<{v}>>");

        step.FormatResult("test").Should().Be("<<test>>");
    }

    [Fact]
    public void FormatResult_Uses_ToString_When_No_Formatter()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var step = new WizardStep<string>("key", "title", prompt);

        step.FormatResult("test").Should().Be("test");
    }

    [Fact]
    public void FormatResult_Uses_ToString_When_Wrong_Type()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var step = new WizardStep<string>("key", "title", prompt,
            formatter: v => $"<<{v}>>");

        // Pass an int when formatter expects string — falls through to ToString
        step.FormatResult(42).Should().Be("42");
    }

    [Fact]
    public void Condition_Can_Be_Set()
    {
        var prompt = new TextPrompt<string>("Enter:");
        Func<WizardResult, bool> condition = r => r.Contains("prev");
        var step = new WizardStep<string>("key", "title", prompt, condition: condition);

        step.Condition.Should().BeSameAs(condition);
    }
}
