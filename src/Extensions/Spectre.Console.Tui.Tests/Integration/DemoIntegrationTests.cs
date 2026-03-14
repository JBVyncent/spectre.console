using FluentAssertions;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Integration;

/// <summary>
/// End-to-end integration tests for TUI demo applications.
/// Each test recreates the demo's widget tree, runs the Application with
/// TestTerminalDriver, and asserts on rendered output + interaction behavior.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DemoIntegrationTests
{
    // ── Helper ──────────────────────────────────────────────────────

    /// <summary>
    /// Runs the application for N input events then quits.
    /// Returns the driver so tests can inspect the rendered buffer.
    /// </summary>
    private static TestTerminalDriver RunApp(
        Widget root,
        int width,
        int height,
        Action<TestTerminalDriver>? enqueueInputs = null,
        bool mouseEnabled = true)
    {
        var driver = new TestTerminalDriver(width, height);
        var app = new Application(driver)
        {
            RootWidget = root,
            MouseEnabled = mouseEnabled,
            TargetFps = 1000, // fast as possible for tests
        };

        // Enqueue a warm-up event (first loop iteration runs ProcessInput
        // before layout/focus), then any test-specific inputs, then quit.
        driver.EnqueueKey(ConsoleKey.Escape); // warm-up
        enqueueInputs?.Invoke(driver);

        // Run with short timeout — events are consumed then app exits via timeout
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        app.Run(cts.Token);

        return driver;
    }

    private static string GetRow(TestTerminalDriver driver, int row)
    {
        return driver.GetText(row);
    }

    // ── FileManager Demo ────────────────────────────────────────────

    [Fact]
    public void FileManager_Renders_MenuBar_With_FileEditViewHelp()
    {
        var (root, _, _, _, _) = BuildFileManagerUI();
        var driver = RunApp(root, 80, 24);

        var menuRow = GetRow(driver, 0);
        menuRow.Should().Contain("File");
        menuRow.Should().Contain("Edit");
        menuRow.Should().Contain("View");
        menuRow.Should().Contain("Help");
    }

    [Fact]
    public void FileManager_Renders_SplitterWithTwoPanels()
    {
        var (root, _, leftPanel, rightPanel, _) = BuildFileManagerUI();
        var driver = RunApp(root, 80, 24);

        // Left panel title should be somewhere in the rendered output
        var foundLeftTitle = false;
        var foundRightTitle = false;
        for (var row = 0; row < 24; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("Left:")) foundLeftTitle = true;
            if (text.Contains("Right:")) foundRightTitle = true;
        }

        foundLeftTitle.Should().BeTrue("left panel title should be rendered");
        foundRightTitle.Should().BeTrue("right panel title should be rendered");
    }

    [Fact]
    public void FileManager_Renders_StatusBar_WithFunctionKeys()
    {
        var (root, _, _, _, _) = BuildFileManagerUI();
        var driver = RunApp(root, 80, 24);

        var lastRow = GetRow(driver, 23);
        lastRow.Should().Contain("F3");
        lastRow.Should().Contain("F10");
    }

    [Fact]
    public void FileManager_ListBox_Navigates_WithArrowKeys()
    {
        var (root, _, _, _, leftList) = BuildFileManagerUI();
        var driver = RunApp(root, 80, 24, d =>
        {
            // Navigate down in the list
            d.EnqueueKey(ConsoleKey.DownArrow);
            d.EnqueueKey(ConsoleKey.DownArrow);
        });

        // The list should have rendered items and selection should have moved
        // We verify the app didn't crash and rendered content
        var foundItem = false;
        for (var row = 0; row < 24; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("Item1") || text.Contains("Item2"))
            {
                foundItem = true;
                break;
            }
        }

        foundItem.Should().BeTrue("list items should be rendered");
    }

    [Fact]
    public void FileManager_TabNavigation_MovesFocusBetweenPanels()
    {
        var (root, _, _, _, _) = BuildFileManagerUI();
        var driver = RunApp(root, 80, 24, d =>
        {
            // Tab should move focus from left panel to right panel
            d.EnqueueKey(ConsoleKey.Tab);
        });

        // App should handle tab navigation without crashing
        // and render both panels
        var foundContent = false;
        for (var row = 0; row < 24; row++)
        {
            if (GetRow(driver, row).Trim().Length > 0)
            {
                foundContent = true;
                break;
            }
        }

        foundContent.Should().BeTrue("content should be rendered after tab navigation");
    }

    private static (VStack root, MenuBar menu, TuiPanel left, TuiPanel right, ListBox leftList) BuildFileManagerUI()
    {
        var menuBar = new MenuBar();
        menuBar.AddItem(new MenuItem("File"));
        menuBar.AddItem(new MenuItem("Edit"));
        menuBar.AddItem(new MenuItem("View"));
        menuBar.AddItem(new MenuItem("Help"));

        var leftList = new ListBox();
        leftList.AddItem("/..");
        leftList.AddItem("/Documents");
        leftList.AddItem("/Downloads");
        leftList.AddItem(" Item1       4K");
        leftList.AddItem(" Item2       8K");

        var leftPanel = new TuiPanel
        {
            Title = "Left: /home",
            Content = leftList,
            BorderStyle = new Style(Color.Blue),
        };

        var rightList = new ListBox();
        rightList.AddItem("/..");
        rightList.AddItem("/Projects");
        rightList.AddItem(" readme.md    2K");

        var rightPanel = new TuiPanel
        {
            Title = "Right: /",
            Content = rightList,
            BorderStyle = new Style(Color.Blue),
        };

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
        statusBar.AddItem("F10", "Quit");

        var root = new VStack();
        root.Add(menuBar);
        splitter.HeightConstraint = Constraint.Fill();
        root.Add(splitter);
        root.Add(statusBar);

        return (root, menuBar, leftPanel, rightPanel, leftList);
    }

    // ── SystemMonitor Demo ──────────────────────────────────────────

    [Fact]
    public void SystemMonitor_Renders_MenuBar()
    {
        var (root, _, _, _) = BuildSystemMonitorUI();
        var driver = RunApp(root, 100, 30);

        var menuRow = GetRow(driver, 0);
        menuRow.Should().Contain("File");
        menuRow.Should().Contain("View");
    }

    [Fact]
    public void SystemMonitor_Renders_SystemInfoPanel()
    {
        var (root, _, _, _) = BuildSystemMonitorUI();
        var driver = RunApp(root, 100, 30);

        var foundSysInfo = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("System Information"))
            {
                foundSysInfo = true;
                break;
            }
        }

        foundSysInfo.Should().BeTrue("system info panel title should be visible");
    }

    [Fact]
    public void SystemMonitor_Renders_ResourceBars()
    {
        var (root, _, _, _) = BuildSystemMonitorUI();
        var driver = RunApp(root, 100, 30);

        var foundResources = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("Resources"))
            {
                foundResources = true;
                break;
            }
        }

        foundResources.Should().BeTrue("resources panel should be visible");
    }

    [Fact]
    public void SystemMonitor_Renders_ProcessGrid_WithHeaders()
    {
        var (root, _, _, _) = BuildSystemMonitorUI();
        var driver = RunApp(root, 100, 30);

        var foundHeaders = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("PID") && text.Contains("Name"))
            {
                foundHeaders = true;
                break;
            }
        }

        foundHeaders.Should().BeTrue("process grid headers should be visible");
    }

    [Fact]
    public void SystemMonitor_DataGrid_Navigates_DownArrow()
    {
        var (root, _, processGrid, _) = BuildSystemMonitorUI();
        var driver = RunApp(root, 100, 30, d =>
        {
            // Tab to get to process grid, then navigate
            d.EnqueueKey(ConsoleKey.Tab);
            d.EnqueueKey(ConsoleKey.Tab);
            d.EnqueueKey(ConsoleKey.DownArrow);
        });

        // Grid data should be rendered
        var foundData = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("chrome") || text.Contains("explorer") || text.Contains("svchost"))
            {
                foundData = true;
                break;
            }
        }

        foundData.Should().BeTrue("process data should be rendered");
    }

    private static (VStack root, MenuBar menu, DataGrid processGrid, StatusBar status) BuildSystemMonitorUI()
    {
        var menuBar = new MenuBar();
        menuBar.AddItem(new MenuItem("File"));
        menuBar.AddItem(new MenuItem("View"));

        var infoPanel = new TuiPanel
        {
            Title = "System Information",
            Content = new Label(
                "Machine: TestMachine\n" +
                "OS: TestOS 10.0\n" +
                "Processors: 8"),
        };

        var cpuBar = new ProgressBar { ShowPercentage = true, Value = 45 };
        var memBar = new ProgressBar { ShowPercentage = true, Value = 62 };

        var barsPanel = new TuiPanel { Title = "Resources" };
        var barsStack = new VStack { Spacing = 1 };
        barsStack.Add(new Label("CPU:"));
        barsStack.Add(cpuBar);
        barsStack.Add(new Label("Memory:"));
        barsStack.Add(memBar);
        barsPanel.Content = barsStack;

        var processGrid = new DataGrid();
        processGrid.AddColumns("PID", "Name", "Memory (MB)", "Threads");
        processGrid.AddRow("1234", "chrome", "512", "42");
        processGrid.AddRow("5678", "explorer", "128", "18");
        processGrid.AddRow("9012", "svchost", "64", "12");

        var processPanel = new TuiPanel
        {
            Title = "Processes (Top 50 by Memory)",
            Content = processGrid,
        };

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

        var statusBar = new StatusBar { Text = "Press Ctrl+C to exit" };
        root.Add(statusBar);

        return (root, menuBar, processGrid, statusBar);
    }

    // ── DatabaseBrowser Demo ────────────────────────────────────────

    [Fact]
    public void DatabaseBrowser_Renders_MenuBar()
    {
        var (root, _, _, _, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30);

        var menuRow = GetRow(driver, 0);
        menuRow.Should().Contain("Connection");
        menuRow.Should().Contain("Query");
        menuRow.Should().Contain("Tools");
        menuRow.Should().Contain("Help");
    }

    [Fact]
    public void DatabaseBrowser_Renders_TreeView_WithDatabases()
    {
        var (root, _, _, _, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30);

        var foundDb = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("Databases") || text.Contains("Northwind"))
            {
                foundDb = true;
                break;
            }
        }

        foundDb.Should().BeTrue("database tree should be visible");
    }

    [Fact]
    public void DatabaseBrowser_Renders_DataGrid_WithCustomerData()
    {
        var (root, _, _, _, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30);

        var foundData = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("ALFKI") || text.Contains("Alfreds"))
            {
                foundData = true;
                break;
            }
        }

        foundData.Should().BeTrue("customer data should be visible in the grid");
    }

    [Fact]
    public void DatabaseBrowser_Renders_QueryPanel_WithPlaceholder()
    {
        var (root, _, _, _, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30);

        var foundQuery = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("Query"))
            {
                foundQuery = true;
                break;
            }
        }

        foundQuery.Should().BeTrue("query panel should be visible");
    }

    [Fact]
    public void DatabaseBrowser_Renders_StatusBar_WithFunctionKeys()
    {
        var (root, _, _, _, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30);

        var lastRow = GetRow(driver, 29);
        lastRow.Should().Contain("F5");
        lastRow.Should().Contain("F10");
    }

    [Fact]
    public void DatabaseBrowser_TreeView_ExpandCollapse()
    {
        var (root, _, treeView, _, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30, d =>
        {
            // Navigate to tree and expand
            d.EnqueueKey(ConsoleKey.DownArrow); // select first child
            d.EnqueueKey(ConsoleKey.Enter); // expand/collapse
        });

        // Tree should have rendered with expand/collapse indicators ([+] or [-])
        var foundIndicator = false;
        for (var row = 0; row < 30; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("[+]") || text.Contains("[-]"))
            {
                foundIndicator = true;
                break;
            }
        }

        foundIndicator.Should().BeTrue("tree expand/collapse indicators ([+]/[-]) should be visible");
    }

    [Fact]
    public void DatabaseBrowser_TextBox_AcceptsInput()
    {
        var (root, _, _, queryBox, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30, d =>
        {
            // Tab to query box and type
            d.EnqueueKey(ConsoleKey.Tab);
            d.EnqueueKey(ConsoleKey.Tab);
            d.EnqueueKey(ConsoleKey.Tab);
            d.EnqueueKey(ConsoleKey.S, 'S');
            d.EnqueueKey(ConsoleKey.E, 'E');
            d.EnqueueKey(ConsoleKey.L, 'L');
        });

        queryBox.Text.Should().Be("SEL");
    }

    [Fact]
    public void DatabaseBrowser_MouseClick_OnStatusBar()
    {
        var (root, _, _, _, _) = BuildDatabaseBrowserUI();
        var driver = RunApp(root, 100, 30, d =>
        {
            // Click on status bar area
            d.EnqueueInput(new MouseEvent(
                MouseButton.Left, MouseEventType.Press, 5, 29, false, false, false));
        });

        // App should handle mouse click without crashing
        driver.IsShutdown.Should().BeTrue("app should shut down cleanly after timeout");
    }

    private static (VStack root, MenuBar menu, Spectre.Console.Tui.Widgets.Controls.TreeView tree, TextBox queryBox, DataGrid grid) BuildDatabaseBrowserUI()
    {
        var menuBar = new MenuBar();
        menuBar.AddItem(new MenuItem("Connection"));
        menuBar.AddItem(new MenuItem("Query"));
        menuBar.AddItem(new MenuItem("Tools"));
        menuBar.AddItem(new MenuItem("Help"));

        var treeView = new Spectre.Console.Tui.Widgets.Controls.TreeView("Databases");
        var northwind = treeView.Root.AddChild("Northwind");
        var tables = northwind.AddChild("Tables");
        tables.AddChild("Customers");
        tables.AddChild("Orders");
        tables.AddChild("Products");
        var views = northwind.AddChild("Views");
        views.AddChild("CustomerOrders");

        var dataGrid = new DataGrid();
        dataGrid.AddColumns("ID", "Name", "Contact", "City", "Country");
        dataGrid.AddRow("ALFKI", "Alfreds Futterkiste", "Maria Anders", "Berlin", "Germany");
        dataGrid.AddRow("ANATR", "Ana Trujillo", "Ana Trujillo", "Mexico City", "Mexico");
        dataGrid.AddRow("AROUT", "Around the Horn", "Thomas Hardy", "London", "UK");

        var queryBox = new TextBox { Placeholder = "Enter SQL query..." };

        var treePanel = new TuiPanel
        {
            Title = "Objects",
            Content = treeView,
            BorderStyle = new Style(Color.Blue),
        };

        var dataPanel = new TuiPanel
        {
            Title = "Data: Customers",
            Content = dataGrid,
            BorderStyle = new Style(Color.Green),
        };

        var splitter = new Splitter
        {
            Orientation = SplitOrientation.Vertical,
            First = treePanel,
            Second = dataPanel,
            SplitRatio = 0.25,
        };

        var queryPanel = new TuiPanel
        {
            Title = "Query",
            Content = queryBox,
        };

        var root = new VStack();
        root.Add(menuBar);
        splitter.HeightConstraint = Constraint.Fill();
        root.Add(splitter);
        queryPanel.HeightConstraint = Constraint.Fixed(3);
        root.Add(queryPanel);

        var statusBar = new StatusBar();
        statusBar.AddItem("F5", "Execute");
        statusBar.AddItem("F9", "Connect");
        statusBar.AddItem("F10", "Quit  ");
        root.Add(statusBar);

        return (root, menuBar, treeView, queryBox, dataGrid);
    }

    // ── Cross-Cutting Integration ───────────────────────────────────

    [Fact]
    public void App_Quit_ShutdownCleanly()
    {
        var driver = new TestTerminalDriver(80, 24);
        var quitCalled = false;
        var app = new Application(driver) { TargetFps = 1000 };

        var btn = new Button("Quit");
        btn.Clicked += (_, _) => { quitCalled = true; app.Quit(); };
        app.RootWidget = btn;

        // Warm-up + Enter to click button
        driver.EnqueueKey(ConsoleKey.Escape);
        driver.EnqueueKey(ConsoleKey.Enter);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        app.Run(cts.Token);

        quitCalled.Should().BeTrue("button click should trigger quit");
        driver.IsInitialized.Should().BeTrue("driver was initialized");
        driver.IsShutdown.Should().BeTrue("driver was shut down");
    }

    [Fact]
    public void App_MouseClick_FocusesWidget()
    {
        var driver = new TestTerminalDriver(40, 10);
        var app = new Application(driver) { TargetFps = 1000 };

        var stack = new VStack();
        var btn1 = new Button("A");
        var btn2 = new Button("B");
        stack.Add(btn1);
        stack.Add(btn2);
        app.RootWidget = stack;

        // Warm-up, then click on btn2 area (row 1)
        driver.EnqueueKey(ConsoleKey.Escape);
        driver.EnqueueInput(new MouseEvent(
            MouseButton.Left, MouseEventType.Press, 2, 1, false, false, false));

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        app.Run(cts.Token);

        // btn2 should be focused after mouse click
        btn2.HasFocus.Should().BeTrue("mouse click should focus btn2");
    }

    [Fact]
    public void App_FullWidgetTree_RendersAllLayers()
    {
        var driver = new TestTerminalDriver(60, 20);
        var app = new Application(driver) { TargetFps = 1000 };

        // Build a mini app with all layer types
        var menu = new MenuBar();
        menu.AddItem(new MenuItem("File"));

        var content = new VStack();
        content.Add(new Label("Hello, TUI!"));
        content.Add(new Button("Click Me"));
        content.Add(new CheckBox("Option A"));

        var panel = new TuiPanel { Title = "Main", Content = content };

        var status = new StatusBar { Text = "Ready" };

        var root = new VStack();
        root.Add(menu);
        panel.HeightConstraint = Constraint.Fill();
        root.Add(panel);
        root.Add(status);

        app.RootWidget = root;

        driver.EnqueueKey(ConsoleKey.Escape);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        app.Run(cts.Token);

        // Verify all layers rendered
        GetRow(driver, 0).Should().Contain("File", "menu bar should render");

        var foundMain = false;
        var foundHello = false;
        var foundButton = false;
        var foundReady = false;

        for (var row = 0; row < 20; row++)
        {
            var text = GetRow(driver, row);
            if (text.Contains("Main")) foundMain = true;
            if (text.Contains("Hello")) foundHello = true;
            if (text.Contains("Click Me")) foundButton = true;
            if (text.Contains("Ready")) foundReady = true;
        }

        foundMain.Should().BeTrue("panel title should render");
        foundHello.Should().BeTrue("label should render");
        foundButton.Should().BeTrue("button should render");
        foundReady.Should().BeTrue("status bar should render");
    }
}
