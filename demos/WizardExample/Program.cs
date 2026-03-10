using Spectre.Console;

AnsiConsole.Write(new FigletText("Wizard")
    .Color(Color.Cyan1)
    .Centered());
AnsiConsole.Write(new Rule("[grey]Create New Project[/]").RuleStyle(Style.Parse("cyan")));
AnsiConsole.WriteLine();

var wizard = new WizardPrompt()
    .Title("Create New Project")
    .AddTextStep("name", "Project Name", "What is the project name?")
    .AddSelectionStep("language", "Language", "Select a programming language:",
        "C#", "F#", "Rust", "Go", "Python", "TypeScript")
    .AddSelectionStep("framework", "Framework", "Select a framework:",
        ".NET 10", ".NET 8", "ASP.NET Core", "Blazor", "MAUI", "Console")
    .AddConfirmStep("tests", "Include Tests", "Include a test project?")
    .AddStep<string>("testFramework", "Test Framework",
        new SelectionPrompt<string>()
            .Title("Select a test framework:")
            .AddChoices("xUnit", "NUnit", "MSTest"),
        condition: result => result.TryGet<bool>("tests", out var v) && v)
    .AddConfirmStep("docker", "Docker Support", "Add Docker support?")
    .AddConfirmStep("ci", "CI/CD", "Set up GitHub Actions CI/CD?")
    .HeaderStyle(Style.Parse("cyan"));

var result = AnsiConsole.Prompt(wizard);

if (result.IsCancelled)
{
    AnsiConsole.MarkupLine("[red bold]Project creation cancelled.[/]");
    return;
}

AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[bold green]Project Created![/]").RuleStyle(Style.Parse("green")));
AnsiConsole.WriteLine();

var summary = new Table()
    .Border(TableBorder.Rounded)
    .Title("[bold]Project Configuration[/]")
    .AddColumn(new TableColumn("[bold]Setting[/]"))
    .AddColumn(new TableColumn("[bold]Value[/]"));

summary.AddRow("Project Name", result.Get<string>("name"));
summary.AddRow("Language", result.Get<string>("language"));
summary.AddRow("Framework", result.Get<string>("framework"));
summary.AddRow("Tests", result.Get<bool>("tests") ? "[green]Yes[/]" : "[red]No[/]");

if (result.TryGet<string>("testFramework", out var tf))
{
    summary.AddRow("Test Framework", tf);
}

summary.AddRow("Docker", result.Get<bool>("docker") ? "[green]Yes[/]" : "[red]No[/]");
summary.AddRow("CI/CD", result.Get<bool>("ci") ? "[green]Yes[/]" : "[red]No[/]");

AnsiConsole.Write(summary);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold cyan]Happy coding![/]");
