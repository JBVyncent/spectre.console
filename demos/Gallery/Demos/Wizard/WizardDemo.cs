using Spectre.Console;

namespace Gallery.Demos.Wizard;

public sealed class WizardDemo : IDemoModule
{
    public string Name => "Wizard";
    public string Description => "Multi-page form with back-navigation, conditional steps, and summary";

    public void Run()
    {
        AnsiConsole.MarkupLine("[bold underline blue]Create New Project Wizard[/]");
        AnsiConsole.MarkupLine("[grey]This demo walks through a multi-step wizard with back-navigation.[/]");
        AnsiConsole.WriteLine();

        var wizard = new WizardPrompt()
            .Title("Create New Project")
            .AddTextStep("name", "Project Name", "What is the project name?")
            .AddSelectionStep("language", "Language", "Select a language:",
                "C#", "F#", "Rust", "Go", "Python")
            .AddSelectionStep("framework", "Framework", "Select a framework:",
                ".NET 10", ".NET 8", "ASP.NET Core", "MAUI", "Console")
            .AddConfirmStep("tests", "Include Tests", "Include a test project?")
            .AddStep<string>("testFramework", "Test Framework",
                new SelectionPrompt<string>()
                    .Title("Select a test framework:")
                    .AddChoices("xUnit", "NUnit", "MSTest"),
                condition: result => result.TryGet<bool>("tests", out var v) && v)
            .AddConfirmStep("docker", "Docker Support", "Add Docker support?")
            .HeaderStyle(Style.Parse("cyan"));

        var result = AnsiConsole.Prompt(wizard);

        if (result.IsCancelled)
        {
            AnsiConsole.MarkupLine("[red]Wizard cancelled.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Project Created Successfully![/]").RuleStyle(Style.Parse("green")));
        AnsiConsole.MarkupLine($"[bold]Project:[/] {result.Get<string>("name")}");
        AnsiConsole.MarkupLine($"[bold]Language:[/] {result.Get<string>("language")}");
        AnsiConsole.MarkupLine($"[bold]Framework:[/] {result.Get<string>("framework")}");
        AnsiConsole.MarkupLine($"[bold]Tests:[/] {(result.Get<bool>("tests") ? "Yes" : "No")}");

        if (result.TryGet<string>("testFramework", out var tf))
        {
            AnsiConsole.MarkupLine($"[bold]Test Framework:[/] {tf}");
        }

        AnsiConsole.MarkupLine($"[bold]Docker:[/] {(result.Get<bool>("docker") ? "Yes" : "No")}");
    }
}
