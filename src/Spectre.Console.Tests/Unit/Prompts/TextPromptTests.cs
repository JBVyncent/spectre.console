namespace Spectre.Console.Tests.Unit;

[ExpectationPath("Prompts/Text")]
public sealed class TextPromptTests
{
    [Fact]
    public void Should_Return_Entered_Text()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Hello World");

        // When
        var result = console.Prompt(new TextPrompt<string>("Enter text:"));

        // Then
        result.Should().Be("Hello World");
    }

    [Fact]
    [Expectation("ConversionError")]
    public Task Should_Return_Validation_Error_If_Value_Cannot_Be_Converted()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("ninety-nine");
        console.Input.PushTextWithEnter("99");

        // When
        console.Prompt(new TextPrompt<int>("Age?"));

        // Then
        return Verifier.Verify(console.Lines);
    }

    [Fact]
    [Expectation("DefaultValue")]
    public Task Should_Chose_Default_Value_If_Nothing_Is_Entered()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Banana")
                .AddChoice("Orange")
                .DefaultValue("Banana"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("InvalidChoice")]
    public Task Should_Return_Error_If_An_Invalid_Choice_Is_Made()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Apple");
        console.Input.PushTextWithEnter("Banana");

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Banana")
                .AddChoice("Orange")
                .DefaultValue("Banana"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("AcceptChoice")]
    public Task Should_Accept_Choice_In_List()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Orange");

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Banana")
                .AddChoice("Orange")
                .DefaultValue("Banana"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("AutoComplete_Empty")]
    public Task Should_Auto_Complete_To_First_Choice_If_Pressing_Tab_On_Empty_String()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Tab);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Banana")
                .AddChoice("Orange")
                .DefaultValue("Banana"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("AutoComplete_BestMatch")]
    public Task Should_Auto_Complete_To_Best_Match()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushText("Band");
        console.Input.PushKey(ConsoleKey.Tab);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Banana")
                .AddChoice("Bandana")
                .AddChoice("Orange"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("AutoComplete_NextChoice")]
    public Task Should_Auto_Complete_To_Next_Choice_When_Pressing_Tab_On_A_Match()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushText("Apple");
        console.Input.PushKey(ConsoleKey.Tab);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Apple")
                .AddChoice("Banana")
                .AddChoice("Orange"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("AutoComplete_PreviousChoice")]
    public Task Should_Auto_Complete_To_Previous_Choice_When_Pressing_ShiftTab_On_A_Match()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushText("Ban");
        console.Input.PushKey(ConsoleKey.Tab);
        console.Input.PushKey(ConsoleKey.Tab);
        var shiftTab = new ConsoleKeyInfo((char)ConsoleKey.Tab, ConsoleKey.Tab, true, false, false);
        console.Input.PushKey(shiftTab);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Banana")
                .AddChoice("Bandana")
                .AddChoice("Orange"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("CustomValidation")]
    public Task Should_Return_Error_If_Custom_Validation_Fails()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("22");
        console.Input.PushTextWithEnter("102");
        console.Input.PushTextWithEnter("ABC");
        console.Input.PushTextWithEnter("99");

        // When
        console.Prompt(
            new TextPrompt<int>("Guess number:")
                .ValidationErrorMessage("Invalid input")
                .Validate(age =>
                {
                    if (age < 99)
                    {
                        return ValidationResult.Error("Too low");
                    }
                    else if (age > 99)
                    {
                        return ValidationResult.Error("Too high");
                    }

                    return ValidationResult.Success();
                }));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("CustomConverter")]
    public Task Should_Use_Custom_Converter()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Banana");

        // When
        var result = console.Prompt(
            new TextPrompt<(int, string)>("Favorite fruit?")
                .AddChoice((1, "Apple"))
                .AddChoice((2, "Banana"))
                .WithConverter(testData => testData.Item2));

        // Then
        result.Item1.Should().Be(2);
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("SecretDefaultValue")]
    public Task Should_Choose_Masked_Default_Value_If_Nothing_Is_Entered_And_Prompt_Is_Secret()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .Secret()
                .DefaultValue("Banana"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("SecretValueBackspaceNullMask")]
    public Task Should_Not_Erase_Prompt_Text_On_Backspace_If_Prompt_Is_Secret_And_Mask_Is_Null()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushText("Bananas");
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .Secret(null));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("SecretDefaultValueCustomMask")]
    public Task Should_Choose_Custom_Masked_Default_Value_If_Nothing_Is_Entered_And_Prompt_Is_Secret_And_Mask_Is_Custom()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .Secret('-')
                .DefaultValue("Banana"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("SecretDefaultValueNullMask")]
    public Task Should_Choose_Empty_Masked_Default_Value_If_Nothing_Is_Entered_And_Prompt_Is_Secret_And_Mask_Is_Null()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .Secret(null)
                .DefaultValue("Banana"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("NoSuffix")]
    public Task Should_Not_Append_Questionmark_Or_Colon_If_No_Choices_Are_Set()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Orange");

        // When
        console.Prompt(
            new TextPrompt<string>("Enter command$"));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("DefaultValueStyleNotSet")]
    public Task Uses_default_style_for_default_value_if_no_style_is_set()
    {
        // Given
        var console = new TestConsole
        {
            EmitAnsiSequences = true,
        };
        console.Input.PushTextWithEnter("Input");

        var prompt = new TextPrompt<string>("Enter Value:")
                .ShowDefaultValue()
                .DefaultValue("default");

        // When
        console.Prompt(prompt);

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("DefaultValueStyleSet")]
    public Task Uses_specified_default_value_style()
    {
        // Given
        var console = new TestConsole
        {
            EmitAnsiSequences = true,
        };
        console.Input.PushTextWithEnter("Input");

        var prompt = new TextPrompt<string>("Enter Value:")
                .ShowDefaultValue()
                .DefaultValue("default")
                .DefaultValueStyle(Color.Red);

        // When
        console.Prompt(prompt);

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("ChoicesStyleNotSet")]
    public Task Uses_default_style_for_choices_if_no_style_is_set()
    {
        // Given
        var console = new TestConsole
        {
            EmitAnsiSequences = true,
        };
        console.Input.PushTextWithEnter("Choice 2");

        var prompt = new TextPrompt<string>("Enter Value:")
                .ShowChoices()
                .AddChoice("Choice 1")
                .AddChoice("Choice 2");

        // When
        console.Prompt(prompt);

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("ChoicesStyleSet")]
    public Task Uses_the_specified_choices_style()
    {
        // Given
        var console = new TestConsole
        {
            EmitAnsiSequences = true,
        };
        console.Input.PushTextWithEnter("Choice 2");

        var prompt = new TextPrompt<string>("Enter Value:")
                .ShowChoices()
                .AddChoice("Choice 1")
                .AddChoice("Choice 2")
                .ChoicesStyle(Color.Red);

        // When
        console.Prompt(prompt);

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("ClearOnFinish")]
    public Task Should_Clear_Prompt_Line_When_ClearOnFinish_Is_Enabled()
    {
        // Given
        var console = new TestConsole
        {
            EmitAnsiSequences = true,
        };
        console.Input.PushTextWithEnter("secret-value");

        // When
        console.Prompt(
            new TextPrompt<string>("Enter a value")
                .Secret()
                .ClearOnFinish());

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    public void Should_Accept_Null_From_Nullable_Type_Converter_When_AllowEmpty()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        // When / Then (should not show "Invalid input" — regression for #714)
        var result = console.Prompt(
            new TextPrompt<Uri?>("Enter URI:")
                .AllowEmpty());

        result.Should().BeNull();
        console.Output.Should().NotContain("Invalid input");
    }

    [Fact]
    public void Should_Not_Throw_When_Choice_Contains_Square_Brackets()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("[01]");

        // When / Then (should not throw — regression for markup injection bug #1181)
        var result = console.Prompt(
            new TextPrompt<string>("Pick one:")
                .AddChoice("[01]")
                .AddChoice("[02]"));

        result.Should().Be("[01]");
    }

    [Fact]
    public void Should_Not_Throw_When_Default_Value_Contains_Square_Brackets()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        // When / Then (should not throw — regression for markup injection bug #1181)
        var result = console.Prompt(
            new TextPrompt<string>("Pick one:")
                .AddChoice("[01]")
                .AddChoice("[02]")
                .DefaultValue("[01]"));

        result.Should().Be("[01]");
    }

    [Fact]
    public void Backspace_Should_Remove_Entire_Surrogate_Pair()
    {
        // Given — push a surrogate pair (😀 = U+1F600 = \uD83D\uDE00) then backspace
        var console = new TestConsole();
        var high = '\uD83D';
        var low = '\uDE00';
        console.Input.PushKey(new ConsoleKeyInfo(high, 0, false, false, false));
        console.Input.PushKey(new ConsoleKeyInfo(low, 0, false, false, false));
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var result = console.Prompt(new TextPrompt<string>("Enter text:").AllowEmpty());

        // Then — both chars of the surrogate pair should be removed, leaving empty string
        result.Should().BeEmpty();
    }

    [Fact]
    public void Backspace_Should_Remove_Surrogate_Pair_But_Keep_Preceding_Text()
    {
        // Given — push "Hi" + 😀 + backspace
        var console = new TestConsole();
        console.Input.PushText("Hi");
        var high = '\uD83D';
        var low = '\uDE00';
        console.Input.PushKey(new ConsoleKeyInfo(high, 0, false, false, false));
        console.Input.PushKey(new ConsoleKeyInfo(low, 0, false, false, false));
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var result = console.Prompt(new TextPrompt<string>("Enter text:"));

        // Then — only the emoji is removed, "Hi" remains
        result.Should().Be("Hi");
    }

    [Fact]
    public void Backspace_After_BMP_Character_Should_Remove_Single_Char()
    {
        // Given — push "abc" + backspace (removes 'c', not a surrogate pair)
        var console = new TestConsole();
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var result = console.Prompt(new TextPrompt<string>("Enter text:"));

        // Then
        result.Should().Be("ab");
    }

    [Fact]
    public void Should_Append_Colon_When_Prompt_Has_No_Colon()
    {
        // Given — prompt text without colon (GitHub #1638)
        var console = new TestConsole();
        console.Input.PushText("test");
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(new TextPrompt<string>("Enter value"));

        // Then — output should include the colon suffix
        console.Output.Should().Contain("Enter value:");
    }

    [Fact]
    public void Should_Not_Double_Colon_When_Prompt_Already_Has_Colon()
    {
        // Given — prompt text already ends with colon
        var console = new TestConsole();
        console.Input.PushText("test");
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(new TextPrompt<string>("Enter value:"));

        // Then — should NOT have double colon
        console.Output.Should().Contain("Enter value:");
        console.Output.Should().NotContain("Enter value::");
    }

    [Fact]
    public void Should_Not_Double_Colon_When_Prompt_Has_Colon_Inside_Markup()
    {
        // Given — colon is the last visible char but inside a markup tag
        var console = new TestConsole();
        console.Input.PushText("test");
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(new TextPrompt<string>("[green]Enter value:[/]"));

        // Then — should NOT have double colon
        console.Output.Should().Contain("Enter value:");
        console.Output.Should().NotContain("Enter value::");
    }

    [Fact]
    public void Should_Append_Colon_When_Prompt_Has_Default_Value()
    {
        // Given — prompt with default value
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(new TextPrompt<string>("Enter value").DefaultValue("foo"));

        // Then — should have colon after the default value display
        console.Output.Should().Contain(":");
    }
}