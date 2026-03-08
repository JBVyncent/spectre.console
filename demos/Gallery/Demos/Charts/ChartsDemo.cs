using Spectre.Console;

namespace Gallery.Demos.Charts;

public sealed class ChartsDemo : IDemoModule
{
    public string Name => "Charts";
    public string Description => "Bar charts with static and live-updating values";

    public void Run()
    {
        RunStaticBarChart();
        AnsiConsole.WriteLine();
        RunLiveBarChart();
    }

    private static void RunStaticBarChart()
    {
        AnsiConsole.MarkupLine("[bold underline blue]Static Bar Chart[/]");
        AnsiConsole.MarkupLine("[grey]A fixed snapshot of categorical data.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new BarChart()
            .Width(60)
            .Label("[bold green]Monthly Downloads (thousands)[/]")
            .AddItem("January",  82, Color.Cyan1)
            .AddItem("February", 65, Color.Blue)
            .AddItem("March",    93, Color.Green)
            .AddItem("April",    71, Color.Yellow)
            .AddItem("May",      58, Color.Orange1)
            .AddItem("June",     110, Color.Red));
    }

    private static void RunLiveBarChart()
    {
        AnsiConsole.MarkupLine("[bold underline blue]Live Bar Chart — Mutable Values[/]");
        AnsiConsole.MarkupLine("[grey]Bar items are mutated in-place between refreshes.[/]");
        AnsiConsole.WriteLine();

        // Create named item references so we can mutate them after AddItem.
        // BarChart.Data holds the same object references, so changes are
        // reflected immediately on the next ctx.Refresh().
        var cpu  = new BarChartItem("CPU",    0.0, Color.Red);
        var mem  = new BarChartItem("Memory", 0.0, Color.Blue);
        var disk = new BarChartItem("Disk",   0.0, Color.Green);
        var net  = new BarChartItem("Network",0.0, Color.Yellow);

        var chart = new BarChart()
            .Width(60)
            .WithMaxValue(100)
            .Label("[bold]System Utilisation (%)[/]")
            .AddItem(cpu)
            .AddItem(mem)
            .AddItem(disk)
            .AddItem(net);

        var rng = new Random(42);

        AnsiConsole.Live(chart).Start(ctx =>
        {
            for (var tick = 0; tick < 8; tick++)
            {
                // Mutate the items; the chart reads Value/Label/Color at render time.
                cpu.Value   = Math.Round(20 + rng.NextDouble() * 75, 1);
                mem.Value   = Math.Round(40 + rng.NextDouble() * 50, 1);
                disk.Value  = Math.Round(5  + rng.NextDouble() * 40, 1);
                net.Value   = Math.Round(10 + rng.NextDouble() * 60, 1);

                // Optionally rename an item to show label mutation too.
                cpu.Label = tick % 2 == 0 ? "CPU  " : "CPU* ";

                ctx.Refresh();
                Thread.Sleep(350);
            }
        });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Final values reflect the last in-place mutation.[/]");
    }
}
