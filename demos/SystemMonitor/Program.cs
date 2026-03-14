using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;

// System Monitor — htop-inspired system information display
var app = new Application(AnsiConsole.Console);

// Menu bar
var menuBar = new MenuBar();
var fileMenu = new MenuItem("File");
fileMenu.Activated += (_, _) => app.Quit();
menuBar.AddItem(fileMenu);

var viewMenu = new MenuItem("View");
viewMenu.Activated += (_, _) =>
{
    // View menu placeholder — future: toggle columns, sort order
};
menuBar.AddItem(viewMenu);

// System info panel
var infoPanel = new TuiPanel
{
    Title = "System Information",
    Content = new Label(
        $"Machine: {Environment.MachineName}\n" +
        $"OS: {Environment.OSVersion}\n" +
        $"Runtime: {Environment.Version}\n" +
        $"Processors: {Environment.ProcessorCount}\n" +
        $"64-bit OS: {Environment.Is64BitOperatingSystem}\n" +
        $"Working Set: {Environment.WorkingSet / 1048576} MB"),
};

// CPU/Memory bars
var cpuBar = new ProgressBar { ShowPercentage = true, Value = 0 };
var memBar = new ProgressBar { ShowPercentage = true, Value = 0 };

var barsPanel = new TuiPanel { Title = "Resources" };
var barsStack = new VStack { Spacing = 1 };
barsStack.Add(new Label("CPU:"));
barsStack.Add(cpuBar);
barsStack.Add(new Label("Memory:"));
barsStack.Add(memBar);
barsPanel.Content = barsStack;

// Process list
var processGrid = new DataGrid();
processGrid.AddColumns("PID", "Name", "Memory (MB)", "Threads");

try
{
    var processes = Process.GetProcesses()
        .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0; } })
        .Take(50);

    foreach (var proc in processes)
    {
        try
        {
            processGrid.AddRow(
                proc.Id.ToString(),
                proc.ProcessName,
                (proc.WorkingSet64 / 1048576).ToString(),
                proc.Threads.Count.ToString());
        }
        catch
        {
            // Skip inaccessible processes
        }
    }
}
catch
{
    processGrid.AddRow("?", "(unable to enumerate)", "?", "?");
}

var processPanel = new TuiPanel
{
    Title = "Processes (Top 50 by Memory)",
    Content = processGrid,
};

// Estimate memory usage
var totalMem = Environment.WorkingSet;
memBar.Value = Math.Min(100, totalMem / 1048576.0 / 10); // rough estimate

// Layout
var topRow = new HStack { Spacing = 1 };
infoPanel.WidthConstraint = Constraint.Percentage(40);
topRow.Add(infoPanel);
barsPanel.WidthConstraint = Constraint.Fill();
topRow.Add(barsPanel);

var root = new VStack();
root.Add(menuBar);
topRow.HeightConstraint = Constraint.Fixed(9);
root.Add(topRow);
processPanel.HeightConstraint = Constraint.Fill();
root.Add(processPanel);

var statusBar = new StatusBar { Text = "Press Ctrl+C to exit | F10 = Quit" };
root.Add(statusBar);

app.RootWidget = root;
app.Run();

AnsiConsole.MarkupLine("[bold cyan]System Monitor closed.[/]");
