namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests for <see cref="SelectionPrompt{T}.ToRenderable"/> — static snapshot rendering
/// without any user interaction.
/// </summary>
public sealed class ToRenderableTests
{
    [Fact]
    public void Should_Return_Non_Null_Renderable()
    {
        var prompt = new SelectionPrompt<string>()
            .AddChoices("A", "B", "C");

        var renderable = prompt.ToRenderable();

        renderable.Should().NotBeNull();
    }

    [Fact]
    public void Should_Return_Empty_Text_For_Empty_Prompt()
    {
        var prompt = new SelectionPrompt<string>();

        var renderable = prompt.ToRenderable();

        renderable.Should().BeSameAs(Text.Empty);
    }

    [Fact]
    public void Should_Render_Without_Throwing_With_Null_Console()
    {
        // ToRenderable(null) should fall back to AnsiConsole.Console without throwing.
        var prompt = new SelectionPrompt<string>()
            .AddChoices("X", "Y");

        var act = () => prompt.ToRenderable(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Should_Render_First_Item_Highlighted_At_Cursor_Zero()
    {
        var console = new TestConsole();
        console.EmitAnsiSequences();

        var prompt = new SelectionPrompt<string>()
            .AddChoices("First", "Second", "Third");

        var renderable = prompt.ToRenderable(console, cursorIndex: 0);
        console.Write(renderable);

        // The arrow indicator should appear next to "First"
        console.Output.Should().Contain("> First");
    }

    [Fact]
    public void Should_Render_Second_Item_Highlighted_At_Cursor_One()
    {
        var console = new TestConsole();
        console.EmitAnsiSequences();

        var prompt = new SelectionPrompt<string>()
            .AddChoices("First", "Second", "Third");

        var renderable = prompt.ToRenderable(console, cursorIndex: 1);
        console.Write(renderable);

        console.Output.Should().Contain("> Second");
    }

    [Fact]
    public void Should_Clamp_Out_Of_Range_Cursor_Index()
    {
        // cursorIndex = 99 should clamp to last item (index 2 → "Third")
        var console = new TestConsole();
        console.EmitAnsiSequences();

        var prompt = new SelectionPrompt<string>()
            .AddChoices("First", "Second", "Third");

        var renderable = prompt.ToRenderable(console, cursorIndex: 99);
        console.Write(renderable);

        console.Output.Should().Contain("> Third");
    }

    [Fact]
    public void Should_Clamp_Negative_Cursor_Index_To_Zero()
    {
        var console = new TestConsole();
        console.EmitAnsiSequences();

        var prompt = new SelectionPrompt<string>()
            .AddChoices("First", "Second", "Third");

        var renderable = prompt.ToRenderable(console, cursorIndex: -5);
        console.Write(renderable);

        console.Output.Should().Contain("> First");
    }

    [Fact]
    public void Should_Include_Title_In_Output()
    {
        var console = new TestConsole();

        var prompt = new SelectionPrompt<string>()
            .Title("Choose wisely")
            .AddChoices("A", "B");

        var renderable = prompt.ToRenderable(console);
        console.Write(renderable);

        console.Output.Should().Contain("Choose wisely");
    }

    [Fact]
    public void Should_Accept_Explicit_Console_Instance()
    {
        var console = new TestConsole();

        var prompt = new SelectionPrompt<string>()
            .AddChoices("A", "B", "C");

        // Passing an explicit console should not throw and should produce output.
        var renderable = prompt.ToRenderable(console, cursorIndex: 0);
        var act = () => console.Write(renderable);

        act.Should().NotThrow();
    }

    [Fact]
    public void Should_Work_With_Value_Type_Choices()
    {
        var console = new TestConsole();
        console.EmitAnsiSequences();

        var prompt = new SelectionPrompt<int>()
            .AddChoices(10, 20, 30);

        var renderable = prompt.ToRenderable(console, cursorIndex: 1);
        console.Write(renderable);

        console.Output.Should().Contain("> 20");
    }
}

/// <summary>
/// Tests for <see cref="SelectionPrompt{T}.AsRenderable"/> and the resulting
/// <see cref="SelectionPromptRenderable{T}"/> interactive wrapper.
/// </summary>
public sealed class SelectionPromptRenderableTests
{
    // ─── AsRenderable factory ────────────────────────────────────────────────

    [Fact]
    public void AsRenderable_Should_Throw_For_Empty_Prompt()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        var ex = Record.Exception(() =>
            new SelectionPrompt<string>().AsRenderable(console));

        ex.Should().BeOfType<InvalidOperationException>()
              .Which.Message.Should().Contain("AddChoice");
    }

    [Fact]
    public void AsRenderable_Should_Return_Non_Null_Renderable()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        var renderable = new SelectionPrompt<string>()
            .AddChoices("A", "B", "C")
            .AsRenderable(console);

        renderable.Should().NotBeNull();
    }

    [Fact]
    public void AsRenderable_With_Null_Console_Falls_Back_To_Default()
    {
        var act = () => new SelectionPrompt<string>()
            .AddChoices("A", "B")
            .AsRenderable(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void AsRenderable_Should_Respect_DefaultValue()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        var renderable = new SelectionPrompt<string>()
            .AddChoices("First", "Second", "Third")
            .DefaultValue("Second")
            .AsRenderable(console);

        // Without any Update() calls, submitting should yield the default.
        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        renderable.GetResult().Should().Be("Second");
    }

    // ─── IsDone / IsCancelled initial state ────────────────────────────────

    [Fact]
    public void IsDone_Should_Be_False_Initially()
    {
        var renderable = CreateRenderable("A", "B", "C");

        renderable.IsDone.Should().BeFalse();
    }

    [Fact]
    public void IsCancelled_Should_Be_False_Initially()
    {
        var renderable = CreateRenderable("A", "B", "C");

        renderable.IsCancelled.Should().BeFalse();
    }

    // ─── Update() with Submit ───────────────────────────────────────────────

    [Fact]
    public void Update_Enter_Should_Set_IsDone_True()
    {
        var renderable = CreateRenderable("A", "B", "C");

        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        renderable.IsDone.Should().BeTrue();
    }

    [Fact]
    public void Update_Enter_Should_Return_True()
    {
        var renderable = CreateRenderable("A", "B", "C");

        var changed = renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        changed.Should().BeTrue();
    }

    [Fact]
    public void Update_After_Done_Should_Return_False()
    {
        var renderable = CreateRenderable("A", "B", "C");
        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo()); // done

        var changed = renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo());

        changed.Should().BeFalse();
    }

    [Fact]
    public void Update_After_Done_Should_Not_Change_IsDone()
    {
        var renderable = CreateRenderable("A", "B", "C");
        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo());
        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        // Still done; no re-entry.
        renderable.IsDone.Should().BeTrue();
    }

    // ─── Navigation ─────────────────────────────────────────────────────────

    [Fact]
    public void Update_DownArrow_Should_Return_True()
    {
        var renderable = CreateRenderable("A", "B", "C");

        var changed = renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo());

        changed.Should().BeTrue();
    }

    [Fact]
    public void Navigate_Then_Submit_Should_Return_Navigated_Item()
    {
        var renderable = CreateRenderable("First", "Second", "Third");

        renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo()); // move to "Second"
        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        renderable.GetResult().Should().Be("Second");
    }

    [Fact]
    public void Navigate_Multiple_Times_Then_Submit()
    {
        var renderable = CreateRenderable("A", "B", "C");

        renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo()); // B
        renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo()); // C
        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        renderable.GetResult().Should().Be("C");
    }

    [Fact]
    public void Update_UpArrow_At_First_Item_Should_Not_Crash()
    {
        var renderable = CreateRenderable("A", "B", "C");

        // No wrap-around, so UpArrow at index 0 should be a no-op.
        var changed = renderable.Update(ConsoleKey.UpArrow.ToConsoleKeyInfo());

        changed.Should().BeFalse();
        renderable.IsDone.Should().BeFalse();
    }

    // ─── Cancel / Abort ──────────────────────────────────────────────────────

    [Fact]
    public void Escape_With_Cancel_Result_Should_Set_IsDone_And_IsCancelled()
    {
        var renderable = CreateRenderableWithCancel("A", "B", "C", cancelValue: "X");

        renderable.Update(ConsoleKey.Escape.ToConsoleKeyInfo());

        renderable.IsDone.Should().BeTrue();
        renderable.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Escape_With_Cancel_Result_GetResult_Should_Return_Cancel_Value()
    {
        var renderable = CreateRenderableWithCancel("A", "B", "C", cancelValue: "CANCEL");

        renderable.Update(ConsoleKey.Escape.ToConsoleKeyInfo());

        renderable.GetResult().Should().Be("CANCEL");
    }

    [Fact]
    public void Escape_Without_Cancel_Result_Should_Not_Finish()
    {
        // When no CancelResult is configured, Escape is ignored by HandleInput.
        var renderable = CreateRenderable("A", "B", "C");

        renderable.Update(ConsoleKey.Escape.ToConsoleKeyInfo());

        renderable.IsDone.Should().BeFalse();
        renderable.IsCancelled.Should().BeFalse();
    }

    // ─── GetResult() ─────────────────────────────────────────────────────────

    [Fact]
    public void GetResult_Before_Done_Should_Throw()
    {
        var renderable = CreateRenderable("A", "B", "C");

        var ex = Record.Exception(() => renderable.GetResult());

        ex.Should().BeOfType<InvalidOperationException>()
              .Which.Message.Should().Contain("IsDone");
    }

    [Fact]
    public void GetResult_After_Submit_Should_Return_Current_Item()
    {
        var renderable = CreateRenderable("Alpha", "Beta", "Gamma");

        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        renderable.GetResult().Should().Be("Alpha");
    }

    [Fact]
    public void GetResult_With_Value_Types()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        var renderable = new SelectionPrompt<int>()
            .AddChoices(10, 20, 30)
            .AsRenderable(console);

        renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo()); // 20
        renderable.Update(ConsoleKey.Enter.ToConsoleKeyInfo());

        renderable.GetResult().Should().Be(20);
    }

    // ─── IRenderable implementation ──────────────────────────────────────────

    [Fact]
    public void Render_Should_Produce_Output()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        var renderable = new SelectionPrompt<string>()
            .AddChoices("First", "Second")
            .AsRenderable(console);

        console.Write(renderable);

        console.Output.Should().NotBeEmpty();
    }

    [Fact]
    public void Render_Changes_After_Navigation()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.EmitAnsiSequences();

        var renderable = new SelectionPrompt<string>()
            .AddChoices("Alpha", "Beta")
            .AsRenderable(console);

        // Snapshot before navigation
        console.Write(renderable);
        var before = console.Output;

        // Navigate down and snapshot again using a fresh console
        var console2 = new TestConsole();
        console2.Profile.Capabilities.Interactive = true;
        console2.EmitAnsiSequences();

        renderable.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo());
        console2.Write(renderable);
        var after = console2.Output;

        // Before: Alpha highlighted; After: Beta highlighted
        before.Should().Contain("> Alpha");
        after.Should().Contain("> Beta");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static SelectionPromptRenderable<string> CreateRenderable(params string[] choices)
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        return new SelectionPrompt<string>()
            .AddChoices(choices)
            .AsRenderable(console);
    }

    private static SelectionPromptRenderable<string> CreateRenderableWithCancel(
        string choice1, string choice2, string choice3, string cancelValue)
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        return new SelectionPrompt<string>()
            .AddChoices(choice1, choice2, choice3)
            .AddCancelResult(cancelValue)
            .AsRenderable(console);
    }
}
