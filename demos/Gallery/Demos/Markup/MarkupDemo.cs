using Spectre.Console;

namespace Gallery.Demos.Markup;

public sealed class MarkupDemo : IDemoModule
{
    public string Name => "Markup";
    public string Description => "Rich text markup, interpolation, and special character handling";

    public void Run()
    {
        // Basic markup
        AnsiConsole.MarkupLine("[bold underline blue]Basic Markup[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Bold[/], [italic]Italic[/], [underline]Underline[/], [strikethrough]Strikethrough[/]");
        AnsiConsole.MarkupLine("[red]Red[/], [green]Green[/], [blue]Blue[/], [yellow]Yellow[/], [cyan]Cyan[/], [magenta]Magenta[/]");
        AnsiConsole.MarkupLine("[bold red on white] Alert! [/] [dim grey]Subtle note[/]");
        AnsiConsole.MarkupLine("[link=https://spectreconsole.net]Spectre.Console Website[/]");
        AnsiConsole.WriteLine();

        // Markup interpolation with non-string types (Bug #1 fix)
        // Non-string arguments are properly escaped so bracket-containing
        // ToString() output doesn't break markup parsing.
        AnsiConsole.MarkupLine("[bold underline blue]Markup Interpolation[/]");
        AnsiConsole.MarkupLine("[grey]Non-string types are safely escaped during interpolation.[/]");
        AnsiConsole.WriteLine();

        var count = 42;
        var price = 19.99m;
        var date = new DateTime(2026, 3, 8);
        AnsiConsole.MarkupInterpolated($"[green]Count:[/] {count}");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupInterpolated($"[green]Price:[/] {price:C}");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupInterpolated($"[green]Date:[/] {date:yyyy-MM-dd}");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Curly braces in output (Bug #6 fix)
        // Strings containing { or } no longer cause FormatException
        // when passed to Write/Markup without format arguments.
        AnsiConsole.MarkupLine("[bold underline blue]Special Characters[/]");
        AnsiConsole.MarkupLine("[grey]Curly braces in strings work correctly without format args.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.WriteLine("JSON-like content: { \"key\": \"value\" }");
        AnsiConsole.WriteLine("C# generic: List<Dictionary<string, int>>");
        AnsiConsole.WriteLine("Template literal: ${variable}");
        AnsiConsole.WriteLine();

        // Escaped markup characters
        AnsiConsole.MarkupLine("[bold underline blue]Escaped Markup[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Use [[double brackets]] to show literal [[ and ]]");
        AnsiConsole.MarkupLine("Markup: [green][[green]text[[/]][/]");
        AnsiConsole.WriteLine();

        // Paragraphs and wrapping
        AnsiConsole.MarkupLine("[bold underline blue]Paragraphs[/]");
        AnsiConsole.WriteLine();

        var paragraph = new Paragraph();
        paragraph.Append("Spectre.Console ", new Style(Color.Blue, decoration: Decoration.Bold));
        paragraph.Append("makes it easy to create ");
        paragraph.Append("beautiful ", new Style(Color.Green));
        paragraph.Append("console applications with ");
        paragraph.Append("rich formatting", new Style(decoration: Decoration.Underline));
        paragraph.Append(". Text wraps naturally to the terminal width.");

        AnsiConsole.Write(paragraph);
        AnsiConsole.WriteLine();
    }
}
