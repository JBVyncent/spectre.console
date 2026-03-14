using Spectre.Console;

namespace Gallery.Demos.Tui;

public sealed class TuiDemo : IDemoModule
{
    public string Name => "TUI Framework";
    public string Description => "Terminal User Interface framework with widgets, containers, and window management";

    public void Run()
    {
        AnsiConsole.MarkupLine("[bold underline blue]TUI Framework — Spectre.Console.Tui[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[grey]The TUI framework provides a full widget-based terminal UI system.[/]");
        AnsiConsole.MarkupLine("[grey]It runs as an interactive application — use the standalone demos to try it live.[/]");
        AnsiConsole.WriteLine();

        // Layer overview
        AnsiConsole.MarkupLine("[bold cyan]Architecture — 5 Layers[/]");
        AnsiConsole.WriteLine();

        var layerTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[yellow]Framework Layers[/]");

        layerTable.AddColumn(new TableColumn("[bold]Layer[/]").Centered());
        layerTable.AddColumn(new TableColumn("[bold]Components[/]").LeftAligned());
        layerTable.AddColumn(new TableColumn("[bold]Description[/]").LeftAligned());

        layerTable.AddRow("[cyan]1[/]", "[green]Screen & Input[/]",
            "Double-buffered rendering, SGR mouse, VT sequences");
        layerTable.AddRow("[cyan]2[/]", "[green]Widget System[/]",
            "Base Widget class, focus chain, hit testing, constraints");
        layerTable.AddRow("[cyan]3[/]", "[green]Core Widgets[/]",
            "Label, Button, TextBox, CheckBox, ListBox, DataGrid, TreeView, etc.");
        layerTable.AddRow("[cyan]4[/]", "[green]Window Management[/]",
            "Window, Dialog, WindowManager with z-order");
        layerTable.AddRow("[cyan]5[/]", "[green]Integration[/]",
            "IRenderable bridge, themes, MessageBox, Application");

        AnsiConsole.Write(layerTable);
        AnsiConsole.WriteLine();

        // Widget catalog
        AnsiConsole.MarkupLine("[bold cyan]Widget Catalog[/]");
        AnsiConsole.WriteLine();

        var widgetTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[yellow]Available Widgets[/]");

        widgetTable.AddColumn(new TableColumn("[bold]Category[/]").Centered());
        widgetTable.AddColumn(new TableColumn("[bold]Widgets[/]").LeftAligned());

        widgetTable.AddRow("[green]Controls[/]",
            "Label, Button, TextBox, CheckBox, RadioButton, ListBox, ComboBox, Slider, ProgressBar, ScrollView, DataGrid, TreeView");
        widgetTable.AddRow("[green]Containers[/]",
            "VStack, HStack, Splitter (resizable)");
        widgetTable.AddRow("[green]Chrome[/]",
            "MenuBar, StatusBar, TabControl, TuiPanel");
        widgetTable.AddRow("[green]Windows[/]",
            "Window (draggable), Dialog (modal), WindowManager");

        AnsiConsole.Write(widgetTable);
        AnsiConsole.WriteLine();

        // Features
        AnsiConsole.MarkupLine("[bold cyan]Key Features[/]");
        AnsiConsole.WriteLine();

        var features = new Table()
            .Border(TableBorder.Simple)
            .HideHeaders();

        features.AddColumn("Feature");
        features.AddColumn("Detail");

        features.AddRow("[green]Double buffering[/]", "Minimal VT output via screen diff — only dirty cells are flushed");
        features.AddRow("[green]Mouse support[/]", "SGR extended mouse protocol — click, drag, scroll wheel");
        features.AddRow("[green]Focus management[/]", "Tab/Shift+Tab navigation with TabIndex ordering");
        features.AddRow("[green]Hit testing[/]", "Recursive depth-first widget tree traversal for mouse routing");
        features.AddRow("[green]Constraint layout[/]", "Fixed, Min, Max, Percentage, Fill — two-pass measure+arrange");
        features.AddRow("[green]IRenderable bridge[/]", "Embed existing Spectre.Console renderables (Table, BarChart, etc.)");
        features.AddRow("[green]Themes[/]", "TuiTheme with Default, Dark, and Blue presets");

        AnsiConsole.Write(features);
        AnsiConsole.WriteLine();

        // Demo apps
        AnsiConsole.MarkupLine("[bold cyan]Standalone Demo Applications[/]");
        AnsiConsole.WriteLine();

        var demos = new Table()
            .Border(TableBorder.Rounded)
            .Title("[yellow]Try These![/]");

        demos.AddColumn(new TableColumn("[bold]Demo[/]").Centered());
        demos.AddColumn(new TableColumn("[bold]Style[/]").LeftAligned());
        demos.AddColumn(new TableColumn("[bold]Run Command[/]").LeftAligned());

        demos.AddRow("[green]File Manager[/]", "Midnight Commander",
            "[grey]dotnet run --project demos/FileManager[/]");
        demos.AddRow("[green]System Monitor[/]", "htop-inspired",
            "[grey]dotnet run --project demos/SystemMonitor[/]");
        demos.AddRow("[green]Database Browser[/]", "SQL workbench",
            "[grey]dotnet run --project demos/DatabaseBrowser[/]");

        AnsiConsole.Write(demos);
        AnsiConsole.WriteLine();

        // Code example
        AnsiConsole.MarkupLine("[bold cyan]Quick Start Example[/]");
        AnsiConsole.WriteLine();

        var panel = new Panel(
            "using Spectre.Console;\n" +
            "using Spectre.Console.Tui;\n" +
            "using Spectre.Console.Tui.Widgets.Controls;\n" +
            "using Spectre.Console.Tui.Widgets.Containers;\n\n" +
            "var app = new Application(AnsiConsole.Console);\n\n" +
            "var root = new VStack();\n" +
            "root.Add(new Label(\"Hello, TUI!\"));\n" +
            "root.Add(new Button(\"Click Me\"));\n\n" +
            "app.RootWidget = root;\n" +
            "app.Run();")
            .Header("[yellow]hello-tui.cs[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("grey"));

        AnsiConsole.Write(panel);
    }
}
