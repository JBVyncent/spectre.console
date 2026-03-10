using Spectre.Console.Phantom.Runner;

namespace Spectre.Console.Phantom.Tests.Runner;

/// <summary>
/// End-to-end tests for the PhantomRunner using the Gallery demo application.
/// These tests launch the real Gallery.exe inside a ConPTY pseudo-terminal,
/// send keystrokes, and assert on the rendered screen state.
/// </summary>
/// <remarks>
/// Gallery uses Spectre.Console's SelectionPrompt which checks
/// <c>Profile.Capabilities.Interactive</c>. Under ConPTY,
/// <c>System.Console.IsInputRedirected</c> returns <c>true</c> (false negative).
/// We set <c>SPECTRE_CONSOLE_FORCE_INTERACTIVE=1</c> to override the detection.
/// </remarks>
[Trait("Category", "Integration")]
public sealed class PhantomRunnerTests
{
    private static readonly string GalleryExe = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "demos", "Gallery", "bin", "Release", "net10.0", "Gallery.exe"));

    private static readonly Dictionary<string, string> ForceInteractive = new()
    {
        ["SPECTRE_CONSOLE_FORCE_INTERACTIVE"] = "1",
    };

    private static PhantomRunner LaunchGallery()
    {
        return PhantomRunner.Launch(
            GalleryExe,
            width: 120,
            height: 50,
            environmentVariables: ForceInteractive);
    }

    [Fact]
    public async Task Should_Launch_Gallery_And_See_Menu()
    {
        await using var runner = LaunchGallery();

        // Wait for menu items to render (not just the header)
        await runner.WaitForText("Tables");
        runner.AssertScreenContains("Select a demo");
        runner.AssertScreenContains("Markup");
        runner.AssertScreenContains("Exit");
    }

    [Fact]
    public async Task Should_Navigate_To_Tables_And_See_Demo_Complete()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Tables");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact]
    public async Task Should_Navigate_To_Rules_Demo()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Rules");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact]
    public async Task Should_Navigate_To_Charts_Demo()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Charts");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact]
    public async Task Should_Navigate_To_Exceptions_Demo()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Exceptions");

        // The Exceptions demo intentionally shows exceptions — so we check it completes
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));

        // Don't call AssertNoExceptions here — this demo SHOULD show exception text
    }

    [Fact]
    public async Task Should_Navigate_To_Unicode_Demo()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Unicode");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact]
    public async Task Should_Navigate_To_BugFixes_Demo()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Bug Fixes");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(20));
        runner.AssertNoExceptions();
    }

    [Fact]
    public async Task Should_Exit_Gallery_Cleanly()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Exit");
        await runner.WaitForText("Thanks for exploring");

        var exitCode = await runner.WaitForExitAsync(timeout: TimeSpan.FromSeconds(5));
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Should_Return_To_Menu_After_Demo()
    {
        await using var runner = LaunchGallery();

        // Navigate to Rules (a quick, non-interactive demo)
        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Rules");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));

        // Answer "Return to menu?" with yes
        await runner.AnswerConfirmation("Return to menu?", yes: true);

        // Should see the menu again
        await runner.WaitForText("Tables");
        runner.AssertScreenContains("Select a demo");
    }

    [Fact]
    public async Task Should_Navigate_To_Markup_Demo()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");
        await runner.NavigateToChoice("Markup");
        await runner.WaitForText("Demo complete", timeout: TimeSpan.FromSeconds(15));
        runner.AssertNoExceptions();
    }

    [Fact]
    public async Task Screen_Snapshot_Contains_Figlet_Header()
    {
        await using var runner = LaunchGallery();

        await runner.WaitForText("Tables");

        // The FigletText "Gallery" header should be visible
        var snapshot = runner.GetScreenSnapshot();
        snapshot.Should().Contain("Gallery");
    }
}
