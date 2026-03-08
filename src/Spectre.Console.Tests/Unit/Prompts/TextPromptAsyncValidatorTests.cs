using System.Threading;

namespace Spectre.Console.Tests.Unit;

public sealed class TextPromptAsyncValidatorTests
{
    // -------------------------------------------------------------------------
    // ValidateAsync(Func<T, Task<ValidationResult>>)
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Accept_Input_When_Async_Validator_Succeeds()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("hello");

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync(_ => Task.FromResult(ValidationResult.Success())));

        result.ShouldBe("hello");
    }

    [Fact]
    public void Should_Reject_Input_And_Retry_When_Async_Validator_Fails()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("bad");
        console.Input.PushTextWithEnter("good");

        var callCount = 0;
        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync(value =>
                {
                    callCount++;
                    var r = value == "good"
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Try again[/]");
                    return Task.FromResult(r);
                }));

        result.ShouldBe("good");
        callCount.ShouldBe(2);
    }

    [Fact]
    public void Should_Display_Custom_Error_Message_From_Async_Validator()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("bad");
        console.Input.PushTextWithEnter("good");

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync(value =>
                    Task.FromResult(value == "good"
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Custom async error[/]"))));

        console.Output.ShouldContain("Custom async error");
    }

    // -------------------------------------------------------------------------
    // ValidateAsync(Func<T, CancellationToken, Task<ValidationResult>>)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Async_Validator()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("ok");

        CancellationToken capturedToken = default;
        using var cts = new CancellationTokenSource();

        await console.PromptAsync(
            new TextPrompt<string>("Enter:")
                .ValidateAsync((_, ct) =>
                {
                    capturedToken = ct;
                    return Task.FromResult(ValidationResult.Success());
                }),
            cts.Token);

        // The token we passed into PromptAsync should have been forwarded
        capturedToken.ShouldBe(cts.Token);
    }

    [Fact]
    public void Should_Accept_Input_When_Async_Validator_With_Token_Succeeds()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("42");

        var result = console.Prompt(
            new TextPrompt<int>("Number:")
                .ValidateAsync((_, _) => Task.FromResult(ValidationResult.Success())));

        result.ShouldBe(42);
    }

    // -------------------------------------------------------------------------
    // ValidateAsync(Func<T, Task<bool>>, string?)
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Accept_Input_When_Bool_Async_Validator_Returns_True()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("positive");

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync(_ => Task.FromResult(true)));

        result.ShouldBe("positive");
    }

    [Fact]
    public void Should_Reject_And_Retry_When_Bool_Async_Validator_Returns_False()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("short");
        console.Input.PushTextWithEnter("long enough");

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync(v => Task.FromResult(v.Length > 5), "[red]Too short[/]"));

        result.ShouldBe("long enough");
    }

    [Fact]
    public void Should_Use_Custom_Error_Message_From_Bool_Async_Validator()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("x");
        console.Input.PushTextWithEnter("valid");

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync(v => Task.FromResult(v.Length > 3), "[red]Too short async[/]"));

        console.Output.ShouldContain("Too short async");
    }

    // -------------------------------------------------------------------------
    // ValidateAsync(Func<T, CancellationToken, Task<bool>>, string?)
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_Accept_Input_When_Bool_Async_Validator_With_Token_Returns_True()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("hello");

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync((_, _) => Task.FromResult(true)));

        result.ShouldBe("hello");
    }

    [Fact]
    public void Should_Reject_And_Retry_When_Bool_Async_Validator_With_Token_Returns_False()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("no");
        console.Input.PushTextWithEnter("yes");

        var result = console.Prompt(
            new TextPrompt<string>("Enter:")
                .ValidateAsync((v, _) => Task.FromResult(v == "yes"), "[red]Must say yes[/]"));

        result.ShouldBe("yes");
    }

    // -------------------------------------------------------------------------
    // Async validator takes precedence over sync validator
    // -------------------------------------------------------------------------

    [Fact]
    public void Async_Validator_Takes_Precedence_Over_Sync_Validator()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("test");

        var syncCalled = false;
        var asyncCalled = false;

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .Validate(v =>
                {
                    syncCalled = true;
                    return ValidationResult.Success();
                })
                .ValidateAsync(v =>
                {
                    asyncCalled = true;
                    return Task.FromResult(ValidationResult.Success());
                }));

        asyncCalled.ShouldBeTrue();
        syncCalled.ShouldBeFalse();
    }

    [Fact]
    public void Sync_Validator_Is_Used_When_Async_Validator_Is_Not_Set()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("test");

        var syncCalled = false;

        console.Prompt(
            new TextPrompt<string>("Enter:")
                .Validate(v =>
                {
                    syncCalled = true;
                    return ValidationResult.Success();
                }));

        syncCalled.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Null guards
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateAsync_Func_Task_ValidationResult_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() =>
            ((TextPrompt<string>)null!).ValidateAsync(_ => Task.FromResult(ValidationResult.Success())));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("obj");
    }

    [Fact]
    public void ValidateAsync_Func_Task_ValidationResult_Should_Throw_For_Null_Validator()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var ex = Record.Exception(() =>
            prompt.ValidateAsync((Func<string, Task<ValidationResult>>)null!));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("validator");
    }

    [Fact]
    public void ValidateAsync_Func_CT_Task_ValidationResult_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() =>
            ((TextPrompt<string>)null!).ValidateAsync((_, _) => Task.FromResult(ValidationResult.Success())));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("obj");
    }

    [Fact]
    public void ValidateAsync_Func_CT_Task_ValidationResult_Should_Throw_For_Null_Validator()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var ex = Record.Exception(() =>
            prompt.ValidateAsync((Func<string, CancellationToken, Task<ValidationResult>>)null!));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("validator");
    }

    [Fact]
    public void ValidateAsync_Bool_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() =>
            ((TextPrompt<string>)null!).ValidateAsync(_ => Task.FromResult(true)));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("obj");
    }

    [Fact]
    public void ValidateAsync_Bool_Should_Throw_For_Null_Validator()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var ex = Record.Exception(() =>
            prompt.ValidateAsync((Func<string, Task<bool>>)null!));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("validator");
    }

    [Fact]
    public void ValidateAsync_Bool_CT_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() =>
            ((TextPrompt<string>)null!).ValidateAsync((_, _) => Task.FromResult(true)));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("obj");
    }

    [Fact]
    public void ValidateAsync_Bool_CT_Should_Throw_For_Null_Validator()
    {
        var prompt = new TextPrompt<string>("Enter:");
        var ex = Record.Exception(() =>
            prompt.ValidateAsync((Func<string, CancellationToken, Task<bool>>)null!));
        ex.ShouldBeOfType<ArgumentNullException>().ParamName.ShouldBe("validator");
    }
}
