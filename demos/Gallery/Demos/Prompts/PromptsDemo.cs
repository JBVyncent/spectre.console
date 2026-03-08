using Spectre.Console;

namespace Gallery.Demos.Prompts;

public sealed class PromptsDemo : IDemoModule
{
    public string Name => "Prompts";
    public string Description => "Text prompts, selections, confirmations, and nullable type handling";

    public void Run()
    {
        AnsiConsole.MarkupLine("[bold underline blue]Text Prompts[/]");
        AnsiConsole.WriteLine();

        // Basic text prompt
        var name = AnsiConsole.Ask<string>("[green]What is your name?[/]");
        AnsiConsole.MarkupInterpolated($"Hello, [bold]{name}[/]!");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Prompt with default
        var color = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Favorite color?[/]")
                .DefaultValue("blue")
                .AddChoice("red")
                .AddChoice("green")
                .AddChoice("blue"));
        AnsiConsole.MarkupInterpolated($"You chose [bold]{color}[/].");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Prompt with validation
        var age = AnsiConsole.Prompt(
            new TextPrompt<int>("[green]Your age?[/]")
                .ValidationErrorMessage("[red]Please enter a valid number[/]")
                .Validate(a => a switch
                {
                    < 0 => ValidationResult.Error("[red]Age cannot be negative[/]"),
                    > 150 => ValidationResult.Error("[red]That seems unlikely[/]"),
                    _ => ValidationResult.Success(),
                }));
        AnsiConsole.MarkupInterpolated($"You are [bold]{age}[/] years old.");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Optional prompt — AllowEmpty with nullable type (Bug #3 fix)
        // TextPrompt<string?> with AllowEmpty correctly returns null
        // on empty input instead of rejecting the conversion result.
        AnsiConsole.MarkupLine("[bold underline blue]Optional Prompts[/]");
        AnsiConsole.MarkupLine("[grey]Press Enter without typing to skip (AllowEmpty).[/]");
        AnsiConsole.WriteLine();

        var nickname = AnsiConsole.Prompt(
            new TextPrompt<string?>("[green]Nickname?[/] [grey](optional)[/]")
                .AllowEmpty());
        AnsiConsole.MarkupInterpolated(
            $"Nickname: [bold]{(string.IsNullOrEmpty(nickname) ? "(none)" : nickname)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Confirmation prompt
        AnsiConsole.MarkupLine("[bold underline blue]Confirmation[/]");
        AnsiConsole.WriteLine();

        var proceed = AnsiConsole.Confirm("Continue to selection prompts?");
        if (!proceed)
        {
            AnsiConsole.MarkupLine("[grey]Skipped selection prompts.[/]");
            return;
        }

        AnsiConsole.WriteLine();

        // Selection prompt
        AnsiConsole.MarkupLine("[bold underline blue]Selection Prompt[/]");
        AnsiConsole.WriteLine();

        var framework = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Favorite .NET framework?[/]")
                .PageSize(5)
                .AddChoices("ASP.NET Core", "Blazor", "MAUI", "WPF", "WinForms", "Console"));
        AnsiConsole.MarkupInterpolated($"You selected [bold]{framework}[/].");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Multi-selection prompt
        AnsiConsole.MarkupLine("[bold underline blue]Multi-Selection Prompt[/]");
        AnsiConsole.WriteLine();

        var tools = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[green]Select your tools:[/]")
                .PageSize(6)
                .InstructionsText("[grey](Space to toggle, Enter to confirm)[/]")
                .AddChoiceGroup("IDEs", "Visual Studio", "VS Code", "Rider")
                .AddChoiceGroup("CLI", "dotnet CLI", "Claude Code", "GitHub CLI"));

        AnsiConsole.MarkupLine("[bold]Selected tools:[/]");
        foreach (var tool in tools)
        {
            AnsiConsole.MarkupInterpolated($"  [cyan]•[/] {tool}");
            AnsiConsole.WriteLine();
        }
    }
}
