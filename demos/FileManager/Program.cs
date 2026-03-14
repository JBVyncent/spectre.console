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

var editMenu = new MenuItem("Edit");
editMenu.Activated += (_, _) =>
{
    // Edit menu placeholder — future: cut/copy/paste operations
};
menuBar.AddItem(editMenu);

var viewMenu = new MenuItem("View");
viewMenu.Activated += (_, _) =>
{
    // View menu placeholder — future: toggle hidden files, sort order
};
menuBar.AddItem(viewMenu);

var helpMenu = new MenuItem("Help");
helpMenu.Activated += (_, _) =>
{
    // Help menu placeholder — future: about dialog, keybindings help
};
menuBar.AddItem(helpMenu);

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
statusBar.AddItem("F3", "View ", () =>
{
    // F3 View placeholder — future: open file in viewer
});
statusBar.AddItem("F5", "Copy ", () =>
{
    // F5 Copy placeholder — future: copy selected file
});
statusBar.AddItem("F6", "Move ", () =>
{
    // F6 Move placeholder — future: move/rename selected file
});
statusBar.AddItem("F7", "MkDir", () =>
{
    // F7 MkDir placeholder — future: create new directory
});
statusBar.AddItem("F8", "Del  ", () =>
{
    // F8 Delete placeholder — future: delete selected file
});
statusBar.AddItem("F10", "Quit", () => app.Quit());

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
