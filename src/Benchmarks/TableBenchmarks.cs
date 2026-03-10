using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Spectre.Console;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class TableBenchmarks
{
    private IAnsiConsole _console = null!;

    [GlobalSetup]
    public void Setup()
    {
        _console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(TextWriter.Null),
            Interactive = InteractionSupport.No,
            EnvironmentVariables = null,
        });
    }

    [Benchmark]
    public void SmallTable_3x3()
    {
        var table = new Table()
            .AddColumn("Name")
            .AddColumn("Value")
            .AddColumn("Status");

        table.AddRow("Alpha", "100", "[green]OK[/]");
        table.AddRow("Beta", "200", "[yellow]Warn[/]");
        table.AddRow("Gamma", "300", "[red]Error[/]");

        _console.Write(table);
    }

    [Benchmark]
    public void MediumTable_5x20()
    {
        var table = new Table()
            .AddColumn("ID")
            .AddColumn("Name")
            .AddColumn("Category")
            .AddColumn("Price")
            .AddColumn("Status");

        for (var i = 0; i < 20; i++)
        {
            table.AddRow(
                i.ToString(),
                $"Item {i}",
                i % 3 == 0 ? "A" : i % 3 == 1 ? "B" : "C",
                $"${i * 10 + 5:F2}",
                i % 2 == 0 ? "[green]Active[/]" : "[red]Inactive[/]");
        }

        _console.Write(table);
    }

    [Benchmark]
    public void LargeTable_10x100()
    {
        var table = new Table();

        for (var col = 0; col < 10; col++)
        {
            table.AddColumn($"Col{col}");
        }

        for (var row = 0; row < 100; row++)
        {
            var cells = new string[10];
            for (var col = 0; col < 10; col++)
            {
                cells[col] = $"R{row}C{col}";
            }

            table.AddRow(cells);
        }

        _console.Write(table);
    }

    [Benchmark]
    public void StyledTable_WithBorders()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[cyan]Dashboard[/]")
            .Caption("[grey]Updated: 2026-03-09[/]")
            .AddColumn(new TableColumn("[bold]Metric[/]").Centered())
            .AddColumn(new TableColumn("[bold]Value[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Trend[/]").Centered());

        table.AddRow("[blue]CPU[/]", "78%", "[green]↑[/]");
        table.AddRow("[blue]Memory[/]", "4.2 GB", "[yellow]→[/]");
        table.AddRow("[blue]Disk[/]", "120 GB", "[red]↓[/]");
        table.AddRow("[blue]Network[/]", "1.2 Gbps", "[green]↑[/]");

        _console.Write(table);
    }
}
