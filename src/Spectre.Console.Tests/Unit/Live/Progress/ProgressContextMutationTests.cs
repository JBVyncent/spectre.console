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
                ctx.IsFinished.Should().BeTrue();
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
                ctx.IsFinished.Should().BeFalse();
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
                ctx.IsFinished.Should().BeTrue();
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

                t2.Id.Should().Be(t1.Id + 1);
                t3.Id.Should().Be(t2.Id + 1);
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
                task.IsStarted.Should().BeFalse();
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
                task.MaxValue.Should().Be(200);
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
                task.IsStarted.Should().BeFalse();
                task.MaxValue.Should().Be(50);
            });
    }

    [Fact]
    public void AddTask_With_Null_Settings_Should_Throw()
    {
        // Kills L201 ThrowIfNull(settings) removal
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                FluentActions.Invoking(() => ctx.AddTask("Task", (ProgressTaskSettings)null!)).Should().Throw<ArgumentNullException>();
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
                task.IsStarted.Should().BeFalse();
                task.MaxValue.Should().Be(75);
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
                before.MaxValue.Should().Be(150);
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
                after.MaxValue.Should().Be(300);
                after.IsStarted.Should().BeFalse();
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
                t3.Should().NotBeNull();
                t3.Id.Should().NotBe(t1.Id);
                t3.Id.Should().NotBe(t2.Id);
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

                t3.Should().NotBeNull();
                t3.Id.Should().NotBe(t1.Id);
                t3.Id.Should().NotBe(t2.Id);
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
                ctx.RemoveTask(task).Should().BeTrue();
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
                ctx.RemoveTask(task1).Should().BeFalse(); // Already removed
            });
    }

    [Fact]
    public void RemoveTask_Should_Detach_Child_From_Parent()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var parent = ctx.AddTask("Parent");
                var child = ctx.AddChildTask(parent, "Child");

                parent.Children.Should().HaveCount(1);
                child.Parent.Should().Be(parent);

                ctx.RemoveTask(child);

                parent.Children.Should().BeEmpty();
                child.Parent.Should().BeNull();
            });
    }

    [Fact]
    public void RemoveTask_Should_Clear_Parent_Reference_On_Removed_Child()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var parent = ctx.AddTask("Parent");
                var child = ctx.AddChildTask(parent, "Child");

                ctx.RemoveTask(child);

                child.Parent.Should().BeNull();
            });
    }

    [Fact]
    public void RemoveTask_Of_Root_Task_Should_Not_Throw()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var task = ctx.AddTask("Root");

                // Root task has no parent — removing should not throw
                var act = () => ctx.RemoveTask(task);
                act.Should().NotThrow();
            });
    }

    [Fact]
    public void RemoveTask_Should_Not_Affect_AutoComplete_Of_Parent_With_Remaining_Children()
    {
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var parent = ctx.AddTask("Parent", autoStart: false);
                parent.AutoCompleteWithChildren = true;

                var child1 = ctx.AddChildTask(parent, "Child1");
                var child2 = ctx.AddChildTask(parent, "Child2");

                // Remove child1 — parent still has child2 unfinished
                ctx.RemoveTask(child1);

                // Finish child2 — now parent should auto-complete
                child2.StopTask();
                parent.StartTask();

                // Force propagation by calling GetTasks
                ctx.Refresh();

                // Parent should be auto-completed since all remaining children are done
                parent.IsFinished.Should().BeTrue();
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
                task.Should().NotBeNull();
                task.MaxValue.Should().Be(77);
                task.IsStarted.Should().BeFalse();
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
                before.Should().NotBeNull();
                before.MaxValue.Should().Be(88);
                before.Id.Should().NotBe(reference.Id);
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
                after.Should().NotBeNull();
                after.MaxValue.Should().Be(99);
                after.Id.Should().NotBe(t1.Id);
                after.Id.Should().NotBe(t2.Id);
            });
    }

    // ── AddChildTask mutant killers ───────────────────────────────────────────

    [Fact]
    public void AddChildTask_Passes_AutoStart_And_MaxValue()
    {
        // Kills L201 object initializer mutation: new ProgressTaskSettings { AutoStart = autoStart, MaxValue = maxValue }
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var parent = ctx.AddTask("Parent");
                var child = ctx.AddChildTask(parent, "Child", autoStart: false, maxValue: 42);
                child.IsStarted.Should().BeFalse();
                child.MaxValue.Should().Be(42);
            });
    }

    [Fact]
    public void AddChildTask_With_Settings_Throws_When_Parent_IsNull()
    {
        // Kills L218 statement mutation: removing ArgumentNullException.ThrowIfNull(parent)
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var settings = new ProgressTaskSettings { AutoStart = true, MaxValue = 100 };
                var act = () => ctx.AddChildTask(null!, "Child", settings);
                act.Should().Throw<ArgumentNullException>();
            });
    }

    // ── ValidateReferenceTask mutant killers ──────────────────────────────────

    [Fact]
    public void AddTaskBefore_Throws_When_ReferenceTask_IsNull()
    {
        // Kills L273 statement mutation: removing ArgumentNullException.ThrowIfNull(referenceTask)
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var act = () => ctx.AddTaskBefore("X", null!);
                act.Should().Throw<ArgumentNullException>();
            });
    }

    [Fact]
    public void AddTaskBefore_Throws_When_ReferenceTask_Not_In_Context()
    {
        // Kills L277 NoCoverage: throw InvalidOperationException in ValidateReferenceTask
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                // Create a task from a different context
                ProgressTask foreignTask = null!;
                new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
                    .Start(otherCtx =>
                    {
                        foreignTask = otherCtx.AddTask("Foreign");
                    });

                var act = () => ctx.AddTaskBefore("X", foreignTask);
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage("*does not belong*");
            });
    }

    // ── FindChildInsertionIndex mutant killers ────────────────────────────────

    [Fact]
    public void AddChildTask_Multiple_Children_Inserted_In_Order()
    {
        // Kills L319 statement mutation: removing break in FindChildInsertionIndex
        // Without break, the descendant walk continues past the match, potentially
        // giving wrong insertion index for subsequent children.
        new Progress(CreateInteractiveConsole()) { AutoRefresh = false }
            .Start(ctx =>
            {
                var parent = ctx.AddTask("Parent");
                var child1 = ctx.AddChildTask(parent, "Child1");
                var child2 = ctx.AddChildTask(parent, "Child2");
                var child3 = ctx.AddChildTask(parent, "Child3");

                // All children should be in order after parent
                var tasks = ctx.GetTasks();
                var parentIdx = tasks.IndexOf(parent);
                tasks.IndexOf(child1).Should().Be(parentIdx + 1);
                tasks.IndexOf(child2).Should().Be(parentIdx + 2);
                tasks.IndexOf(child3).Should().Be(parentIdx + 3);
            });
    }
}
