using System.Text;
using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

/// <summary>
/// Unit tests for <see cref="PhantomConsoleOutput"/> and its inner <c>PhantomTextWriter</c>.
/// Covers all write methods, encoding, reset, and property accessors.
/// </summary>
public sealed class PhantomConsoleOutputTests
{
    // ── Construction ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_Should_Throw_For_Null_Terminal()
    {
        FluentActions.Invoking(() => new PhantomConsoleOutput(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Initialize_Properties()
    {
        var terminal = new PhantomTerminal(40, 10);
        var output = new PhantomConsoleOutput(terminal);

        output.Terminal.Should().Be(terminal);
        output.Writer.Should().NotBeNull();
        output.IsTerminal.Should().BeTrue();
        output.Width.Should().Be(40);
        output.Height.Should().Be(10);
    }

    // ── Writer.Write(string) ─────────────────────────────────────────

    [Fact]
    public void Writer_Write_String_Should_Capture_And_Process()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("Hello");

        output.RawOutput.Should().Be("Hello");
        terminal.GetRowText(0).Should().Be("Hello");
    }

    [Fact]
    public void Writer_Write_Null_String_Should_Be_Ignored()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write((string?)null);

        output.RawOutput.Should().BeEmpty();
    }

    // ── Writer.Write(char) ───────────────────────────────────────────

    [Fact]
    public void Writer_Write_Char_Should_Capture_And_Process()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write('X');

        output.RawOutput.Should().Be("X");
        terminal.GetRowText(0).Should().Be("X");
    }

    // ── Writer.Write(ReadOnlySpan<char>) ─────────────────────────────

    [Fact]
    public void Writer_Write_Span_Should_Capture_And_Process()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("Span".AsSpan());

        output.RawOutput.Should().Be("Span");
        terminal.GetRowText(0).Should().Be("Span");
    }

    // ── Writer.WriteLine(string) ─────────────────────────────────────

    [Fact]
    public void Writer_WriteLine_String_Should_Append_NewLine()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.WriteLine("Line 1");
        output.Writer.Write("Line 2");

        terminal.GetRowText(0).Should().Be("Line 1");
        terminal.GetRowText(1).Should().Be("Line 2");
    }

    [Fact]
    public void Writer_WriteLine_Null_Should_Only_Write_NewLine()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.WriteLine((string?)null);
        output.Writer.Write("After");

        terminal.GetRowText(0).Should().BeEmpty();
        terminal.GetRowText(1).Should().Be("After");
    }

    // ── Writer.WriteLine() (no args) ─────────────────────────────────

    [Fact]
    public void Writer_WriteLine_Empty_Should_Write_Only_NewLine()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("A");
        output.Writer.WriteLine();
        output.Writer.Write("B");

        terminal.GetRowText(0).Should().Be("A");
        terminal.GetRowText(1).Should().Be("B");
    }

    // ── Writer.Encoding ──────────────────────────────────────────────

    [Fact]
    public void Writer_Encoding_Should_Be_UTF8()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Encoding.Should().Be(Encoding.UTF8);
    }

    // ── SetEncoding (no-op) ──────────────────────────────────────────

    [Fact]
    public void SetEncoding_Should_Not_Throw()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        FluentActions.Invoking(() => output.SetEncoding(Encoding.ASCII)).Should().NotThrow();
        FluentActions.Invoking(() => output.SetEncoding(Encoding.UTF8)).Should().NotThrow();
    }

    // ── RawOutput ────────────────────────────────────────────────────

    [Fact]
    public void RawOutput_Should_Accumulate_All_Writes()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("A");
        output.Writer.Write("B");
        output.Writer.Write("C");

        output.RawOutput.Should().Be("ABC");
    }

    [Fact]
    public void RawOutput_Should_Include_ANSI_Sequences()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("\x1b[1mBold\x1b[0m");

        output.RawOutput.Should().Contain("\x1b[1m");
        output.RawOutput.Should().Contain("\x1b[0m");
    }

    // ── Reset ────────────────────────────────────────────────────────

    [Fact]
    public void Reset_Should_Clear_Raw_Output_And_Reset_Terminal()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("Before");
        output.RawOutput.Should().NotBeEmpty();
        terminal.GetRowText(0).Should().Be("Before");

        output.Reset();

        output.RawOutput.Should().BeEmpty();
        terminal.GetRowText(0).Should().BeEmpty();
        terminal.CursorRow.Should().Be(0);
        terminal.CursorCol.Should().Be(0);
    }

    // ── PhantomConsole Factory ────────────────────────────────────────

    [Fact]
    public void PhantomConsole_Create_Should_Return_Working_Console()
    {
        var (console, output) = PhantomConsole.Create(60, 20);

        console.Should().NotBeNull();
        output.Should().NotBeNull();
        output.Width.Should().Be(60);
        output.Height.Should().Be(20);
    }

    [Fact]
    public void PhantomConsole_Create_Default_Dimensions()
    {
        var (console, output) = PhantomConsole.Create();

        output.Width.Should().Be(80);
        output.Height.Should().Be(24);
    }

    [Fact]
    public void PhantomConsole_CreateInteractive_Should_Return_Working_Console()
    {
        var (console, output) = PhantomConsole.CreateInteractive(50, 15);

        console.Should().NotBeNull();
        output.Should().NotBeNull();
        output.Width.Should().Be(50);
        output.Height.Should().Be(15);
    }

    [Fact]
    public void PhantomConsole_Create_Should_Support_Different_ColorSystems()
    {
        var (console1, _) = PhantomConsole.Create(colorSystem: ColorSystem.Standard);
        var (console2, _) = PhantomConsole.Create(colorSystem: ColorSystem.EightBit);
        var (console3, _) = PhantomConsole.Create(colorSystem: ColorSystem.TrueColor);

        console1.Should().NotBeNull();
        console2.Should().NotBeNull();
        console3.Should().NotBeNull();
    }

    [Fact]
    public void PhantomConsole_Create_Should_Support_NoAnsi()
    {
        var (console, output) = PhantomConsole.Create(ansiSupport: AnsiSupport.No);
        console.Should().NotBeNull();
        output.Should().NotBeNull();
    }

    // ── Multiple write types ─────────────────────────────────────────

    [Fact]
    public void Writer_Mixed_Write_Methods_Should_All_Capture()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write('A');
        output.Writer.Write("BC");
        output.Writer.Write("DE".AsSpan());
        output.Writer.WriteLine("FG");
        output.Writer.WriteLine();

        output.RawOutput.Should().StartWith("ABCDEFG");
        terminal.GetRowText(0).Should().Be("ABCDEFG");
    }
}
