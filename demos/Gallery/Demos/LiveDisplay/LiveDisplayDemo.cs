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
        AnsiConsole.WriteLine();

        // Targeted erase operations (#1570)
        // Demonstrate the new partial-erase methods:
        //   ClearLine()       ESC[2K — erase entire current line
        //   ClearLineToEnd()  ESC[0K — erase from cursor to end of line
        //   ClearLineToStart() ESC[1K — erase from start of line to cursor
        //   ClearToBottom()   ESC[0J — erase from cursor to bottom
        //   ClearToTop()      ESC[1J — erase from cursor to top
        AnsiConsole.MarkupLine("[bold underline blue]Targeted Erase Operations[/]");
        AnsiConsole.MarkupLine("[grey](ESC[2K / ESC[0K / ESC[1K / ESC[0J / ESC[1J)[/]");
        AnsiConsole.WriteLine();

        // Write a line, pause, then erase it and replace with clean content
        AnsiConsole.Markup("[yellow]Loading...[/]");
        Thread.Sleep(600);

        // Move cursor back to the start of the line and erase it
        AnsiConsole.Cursor.MoveLeft(10);
        AnsiConsole.ClearLine();
        AnsiConsole.MarkupLine("[green]Done.      [/]");

        AnsiConsole.MarkupLine("[grey]ClearLine() erased the 'Loading...' text in place.[/]");
        AnsiConsole.WriteLine();

        // Write several lines then erase below cursor
        AnsiConsole.MarkupLine("[cyan]Line one[/]");
        AnsiConsole.MarkupLine("[cyan]Line two[/]");
        AnsiConsole.Markup("[cyan]Line thr");
        AnsiConsole.ClearToBottom();   // erase from here to bottom
        AnsiConsole.MarkupLine("[green]ree (ClearToBottom erased the rest of the line)[/]");
    }
}
