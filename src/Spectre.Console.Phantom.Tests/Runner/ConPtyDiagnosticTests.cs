using System.Text;
using Spectre.Console.Phantom.Runner;

namespace Spectre.Console.Phantom.Tests.Runner;

/// <summary>
/// Integration tests for the PhantomRunner ConPTY host subprocess infrastructure.
/// These tests launch real processes inside ConPTY via the Phantom host subprocess
/// and verify that output reading and keystroke input work end-to-end.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConPtyDiagnosticTests
{
    [Fact]
    public async Task PhantomRunner_Reads_Cmd_Echo()
    {
        await using var runner = PhantomRunner.Launch(
            "cmd.exe /c echo HelloPhantom",
            width: 80, height: 24);

        runner.DefaultTimeout = TimeSpan.FromSeconds(5);
        await runner.WaitForText("HelloPhantom");
        runner.AssertScreenContains("HelloPhantom");
    }

    [Fact]
    public async Task PhantomRunner_Reads_Multiple_Lines()
    {
        await using var runner = PhantomRunner.Launch(
            "cmd.exe /c \"echo Line1 && echo Line2 && echo Line3\"",
            width: 80, height: 24);

        runner.DefaultTimeout = TimeSpan.FromSeconds(5);
        await runner.WaitForText("Line3");
        runner.AssertScreenContains("Line1");
        runner.AssertScreenContains("Line2");
        runner.AssertScreenContains("Line3");
    }

    [Fact]
    public async Task PhantomRunner_Can_Send_Keystrokes()
    {
        await using var runner = PhantomRunner.Launch(
            "cmd.exe",
            width: 80, height: 24);

        runner.DefaultTimeout = TimeSpan.FromSeconds(5);

        // Wait for cmd prompt, then send a command
        await runner.WaitForText(">");
        runner.TypeLine("echo InteractiveTest");
        await runner.WaitForText("InteractiveTest");
        runner.AssertScreenContains("InteractiveTest");

        // Exit cmd
        runner.TypeLine("exit");
    }

    [Fact]
    public async Task PhantomRunner_Reads_Gallery()
    {
        var galleryExe = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "demos", "Gallery", "bin", "Release", "net10.0", "Gallery.exe"));

        File.Exists(galleryExe).Should().BeTrue($"Gallery.exe should exist at {galleryExe}");

        await using var runner = PhantomRunner.Launch(
            galleryExe,
            width: 120,
            height: 50,
            environmentVariables: new Dictionary<string, string>
            {
                ["SPECTRE_CONSOLE_FORCE_INTERACTIVE"] = "1",
            });
        runner.DefaultTimeout = TimeSpan.FromSeconds(15);

        await runner.WaitForText("Tables");
        runner.AssertScreenContains("Select a demo");
    }

    [Fact]
    public async Task PhantomRunner_Detects_Process_Exit()
    {
        await using var runner = PhantomRunner.Launch(
            "cmd.exe /c echo done",
            width: 80, height: 24);

        var exitCode = await runner.WaitForExitAsync(TimeSpan.FromSeconds(5));
        runner.HasExited.Should().BeTrue();
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PhantomRunner_Can_Kill_Process()
    {
        await using var runner = PhantomRunner.Launch(
            "cmd.exe",
            width: 80, height: 24);

        runner.DefaultTimeout = TimeSpan.FromSeconds(5);
        await runner.WaitForText(">"); // Wait for prompt to confirm it started

        runner.Kill();

        // Should be able to detect exit after kill
        await Task.Delay(500);
        runner.HasExited.Should().BeTrue();
    }

    [Fact]
    public async Task PhantomRunner_Invalid_Command_Does_Not_Throw_On_Launch()
    {
        // The host launches successfully but the child process fails inside ConPTY.
        // PhantomRunner.Launch itself should not throw since the host starts fine.
        var act = () => PhantomRunner.Launch(
            "nonexistent_program_12345.exe",
            width: 80, height: 24);

        await using var runner = act.Should().NotThrow().Subject;
    }
}
