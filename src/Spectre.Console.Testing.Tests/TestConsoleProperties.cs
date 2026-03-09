namespace Spectre.Console.Testing.Tests;

public sealed class TestConsoleProperties
{
    [Fact]
    public void Output_IsNeverNull()
    {
        using var console = new TestConsole();
        console.Output.Should().NotBeNull();
    }

    [Fact]
    public void Output_StartsEmpty()
    {
        using var console = new TestConsole();
        console.Output.Should().BeEmpty();
    }

    [Fact]
    public void Lines_StartsWithOneEmptyElement()
    {
        // Even with no output, Lines splits "" by newline → [""]
        using var console = new TestConsole();
        console.Lines.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Property]
    public bool PlainTextMarkup_AppearsInOutput(NonNull<string> input)
    {
        // Plain text with no markup characters should appear verbatim in Output.
        var safe = new string(input.Get
            .Where(c => c != '[' && c != ']' && c > 0x1F && c < 0x7F)
            .ToArray());
        if (safe.Length == 0) return true;

        using var console = new TestConsole();
        console.Write(new Markup(Markup.Escape(safe)));
        return console.Output.Contains(safe);
    }

    [Property]
    public bool WriteMarkupLine_AddsNewline(NonNull<string> input)
    {
        var safe = new string(input.Get
            .Where(c => c != '[' && c != ']' && c > 0x1F && c < 0x7F)
            .ToArray());
        if (safe.Length == 0) return true;

        using var console = new TestConsole();
        console.WriteLine(Markup.Escape(safe));
        return console.Output.Contains('\n');
    }

    [Property]
    public bool TwoWrites_AccumulateInOutput(NonNull<string> first, NonNull<string> second)
    {
        var s1 = new string(first.Get.Where(c => c != '[' && c != ']' && c > 0x1F && c < 0x7F).ToArray());
        var s2 = new string(second.Get.Where(c => c != '[' && c != ']' && c > 0x1F && c < 0x7F).ToArray());
        if (s1.Length == 0 || s2.Length == 0) return true;

        using var console = new TestConsole();
        console.Write(new Markup(Markup.Escape(s1)));
        console.Write(new Markup(Markup.Escape(s2)));
        return console.Output.Contains(s1) && console.Output.Contains(s2);
    }

    [Fact]
    public void Lines_SplitsOutputByNewline()
    {
        using var console = new TestConsole();
        console.WriteLine("line1");
        console.WriteLine("line2");
        console.Lines.Should().Contain("line1");
        console.Lines.Should().Contain("line2");
    }

    [Fact]
    public void Lines_Count_MatchesNewlines()
    {
        using var console = new TestConsole();
        console.WriteLine("a");
        console.WriteLine("b");
        console.WriteLine("c");
        // 3 newlines → at least 3 non-empty lines
        console.Lines.Count(l => !string.IsNullOrEmpty(l)).Should().Be(3);
    }

    [Fact]
    public void EmitAnsiSequences_FalseByDefault_OutputIsPlainText()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences.Should().BeFalse();
        console.Write(new Markup("[red]Hello[/]"));
        // Without ANSI sequences, output should be plain "Hello"
        console.Output.Should().Contain("Hello");
        console.Output.Should().NotContain("\x1b[");
    }

    [Fact]
    public void Profile_HasSensibleDefaults()
    {
        using var console = new TestConsole();
        console.Profile.Width.Should().BeGreaterThan(0);
        console.Profile.Height.Should().BeGreaterThan(0);
    }
}
