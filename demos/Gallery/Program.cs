using Gallery.Demos;
using Gallery.Demos.BugFixes;
using Gallery.Demos.Charts;
using Gallery.Demos.Exceptions;
using Gallery.Demos.LiveDisplay;
using Gallery.Demos.Markup;
using Gallery.Demos.Progress;
using Gallery.Demos.Prompts;
using Gallery.Demos.Rules;
using Gallery.Demos.Sixel;
using Gallery.Demos.Status;
using Gallery.Demos.Tables;
using Gallery.Demos.Themes;
using Gallery.Demos.Unicode;
using Spectre.Console;

// Register all demo modules
var modules = new IDemoModule[]
{
    new TablesDemo(),
    new MarkupDemo(),
    new PromptsDemo(),
    new LiveDisplayDemo(),
    new ChartsDemo(),
    new ProgressDemo(),
    new StatusDemo(),
    new RulesDemo(),
    new ExceptionsDemo(),
    new SixelDemo(),
    new ThemesDemo(),
    new UnicodeDemo(),
    new BugFixesDemo(),
};

// Header
AnsiConsole.Write(new FigletText("Gallery")
    .Color(Color.Cyan1)
    .Centered());
AnsiConsole.Write(new Rule("[grey]Spectre.Console Feature Gallery[/]").RuleStyle(Style.Parse("cyan")));
AnsiConsole.WriteLine();

while (true)
{
    var choices = modules.Select(m => $"{m.Name} — {m.Description}").ToList();
    choices.Add("Exit");

    var selection = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold green]Select a demo to run:[/]")
            .PageSize(15)
            .HighlightStyle(Style.Parse("cyan bold"))
            .AddChoices(choices));

    if (selection.Contains("Exit"))
    {
        break;
    }

    // Find the selected module by matching the name prefix
    var selectedName = selection.Split(" — ")[0];
    var module = modules.First(m => m.Name == selectedName);

    AnsiConsole.Clear();
    AnsiConsole.Write(new Rule($"[bold cyan]{module.Name}[/]").RuleStyle(Style.Parse("grey")));
    AnsiConsole.MarkupLine($"[grey]{module.Description}[/]");
    AnsiConsole.WriteLine();

    try
    {
        module.Run();
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    }

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[grey]Demo complete[/]").RuleStyle(Style.Parse("grey")));
    AnsiConsole.WriteLine();

    if (!AnsiConsole.Confirm("[grey]Return to menu?[/]"))
    {
        break;
    }

    AnsiConsole.Clear();
}

AnsiConsole.MarkupLine("[bold cyan]Thanks for exploring the Gallery![/]");
