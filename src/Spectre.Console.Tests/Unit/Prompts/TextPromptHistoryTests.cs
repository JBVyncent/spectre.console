namespace Spectre.Console.Tests.Unit;

public sealed class TextPromptHistoryTests
{
    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Recall_Most_Recent_Entry_On_UpArrow()
    {
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var history = new List<string> { "first", "second", "third" };

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        result.ShouldBe("third");
    }

    [Fact]
    public void Should_Navigate_Back_Two_Entries_On_Two_UpArrows()
    {
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var history = new List<string> { "first", "second", "third" };

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        result.ShouldBe("second");
    }

    [Fact]
    public void Should_Navigate_To_Oldest_Entry_On_Maximum_UpArrows()
    {
        var console = new TestConsole();
        for (var i = 0; i < 10; i++)
        {
            console.Input.PushKey(ConsoleKey.UpArrow);
        }

        console.Input.PushKey(ConsoleKey.Enter);

        var history = new List<string> { "first", "second", "third" };

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        result.ShouldBe("first");
    }

    [Fact]
    public void Should_Restore_Live_Text_On_DownArrow_After_History_Navigation()
    {
        // User types "live", navigates up to history, then navigates back down to live text.
        var console = new TestConsole();
        console.Input.PushText("live");
        console.Input.PushKey(ConsoleKey.UpArrow);  // go to "third"
        console.Input.PushKey(ConsoleKey.DownArrow); // back to "live"
        console.Input.PushKey(ConsoleKey.Enter);

        var history = new List<string> { "first", "second", "third" };

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        result.ShouldBe("live");
    }

    [Fact]
    public void Should_Navigate_Forward_Through_History_Entries()
    {
        // Up 3 times → "first"; down → "second"; down → "third"; Enter.
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var history = new List<string> { "first", "second", "third" };

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        result.ShouldBe("third");
    }

    [Fact]
    public void Should_Not_Throw_When_DownArrow_At_Current_Position_With_No_History()
    {
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushTextWithEnter("hello");

        Should.NotThrow(() =>
        {
            var result = console.Prompt(
                new TextPrompt<string>("Enter:")
                    .WithHistory(new List<string>()));
            result.ShouldBe("hello");
        });
    }

    // -------------------------------------------------------------------------
    // History append
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Append_Submitted_Input_To_History()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("hello");

        var history = new List<string>();

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        history.Count.ShouldBe(1);
        history[0].ShouldBe("hello");
    }

    [Fact]
    public void Should_Append_Multiple_Submissions_To_History()
    {
        var history = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var console = new TestConsole();
            console.Input.PushTextWithEnter($"entry{i}");

            console.Prompt(
                new TextPrompt<string>("Enter:")
                    .WithHistory(history));
        }

        history.Count.ShouldBe(3);
        history[0].ShouldBe("entry0");
        history[1].ShouldBe("entry1");
        history[2].ShouldBe("entry2");
    }

    [Fact]
    public void Should_Suppress_Consecutive_Duplicate_Entries()
    {
        var history = new List<string> { "existing" };

        var console = new TestConsole();
        console.Input.PushTextWithEnter("existing");

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        // "existing" should not be duplicated
        history.Count.ShouldBe(1);
        history[0].ShouldBe("existing");
    }

    [Fact]
    public void Should_Allow_Same_Value_If_Not_Consecutive()
    {
        var history = new List<string> { "a", "b" };

        var console = new TestConsole();
        console.Input.PushTextWithEnter("a");

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .WithHistory(history));

        // "a" can appear again because the last entry was "b", not "a"
        history.Count.ShouldBe(3);
        history[^1].ShouldBe("a");
    }

    [Fact]
    public void Should_Not_Add_Empty_Input_To_History_When_Default_Is_Used()
    {
        // Empty input with a default value → default is returned but nothing added to history.
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        var history = new List<string>();

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .DefaultValue("default")
                .WithHistory(history));

        history.Count.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // No-op when history is not set
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Ignore_UpArrow_When_No_History_Is_Set()
    {
        // Without history, UpArrow is a no-op and the user types normally.
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushTextWithEnter("hello");

        var result = console.Prompt(new TextPrompt<string>("Enter:"));

        result.ShouldBe("hello");
    }

    // -------------------------------------------------------------------------
    // Null guards
    // -------------------------------------------------------------------------

    [Fact]
    public void WithHistory_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() => ((TextPrompt<string>)null!).WithHistory(new List<string>()));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("obj");
    }

    [Fact]
    public void WithHistory_Should_Throw_For_Null_History()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var ex = Record.Exception(() => prompt.WithHistory(null!));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("history");
    }
}
