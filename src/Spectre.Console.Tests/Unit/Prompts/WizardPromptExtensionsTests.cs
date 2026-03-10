namespace Spectre.Console.Tests.Unit.Prompts;

public sealed class WizardPromptExtensionsTests
{
    [Fact]
    public void AddStep_Generic_Returns_Same_Instance()
    {
        var wizard = new WizardPrompt();
        var result = wizard.AddStep<string>("k", "t", new TextPrompt<string>("x"));

        result.Should().BeSameAs(wizard);
    }

    [Fact]
    public void AddStep_Generic_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;

        var act = () => wizard!.AddStep<string>("k", "t", new TextPrompt<string>("x"));

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("wizard");
    }

    [Fact]
    public void AddStep_Generic_Throws_On_Null_Key()
    {
        var wizard = new WizardPrompt();

        var act = () => wizard.AddStep<string>(null!, "t", new TextPrompt<string>("x"));

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("key");
    }

    [Fact]
    public void AddStep_Generic_Throws_On_Null_Title()
    {
        var wizard = new WizardPrompt();

        var act = () => wizard.AddStep<string>("k", null!, new TextPrompt<string>("x"));

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("title");
    }

    [Fact]
    public void AddStep_Generic_Throws_On_Null_Prompt()
    {
        var wizard = new WizardPrompt();

        var act = () => wizard.AddStep<string>("k", "t", null!);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("prompt");
    }

    [Fact]
    public void AddTextStep_Returns_Same_Instance()
    {
        var wizard = new WizardPrompt();
        var result = wizard.AddTextStep("k", "t", "Enter:");

        result.Should().BeSameAs(wizard);
    }

    [Fact]
    public void AddTextStep_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;

        var act = () => wizard!.AddTextStep("k", "t", "Enter:");

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("wizard");
    }

    [Fact]
    public void AddSelectionStep_Returns_Same_Instance()
    {
        var wizard = new WizardPrompt();
        var result = wizard.AddSelectionStep("k", "t", "Pick:", "A", "B");

        result.Should().BeSameAs(wizard);
    }

    [Fact]
    public void AddSelectionStep_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;

        var act = () => wizard!.AddSelectionStep("k", "t", "Pick:", "A", "B");

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("wizard");
    }

    [Fact]
    public void AddConfirmStep_Returns_Same_Instance()
    {
        var wizard = new WizardPrompt();
        var result = wizard.AddConfirmStep("k", "t", "Confirm?");

        result.Should().BeSameAs(wizard);
    }

    [Fact]
    public void AddConfirmStep_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;

        var act = () => wizard!.AddConfirmStep("k", "t", "Confirm?");

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("wizard");
    }

    [Fact]
    public void Title_Sets_Property()
    {
        var wizard = new WizardPrompt().Title("My Wizard");

        wizard.Title.Should().Be("My Wizard");
    }

    [Fact]
    public void Title_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;

        var act = () => wizard!.Title("x");

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("wizard");
    }

    [Fact]
    public void WithSummary_Enables_Summary()
    {
        var wizard = new WizardPrompt();
        wizard.ShowSummary = false;
        wizard.WithSummary();

        wizard.ShowSummary.Should().BeTrue();
    }

    [Fact]
    public void HideSummary_Disables_Summary()
    {
        var wizard = new WizardPrompt().HideSummary();

        wizard.ShowSummary.Should().BeFalse();
    }

    [Fact]
    public void SummaryTitle_Sets_Property()
    {
        var wizard = new WizardPrompt().SummaryTitle("Review");

        wizard.SummaryTitle.Should().Be("Review");
    }

    [Fact]
    public void HeaderStyle_Sets_Property()
    {
        var style = new Style(Color.Red);
        var wizard = new WizardPrompt().HeaderStyle(style);

        wizard.HeaderStyle.Should().Be(style);
    }

    [Fact]
    public void StepIndicator_Sets_Property()
    {
        var wizard = new WizardPrompt().StepIndicator(false);

        wizard.ShowStepIndicator.Should().BeFalse();
    }

    [Fact]
    public void WithSummary_Throws_On_Null()
    {
        WizardPrompt? wizard = null;
        var act = () => wizard!.WithSummary();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HideSummary_Throws_On_Null()
    {
        WizardPrompt? wizard = null;
        var act = () => wizard!.HideSummary();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SummaryTitle_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;
        var act = () => wizard!.SummaryTitle("x");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HeaderStyle_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;
        var act = () => wizard!.HeaderStyle(Style.Plain);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StepIndicator_Throws_On_Null_Wizard()
    {
        WizardPrompt? wizard = null;
        var act = () => wizard!.StepIndicator(true);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddTextStep_Throws_On_Null_Key()
    {
        var act = () => new WizardPrompt().AddTextStep(null!, "t", "p");
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("key");
    }

    [Fact]
    public void AddTextStep_Throws_On_Null_Title()
    {
        var act = () => new WizardPrompt().AddTextStep("k", null!, "p");
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("title");
    }

    [Fact]
    public void AddTextStep_Throws_On_Null_PromptText()
    {
        var act = () => new WizardPrompt().AddTextStep("k", "t", null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("promptText");
    }

    [Fact]
    public void AddSelectionStep_Throws_On_Null_Key()
    {
        var act = () => new WizardPrompt().AddSelectionStep(null!, "t", "p", "A");
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("key");
    }

    [Fact]
    public void AddSelectionStep_Throws_On_Null_Title()
    {
        var act = () => new WizardPrompt().AddSelectionStep("k", null!, "p", "A");
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("title");
    }

    [Fact]
    public void AddSelectionStep_Throws_On_Null_PromptTitle()
    {
        var act = () => new WizardPrompt().AddSelectionStep("k", "t", null!, "A");
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("promptTitle");
    }

    [Fact]
    public void AddConfirmStep_Throws_On_Null_Key()
    {
        var act = () => new WizardPrompt().AddConfirmStep(null!, "t", "p");
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("key");
    }

    [Fact]
    public void AddConfirmStep_Throws_On_Null_Title()
    {
        var act = () => new WizardPrompt().AddConfirmStep("k", null!, "p");
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("title");
    }

    [Fact]
    public void AddConfirmStep_Throws_On_Null_PromptText()
    {
        var act = () => new WizardPrompt().AddConfirmStep("k", "t", null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("promptText");
    }

    [Fact]
    public void SummaryTitle_Throws_On_Null_Title()
    {
        var act = () => new WizardPrompt().SummaryTitle(null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("title");
    }

    [Fact]
    public void Title_Throws_On_Null_Title()
    {
        var act = () => new WizardPrompt().Title(null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("title");
    }
}
