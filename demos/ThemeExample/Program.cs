using Spectre.Console;

// Header
AnsiConsole.Write(new FigletText("Theme Example")
    .Color(Color.Cyan1)
    .Centered());
AnsiConsole.Write(new Rule("[grey]Spectre.Console Theme Showcase[/]").RuleStyle(Style.Parse("cyan")));
AnsiConsole.WriteLine();

var themes = new[]
{
    Theme.Default,
    Theme.Nord,
    Theme.Dracula,
    Theme.SolarizedDark,
    Theme.Monokai,
};

while (true)
{
    var choices = themes.Select(t => t.Name).ToList();
    choices.Add("Exit");

    var selection = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold green]Select a theme to preview:[/]")
            .PageSize(10)
            .HighlightStyle(Style.Parse("cyan bold"))
            .AddChoices(choices));

    if (selection == "Exit")
    {
        break;
    }

    var theme = themes.First(t => t.Name == selection);

    AnsiConsole.Clear();
    RenderThemePreview(theme);

    AnsiConsole.WriteLine();
    if (!AnsiConsole.Confirm("[grey]Return to theme selector?[/]"))
    {
        break;
    }

    AnsiConsole.Clear();
}

AnsiConsole.MarkupLine("[bold cyan]Thanks for exploring themes![/]");

static void RenderThemePreview(Theme theme)
{
    // FigletText banner
    AnsiConsole.Write(new FigletText(theme.Name)
        .Centered()
        .UseTheme(theme));

    // Rule divider
    AnsiConsole.Write(new Rule($"[bold]{theme.Name} Theme Preview[/]").UseTheme(theme));
    AnsiConsole.WriteLine();

    // Side-by-side Table and Panel
    var table = new Table()
        .Border(TableBorder.Rounded)
        .Title($"[bold]Sample Data[/]")
        .UseTheme(theme);

    table.AddColumn(new TableColumn("[bold]Language[/]").Centered());
    table.AddColumn(new TableColumn("[bold]Paradigm[/]").Centered());
    table.AddColumn(new TableColumn("[bold]Year[/]").Centered());
    table.AddColumn(new TableColumn("[bold]Popularity[/]").Centered());

    table.AddRow("C#", "OOP / Functional", "2000", "[green]Very High[/]");
    table.AddRow("Rust", "Systems / Functional", "2010", "[yellow]High[/]");
    table.AddRow("Python", "Multi-paradigm", "1991", "[green]Very High[/]");
    table.AddRow("Haskell", "Functional", "1990", "[grey]Niche[/]");
    table.AddRow("Go", "Procedural / Concurrent", "2009", "[green]High[/]");

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();

    // Panel
    var panel = new Panel(
            "[bold]Theme Properties[/]\n\n" +
            $"Border Style: {DescribeStyle(theme.BorderStyle)}\n" +
            $"Tree Style: {DescribeStyle(theme.TreeStyle)}\n" +
            $"Rule Style: {DescribeStyle(theme.RuleStyle)}\n" +
            $"Accent Style: {DescribeStyle(theme.AccentStyle)}\n" +
            $"Highlight Style: {DescribeStyle(theme.HighlightStyle)}\n" +
            $"Dim Style: {DescribeStyle(theme.DimStyle)}")
        .Header($"[bold]{theme.Name} — Configuration[/]")
        .Border(BoxBorder.Rounded)
        .Expand()
        .UseTheme(theme);

    AnsiConsole.Write(panel);
    AnsiConsole.WriteLine();

    // Tree
    var tree = new Tree($"[bold]Project Structure[/]")
        .UseTheme(theme);

    var src = tree.AddNode("[yellow]src[/]");
    var lib = src.AddNode("Spectre.Console");
    lib.AddNode("Theming/Theme.cs");
    lib.AddNode("Theming/IThemeable.cs");
    lib.AddNode("Theming/ThemeExtensions.cs");
    lib.AddNode("Widgets/Table.cs");
    lib.AddNode("Widgets/Panel.cs");
    var tests = src.AddNode("Spectre.Console.Tests");
    tests.AddNode("ThemeTests.cs");

    var demos = tree.AddNode("[yellow]demos[/]");
    demos.AddNode("ThemeExample/Program.cs");
    demos.AddNode("Gallery/Demos/Themes/ThemesDemo.cs");

    AnsiConsole.Write(tree);
    AnsiConsole.WriteLine();

    // Another Rule at the bottom
    AnsiConsole.Write(new Rule("[grey]End of preview[/]").UseTheme(theme));
}

static string DescribeStyle(Style? style)
{
    if (style == null)
    {
        return "[grey]<default>[/]";
    }

    var parts = new List<string>();
    if (style.Value.Foreground != Color.Default)
    {
        parts.Add($"fg=#{style.Value.Foreground.ToHex()}");
    }

    if (style.Value.Background != Color.Default)
    {
        parts.Add($"bg=#{style.Value.Background.ToHex()}");
    }

    if (style.Value.Decoration != Decoration.None)
    {
        parts.Add(style.Value.Decoration.ToString().ToLowerInvariant());
    }

    return parts.Count > 0 ? string.Join(", ", parts) : "[grey]<plain>[/]";
}
