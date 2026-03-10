namespace Spectre.Console.Tests.Unit.Prompts;

public sealed class WizardPromptTests
{
    [Fact]
    public void Show_Throws_When_No_Steps()
    {
        var console = new TestConsole();
        var wizard = new WizardPrompt();

        var act = () => wizard.Show(console);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no steps*");
    }

    [Fact]
    public void Show_Throws_On_Null_Console()
    {
        var wizard = new WizardPrompt()
            .AddTextStep("name", "Name", "Enter:");

        var act = () => wizard.Show(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Properties_Have_Defaults()
    {
        var wizard = new WizardPrompt();

        wizard.Title.Should().BeNull();
        wizard.ShowSummary.Should().BeTrue();
        wizard.SummaryTitle.Should().Be("Review Your Answers");
        wizard.HeaderStyle.Should().BeNull();
        wizard.ShowStepIndicator.Should().BeTrue();
    }

    [Fact]
    public void AddStep_Returns_Same_Instance_And_Adds_Step()
    {
        var wizard = new WizardPrompt();
        var step = new WizardStep<string>("k", "t", new TextPrompt<string>("x"));

        var result = wizard.AddStep(step);

        result.Should().BeSameAs(wizard);
        wizard.Steps.Should().HaveCount(1);
        wizard.Steps[0].Should().BeSameAs(step);
    }

    [Fact]
    public void AddStep_Throws_On_Null()
    {
        var wizard = new WizardPrompt();

        var act = () => wizard.AddStep(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
