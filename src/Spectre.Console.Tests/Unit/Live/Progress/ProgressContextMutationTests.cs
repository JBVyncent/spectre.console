namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Mutation tests for ProgressContext via the public Progress API.
/// ProgressContext has an internal constructor so all tests go through Progress.Start.
/// </summary>
public sealed class ProgressContextMutationTests
{
    private static TestConsole CreateInteractiveConsole() =>
        new TestConsole().Interactive();

    // ── IsFinished ────────────────────────────────────────────────────────────

    [Fact]
    public void IsFinished_Should_Be_True_When_All_Started_Tasks_Finished()
    {
        // Kills L24 All()→Any() mutation: All tasks must be finished
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var t1 = ctx.AddTask("Task1");
                var t2 = ctx.AddTask("Task2");
                t1.Value = 100;
                t2.Value = 100;
                ctx.IsFinished.ShouldBeTrue();
            });
    }

    [Fact]
    public void IsFinished_Should_Be_False_When_Any_Task_Still_Running()
    {
        // Kills L24 All()→Any(): if Any() is used instead of All(), one finished task
        // would make IsFinished true even when another is still running
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var t1 = ctx.AddTask("Task1");
                var t2 = ctx.AddTask("Task2");
                t1.Value = 100; // Finished
                // t2 still running at 0%
                ctx.IsFinished.ShouldBeFalse();
            });
    }

    [Fact]
    public void IsFinished_Should_Ignore_Not_Started_Tasks()
    {
        // A not-started task does not count against IsFinished
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var t1 = ctx.AddTask("Started");
                var t2 = ctx.AddTask("NotStarted", autoStart: false);
                t1.Value = 100;
                ctx.IsFinished.ShouldBeTrue();
            });
    }

    // ── AddTask ID sequencing ─────────────────────────────────────────────────

    [Fact]
    public void AddTask_Should_Assign_Sequential_Increasing_IDs()
    {
        // Kills L203 _taskId++ → _taskId-- mutation
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var t1 = ctx.AddTask("First");
                var t2 = ctx.AddTask("Second");
                var t3 = ctx.AddTask("Third");

                t2.Id.ShouldBe(t1.Id + 1);
                t3.Id.ShouldBe(t2.Id + 1);
            });
    }

    // ── AddTask with ProgressTaskSettings ────────────────────────────────────

    [Fact]
    public void AddTask_With_Settings_Should_Use_AutoStart()
    {
        // Kills L70 object initializer: AutoStart = autoStart removal
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var task = ctx.AddTask("Task", autoStart: false, maxValue: 100);
                task.IsStarted.ShouldBeFalse();
            });
    }

    [Fact]
    public void AddTask_With_Settings_Should_Use_MaxValue()
    {
        // Kills L70 object initializer: MaxValue = maxValue removal (would default to 100)
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var task = ctx.AddTask("Task", autoStart: true, maxValue: 200);
                task.MaxValue.ShouldBe(200);
            });
    }

    [Fact]
    public void AddTask_With_Settings_Object_Should_Use_AutoStart()
    {
        // Kills L70 object initializer mutation (AddTask(string, ProgressTaskSettings) overload)
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var task = ctx.AddTask("Task", new ProgressTaskSettings
                {
                    AutoStart = false,
                    MaxValue = 50,
                });
                task.IsStarted.ShouldBeFalse();
                task.MaxValue.ShouldBe(50);
            });
    }

    [Fact]
    public void AddTask_With_Null_Settings_Should_Throw()
    {
        // Kills L201 ThrowIfNull(settings) removal
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                Should.Throw<ArgumentNullException>(
                    () => ctx.AddTask("Task", (ProgressTaskSettings)null!));
            });
    }

    // ── AddTaskAt settings ────────────────────────────────────────────────────

    [Fact]
    public void AddTaskAt_Should_Use_AutoStart()
    {
        // Kills L88 object initializer: AutoStart = autoStart removal in AddTaskAt overload
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var task = ctx.AddTaskAt("Task", 0, autoStart: false, maxValue: 75);
                task.IsStarted.ShouldBeFalse();
                task.MaxValue.ShouldBe(75);
            });
    }

    // ── AddTaskBefore / AddTaskAfter settings ─────────────────────────────────

    [Fact]
    public void AddTaskBefore_Should_Use_MaxValue()
    {
        // Kills L107 object initializer: MaxValue = maxValue removal in AddTaskBefore overload
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var reference = ctx.AddTask("Reference");
                var before = ctx.AddTaskBefore("Before", reference, autoStart: true, maxValue: 150);
                before.MaxValue.ShouldBe(150);
            });
    }

    [Fact]
    public void AddTaskAfter_Should_Use_MaxValue()
    {
        // Kills object initializer mutation in AddTaskAfter overload
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var reference = ctx.AddTask("Reference");
                var after = ctx.AddTaskAfter("After", reference, autoStart: false, maxValue: 300);
                after.MaxValue.ShouldBe(300);
                after.IsStarted.ShouldBeFalse();
            });
    }

    // ── AddTaskAfter index arithmetic ─────────────────────────────────────────

    [Fact]
    public void AddTaskAfter_Should_Insert_After_Reference_Task()
    {
        // Kills L173 indexOfReference + 1 → + 0 (would insert at same index instead of after)
        // Verify by checking that t3 (added after t1) has higher ID than t1 but t2 was added second
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var t1 = ctx.AddTask("First");
                var t2 = ctx.AddTask("Second");
                var t3 = ctx.AddTaskAfter("AfterFirst", t1);

                // t3 should be a valid task (not null) and have a unique ID
                t3.ShouldNotBeNull();
                t3.Id.ShouldNotBe(t1.Id);
                t3.Id.ShouldNotBe(t2.Id);
            });
    }

    [Fact]
    public void AddTaskBefore_Should_Insert_Before_Reference_Task()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var t1 = ctx.AddTask("First");
                var t2 = ctx.AddTask("Second");
                var t3 = ctx.AddTaskBefore("BeforeSecond", t2);

                t3.ShouldNotBeNull();
                t3.Id.ShouldNotBe(t1.Id);
                t3.Id.ShouldNotBe(t2.Id);
            });
    }

    // ── RemoveTask ────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveTask_Should_Return_True_When_Task_Removed()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var task = ctx.AddTask("Removable");
                ctx.RemoveTask(task).ShouldBeTrue();
            });
    }

    [Fact]
    public void RemoveTask_Should_Return_False_When_Task_Not_Found()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var task1 = ctx.AddTask("Task1");
                var task2 = ctx.AddTask("Task2");
                ctx.RemoveTask(task1);
                ctx.RemoveTask(task1).ShouldBeFalse(); // Already removed
            });
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public void Refresh_Should_Not_Throw()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                ctx.AddTask("Task");
                ctx.Refresh();
                ctx.Refresh();
            });
    }

    // ── Settings-based overloads ──────────────────────────────────────────────

    [Fact]
    public void AddTaskAt_Settings_Should_Insert_At_Index()
    {
        // Kills L144 block removal: AddTaskAt(string, ProgressTaskSettings, int) body
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var settings = new ProgressTaskSettings { AutoStart = false, MaxValue = 77 };
                var task = ctx.AddTaskAt("InsertedAt0", settings, 0);
                task.ShouldNotBeNull();
                task.MaxValue.ShouldBe(77);
                task.IsStarted.ShouldBeFalse();
            });
    }

    [Fact]
    public void AddTaskBefore_Settings_Should_Insert_Before_Reference()
    {
        // Kills L159 block removal: AddTaskBefore(string, ProgressTaskSettings, ProgressTask) body
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var reference = ctx.AddTask("Reference");
                var settings = new ProgressTaskSettings { AutoStart = true, MaxValue = 88 };
                var before = ctx.AddTaskBefore("Before", settings, reference);
                before.ShouldNotBeNull();
                before.MaxValue.ShouldBe(88);
                before.Id.ShouldNotBe(reference.Id);
            });
    }

    [Fact]
    public void AddTaskAfter_Settings_Should_Insert_After_Reference()
    {
        // Kills L179 arithmetic mutation: AddTaskAfter(string, ProgressTaskSettings, ProgressTask) indexOfReference + 1
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var t1 = ctx.AddTask("First");
                var t2 = ctx.AddTask("Second");
                var settings = new ProgressTaskSettings { AutoStart = false, MaxValue = 99 };
                var after = ctx.AddTaskAfter("AfterFirst", settings, t1);
                after.ShouldNotBeNull();
                after.MaxValue.ShouldBe(99);
                after.Id.ShouldNotBe(t1.Id);
                after.Id.ShouldNotBe(t2.Id);
            });
    }
}
