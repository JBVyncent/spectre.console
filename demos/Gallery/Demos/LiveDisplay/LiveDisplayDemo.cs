using Spectre.Console;

namespace Gallery.Demos.LiveDisplay;

public sealed class LiveDisplayDemo : IDemoModule
{
    public string Name => "Live Display";
    public string Description => "Live-updating displays with correct cursor management";

    public void Run()
    {
        // Basic live display
        AnsiConsole.MarkupLine("[bold underline blue]Live Display[/]");
        AnsiConsole.MarkupLine("[grey]Watch the table update in real time.[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Timestamp");
        table.AddColumn("Event");
        table.AddColumn("Status");

        AnsiConsole.Live(table)
            .Start(ctx =>
            {
                var events = new[]
                {
                    ("Initializing", "[yellow]Pending[/]"),
                    ("Connecting to database", "[yellow]Pending[/]"),
                    ("Loading configuration", "[yellow]Pending[/]"),
                    ("Starting services", "[yellow]Pending[/]"),
                    ("Ready", "[green]Complete[/]"),
                };

                foreach (var (name, status) in events)
                {
                    table.AddRow(
                        $"[grey]{DateTime.Now:HH:mm:ss.fff}[/]",
                        name,
                        status);
                    ctx.Refresh();
                    Thread.Sleep(400);
                }

                // Update previous rows to show completion
                Thread.Sleep(300);
            });

        AnsiConsole.WriteLine();

        // Live display with partial-line content (Bug #10 fix)
        // Writing content without a trailing newline during a live
        // display no longer causes cursor corruption. The save/restore
        // cursor approach preserves user content at any column position.
        AnsiConsole.MarkupLine("[bold underline blue]Live Display with Inline Content[/]");
        AnsiConsole.MarkupLine("[grey]Content written mid-line is preserved across refreshes.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Markup("[bold green]Status: [/]");

        var statusTable = new Table().Border(TableBorder.Simple);
        statusTable.AddColumn("Step");
        statusTable.AddColumn("Result");

        AnsiConsole.Live(statusTable)
            .Start(ctx =>
            {
                var steps = new[] { "Validate", "Compile", "Test", "Deploy" };
                foreach (var step in steps)
                {
                    statusTable.AddRow(step, "[green]OK[/]");
                    ctx.Refresh();
                    Thread.Sleep(350);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]All steps completed.[/]");
    }
}
