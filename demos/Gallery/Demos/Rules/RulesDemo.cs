using Spectre.Console;

namespace Gallery.Demos.Rules;

public sealed class RulesDemo : IDemoModule
{
    public string Name => "Rules";
    public string Description => "Horizontal rules with titles, alignment, and edge cases";

    public void Run()
    {
        // Basic rules
        AnsiConsole.MarkupLine("[bold underline blue]Horizontal Rules[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule());
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[yellow]Section Title[/]"));
        AnsiConsole.WriteLine();

        // Alignment
        AnsiConsole.Write(new Rule("[green]Left Aligned[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[blue]Centered[/]"));
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[red]Right Aligned[/]").RightJustified());
        AnsiConsole.WriteLine();

        // Different styles
        AnsiConsole.Write(new Rule("[cyan]Double Line[/]").DoubleBorder());
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[magenta]Heavy Line[/]").HeavyBorder());
        AnsiConsole.WriteLine();

        // Rules as section dividers
        AnsiConsole.Write(new Rule("[bold yellow]Results Summary[/]").RightJustified());
        AnsiConsole.MarkupLine("  [green]Tests passed:[/] 2254");
        AnsiConsole.MarkupLine("  [red]Tests failed:[/] 0");
        AnsiConsole.MarkupLine("  [yellow]Warnings:[/] 0");
        AnsiConsole.Write(new Rule());

        // Edge case: narrow width (Bug #7 fix)
        // Rules no longer cause MemoryOverflow when rendered at very
        // small widths. The Measure override returns min=1, and
        // negative repeat counts are clamped to zero.
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold underline blue]Narrow Rules[/]");
        AnsiConsole.MarkupLine("[grey]Rules render gracefully even at minimal widths.[/]");
        AnsiConsole.WriteLine();

        // Render a rule inside a narrow panel to demonstrate small-width handling
        var narrowPanel = new Panel(new Rule("[grey]Tiny[/]"))
            .Header("[yellow]Narrow Container[/]")
            .Border(BoxBorder.Rounded)
            .Expand();
        AnsiConsole.Write(narrowPanel);
    }
}
