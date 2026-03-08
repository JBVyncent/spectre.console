using System.Text;
using Shouldly;
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
        Should.Throw<ArgumentNullException>(() => new PhantomConsoleOutput(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_Properties()
    {
        var terminal = new PhantomTerminal(40, 10);
        var output = new PhantomConsoleOutput(terminal);

        output.Terminal.ShouldBe(terminal);
        output.Writer.ShouldNotBeNull();
        output.IsTerminal.ShouldBeTrue();
        output.Width.ShouldBe(40);
        output.Height.ShouldBe(10);
    }

    // ── Writer.Write(string) ─────────────────────────────────────────

    [Fact]
    public void Writer_Write_String_Should_Capture_And_Process()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("Hello");

        output.RawOutput.ShouldBe("Hello");
        terminal.GetRowText(0).ShouldBe("Hello");
    }

    [Fact]
    public void Writer_Write_Null_String_Should_Be_Ignored()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write((string?)null);

        output.RawOutput.ShouldBeEmpty();
    }

    // ── Writer.Write(char) ───────────────────────────────────────────

    [Fact]
    public void Writer_Write_Char_Should_Capture_And_Process()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write('X');

        output.RawOutput.ShouldBe("X");
        terminal.GetRowText(0).ShouldBe("X");
    }

    // ── Writer.Write(ReadOnlySpan<char>) ─────────────────────────────

    [Fact]
    public void Writer_Write_Span_Should_Capture_And_Process()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("Span".AsSpan());

        output.RawOutput.ShouldBe("Span");
        terminal.GetRowText(0).ShouldBe("Span");
    }

    // ── Writer.WriteLine(string) ─────────────────────────────────────

    [Fact]
    public void Writer_WriteLine_String_Should_Append_NewLine()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.WriteLine("Line 1");
        output.Writer.Write("Line 2");

        terminal.GetRowText(0).ShouldBe("Line 1");
        terminal.GetRowText(1).ShouldBe("Line 2");
    }

    [Fact]
    public void Writer_WriteLine_Null_Should_Only_Write_NewLine()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.WriteLine((string?)null);
        output.Writer.Write("After");

        terminal.GetRowText(0).ShouldBeEmpty();
        terminal.GetRowText(1).ShouldBe("After");
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

        terminal.GetRowText(0).ShouldBe("A");
        terminal.GetRowText(1).ShouldBe("B");
    }

    // ── Writer.Encoding ──────────────────────────────────────────────

    [Fact]
    public void Writer_Encoding_Should_Be_UTF8()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Encoding.ShouldBe(Encoding.UTF8);
    }

    // ── SetEncoding (no-op) ──────────────────────────────────────────

    [Fact]
    public void SetEncoding_Should_Not_Throw()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        Should.NotThrow(() => output.SetEncoding(Encoding.ASCII));
        Should.NotThrow(() => output.SetEncoding(Encoding.UTF8));
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

        output.RawOutput.ShouldBe("ABC");
    }

    [Fact]
    public void RawOutput_Should_Include_ANSI_Sequences()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("\x1b[1mBold\x1b[0m");

        output.RawOutput.ShouldContain("\x1b[1m");
        output.RawOutput.ShouldContain("\x1b[0m");
    }

    // ── Reset ────────────────────────────────────────────────────────

    [Fact]
    public void Reset_Should_Clear_Raw_Output_And_Reset_Terminal()
    {
        var terminal = new PhantomTerminal(80, 24);
        var output = new PhantomConsoleOutput(terminal);

        output.Writer.Write("Before");
        output.RawOutput.ShouldNotBeEmpty();
        terminal.GetRowText(0).ShouldBe("Before");

        output.Reset();

        output.RawOutput.ShouldBeEmpty();
        terminal.GetRowText(0).ShouldBeEmpty();
        terminal.CursorRow.ShouldBe(0);
        terminal.CursorCol.ShouldBe(0);
    }

    // ── PhantomConsole Factory ────────────────────────────────────────

    [Fact]
    public void PhantomConsole_Create_Should_Return_Working_Console()
    {
        var (console, output) = PhantomConsole.Create(60, 20);

        console.ShouldNotBeNull();
        output.ShouldNotBeNull();
        output.Width.ShouldBe(60);
        output.Height.ShouldBe(20);
    }

    [Fact]
    public void PhantomConsole_Create_Default_Dimensions()
    {
        var (console, output) = PhantomConsole.Create();

        output.Width.ShouldBe(80);
        output.Height.ShouldBe(24);
    }

    [Fact]
    public void PhantomConsole_CreateInteractive_Should_Return_Working_Console()
    {
        var (console, output) = PhantomConsole.CreateInteractive(50, 15);

        console.ShouldNotBeNull();
        output.ShouldNotBeNull();
        output.Width.ShouldBe(50);
        output.Height.ShouldBe(15);
    }

    [Fact]
    public void PhantomConsole_Create_Should_Support_Different_ColorSystems()
    {
        var (console1, _) = PhantomConsole.Create(colorSystem: ColorSystem.Standard);
        var (console2, _) = PhantomConsole.Create(colorSystem: ColorSystem.EightBit);
        var (console3, _) = PhantomConsole.Create(colorSystem: ColorSystem.TrueColor);

        console1.ShouldNotBeNull();
        console2.ShouldNotBeNull();
        console3.ShouldNotBeNull();
    }

    [Fact]
    public void PhantomConsole_Create_Should_Support_NoAnsi()
    {
        var (console, output) = PhantomConsole.Create(ansiSupport: AnsiSupport.No);
        console.ShouldNotBeNull();
        output.ShouldNotBeNull();
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

        output.RawOutput.ShouldStartWith("ABCDEFG");
        terminal.GetRowText(0).ShouldBe("ABCDEFG");
    }
}
