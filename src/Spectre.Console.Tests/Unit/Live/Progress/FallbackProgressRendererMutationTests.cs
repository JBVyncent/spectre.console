namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests for FallbackProgressRenderer via Progress with a non-interactive (non-ANSI) console.
/// A TestConsole without .Interactive() uses FallbackProgressRenderer since caps.Interactive is false.
/// </summary>
public sealed class FallbackProgressRendererMutationTests
{
    private static Progress CreateFallbackProgress(TestConsole console, TimeProvider? timeProvider = null)
        => new Progress(console, timeProvider) { AutoRefresh = false };

    // ── First encounter (new task added to milestone map) ────────────────────

    [Fact]
    public void Should_Output_Task_Name_On_First_Encounter()
    {
        // Kills L84-87 TryAdvance: first call adds task to map and returns true
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            ctx.AddTask("Loading");
            ctx.Refresh();
        });

        console.Output.ShouldContain("Loading");
    }

    [Fact]
    public void Should_Include_Percentage_In_Output()
    {
        // Kills L115 Markup format string mutation "[blue]{name}[/]: {(int)percentage}%"
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Transfer");
            task.Value = 50;
            ctx.Refresh();
        });

        console.Output.ShouldContain("Transfer");
        console.Output.ShouldContain("50%");
    }

    [Fact]
    public void Should_Include_Zero_Percentage_On_New_Task()
    {
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            ctx.AddTask("Initializing");
            ctx.Refresh();
        });

        console.Output.ShouldContain("0%");
    }

    // ── Skip conditions ───────────────────────────────────────────────────────

    [Fact]
    public void Should_Skip_Task_Not_Started()
    {
        // Kills L33 NoCoverage: !task.IsStarted guard
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            ctx.AddTask("Waiting", autoStart: false);
            ctx.Refresh();
        });

        console.Output.ShouldNotContain("Waiting");
    }

    [Fact]
    public void Should_Skip_Finished_Task()
    {
        // Kills L33 NoCoverage: task.IsFinished guard
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Done");
            task.Value = 100; // IsFinished = true
            ctx.Refresh();
        });

        console.Output.ShouldNotContain("Done");
    }

    [Fact]
    public void Should_Skip_Stopped_Task()
    {
        // Exercises the IsFinished path via StopTask
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Stopped");
            task.StopTask();
            ctx.Refresh();
        });

        console.Output.ShouldNotContain("Stopped");
    }

    // ── Milestone advance ─────────────────────────────────────────────────────

    [Fact]
    public void Should_Output_When_Milestone_Advances()
    {
        // Kills L90-96 TryAdvance milestone advance path
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Upload");
            ctx.Refresh();          // First: new task → outputs 0%
            task.Value = 30;        // > 25% milestone → advance to 50%
            ctx.Refresh();          // Second: milestone advanced → outputs 30%
        });

        console.Output.ShouldContain("30%");
    }

    [Fact]
    public void Should_Not_Output_When_Below_Milestone()
    {
        // Kills L90 percentage > milestone check
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Copy");
            ctx.Refresh();       // First: new task, outputs once (milestone=25)
            task.Value = 10;     // < 25% milestone, no output
            ctx.Refresh();
            task.Value = 20;     // Still < 25%, no output
            ctx.Refresh();
        });

        // "Copy" should appear exactly once (only the first encounter)
        var count = console.Output.Split("Copy").Length - 1;
        count.ShouldBe(1);
    }

    [Fact]
    public void Should_Advance_Through_Multiple_Milestones()
    {
        // Kills L93 nextMilestone != null check and milestone update logic
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("AllMilestones");
            ctx.Refresh();        // 0% → new → output
            task.Value = 26;      // > 25 → advance to 50 → output
            ctx.Refresh();
            task.Value = 51;      // > 50 → advance to 75 → output
            ctx.Refresh();
            task.Value = 76;      // > 75 → advance to 95 → output
            ctx.Refresh();
        });

        var output = console.Output;
        output.ShouldContain("AllMilestones");
        output.ShouldContain("26%");
        output.ShouldContain("51%");
        output.ShouldContain("76%");
    }

    // ── Process: renderable consumed after each refresh ───────────────────────

    [Fact]
    public void Process_Should_Consume_Renderable_On_Each_Refresh()
    {
        // Kills L76 NoCoverage: _renderable = null (renderable should be cleared after each render)
        // After first refresh, second refresh with no milestone change should NOT re-emit the task
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            ctx.AddTask("Consume");
            ctx.Refresh();   // First: outputs
            ctx.Refresh();   // Second: no milestone change → no output
        });

        // "Consume" should appear exactly once
        var count = console.Output.Split("Consume").Length - 1;
        count.ShouldBe(1);
    }

    [Fact]
    public void Process_Should_Return_Empty_When_No_Renderable()
    {
        // Verifies that Process returns without adding when _renderable is null
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Quiet", autoStart: false); // Never started → never output
            ctx.Refresh();
            ctx.Refresh();
        });

        console.Output.ShouldNotContain("Quiet");
    }

    // ── Heartbeat: output after 30 seconds without milestone progress ─────────

    [Fact]
    public void Should_Output_Heartbeat_After_30_Seconds_Without_Milestone_Progress()
    {
        // Kills L47 NoCoverage: hasStartedTasks && updates.Count == 0 && elapsed > 30s
        var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var console = new TestConsole();
        CreateFallbackProgress(console, tp).Start(ctx =>
        {
            var task = ctx.AddTask("Slow");
            ctx.Refresh(); // First: new task → adds to map (milestone=25), outputs

            // task.Value stays at 0 (< milestone=25) → TryAdvance returns false normally
            tp.Advance(TimeSpan.FromSeconds(31));
            ctx.Refresh(); // 31s elapsed since _lastUpdate → heartbeat triggers → outputs again
        });

        // "Slow" should appear in output at least twice (first encounter + heartbeat)
        var count = console.Output.Split("Slow").Length - 1;
        count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Should_Not_Output_Heartbeat_Within_30_Seconds()
    {
        // Kills L47 > vs < mutation: within 30s, no heartbeat
        var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var console = new TestConsole();
        CreateFallbackProgress(console, tp).Start(ctx =>
        {
            var task = ctx.AddTask("Quick");
            ctx.Refresh(); // First: new task → outputs

            tp.Advance(TimeSpan.FromSeconds(29)); // Under the 30s threshold
            ctx.Refresh(); // No heartbeat → no output (milestone not advanced)
        });

        // "Quick" should appear exactly once (only first encounter)
        var count = console.Output.Split("Quick").Length - 1;
        count.ShouldBe(1);
    }

    // ── Multiple tasks ────────────────────────────────────────────────────────

    [Fact]
    public void Should_Output_Multiple_Tasks()
    {
        // Tests the foreach loop in Update
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            ctx.AddTask("Task1");
            ctx.AddTask("Task2");
            ctx.Refresh();
        });

        console.Output.ShouldContain("Task1");
        console.Output.ShouldContain("Task2");
    }

    [Fact]
    public void Should_Update_LastUpdate_When_Output_Produced()
    {
        // Kills L57 NoCoverage: _lastUpdate = _timeProvider.GetLocalNow().LocalDateTime
        // If _lastUpdate is never updated, heartbeat would fire every refresh
        var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var console = new TestConsole();
        CreateFallbackProgress(console, tp).Start(ctx =>
        {
            var task = ctx.AddTask("Track");
            ctx.Refresh(); // Updates _lastUpdate

            // Advance 29s — below heartbeat threshold (lastUpdate was just refreshed)
            tp.Advance(TimeSpan.FromSeconds(29));
            ctx.Refresh(); // No heartbeat since only 29s elapsed

            // Only one appearance (first encounter, no heartbeat in 29s)
        });

        var count = console.Output.Split("Track").Length - 1;
        count.ShouldBe(1);
    }

    // ── BuildTaskGrid ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildTaskGrid_Should_Return_Null_When_No_Updates()
    {
        // Kills L110 NoCoverage: updates.Count > 0 in BuildTaskGrid
        // No output → _renderable = null → Process returns nothing extra
        var console = new TestConsole();
        var initialLength = 0;

        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Silent", autoStart: false);
            ctx.Refresh(); // Not started → skipped → no output
            initialLength = console.Output.Length;
            ctx.Refresh(); // Still no output
        });

        // Output length should be the same before and after second refresh
        console.Output.Length.ShouldBe(initialLength);
    }

    // ── GetNextMilestone ──────────────────────────────────────────────────────

    [Fact]
    public void GetNextMilestone_Should_Return_Null_After_100_Percent()
    {
        // Kills L105 NoCoverage: Array.Find p > percentage — at 100% there's no higher milestone
        // After task reaches 100%, it becomes IsFinished and is skipped
        var console = new TestConsole();
        CreateFallbackProgress(console).Start(ctx =>
        {
            var task = ctx.AddTask("Completing");
            ctx.Refresh();        // New task, milestone=25
            task.Value = 99.5;    // > 99 → advance to 100 (last milestone)
            ctx.Refresh();        // Outputs at 99.5%
            task.Value = 100;     // Finished → will be skipped on next refresh
            ctx.Refresh();        // IsFinished → skipped
        });

        // Task should appear in output (from the milestone advances) but not after completion
        console.Output.ShouldContain("Completing");
    }
}
