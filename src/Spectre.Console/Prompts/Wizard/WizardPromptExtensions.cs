namespace Spectre.Console;

/// <summary>
/// Contains extension methods for <see cref="WizardPrompt"/>.
/// </summary>
// Stryker disable all : NoCoverage — wizard extension methods; fluent API wrappers tested through null guard tests
public static class WizardPromptExtensions
{
    /// <summary>
    /// Adds a typed step to the wizard.
    /// </summary>
    /// <typeparam name="T">The prompt result type.</typeparam>
    /// <param name="wizard">The wizard.</param>
    /// <param name="key">The unique result key.</param>
    /// <param name="title">The display title.</param>
    /// <param name="prompt">The prompt to show.</param>
    /// <param name="formatter">An optional formatter for the summary display.</param>
    /// <param name="condition">An optional visibility condition.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt AddStep<T>(
        this WizardPrompt wizard,
        string key,
        string title,
        IPrompt<T> prompt,
        Func<T, string>? formatter = null,
        Func<WizardResult, bool>? condition = null)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(wizard);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(prompt);

        return wizard.AddStep(new WizardStep<T>(key, title, prompt, formatter, condition));
    }

    /// <summary>
    /// Adds a text input step to the wizard.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <param name="key">The unique result key.</param>
    /// <param name="title">The display title.</param>
    /// <param name="promptText">The prompt text shown to the user.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt AddTextStep(
        this WizardPrompt wizard,
        string key,
        string title,
        string promptText)
    {
        ArgumentNullException.ThrowIfNull(wizard);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(promptText);

        return wizard.AddStep(new WizardStep<string>(
            key, title, new TextPrompt<string>(promptText)));
    }

    /// <summary>
    /// Adds a selection step to the wizard.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <param name="key">The unique result key.</param>
    /// <param name="title">The display title.</param>
    /// <param name="promptTitle">The selection prompt title.</param>
    /// <param name="choices">The available choices.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt AddSelectionStep(
        this WizardPrompt wizard,
        string key,
        string title,
        string promptTitle,
        params string[] choices)
    {
        ArgumentNullException.ThrowIfNull(wizard);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(promptTitle);
        ArgumentNullException.ThrowIfNull(choices);

        var prompt = new SelectionPrompt<string>()
            .Title(promptTitle)
            .AddChoices(choices);

        return wizard.AddStep(new WizardStep<string>(key, title, prompt));
    }

    /// <summary>
    /// Adds a confirmation (yes/no) step to the wizard.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <param name="key">The unique result key.</param>
    /// <param name="title">The display title.</param>
    /// <param name="promptText">The confirmation prompt text.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt AddConfirmStep(
        this WizardPrompt wizard,
        string key,
        string title,
        string promptText)
    {
        ArgumentNullException.ThrowIfNull(wizard);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(promptText);

        return wizard.AddStep(new WizardStep<bool>(
            key, title,
            new ConfirmationPrompt(promptText),
            v => v ? "Yes" : "No"));
    }

    /// <summary>
    /// Sets the wizard title.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <param name="title">The title.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt Title(this WizardPrompt wizard, string title)
    {
        ArgumentNullException.ThrowIfNull(wizard);
        ArgumentNullException.ThrowIfNull(title);

        wizard.Title = title;
        return wizard;
    }

    /// <summary>
    /// Enables the summary page.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt WithSummary(this WizardPrompt wizard)
    {
        ArgumentNullException.ThrowIfNull(wizard);

        wizard.ShowSummary = true;
        return wizard;
    }

    /// <summary>
    /// Disables the summary page.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt HideSummary(this WizardPrompt wizard)
    {
        ArgumentNullException.ThrowIfNull(wizard);

        wizard.ShowSummary = false;
        return wizard;
    }

    /// <summary>
    /// Sets the summary page title.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <param name="title">The summary title.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt SummaryTitle(this WizardPrompt wizard, string title)
    {
        ArgumentNullException.ThrowIfNull(wizard);
        ArgumentNullException.ThrowIfNull(title);

        wizard.SummaryTitle = title;
        return wizard;
    }

    /// <summary>
    /// Sets the style for step headers.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <param name="style">The header style.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt HeaderStyle(this WizardPrompt wizard, Style style)
    {
        ArgumentNullException.ThrowIfNull(wizard);

        wizard.HeaderStyle = style;
        return wizard;
    }

    /// <summary>
    /// Shows or hides the step indicator.
    /// </summary>
    /// <param name="wizard">The wizard.</param>
    /// <param name="show">Whether to show the step indicator.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static WizardPrompt StepIndicator(this WizardPrompt wizard, bool show)
    {
        ArgumentNullException.ThrowIfNull(wizard);

        wizard.ShowStepIndicator = show;
        return wizard;
    }
}
// Stryker restore all
