using Spectre.Console;

namespace Gallery.Demos.Themes;

public sealed class ThemesDemo : IDemoModule
{
    public string Name => "Themes";
    public string Description => "Built-in themes applied to Tables, Panels, Trees, Rules, and FigletText";

    public void Run()
    {
        var themes = new[]
        {
            Theme.Default,
            Theme.Nord,
            Theme.Dracula,
            Theme.SolarizedDark,
            Theme.Monokai,
        };

        foreach (var theme in themes)
        {
            RenderThemeShowcase(theme);
            AnsiConsole.WriteLine();
        }
    }

    private static void RenderThemeShowcase(Theme theme)
    {
        // Theme header
        AnsiConsole.Write(new Rule($"[bold]{theme.Name} Theme[/]").UseTheme(theme));
        AnsiConsole.WriteLine();

        // Table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{theme.Name} — Table[/]")
            .UseTheme(theme);

        table.AddColumn(new TableColumn("[bold]Language[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Typing[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Year[/]").Centered());

        table.AddRow("C#", "Static", "2000");
        table.AddRow("Rust", "Static", "2010");
        table.AddRow("Python", "Dynamic", "1991");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Panel
        var panel = new Panel("This is a themed panel with some content inside.\nPanels wrap text and respect border styling.")
            .Header($"[bold]{theme.Name} — Panel[/]")
            .Border(BoxBorder.Rounded)
            .UseTheme(theme);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Tree
        var tree = new Tree($"[bold]{theme.Name} — Tree[/]")
            .UseTheme(theme);

        var src = tree.AddNode("[yellow]src[/]");
        src.AddNode("Program.cs");
        src.AddNode("Startup.cs");
        var tests = tree.AddNode("[yellow]tests[/]");
        tests.AddNode("UnitTests.cs");
        tests.AddNode("IntegrationTests.cs");

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();

        // FigletText
        var figlet = new FigletText(theme.Name)
            .Centered()
            .UseTheme(theme);

        AnsiConsole.Write(figlet);
        AnsiConsole.WriteLine();
    }
}
