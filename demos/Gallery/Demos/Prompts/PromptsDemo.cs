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

        // Selection prompt — basic
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

        // Selection prompt — with DefaultValue (issue #508)
        // The cursor is pre-positioned on the previously-used framework so the user
        // can press Enter to confirm or navigate to something different.
        AnsiConsole.MarkupLine("[bold underline blue]Selection Prompt with Default Value[/]");
        AnsiConsole.MarkupLine("[grey]Cursor starts on your previous pick — press Enter to confirm or navigate.[/]");
        AnsiConsole.WriteLine();

        var frameworkAgain = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Confirm or change your .NET framework:[/]")
                .PageSize(5)
                .AddChoices("ASP.NET Core", "Blazor", "MAUI", "WPF", "WinForms", "Console")
                .DefaultValue(framework));
        AnsiConsole.MarkupInterpolated($"Confirmed: [bold]{frameworkAgain}[/].");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Prompt history (#158)
        // WithHistory() attaches a shared list; UpArrow/DownArrow navigate through it.
        // Successful submissions are appended automatically.
        AnsiConsole.MarkupLine("[bold underline blue]Prompt History[/]");
        AnsiConsole.MarkupLine("[grey]Press UpArrow to recall previous entries, DownArrow to move forward.[/]");
        AnsiConsole.WriteLine();

        var history = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var historyEntry = AnsiConsole.Prompt(
                new TextPrompt<string>($"[green]Entry {i + 1}[/] (try UpArrow for history):")
                    .WithHistory(history));
            AnsiConsole.MarkupInterpolated($"  Entered: [bold]{historyEntry}[/]");
            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine($"History contains [bold]{history.Count}[/] entries.");
        AnsiConsole.WriteLine();

        // Editable default value (#595)
        // PrefillDefaultValue() writes the default into the input buffer.
        // The user can backspace, edit, or press Enter to accept unchanged.
        AnsiConsole.MarkupLine("[bold underline blue]Editable Default[/]");
        AnsiConsole.MarkupLine("[grey]The previous name is pre-filled — edit it or press Enter to confirm.[/]");
        AnsiConsole.WriteLine();

        var editedName = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Your name:[/]")
                .DefaultValue(name)
                .PrefillDefaultValue());
        AnsiConsole.MarkupInterpolated($"Name confirmed: [bold]{editedName}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Async validator (#230)
        // ValidateAsync accepts a Func<T, Task<ValidationResult>> (or with CancellationToken).
        // The validator can do async I/O (database lookup, API call, etc.) and return
        // ValidationResult.Success() or ValidationResult.Error(message).
        AnsiConsole.MarkupLine("[bold underline blue]Async Validator[/]");
        AnsiConsole.MarkupLine("[grey]Enter any name — the validator simulates an async uniqueness check.[/]");
        AnsiConsole.MarkupLine("[grey]Try 'taken' to see the error, anything else passes.[/]");
        AnsiConsole.WriteLine();

        var username = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Choose a username:[/]")
                .ValidateAsync(async name =>
                {
                    // Simulate an async uniqueness check (e.g., a database query).
                    await Task.Delay(50);
                    return name.Equals("taken", StringComparison.OrdinalIgnoreCase)
                        ? ValidationResult.Error("[red]That username is already taken[/]")
                        : ValidationResult.Success();
                }));

        AnsiConsole.MarkupInterpolated($"Welcome, [bold]{username}[/]!");
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

        AnsiConsole.WriteLine();

        // ToRenderable — static snapshot (#1281)
        // ToRenderable() turns a configured SelectionPrompt into a plain IRenderable
        // snapshot at a given cursor position. No user interaction required.
        AnsiConsole.MarkupLine("[bold underline blue]Prompt as IRenderable — Static Snapshot[/]");
        AnsiConsole.MarkupLine("[grey]The same prompt rendered three times at different cursor positions.[/]");
        AnsiConsole.WriteLine();

        var snapshotPrompt = new SelectionPrompt<string>()
            .Title("[green]Favorite season?[/]")
            .AddChoices("Spring", "Summer", "Autumn", "Winter");

        for (var i = 0; i < 4; i++)
        {
            AnsiConsole.MarkupInterpolated($"[grey]Cursor at index {i}:[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(snapshotPrompt.ToRenderable(AnsiConsole.Console, cursorIndex: i));
            AnsiConsole.WriteLine();
        }

        // AsRenderable — interactive IRenderable wrapper (#1281)
        // AsRenderable() returns a SelectionPromptRenderable<T> that implements IRenderable
        // and can be driven key-by-key — ideal for embedding in a Live display or custom
        // rendering loops without blocking a thread.
        AnsiConsole.MarkupLine("[bold underline blue]Prompt as IRenderable — Interactive[/]");
        AnsiConsole.MarkupLine("[grey]Use arrow keys to navigate, Enter to select.[/]");
        AnsiConsole.WriteLine();

        var interactivePrompt = new SelectionPrompt<string>()
            .Title("[green]Pick a planet:[/]")
            .AddChoices("Mercury", "Venus", "Earth", "Mars", "Jupiter");

        var promptRenderable = interactivePrompt.AsRenderable(AnsiConsole.Console);

        AnsiConsole.Live(promptRenderable)
            .Start(ctx =>
            {
                ctx.Refresh();
                while (!promptRenderable.IsDone)
                {
                    var key = Console.ReadKey(true);
                    if (promptRenderable.Update(key))
                    {
                        ctx.Refresh();
                    }
                }
            });

        AnsiConsole.MarkupInterpolated($"You chose: [bold]{promptRenderable.GetResult()}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }
}
