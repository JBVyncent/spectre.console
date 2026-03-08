using Spectre.Console;

namespace Gallery.Demos.Tables;

public sealed class TablesDemo : IDemoModule
{
    public string Name => "Tables";
    public string Description => "Table rendering with borders, formatting, and edge cases";

    public void Run()
    {
        // Standard table with various column types
        AnsiConsole.MarkupLine("[bold underline blue]Standard Table[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[yellow]User Directory[/]")
            .Caption("[grey]Sample data[/]");

        table.AddColumn(new TableColumn("[bold]Name[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Role[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Status[/]").Centered());

        table.AddRow("[green]Alice[/]", "Engineer", "[green]Active[/]");
        table.AddRow("[blue]Bob[/]", "Designer", "[green]Active[/]");
        table.AddRow("[yellow]Charlie[/]", "Manager", "[red]Away[/]");
        table.AddRow("[cyan]Diana[/]", "DevOps", "[green]Active[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Nested table
        AnsiConsole.MarkupLine("[bold underline blue]Nested Tables[/]");
        AnsiConsole.WriteLine();

        var inner = new Table().Border(TableBorder.Simple);
        inner.AddColumn("Detail");
        inner.AddColumn("Value");
        inner.AddRow("CPU", "12 cores");
        inner.AddRow("RAM", "64 GB");

        var outer = new Table().Border(TableBorder.Heavy);
        outer.AddColumn("Server");
        outer.AddColumn("Specs");
        outer.AddRow(new Text("prod-web-01"), inner);

        AnsiConsole.Write(outer);
        AnsiConsole.WriteLine();

        // Edge case: Tab characters in cells (Bug #2 fix)
        // Tabs are replaced with spaces to maintain border alignment.
        AnsiConsole.MarkupLine("[bold underline blue]Tab Characters in Cells[/]");
        AnsiConsole.MarkupLine("[grey]Tabs are normalized to spaces for consistent border alignment.[/]");
        AnsiConsole.WriteLine();

        var tabTable = new Table().Border(TableBorder.Square);
        tabTable.AddColumn("Input");
        tabTable.AddColumn("Content");
        tabTable.AddRow("Normal", "Hello World");
        tabTable.AddRow("With tabs", "Col1\tCol2\tCol3");
        tabTable.AddRow("Mixed", "Start\tmiddle\tend");

        AnsiConsole.Write(tabTable);
        AnsiConsole.WriteLine();

        // Various border styles
        AnsiConsole.MarkupLine("[bold underline blue]Border Styles[/]");
        AnsiConsole.WriteLine();

        var borders = new (string Name, TableBorder Border)[]
        {
            ("Minimal", TableBorder.Minimal),
            ("Ascii", TableBorder.Ascii),
            ("Double", TableBorder.Double),
            ("Markdown", TableBorder.Markdown),
        };

        foreach (var (name, border) in borders)
        {
            var demo = new Table().Border(border).Title($"[grey]{name}[/]");
            demo.AddColumn("A");
            demo.AddColumn("B");
            demo.AddRow("1", "2");
            demo.AddRow("3", "4");
            AnsiConsole.Write(demo);
            AnsiConsole.WriteLine();
        }
    }
}
