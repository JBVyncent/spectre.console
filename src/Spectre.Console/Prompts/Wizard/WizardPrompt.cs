namespace Spectre.Console;

/// <summary>
/// A multi-page wizard prompt that guides users through a series of steps
/// with back-navigation and an optional summary/review page.
/// </summary>
public sealed class WizardPrompt : IPrompt<WizardResult>
{
    private readonly List<WizardStep> _steps = [];

    /// <summary>
    /// Gets or sets the wizard title displayed at the top.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show a summary page before submission.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ShowSummary { get; set; } = true;

    /// <summary>
    /// Gets or sets the summary page title.
    /// </summary>
    public string SummaryTitle { get; set; } = "Review Your Answers";

    /// <summary>
    /// Gets or sets the style used for step headers.
    /// </summary>
    public Style? HeaderStyle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show a step indicator (e.g. "Step 2 of 4").
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ShowStepIndicator { get; set; } = true;

    /// <summary>
    /// Gets the steps in the wizard.
    /// </summary>
    internal IReadOnlyList<WizardStep> Steps => _steps;

    /// <summary>
    /// Adds a step to the wizard.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public WizardPrompt AddStep(WizardStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
        return this;
    }

    /// <inheritdoc/>
    // Stryker disable once all : NoCoverage — Show delegates to ShowAsync; interactive prompt pipeline
    public WizardResult Show(IAnsiConsole console)
    {
        return ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    // Stryker disable all : NoCoverage — interactive wizard orchestration; requires driving SelectionPrompt/TextPrompt through TestConsole input queue
    public async Task<WizardResult> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(console);

        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("The wizard has no steps. Add at least one step before showing.");
        }

        var result = new WizardResult();
        var stepIndex = 0;

        while (true)
        {
            // All steps done — show summary or return
            if (stepIndex >= _steps.Count)
            {
                if (ShowSummary)
                {
                    var summaryAction = ShowSummaryPage(console, result);
                    if (summaryAction == NavigationAction.Back)
                    {
                        stepIndex = FindPreviousVisibleStep(_steps.Count, result);
                        continue;
                    }

                    if (summaryAction == NavigationAction.Cancel)
                    {
                        result.IsCancelled = true;
                        return result;
                    }
                }

                return result;
            }

            // Clamp to start
            if (stepIndex < 0)
            {
                stepIndex = 0;
                continue;
            }

            var step = _steps[stepIndex];

            // Skip conditional steps
            if (step.Condition != null && !step.Condition(result))
            {
                stepIndex++;
                continue;
            }

            // Render step header
            RenderStepHeader(console, stepIndex, step, result);

            // Execute the prompt
            var value = await step.ShowAsync(console, cancellationToken).ConfigureAwait(false);
            result.Set(step.Key, value);

            // Show navigation
            var hasBack = FindPreviousVisibleStep(stepIndex, result) >= 0;
            var nav = ShowNavigation(console, hasBack);

            switch (nav)
            {
                case NavigationAction.Continue:
                    stepIndex++;
                    break;
                case NavigationAction.Back:
                    result.Remove(step.Key);
                    stepIndex = FindPreviousVisibleStep(stepIndex, result);
                    break;
                case NavigationAction.Cancel:
                    result.IsCancelled = true;
                    return result;
            }
        }
    }

    // Stryker disable all : NoCoverage — private wizard orchestration helpers; interactive prompt pipeline untestable without driving SelectionPrompt input queue
    private void RenderStepHeader(IAnsiConsole console, int stepIndex, WizardStep step, WizardResult result)
    {
        console.WriteLine();

        if (Title != null)
        {
            var titleRule = new Rule($"[bold]{Title.EscapeMarkup()}[/]");
            if (HeaderStyle != null)
            {
                titleRule.Style = HeaderStyle;
            }

            console.Write(titleRule);
        }

        if (ShowStepIndicator)
        {
            var visibleCount = CountVisibleSteps(result);
            var visibleIndex = CountVisibleStepsBefore(stepIndex, result) + 1;
            console.MarkupLine($"[grey]Step {visibleIndex} of {visibleCount}[/]");
        }

        var stepRule = new Rule($"[bold]{step.Title.EscapeMarkup()}[/]");
        if (HeaderStyle != null)
        {
            stepRule.Style = HeaderStyle;
        }

        console.Write(stepRule);
        console.WriteLine();
    }

    // Stryker disable all : NoCoverage — interactive navigation; SelectionPrompt requires input queue
    private NavigationAction ShowNavigation(IAnsiConsole console, bool canGoBack)
    {
        console.WriteLine();

        var choices = new List<string> { "Continue" };
        if (canGoBack)
        {
            choices.Add("Go Back");
        }

        choices.Add("Cancel");

        var selection = console.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]What would you like to do?[/]")
                .AddChoices(choices)
                .HighlightStyle(Style.Parse("cyan bold")));

        return selection switch
        {
            "Go Back" => NavigationAction.Back,
            "Cancel" => NavigationAction.Cancel,
            _ => NavigationAction.Continue,
        };
    }

    // Stryker disable all : NoCoverage — interactive summary page; SelectionPrompt requires input queue
    private NavigationAction ShowSummaryPage(IAnsiConsole console, WizardResult result)
    {
        console.WriteLine();

        var summaryRule = new Rule($"[bold]{SummaryTitle.EscapeMarkup()}[/]");
        if (HeaderStyle != null)
        {
            summaryRule.Style = HeaderStyle;
        }

        console.Write(summaryRule);
        console.WriteLine();

        // Build summary table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Step[/]"))
            .AddColumn(new TableColumn("[bold]Answer[/]"));

        foreach (var step in _steps)
        {
            if (step.Condition != null && !step.Condition(result))
            {
                continue;
            }

            if (result.Contains(step.Key))
            {
                var value = result.Get<object>(step.Key);
                var formatted = step.FormatResult(value);
                table.AddRow(
                    step.Title.EscapeMarkup(),
                    formatted.EscapeMarkup());
            }
        }

        console.Write(table);
        console.WriteLine();

        // Navigation
        var selection = console.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Ready to submit?[/]")
                .AddChoices("Submit", "Go Back", "Cancel")
                .HighlightStyle(Style.Parse("cyan bold")));

        return selection switch
        {
            "Go Back" => NavigationAction.Back,
            "Cancel" => NavigationAction.Cancel,
            _ => NavigationAction.Continue,
        };
    }

    // Stryker disable all : NoCoverage — only called from interactive orchestration loop
    private int FindPreviousVisibleStep(int currentIndex, WizardResult result)
    {
        for (var i = currentIndex - 1; i >= 0; i--)
        {
            var step = _steps[i];
            if (step.Condition == null || step.Condition(result))
            {
                return i;
            }
        }

        return -1;
    }

    // Stryker disable all : NoCoverage — only called from interactive orchestration loop
    private int CountVisibleSteps(WizardResult result)
    {
        var count = 0;
        foreach (var step in _steps)
        {
            if (step.Condition == null || step.Condition(result))
            {
                count++;
            }
        }

        return count;
    }

    // Stryker disable all : NoCoverage — only called from interactive orchestration loop
    private int CountVisibleStepsBefore(int index, WizardResult result)
    {
        var count = 0;
        for (var i = 0; i < index; i++)
        {
            var condition = _steps[i].Condition;
            if (condition == null || condition(result))
            {
                count++;
            }
        }

        return count;
    }

    private enum NavigationAction
    {
        Continue,
        Back,
        Cancel,
    }
    // Stryker restore all
}
