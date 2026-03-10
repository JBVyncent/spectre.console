using Spectre.Console.Phantom.Runner;

namespace Spectre.Console.Phantom.Tests.Runner;

/// <summary>
/// End-to-end tests for the PhantomRunner using the Gallery demo application.
/// These tests launch the real Gallery.exe inside a ConPTY pseudo-terminal,
/// send keystrokes, and assert on the rendered screen state.
/// </summary>
/// <remarks>
/// All Gallery tests are currently skipped because Spectre.Console's SelectionPrompt
/// detects a non-interactive terminal under ConPTY. Needs SPECTRE_CONSOLE_FORCE_INTERACTIVE
/// env var support or ConPTY console mode fixes to enable.
/// </remarks>
[Trait("Category", "Integration")]
public sealed class PhantomRunnerTests
{
    private const string SkipReason =
        "Gallery uses SelectionPrompt which detects non-interactive terminal under ConPTY. " +
        "Needs SPECTRE_CONSOLE_FORCE_INTERACTIVE env var or ConPTY console mode fixes.";

    private static readonly string GalleryExe = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "demos", "Gallery", "bin", "Release", "net10.0", "Gallery.exe"));

    [Fact(Skip = SkipReason)]
    public async Task Should_Launch_Gallery_And_See_Menu()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        runner.AssertScreenContains("Tables");
        runner.AssertScreenContains("Markup");
        runner.AssertScreenContains("Exit");
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Navigate_To_Tables_And_See_Demo_Complete()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Tables");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Navigate_To_Rules_Demo()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Rules");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Navigate_To_Charts_Demo()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Charts");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Navigate_To_Exceptions_Demo()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Exceptions");

        // The Exceptions demo intentionally shows exceptions — so we check it completes
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));

        // Don't call AssertNoExceptions here — this demo SHOULD show exception text
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Navigate_To_Unicode_Demo()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Unicode");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Navigate_To_BugFixes_Demo()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Bug Fixes");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(20));
        runner.AssertNoExceptions();
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Exit_Gallery_Cleanly()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Exit");
        await runner.WaitForText("Thanks for exploring");

        var exitCode = await runner.WaitForExitAsync(timeout: TimeSpan.FromSeconds(5));
        exitCode.Should().Be(0);
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Return_To_Menu_After_Demo()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        // Navigate to Rules (a quick, non-interactive demo)
        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Rules");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));

        // Answer "Return to menu?" with yes
        await runner.AnswerConfirmation("Return to menu?", yes: true);

        // Should see the menu again
        await runner.WaitForText("Select a demo");
        runner.AssertScreenContains("Tables");
    }

    [Fact(Skip = SkipReason)]
    public async Task Should_Navigate_To_Markup_Demo()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");
        await runner.NavigateToChoice("Markup");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact(Skip = SkipReason)]
    public async Task Screen_Snapshot_Contains_Figlet_Header()
    {
        await using var runner = PhantomRunner.Launch(GalleryExe, width: 120, height: 50);

        await runner.WaitForText("Select a demo");

        // The FigletText "Gallery" header should be visible
        var snapshot = runner.GetScreenSnapshot();
        snapshot.Should().Contain("Gallery");
    }
}
