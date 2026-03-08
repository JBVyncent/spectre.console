using Spectre.Console;

namespace Gallery.Demos.Progress;

public sealed class ProgressDemo : IDemoModule
{
    public string Name => "Progress";
    public string Description => "Progress bars with multiple tasks, spinners, and dynamic updates";

    public void Run()
    {
        AnsiConsole.MarkupLine("[bold underline blue]Progress Display[/]");
        AnsiConsole.MarkupLine("[grey]Multiple concurrent tasks with progress tracking.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .Start(ctx =>
            {
                var download = ctx.AddTask("[green]Downloading packages[/]", maxValue: 100);
                var build = ctx.AddTask("[blue]Building solution[/]", maxValue: 100);
                var test = ctx.AddTask("[yellow]Running tests[/]", maxValue: 100);

                // Simulate concurrent progress
                var random = new Random(42);
                while (!ctx.IsFinished)
                {
                    if (!download.IsFinished)
                    {
                        download.Increment(random.Next(1, 8));
                    }

                    if (download.Percentage > 30 && !build.IsFinished)
                    {
                        build.Increment(random.Next(1, 5));
                    }

                    if (build.Percentage > 50 && !test.IsFinished)
                    {
                        test.Increment(random.Next(1, 6));
                    }

                    Thread.Sleep(80);
                }
            });

        AnsiConsole.WriteLine();

        // Progress with dynamically added tasks
        AnsiConsole.MarkupLine("[bold underline blue]Dynamic Task Addition[/]");
        AnsiConsole.MarkupLine("[grey]Tasks added while progress is running.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .Start(ctx =>
            {
                var task1 = ctx.AddTask("[cyan]Phase 1: Analysis[/]", maxValue: 100);

                while (!task1.IsFinished)
                {
                    task1.Increment(5);
                    Thread.Sleep(50);
                }

                var task2 = ctx.AddTask("[cyan]Phase 2: Transform[/]", maxValue: 100);
                while (!task2.IsFinished)
                {
                    task2.Increment(4);
                    Thread.Sleep(50);
                }

                var task3 = ctx.AddTask("[cyan]Phase 3: Output[/]", maxValue: 100);
                while (!task3.IsFinished)
                {
                    task3.Increment(8);
                    Thread.Sleep(50);
                }
            });

        AnsiConsole.WriteLine();
    }
}
