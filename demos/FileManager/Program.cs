using Spectre.Console;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;

// File Manager — Midnight Commander style dual-pane file browser
var app = new Application(AnsiConsole.Console);

// Build the UI
var menuBar = new MenuBar();
var fileMenu = new MenuItem("File");
fileMenu.Activated += (_, _) => app.Quit();
menuBar.AddItem(fileMenu);
menuBar.AddItem(new MenuItem("Edit"));
menuBar.AddItem(new MenuItem("View"));
menuBar.AddItem(new MenuItem("Help"));

var leftPanel = CreateFilePanel("Left", Environment.CurrentDirectory);
var rightPanel = CreateFilePanel("Right", Path.GetDirectoryName(Environment.CurrentDirectory) ?? "/");

var splitter = new Splitter
{
    Orientation = SplitOrientation.Vertical,
    First = leftPanel,
    Second = rightPanel,
    SplitRatio = 0.5,
};

var statusBar = new StatusBar();
statusBar.AddItem("F3", "View ");
statusBar.AddItem("F5", "Copy ");
statusBar.AddItem("F6", "Move ");
statusBar.AddItem("F7", "MkDir");
statusBar.AddItem("F8", "Del  ");
statusBar.AddItem("F10", "Quit");

var root = new VStack();
root.Add(menuBar);
splitter.HeightConstraint = Constraint.Fill();
root.Add(splitter);
root.Add(statusBar);

app.RootWidget = root;
app.Run();

AnsiConsole.MarkupLine("[bold cyan]File Manager closed.[/]");

static TuiPanel CreateFilePanel(string title, string path)
{
    var listBox = new ListBox();

    try
    {
        var dir = new DirectoryInfo(path);
        listBox.AddItem("/..");

        foreach (var subDir in dir.GetDirectories().OrderBy(d => d.Name))
        {
            listBox.AddItem($"/{subDir.Name}");
        }

        foreach (var file in dir.GetFiles().OrderBy(f => f.Name))
        {
            var size = file.Length < 1024 ? $"{file.Length}B" :
                       file.Length < 1048576 ? $"{file.Length / 1024}K" :
                       $"{file.Length / 1048576}M";
            listBox.AddItem($" {file.Name,-30} {size,8}");
        }
    }
    catch
    {
        listBox.AddItem("(access denied)");
    }

    return new TuiPanel
    {
        Title = $"{title}: {path}",
        Content = listBox,
        BorderStyle = new Style(Color.Blue),
    };
}
