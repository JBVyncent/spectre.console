using Spectre.Console;

namespace Gallery.Demos.Status;

public sealed class StatusDemo : IDemoModule
{
    public string Name => "Status";
    public string Description => "Spinner-based status display with live updates";

    public void Run()
    {
        AnsiConsole.MarkupLine("[bold underline blue]Status Spinner[/]");
        AnsiConsole.MarkupLine("[grey]Animated spinner with changing status text.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .Start("Initializing...", ctx =>
            {
                Thread.Sleep(800);

                ctx.Status("Connecting to server...");
                ctx.Spinner(Spinner.Known.Arrow);
                Thread.Sleep(800);

                ctx.Status("Downloading data...");
                ctx.Spinner(Spinner.Known.Star);
                Thread.Sleep(800);

                ctx.Status("Processing results...");
                ctx.Spinner(Spinner.Known.Bounce);
                Thread.Sleep(800);

                ctx.Status("Finalizing...");
                ctx.Spinner(Spinner.Known.Dots2);
                Thread.Sleep(600);
            });

        AnsiConsole.MarkupLine("[green]Done![/]");
        AnsiConsole.WriteLine();

        // Status with inline content (Bug #10 fix)
        // Writing partial-line content before a Status display
        // no longer corrupts the output on refresh.
        AnsiConsole.MarkupLine("[bold underline blue]Status with Preceding Content[/]");
        AnsiConsole.MarkupLine("[grey]Content written before status is preserved.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Markup("[bold]Operation: [/]");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Working...", ctx =>
            {
                Thread.Sleep(600);
                ctx.Status("Almost done...");
                Thread.Sleep(600);
            });

        AnsiConsole.MarkupLine("[green]Complete![/]");
        AnsiConsole.WriteLine();

        // Multiple spinner styles
        AnsiConsole.MarkupLine("[bold underline blue]Spinner Styles[/]");
        AnsiConsole.WriteLine();

        var spinners = new[]
        {
            ("Dots", Spinner.Known.Dots),
            ("Line", Spinner.Known.Line),
            ("Star", Spinner.Known.Star),
            ("Arrow", Spinner.Known.Arrow),
            ("Bounce", Spinner.Known.Bounce),
        };

        foreach (var (name, spinner) in spinners)
        {
            AnsiConsole.Status()
                .Spinner(spinner)
                .SpinnerStyle(Style.Parse("cyan"))
                .Start($"[grey]{name} spinner[/]", _ =>
                {
                    Thread.Sleep(1200);
                });
        }

        AnsiConsole.MarkupLine("[green]All spinners demonstrated.[/]");
    }
}
