using Spectre.Console;

namespace Gallery.Demos.Unicode;

public sealed class UnicodeDemo : IDemoModule
{
    public string Name => "Unicode";
    public string Description => "Non-BMP characters, emoji, CJK, and surrogate pair handling";

    public void Run()
    {
        // ── Section 1: Non-BMP Emoji ──────────────────────────────────────
        AnsiConsole.MarkupLine("[bold underline blue]Non-BMP Emoji (Surrogate Pairs)[/]");
        AnsiConsole.MarkupLine("[grey]These characters live outside the Basic Multilingual Plane (U+10000+).[/]");
        AnsiConsole.MarkupLine("[grey]Each is encoded as a surrogate pair (2 UTF-16 code units) but displays as one glyph.[/]");
        AnsiConsole.WriteLine();

        var emojiTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Emoji")
            .AddColumn("Codepoint")
            .AddColumn("Name")
            .AddColumn("Width");

        emojiTable.AddRow("\U0001F389", "U+1F389", "Party Popper", "2 cells");
        emojiTable.AddRow("\U0001F680", "U+1F680", "Rocket", "2 cells");
        emojiTable.AddRow("\U0001F30D", "U+1F30D", "Globe (Europe-Africa)", "2 cells");
        emojiTable.AddRow("\U0001F4A1", "U+1F4A1", "Light Bulb", "2 cells");
        emojiTable.AddRow("\U0001F9E0", "U+1F9E0", "Brain", "2 cells");
        emojiTable.AddRow("\U0001F916", "U+1F916", "Robot", "2 cells");

        AnsiConsole.Write(emojiTable);
        AnsiConsole.WriteLine();

        // ── Section 2: CJK Extension B ───────────────────────────────────
        AnsiConsole.MarkupLine("[bold underline blue]CJK Unified Ideographs Extension B[/]");
        AnsiConsole.MarkupLine("[grey]Rare CJK characters (U+20000+) that require surrogate pairs in UTF-16.[/]");
        AnsiConsole.WriteLine();

        var cjkTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Character")
            .AddColumn("Codepoint")
            .AddColumn("Width");

        cjkTable.AddRow("\U00020000", "U+20000", "2 cells");
        cjkTable.AddRow("\U00020001", "U+20001", "2 cells");
        cjkTable.AddRow("\U00020002", "U+20002", "2 cells");

        AnsiConsole.Write(cjkTable);
        AnsiConsole.WriteLine();

        // ── Section 3: Mixed BMP and Non-BMP ─────────────────────────────
        AnsiConsole.MarkupLine("[bold underline blue]Mixed BMP and Non-BMP Text[/]");
        AnsiConsole.MarkupLine("[grey]Tables with ASCII, BMP wide chars (CJK), and non-BMP emoji in the same row.[/]");
        AnsiConsole.WriteLine();

        var mixedTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Content")
            .AddColumn("Cell Width");

        mixedTable.AddRow("Hello", "5 cells");
        mixedTable.AddRow("\u6D4B\u8BD5", "4 cells (BMP CJK)");
        mixedTable.AddRow("A\U0001F389B", "4 cells (ASCII + emoji + ASCII)");
        mixedTable.AddRow("\U0001F680\U0001F30D\U0001F4A1", "6 cells (3 emoji)");
        mixedTable.AddRow("Code \U0001F916 Review", "13 cells");

        AnsiConsole.Write(mixedTable);
        AnsiConsole.WriteLine();

        // ── Section 4: Overflow Handling ─────────────────────────────────
        AnsiConsole.MarkupLine("[bold underline blue]Overflow with Non-BMP Characters[/]");
        AnsiConsole.MarkupLine("[grey]Surrogate pairs are never split. Truncation and folding respect codepoint boundaries.[/]");
        AnsiConsole.WriteLine();

        var overflowTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Fold (width 8)").Width(8))
            .AddColumn(new TableColumn("Crop (width 8)").Width(8))
            .AddColumn(new TableColumn("Ellipsis (width 8)").Width(8));

        var emojiText = "\U0001F389\U0001F680\U0001F30D\U0001F4A1\U0001F9E0\U0001F916";
        overflowTable.AddRow(
            new Spectre.Console.Markup(emojiText).Overflow(Overflow.Fold),
            new Spectre.Console.Markup(emojiText).Overflow(Overflow.Crop),
            new Spectre.Console.Markup(emojiText).Overflow(Overflow.Ellipsis));

        AnsiConsole.Write(overflowTable);
        AnsiConsole.WriteLine();

        // ── Section 5: Panel with Non-BMP Content ────────────────────────
        AnsiConsole.MarkupLine("[bold underline blue]Panels and Rules[/]");
        AnsiConsole.MarkupLine("[grey]Layout widgets handle non-BMP width correctly for padding and centering.[/]");
        AnsiConsole.WriteLine();

        var panel = new Panel(
                new Rows(
                    new Spectre.Console.Markup("[bold]Status:[/] \U0001F7E2 Online"),
                    new Spectre.Console.Markup("[bold]Engine:[/] \U0001F916 Claude Opus 4.6"),
                    new Spectre.Console.Markup("[bold]Target:[/] \U0001F3AF 100% Mutation Score")))
            .Header("[cyan]System Dashboard[/]")
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[green]\U0001F389 Unicode support is working correctly \U0001F389[/]")
            .RuleStyle(Style.Parse("green")));
        AnsiConsole.WriteLine();
    }
}
