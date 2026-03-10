using Spectre.Console;

namespace Gallery.Demos.BugFixes;

public sealed class BugFixesDemo : IDemoModule
{
    public string Name => "Bug Fixes";
    public string Description => "Demonstrations of upstream bug fixes from GitHub issues";

    public void Run()
    {
        // #1653 — Search highlight case-insensitive matching
        AnsiConsole.MarkupLine("[bold underline blue]#1653 — Search Highlight Case-Insensitivity[/]");
        AnsiConsole.MarkupLine("[grey]SelectionPrompt search now highlights matches case-insensitively.[/]");
        AnsiConsole.MarkupLine("[grey]Previously, searching 'hello' wouldn't highlight 'Hello'.[/]");
        AnsiConsole.WriteLine();

        // #1638 — TextPrompt colon suffix
        AnsiConsole.MarkupLine("[bold underline blue]#1638 — TextPrompt Colon Suffix[/]");
        AnsiConsole.MarkupLine("[grey]TextPrompt now appends a colon consistently unless the prompt[/]");
        AnsiConsole.MarkupLine("[grey]already ends with a common terminator (: ? > $ #).[/]");
        AnsiConsole.WriteLine();

        // #1592 — File URI format for Windows Terminal
        AnsiConsole.MarkupLine("[bold underline blue]#1592 — Exception File URI Format[/]");
        AnsiConsole.MarkupLine("[grey]Exception paths now use file:///path format instead of[/]");
        AnsiConsole.MarkupLine("[grey]file://HOSTNAME/path, which Windows Terminal requires.[/]");
        AnsiConsole.WriteLine();

        try
        {
            throw new InvalidOperationException("Example exception for link demo");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
        }

        AnsiConsole.WriteLine();

        // #1599 — Combining characters in table cells
        AnsiConsole.MarkupLine("[bold underline blue]#1599 — Combining Characters Cell Width[/]");
        AnsiConsole.MarkupLine("[grey]Combining characters (accents, diacritics) no longer break table borders.[/]");
        AnsiConsole.MarkupLine("[grey]SegmentLine.Length now uses cell width, not string length.[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Plain");
        table.AddColumn("With Diacritics");
        table.AddRow("cafe", "caf\u0065\u0301");        // café with combining acute
        table.AddRow("naive", "na\u0069\u0308ve");       // naïve with combining diaeresis
        table.AddRow("resume", "r\u0065\u0301sum\u0065\u0301"); // résumé with combining accents
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // #1579 — Task.WhenAll AggregateException preservation
        AnsiConsole.MarkupLine("[bold underline blue]#1579 — AggregateException Preservation[/]");
        AnsiConsole.MarkupLine("[grey]Progress.StartAsync now preserves all exceptions from Task.WhenAll,[/]");
        AnsiConsole.MarkupLine("[grey]not just the first one. Previously, 'await' unwrapped the[/]");
        AnsiConsole.MarkupLine("[grey]AggregateException, losing all but the first inner exception.[/]");
        AnsiConsole.WriteLine();

        // #1707 — FigletText OOM protection
        AnsiConsole.MarkupLine("[bold underline blue]#1707 — FigletText OOM Protection[/]");
        AnsiConsole.MarkupLine("[grey]FigletText.Render now caps maxWidth at 1000 to prevent OOM when[/]");
        AnsiConsole.MarkupLine("[grey]test harnesses pass int.MaxValue as the console width.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new FigletText("OK").Color(Color.Green));
        AnsiConsole.WriteLine();

        // #1569 — Panel in Layout preserves borders
        AnsiConsole.MarkupLine("[bold underline blue]#1569 — Panel Border Preservation in Layout[/]");
        AnsiConsole.MarkupLine("[grey]Non-expanded Panels inside Layout now respect height constraints,[/]");
        AnsiConsole.MarkupLine("[grey]preserving the bottom border instead of silently truncating.[/]");
        AnsiConsole.WriteLine();

        var layout = new Layout()
            .SplitColumns(
                new Layout("Left").Update(
                    new Panel("This text wraps inside the panel and the bottom border is preserved.")
                        .Header("[cyan]Left Panel[/]")
                        .Expand()),
                new Layout("Right").Update(
                    new Panel("Short content.")
                        .Header("[cyan]Right Panel[/]")
                        .Expand()));
        AnsiConsole.Write(layout);
        AnsiConsole.WriteLine();

        // #1723 — Live display recording
        AnsiConsole.MarkupLine("[bold underline blue]#1723 — Live Display Recording[/]");
        AnsiConsole.MarkupLine("[grey]LiveDisplay.Completed now always re-renders the final state,[/]");
        AnsiConsole.MarkupLine("[grey]so small tables are properly captured by Record/ExportText.[/]");
        AnsiConsole.MarkupLine("[grey]Previously, only tables that overflowed the terminal were recorded.[/]");
        AnsiConsole.WriteLine();

        var liveTable = new Table().Border(TableBorder.Rounded);
        liveTable.AddColumn("Item");
        liveTable.AddColumn("Status");

        AnsiConsole.Live(liveTable)
            .AutoClear(false)
            .Start(ctx =>
            {
                var items = new[] { "Build", "Test", "Package" };
                foreach (var item in items)
                {
                    liveTable.AddRow(item, "[green]Done[/]");
                    ctx.Refresh();
                    Thread.Sleep(300);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]All bug fix demos complete.[/]");
    }
}
