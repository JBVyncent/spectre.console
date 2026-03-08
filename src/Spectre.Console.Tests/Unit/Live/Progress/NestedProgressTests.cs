namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests for nested progress tasks (#1419): parent/child relationships,
/// IndentLevel, AutoCompleteWithChildren, TaskDescriptionColumn indentation,
/// and flat-list ordering.
/// </summary>
public sealed class NestedProgressTests
{
    private static Progress MakeProgress(TestConsole? console = null)
    {
        var c = console ?? new TestConsole().Interactive();
        return new Progress(c) { AutoRefresh = false };
    }

    // ─── Parent / Child references ───────────────────────────────────────────

    [Fact]
    public void AddChildTask_Should_Set_Parent_On_Child()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var child = ctx.AddChildTask(parent, "Child");

            child.Parent.ShouldBeSameAs(parent);
        });
    }

    [Fact]
    public void AddChildTask_Should_Add_Child_To_Parent_Children()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var child = ctx.AddChildTask(parent, "Child");

            parent.Children.ShouldContain(child);
        });
    }

    [Fact]
    public void AddChildTask_Multiple_Children_All_Appear_In_Children_Collection()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var c1 = ctx.AddChildTask(parent, "Child1");
            var c2 = ctx.AddChildTask(parent, "Child2");

            parent.Children.Count.ShouldBe(2);
            parent.Children[0].ShouldBeSameAs(c1);
            parent.Children[1].ShouldBeSameAs(c2);
        });
    }

    [Fact]
    public void Parent_Should_Be_Null_For_Root_Task()
    {
        MakeProgress().Start(ctx =>
        {
            var root = ctx.AddTask("Root");

            root.Parent.ShouldBeNull();
        });
    }

    [Fact]
    public void Children_Should_Be_Empty_For_Leaf_Task()
    {
        MakeProgress().Start(ctx =>
        {
            var leaf = ctx.AddTask("Leaf");

            leaf.Children.ShouldBeEmpty();
        });
    }

    [Fact]
    public void AddChildTask_Grandchild_Should_Set_Parent_To_Direct_Child()
    {
        MakeProgress().Start(ctx =>
        {
            var root = ctx.AddTask("Root");
            var child = ctx.AddChildTask(root, "Child");
            var grandchild = ctx.AddChildTask(child, "Grandchild");

            grandchild.Parent.ShouldBeSameAs(child);
            child.Children.ShouldContain(grandchild);
            root.Children.ShouldNotContain(grandchild);
        });
    }

    [Fact]
    public void AddChildTask_Returns_Task_With_Correct_Description()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var child = ctx.AddChildTask(parent, "My Child");

            child.Description.ShouldBe("My Child");
        });
    }

    [Fact]
    public void AddChildTask_With_Settings_Should_Respect_MaxValue()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var child = ctx.AddChildTask(parent, "Child", new ProgressTaskSettings { MaxValue = 50 });

            child.MaxValue.ShouldBe(50);
        });
    }

    [Fact]
    public void AddChildTask_With_Settings_Should_Respect_AutoStart_False()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var child = ctx.AddChildTask(parent, "Child", new ProgressTaskSettings { AutoStart = false });

            child.IsStarted.ShouldBeFalse();
        });
    }

    // ─── IndentLevel ─────────────────────────────────────────────────────────

    [Fact]
    public void IndentLevel_Should_Be_Zero_For_Root_Task()
    {
        MakeProgress().Start(ctx =>
        {
            var root = ctx.AddTask("Root");

            root.IndentLevel.ShouldBe(0);
        });
    }

    [Fact]
    public void IndentLevel_Should_Be_One_For_Direct_Child()
    {
        MakeProgress().Start(ctx =>
        {
            var root = ctx.AddTask("Root");
            var child = ctx.AddChildTask(root, "Child");

            child.IndentLevel.ShouldBe(1);
        });
    }

    [Fact]
    public void IndentLevel_Should_Be_Two_For_Grandchild()
    {
        MakeProgress().Start(ctx =>
        {
            var root = ctx.AddTask("Root");
            var child = ctx.AddChildTask(root, "Child");
            var grandchild = ctx.AddChildTask(child, "Grandchild");

            grandchild.IndentLevel.ShouldBe(2);
        });
    }

    [Fact]
    public void IndentLevel_Sibling_Children_Have_Same_Level()
    {
        MakeProgress().Start(ctx =>
        {
            var root = ctx.AddTask("Root");
            var c1 = ctx.AddChildTask(root, "C1");
            var c2 = ctx.AddChildTask(root, "C2");

            c1.IndentLevel.ShouldBe(1);
            c2.IndentLevel.ShouldBe(1);
        });
    }

    // ─── Flat-list ordering ──────────────────────────────────────────────────

    [Fact]
    public void AddChildTask_Should_Be_Inserted_After_Parent()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var sibling = ctx.AddTask("Sibling");
            var child = ctx.AddChildTask(parent, "Child");

            var tasks = ctx.GetTasks();
            tasks[0].ShouldBeSameAs(parent);
            tasks[1].ShouldBeSameAs(child);
            tasks[2].ShouldBeSameAs(sibling);
        });
    }

    [Fact]
    public void Second_Child_Should_Be_Inserted_After_First_Child()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            var sibling = ctx.AddTask("Sibling");
            var c1 = ctx.AddChildTask(parent, "Child1");
            var c2 = ctx.AddChildTask(parent, "Child2");

            var tasks = ctx.GetTasks();
            tasks[0].ShouldBeSameAs(parent);
            tasks[1].ShouldBeSameAs(c1);
            tasks[2].ShouldBeSameAs(c2);
            tasks[3].ShouldBeSameAs(sibling);
        });
    }

    [Fact]
    public void Grandchild_Should_Be_Inserted_After_Its_Parent_Child()
    {
        MakeProgress().Start(ctx =>
        {
            var root = ctx.AddTask("Root");
            var child = ctx.AddChildTask(root, "Child");
            var grandchild = ctx.AddChildTask(child, "Grandchild");
            var child2 = ctx.AddChildTask(root, "Child2");

            var tasks = ctx.GetTasks();
            // Expected order: Root → Child → Grandchild → Child2
            tasks[0].ShouldBeSameAs(root);
            tasks[1].ShouldBeSameAs(child);
            tasks[2].ShouldBeSameAs(grandchild);
            tasks[3].ShouldBeSameAs(child2);
        });
    }

    [Fact]
    public void AddChildTask_To_Parent_Not_In_Context_Should_Throw()
    {
        MakeProgress().Start(ctx =>
        {
            var orphan = new ProgressTask(999, "Orphan", 100);
            var ex = Record.Exception(() => ctx.AddChildTask(orphan, "Child"));

            ex.ShouldBeOfType<InvalidOperationException>()
              .Message.ShouldContain("does not belong");
        });
    }

    [Fact]
    public void AddChildTask_Null_Parent_Should_Throw()
    {
        MakeProgress().Start(ctx =>
        {
            var ex = Record.Exception(() => ctx.AddChildTask(null!, "Child"));
            ex.ShouldBeOfType<ArgumentNullException>();
        });
    }

    // ─── AutoCompleteWithChildren ────────────────────────────────────────────

    [Fact]
    public void AutoCompleteWithChildren_Default_Should_Be_False()
    {
        var task = new ProgressTask(1, "Task", 100);
        task.AutoCompleteWithChildren.ShouldBeFalse();
    }

    [Fact]
    public void AutoCompleteWithChildren_False_Parent_Stays_Running_When_Children_Done()
    {
        // When AutoCompleteWithChildren = false (default), finishing children
        // should not auto-finish the parent.
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            parent.AutoCompleteWithChildren = false;
            var child = ctx.AddChildTask(parent, "Child");

            child.Value = child.MaxValue; // finish child

            // GetTasks() triggers PropagateAutoComplete — but flag is false.
            ctx.GetTasks();

            parent.IsFinished.ShouldBeFalse();
        });
    }

    [Fact]
    public void AutoCompleteWithChildren_Should_Complete_Parent_When_All_Children_Done()
    {
        // Kills AutoCompleteWithChildren guard mutations.
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            parent.AutoCompleteWithChildren = true;
            var c1 = ctx.AddChildTask(parent, "Child1");
            var c2 = ctx.AddChildTask(parent, "Child2");

            c1.Value = c1.MaxValue;
            c2.Value = c2.MaxValue;

            // GetTasks drives auto-completion.
            ctx.GetTasks();

            parent.IsFinished.ShouldBeTrue();
        });
    }

    [Fact]
    public void AutoCompleteWithChildren_Should_Not_Complete_Parent_While_Some_Children_Running()
    {
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            parent.AutoCompleteWithChildren = true;
            var c1 = ctx.AddChildTask(parent, "Child1");
            var c2 = ctx.AddChildTask(parent, "Child2");

            c1.Value = c1.MaxValue; // only one done
            ctx.GetTasks();

            parent.IsFinished.ShouldBeFalse();
        });
    }

    [Fact]
    public void AutoCompleteWithChildren_Should_Not_Trigger_For_Childless_Parent()
    {
        // A parent with no children and AutoCompleteWithChildren=true must NOT
        // auto-complete (guards against the edge case where Children.All() on an
        // empty sequence returns true).
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            parent.AutoCompleteWithChildren = true;

            ctx.GetTasks();

            parent.IsFinished.ShouldBeFalse();
        });
    }

    [Fact]
    public void AutoCompleteWithChildren_Already_Finished_Parent_Is_Not_Stopped_Again()
    {
        // Ensure PropagateAutoComplete skips already-finished parents gracefully.
        MakeProgress().Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            parent.AutoCompleteWithChildren = true;
            var child = ctx.AddChildTask(parent, "Child");

            child.Value = child.MaxValue;
            ctx.GetTasks(); // first propagation — parent stops
            ctx.GetTasks(); // second call — should be idempotent

            parent.IsFinished.ShouldBeTrue();
        });
    }

    // ─── TaskDescriptionColumn indentation ───────────────────────────────────

    [Fact]
    public void TaskDescriptionColumn_Should_Not_Indent_Root_Task()
    {
        var console = new TestConsole().Width(80).Interactive();
        var progress = new Progress(console)
            .Columns(new TaskDescriptionColumn())
            .AutoRefresh(false)
            .AutoClear(true);

        progress.Start(ctx =>
        {
            ctx.AddTask("RootTask");
        });

        console.Output.ShouldContain("RootTask");
        console.Output.ShouldNotContain("  RootTask");
    }

    [Fact]
    public void TaskDescriptionColumn_Should_Indent_Child_Task_By_Two_Spaces()
    {
        var console = new TestConsole().Width(80).Interactive();
        var progress = new Progress(console)
            .Columns(new TaskDescriptionColumn())
            .AutoRefresh(false)
            .AutoClear(true);

        progress.Start(ctx =>
        {
            var parent = ctx.AddTask("Parent");
            ctx.AddChildTask(parent, "ChildTask");
        });

        console.Output.ShouldContain("  ChildTask");
    }

    [Fact]
    public void TaskDescriptionColumn_Should_Indent_Grandchild_Task_By_Four_Spaces()
    {
        var console = new TestConsole().Width(80).Interactive();
        var progress = new Progress(console)
            .Columns(new TaskDescriptionColumn())
            .AutoRefresh(false)
            .AutoClear(true);

        progress.Start(ctx =>
        {
            var root = ctx.AddTask("Root");
            var child = ctx.AddChildTask(root, "Child");
            ctx.AddChildTask(child, "GrandchildTask");
        });

        console.Output.ShouldContain("    GrandchildTask");
    }
}
