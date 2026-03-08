namespace Spectre.Console.Tests.Unit;

public sealed class TextPromptPrefillTests
{
    // -------------------------------------------------------------------------
    // Basic behaviour
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Return_Prefilled_Default_When_Enter_Is_Pressed_Immediately()
    {
        // The default value is pre-filled so pressing Enter without typing accepts it.
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        var result = console.Prompt(
            new TextPrompt<string>("Name:")
                .DefaultValue("Alice")
                .PrefillDefaultValue());

        result.ShouldBe("Alice");
    }

    [Fact]
    public void Should_Allow_User_To_Accept_Integer_Prefill()
    {
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        var result = console.Prompt(
            new TextPrompt<int>("Age:")
                .DefaultValue(30)
                .PrefillDefaultValue());

        result.ShouldBe(30);
    }

    [Fact]
    public void Should_Allow_User_To_Edit_Prefilled_Default_By_Backspacing()
    {
        // Pre-fill "Hello", backspace removes 'o', enter accepts "Hell".
        // The TestConsole input must supply: Backspace + Enter.
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Enter);

        var result = console.Prompt(
            new TextPrompt<string>("Word:")
                .DefaultValue("Hello")
                .PrefillDefaultValue());

        result.ShouldBe("Hell");
    }

    [Fact]
    public void Should_Allow_User_To_Append_To_Prefilled_Default()
    {
        // Pre-fill "Hello", user types '!' and presses Enter → "Hello!".
        var console = new TestConsole();
        console.Input.PushText("!");
        console.Input.PushKey(ConsoleKey.Enter);

        var result = console.Prompt(
            new TextPrompt<string>("Word:")
                .DefaultValue("Hello")
                .PrefillDefaultValue());

        result.ShouldBe("Hello!");
    }

    [Fact]
    public void Should_Allow_User_To_Replace_Entire_Prefill()
    {
        // Backspace 5 times over "Hello", then type "World", then Enter.
        var console = new TestConsole();
        for (var i = 0; i < 5; i++)
        {
            console.Input.PushKey(ConsoleKey.Backspace);
        }

        console.Input.PushText("World");
        console.Input.PushKey(ConsoleKey.Enter);

        var result = console.Prompt(
            new TextPrompt<string>("Word:")
                .DefaultValue("Hello")
                .PrefillDefaultValue());

        result.ShouldBe("World");
    }

    // -------------------------------------------------------------------------
    // PrefillDefaultValue without a default has no effect
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Behave_Normally_When_No_Default_Is_Set()
    {
        // PrefillDefaultValue() is a no-op when no DefaultValue is configured.
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Hello");

        var result = console.Prompt(
            new TextPrompt<string>("Name:")
                .PrefillDefaultValue());   // no default — no-op

        result.ShouldBe("Hello");
    }

    // -------------------------------------------------------------------------
    // Display output
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Display_Prefilled_Text_In_Output()
    {
        // The prefilled text must appear in the console output before the user types.
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        console.Prompt(
            new TextPrompt<string>("Name:")
                .DefaultValue("Alice")
                .PrefillDefaultValue());

        console.Output.ShouldContain("Alice");
    }

    // -------------------------------------------------------------------------
    // Prefill with choices: Enter accepts the pre-filled choice
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Accept_Prefilled_Choice_On_Enter()
    {
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        var result = console.Prompt(
            new TextPrompt<string>("Fruit:")
                .AddChoice("Banana")
                .AddChoice("Orange")
                .DefaultValue("Orange")
                .PrefillDefaultValue());

        result.ShouldBe("Orange");
    }

    // -------------------------------------------------------------------------
    // Null guard on extension method
    // -------------------------------------------------------------------------

    [Fact]
    public void PrefillDefaultValue_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() => ((TextPrompt<string>)null!).PrefillDefaultValue());
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("obj");
    }
}
