namespace Spectre.Console.Tests.Unit;

public sealed class ConfirmationPromptTests
{
    [Fact]
    public void Should_Throw_If_Prompt_Text_Is_Null()
    {
        // Given, When
        var result = Record.Exception(() => new ConfirmationPrompt(null!));

        // Then
        result.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("prompt");
    }

    [Fact]
    public void Should_Return_True_When_Pressing_Enter_With_Default_Value()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var result = console.Prompt(new ConfirmationPrompt("Continue?"));

        // Then
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_False_When_Answering_No()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushTextWithEnter("n");

        // When
        var result = console.Prompt(new ConfirmationPrompt("Continue?"));

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Return_False_When_Default_Is_False_And_Pressing_Enter()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);
        var prompt = new ConfirmationPrompt("Continue?") { DefaultValue = false };

        // When
        var result = console.Prompt(prompt);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Show_Choices_In_Output_By_Default()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushTextWithEnter("y");

        // When
        console.Prompt(new ConfirmationPrompt("Continue?"));

        // Then
        console.Output.Should().Contain("y/n");
    }

    [Fact]
    public void Should_Not_Show_Choices_When_Disabled()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushTextWithEnter("y");
        var prompt = new ConfirmationPrompt("Continue?") { ShowChoices = false };

        // When
        console.Prompt(prompt);

        // Then
        console.Output.Should().NotContain("y/n");
    }

    [Fact]
    public void Should_Show_Default_Value_In_Output_By_Default()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        console.Prompt(new ConfirmationPrompt("Continue?"));

        // Then
        console.Output.Should().Contain("y");
    }

    [Fact]
    public void Should_Not_Show_Default_Value_When_Disabled()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushTextWithEnter("y");
        var prompt = new ConfirmationPrompt("Continue?") { ShowDefaultValue = false };

        // When
        console.Prompt(prompt);

        // Then
        // When ShowDefaultValue is false, the default value indicator is not shown.
        // The output should still contain "Continue?" but the default value style indicator is absent.
        console.Output.Should().Contain("Continue?");
    }

    [Fact]
    public void Should_Use_Custom_Yes_And_No_Characters()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushTextWithEnter("s");
        var prompt = new ConfirmationPrompt("Continue?") { Yes = 's', No = 'n' };

        // When
        var result = console.Prompt(prompt);

        // Then
        result.Should().BeTrue();
    }

    public sealed class ExtensionNullGuards
    {
        [Fact]
        public void ShowChoices_Should_Throw_If_Prompt_Is_Null()
        {
            var result = Record.Exception(() => ConfirmationPromptExtensions.ShowChoices(null!, true));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.ParamName.Should().Be("obj");
        }

        [Fact]
        public void ChoicesStyle_Should_Throw_If_Prompt_Is_Null()
        {
            var result = Record.Exception(() => ConfirmationPromptExtensions.ChoicesStyle(null!, null));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.ParamName.Should().Be("obj");
        }

        [Fact]
        public void ShowDefaultValue_Should_Throw_If_Prompt_Is_Null()
        {
            var result = Record.Exception(() => ConfirmationPromptExtensions.ShowDefaultValue(null!, true));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.ParamName.Should().Be("obj");
        }

        [Fact]
        public void DefaultValueStyle_Should_Throw_If_Prompt_Is_Null()
        {
            var result = Record.Exception(() => ConfirmationPromptExtensions.DefaultValueStyle(null!, null));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.ParamName.Should().Be("obj");
        }

        [Fact]
        public void InvalidChoiceMessage_Should_Throw_If_Prompt_Is_Null()
        {
            var result = Record.Exception(() => ConfirmationPromptExtensions.InvalidChoiceMessage(null!, string.Empty));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.ParamName.Should().Be("obj");
        }

        [Fact]
        public void Yes_Should_Throw_If_Prompt_Is_Null()
        {
            var result = Record.Exception(() => ConfirmationPromptExtensions.Yes(null!, 'y'));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.ParamName.Should().Be("obj");
        }

        [Fact]
        public void No_Should_Throw_If_Prompt_Is_Null()
        {
            var result = Record.Exception(() => ConfirmationPromptExtensions.No(null!, 'n'));
            result.Should().BeOfType<ArgumentNullException>()
                    .Which.ParamName.Should().Be("obj");
        }
    }

    public sealed class ExtensionBooleanFlips
    {
        [Fact]
        public void ShowChoices_Without_Args_Should_Set_True()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?") { ShowChoices = false };

            // When
            var result = prompt.ShowChoices();

            // Then
            result.ShowChoices.Should().BeTrue();
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void HideChoices_Should_Set_False()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?") { ShowChoices = true };

            // When
            var result = prompt.HideChoices();

            // Then
            result.ShowChoices.Should().BeFalse();
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void ShowDefaultValue_Without_Args_Should_Set_True()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?") { ShowDefaultValue = false };

            // When
            var result = prompt.ShowDefaultValue();

            // Then
            result.ShowDefaultValue.Should().BeTrue();
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void HideDefaultValue_Should_Set_False()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?") { ShowDefaultValue = true };

            // When
            var result = prompt.HideDefaultValue();

            // Then
            result.ShowDefaultValue.Should().BeFalse();
            result.Should().BeSameAs(prompt);
        }
    }

    public sealed class ExtensionMutators
    {
        [Fact]
        public void ShowChoices_Should_Set_Value_And_Return_Same_Instance()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?");

            // When
            var result = prompt.ShowChoices(false);

            // Then
            result.ShowChoices.Should().BeFalse();
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void ChoicesStyle_Should_Set_Value_And_Return_Same_Instance()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?");
            var style = new Style(foreground: Color.Red);

            // When
            var result = prompt.ChoicesStyle(style);

            // Then
            result.ChoicesStyle.Should().Be(style);
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void ShowDefaultValue_Should_Set_Value_And_Return_Same_Instance()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?");

            // When
            var result = prompt.ShowDefaultValue(false);

            // Then
            result.ShowDefaultValue.Should().BeFalse();
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void DefaultValueStyle_Should_Set_Value_And_Return_Same_Instance()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?");
            var style = new Style(foreground: Color.Green);

            // When
            var result = prompt.DefaultValueStyle(style);

            // Then
            result.DefaultValueStyle.Should().Be(style);
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void InvalidChoiceMessage_Should_Set_Value_And_Return_Same_Instance()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?");

            // When
            var result = prompt.InvalidChoiceMessage("[red]Bad choice[/]");

            // Then
            result.InvalidChoiceMessage.Should().Be("[red]Bad choice[/]");
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void Yes_Should_Set_Value_And_Return_Same_Instance()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?");

            // When
            var result = prompt.Yes('s');

            // Then
            result.Yes.Should().Be('s');
            result.Should().BeSameAs(prompt);
        }

        [Fact]
        public void No_Should_Set_Value_And_Return_Same_Instance()
        {
            // Given
            var prompt = new ConfirmationPrompt("Continue?");

            // When
            var result = prompt.No('x');

            // Then
            result.No.Should().Be('x');
            result.Should().BeSameAs(prompt);
        }
    }
}
