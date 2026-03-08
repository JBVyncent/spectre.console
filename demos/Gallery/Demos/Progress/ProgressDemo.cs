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

        // Nested progress (#1419)
        // AddChildTask() creates sub-tasks that display indented under their parent.
        // Setting AutoCompleteWithChildren = true causes the parent to automatically
        // complete when all of its children have finished — no manual StopTask() call
        // needed on the parent.
        AnsiConsole.MarkupLine("[bold underline blue]Nested Progress[/]");
        AnsiConsole.MarkupLine("[grey]Parent tasks auto-complete when all children finish.[/]");
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
                // Top-level pipeline task; will auto-complete when both stages finish.
                var pipeline = ctx.AddTask("[bold]CI Pipeline[/]", maxValue: 100);
                pipeline.AutoCompleteWithChildren = true;

                // Stage 1 — has its own sub-tasks and also auto-completes.
                var stage1 = ctx.AddChildTask(pipeline, "[green]Stage 1: Build[/]", maxValue: 100);
                stage1.AutoCompleteWithChildren = true;

                var compile = ctx.AddChildTask(stage1, "Compile", maxValue: 60);
                var link = ctx.AddChildTask(stage1, "Link", maxValue: 40);

                // Stage 2 — starts after stage 1 completes.
                var stage2 = ctx.AddChildTask(pipeline, "[yellow]Stage 2: Test[/]", autoStart: false, maxValue: 100);
                stage2.AutoCompleteWithChildren = true;

                var random = new Random(42);

                // Drive stage 1.
                while (!stage1.IsFinished)
                {
                    if (!compile.IsFinished)
                    {
                        compile.Increment(random.Next(3, 9));
                    }

                    if (compile.Percentage > 60 && !link.IsFinished)
                    {
                        link.Increment(random.Next(4, 10));
                    }

                    Thread.Sleep(60);
                }

                // Stage 2 begins.
                stage2.StartTask();
                var unitTests = ctx.AddChildTask(stage2, "Unit tests", maxValue: 50);
                var integration = ctx.AddChildTask(stage2, "Integration tests", maxValue: 50);

                while (!stage2.IsFinished)
                {
                    if (!unitTests.IsFinished)
                    {
                        unitTests.Increment(random.Next(4, 8));
                    }

                    if (unitTests.Percentage > 40 && !integration.IsFinished)
                    {
                        integration.Increment(random.Next(3, 7));
                    }

                    Thread.Sleep(60);
                }
            });

        AnsiConsole.WriteLine();
    }
}
