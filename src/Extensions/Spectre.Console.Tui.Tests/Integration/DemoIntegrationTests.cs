using FluentAssertions;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Integration;

/// <summary>
/// BEHAVIORAL integration tests for TUI demo applications.
///
/// These tests verify user interactions, not just rendering.
/// Every test follows: Given [setup] → When [user action] → Then [observable outcome].
///
/// PROCESS RULE: If a UI element displays a capability (e.g. "F5 Copy"),
/// there MUST be a test that verifies that capability works.
/// If the test fails, the feature is broken — not the test.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DemoIntegrationTests
{
    // ── Test Harness ─────────────────────────────────────────────────
    // Wraps Application + TestTerminalDriver into a Playwright-like API
    // that captures state at each interaction step.

    private sealed class TuiTestHarness : IDisposable
    {
        private readonly TestTerminalDriver _driver;
        private readonly Application _app;

        public TestTerminalDriver Driver => _driver;
        public Application App => _app;

        public TuiTestHarness(Widget root, int width = 80, int height = 24)
        {
            _driver = new TestTerminalDriver(width, height);
            _app = new Application(_driver)
            {
                RootWidget = root,
                MouseEnabled = true,
                TargetFps = 1000,
            };

            // Warm-up: first loop iteration runs ProcessInput before
            // layout/focus chain is built. Burn it with Escape.
            _driver.EnqueueKey(ConsoleKey.Escape);
        }

        /// <summary>Enqueue a key press.</summary>
        public TuiTestHarness PressKey(ConsoleKey key, char keyChar = '\0',
            bool shift = false, bool alt = false, bool control = false)
        {
            _driver.EnqueueKey(key, keyChar, shift, alt, control);
            return this;
        }

        /// <summary>Enqueue a mouse click at screen coordinates.</summary>
        public TuiTestHarness Click(int col, int row)
        {
            _driver.EnqueueInput(new MouseEvent(
                MouseButton.Left, MouseEventType.Press, col, row, false, false, false));
            return this;
        }

        /// <summary>Enqueue Tab presses to move focus forward.</summary>
        public TuiTestHarness Tab(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                _driver.EnqueueKey(ConsoleKey.Tab);
            }

            return this;
        }

        /// <summary>Type a string as individual key presses.</summary>
        public TuiTestHarness Type(string text)
        {
            foreach (var ch in text)
            {
                var key = char.ToUpper(ch) switch
                {
                    >= 'A' and <= 'Z' => (ConsoleKey)((int)ConsoleKey.A + (char.ToUpper(ch) - 'A')),
                    >= '0' and <= '9' => (ConsoleKey)((int)ConsoleKey.D0 + (ch - '0')),
                    ' ' => ConsoleKey.Spacebar,
                    '*' => ConsoleKey.Multiply,
                    _ => ConsoleKey.NoName,
                };
                _driver.EnqueueKey(key, ch);
            }

            return this;
        }

        /// <summary>Run the app and let all enqueued events process.</summary>
        public TuiTestHarness Run(int timeoutMs = 500)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            _app.Run(cts.Token);
            return this;
        }

        /// <summary>Get all rendered text as a single string for searching.</summary>
        public string GetScreenText()
        {
            var lines = new System.Text.StringBuilder();
            for (var row = 0; row < _driver.Height; row++)
            {
                lines.AppendLine(_driver.GetText(row));
            }

            return lines.ToString();
        }

        /// <summary>Get a specific row's text.</summary>
        public string GetRow(int row) => _driver.GetText(row);

        /// <summary>Check if any row contains the text.</summary>
        public bool ScreenContains(string text)
        {
            for (var row = 0; row < _driver.Height; row++)
            {
                if (_driver.GetText(row).Contains(text))
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose() => _app.Dispose();
    }

    // ════════════════════════════════════════════════════════════════════
    // FILEMANAGER BEHAVIORAL TESTS
    // ════════════════════════════════════════════════════════════════════

    // ── Menu Behavior ───────────────────────────────────────────────

    [Fact]
    public void FileManager_Menu_File_Activate_QuitsApp()
    {
        // GIVEN a FileManager with File menu wired to Quit
        var quitCalled = false;
        var (root, app) = BuildFileManagerUI(onQuit: () => quitCalled = true);

        using var harness = new TuiTestHarness(root, 80, 24);

        // WHEN user navigates to File and presses Enter
        harness
            .PressKey(ConsoleKey.RightArrow) // select File (index 0)
            .PressKey(ConsoleKey.Enter)      // activate File
            .Run();

        // THEN the quit action fires
        quitCalled.Should().BeTrue(
            "pressing Enter on 'File' menu should trigger the Activated handler which calls Quit");
    }

    [Fact]
    public void FileManager_Menu_Edit_Activate_HasHandler()
    {
        // GIVEN a FileManager with Edit menu
        var editActivated = false;
        var (root, _) = BuildFileManagerUI(onEdit: () => editActivated = true);

        using var harness = new TuiTestHarness(root, 80, 24);

        // WHEN user navigates to Edit and presses Enter
        harness
            .PressKey(ConsoleKey.RightArrow) // select File
            .PressKey(ConsoleKey.RightArrow) // select Edit
            .PressKey(ConsoleKey.Enter)      // activate Edit
            .Run();

        // THEN the Edit handler fires
        editActivated.Should().BeTrue(
            "every menu item shown to the user must have a working handler — 'Edit' is displayed but has no action");
    }

    [Fact]
    public void FileManager_Menu_AltF_Shortcut_ActivatesFile()
    {
        // GIVEN a FileManager
        var fileActivated = false;
        var (root, _) = BuildFileManagerUI(onQuit: () => fileActivated = true);

        using var harness = new TuiTestHarness(root, 80, 24);

        // WHEN user presses Alt+F
        harness
            .PressKey(ConsoleKey.F, 'f', alt: true)
            .Run();

        // THEN File menu activates
        fileActivated.Should().BeTrue(
            "Alt+F should activate the File menu item");
    }

    // ── StatusBar F-Key Behavior ────────────────────────────────────

    [Fact]
    public void FileManager_F10_QuitsApp()
    {
        // GIVEN a FileManager with F10=Quit in the status bar
        var quitCalled = false;
        var (root, _) = BuildFileManagerUI(onF10Quit: () => quitCalled = true);

        using var harness = new TuiTestHarness(root, 80, 24);

        // WHEN user presses F10 (regardless of which widget has focus)
        harness
            .PressKey(ConsoleKey.F10)
            .Run();

        // THEN the F10 quit action fires
        quitCalled.Should().BeTrue(
            "F10 is displayed as 'Quit' in the status bar — pressing F10 must invoke it. " +
            "Currently, F-keys are not routed to StatusBar actions. " +
            "This requires Application-level F-key → StatusBar routing.");
    }

    [Fact]
    public void FileManager_F5_CopyAction_Fires()
    {
        // GIVEN a FileManager with F5=Copy in the status bar
        var copyCalled = false;
        var (root, _) = BuildFileManagerUI(onF5Copy: () => copyCalled = true);

        using var harness = new TuiTestHarness(root, 80, 24);

        // WHEN user presses F5
        harness
            .PressKey(ConsoleKey.F5)
            .Run();

        // THEN the copy action fires
        copyCalled.Should().BeTrue(
            "F5 is displayed as 'Copy' in the status bar — pressing F5 must invoke it");
    }

    // ── List Navigation Behavior ────────────────────────────────────

    [Fact]
    public void FileManager_LeftList_DownArrow_ChangesSelection()
    {
        // GIVEN a FileManager with items in the left list
        var (root, _) = BuildFileManagerUI();

        using var harness = new TuiTestHarness(root, 80, 24);

        // WHEN user presses DownArrow twice
        harness
            .PressKey(ConsoleKey.DownArrow)
            .PressKey(ConsoleKey.DownArrow)
            .Run();

        // THEN selection moved (verify via widget state, not just rendering)
        var leftList = FindWidget<ListBox>(root);
        leftList.Should().NotBeNull("left panel should contain a ListBox");
        leftList!.SelectedIndex.Should().BeGreaterThan(0,
            "pressing DownArrow should move selection down in the list");
    }

    [Fact]
    public void FileManager_Tab_MovesFocusToRightPanel()
    {
        // GIVEN a FileManager with two panels
        var (root, _) = BuildFileManagerUI();

        using var harness = new TuiTestHarness(root, 80, 24);

        // WHEN user presses Tab to move to right panel
        harness
            .Tab(1)
            .PressKey(ConsoleKey.DownArrow) // interact with right panel
            .Run();

        // THEN the right panel's list should have received the DownArrow
        var lists = FindAllWidgets<ListBox>(root);
        lists.Should().HaveCountGreaterThanOrEqualTo(2, "FileManager needs two list panels");

        // The second list should have focus or have changed selection
        var rightList = lists[1];
        rightList.HasFocus.Should().BeTrue(
            "Tab should move focus from left list to right list");
    }

    // ════════════════════════════════════════════════════════════════════
    // SYSTEMMONITOR BEHAVIORAL TESTS
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void SystemMonitor_Menu_File_Activate_Quits()
    {
        var quitCalled = false;
        var (root, _) = BuildSystemMonitorUI(onQuit: () => quitCalled = true);

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .PressKey(ConsoleKey.RightArrow) // select File
            .PressKey(ConsoleKey.Enter)
            .Run();

        quitCalled.Should().BeTrue(
            "activating File menu should quit the application");
    }

    [Fact]
    public void SystemMonitor_ProcessGrid_DownArrow_MovesSelection()
    {
        var (root, grid) = BuildSystemMonitorUI();

        using var harness = new TuiTestHarness(root, 100, 30);

        // Tab to the process grid, then navigate
        harness
            .Tab(1) // past menubar to grid
            .PressKey(ConsoleKey.DownArrow)
            .PressKey(ConsoleKey.DownArrow)
            .Run();

        grid.SelectedRow.Should().Be(2,
            "two DownArrow presses should move selection to row 2");
    }

    [Fact]
    public void SystemMonitor_ProcessGrid_Enter_ActivatesRow()
    {
        var activatedRow = -1;
        var (root, grid) = BuildSystemMonitorUI();
        grid.RowActivated += (_, row) => activatedRow = row;

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .Tab(1)
            .PressKey(ConsoleKey.DownArrow) // row 1
            .PressKey(ConsoleKey.Enter)
            .Run();

        activatedRow.Should().Be(1,
            "pressing Enter on a DataGrid row should fire RowActivated");
    }

    [Fact]
    public void SystemMonitor_CtrlC_Quits()
    {
        var (root, _) = BuildSystemMonitorUI();

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .PressKey(ConsoleKey.C, '\x03', control: true)
            .Run();

        harness.Driver.IsShutdown.Should().BeTrue(
            "Ctrl+C should quit the application cleanly");
    }

    [Fact]
    public void SystemMonitor_Renders_AllSections()
    {
        var (root, _) = BuildSystemMonitorUI();

        using var harness = new TuiTestHarness(root, 100, 30);
        harness.Run();

        // Verify ALL sections are visible — not just one
        harness.ScreenContains("System Information").Should().BeTrue("info panel must render");
        harness.ScreenContains("Resources").Should().BeTrue("resources panel must render");
        harness.ScreenContains("Processes").Should().BeTrue("process panel must render");
        harness.ScreenContains("PID").Should().BeTrue("grid headers must render");
        harness.ScreenContains("chrome").Should().BeTrue("grid data must render");
    }

    // ════════════════════════════════════════════════════════════════════
    // DATABASEBROWSER BEHAVIORAL TESTS
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void DatabaseBrowser_Menu_Connection_Activate_Quits()
    {
        var quitCalled = false;
        var (root, _, _, _) = BuildDatabaseBrowserUI(onQuit: () => quitCalled = true);

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .PressKey(ConsoleKey.RightArrow) // select Connection
            .PressKey(ConsoleKey.Enter)
            .Run();

        quitCalled.Should().BeTrue(
            "activating Connection menu should quit the application");
    }

    [Fact]
    public void DatabaseBrowser_Menu_Query_Activate_HasHandler()
    {
        var queryActivated = false;
        var (root, _, _, _) = BuildDatabaseBrowserUI(onQuery: () => queryActivated = true);

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .PressKey(ConsoleKey.RightArrow) // Connection
            .PressKey(ConsoleKey.RightArrow) // Query
            .PressKey(ConsoleKey.Enter)
            .Run();

        queryActivated.Should().BeTrue(
            "every displayed menu item must have a working handler — 'Query' is shown but does nothing");
    }

    [Fact]
    public void DatabaseBrowser_TreeView_RightArrow_ExpandsNode()
    {
        var (root, tree, _, _) = BuildDatabaseBrowserUI();

        using var harness = new TuiTestHarness(root, 100, 30);

        // Tab to tree, navigate down to Northwind, expand it
        harness
            .Tab(1) // focus tree
            .PressKey(ConsoleKey.DownArrow)  // Northwind
            .PressKey(ConsoleKey.RightArrow) // expand
            .Run();

        // Verify: the expanded node's children should be in the flat list
        var northwind = tree.Root.Children[0]; // "Northwind"
        northwind.IsExpanded.Should().BeTrue(
            "pressing RightArrow on a tree node with children should expand it");
    }

    [Fact]
    public void DatabaseBrowser_TreeView_LeftArrow_CollapsesNode()
    {
        var (root, tree, _, _) = BuildDatabaseBrowserUI();

        // Pre-expand Northwind
        tree.Root.Children[0].IsExpanded = true;

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .Tab(1)
            .PressKey(ConsoleKey.DownArrow)  // Northwind
            .PressKey(ConsoleKey.LeftArrow)  // collapse
            .Run();

        tree.Root.Children[0].IsExpanded.Should().BeFalse(
            "pressing LeftArrow on an expanded tree node should collapse it");
    }

    [Fact]
    public void DatabaseBrowser_TreeView_ExpandedNode_ShowsChildren_OnScreen()
    {
        var (root, tree, _, _) = BuildDatabaseBrowserUI();

        // Pre-expand Northwind and its Tables child
        tree.Root.Children[0].IsExpanded = true;
        tree.Root.Children[0].Children[0].IsExpanded = true; // Tables

        using var harness = new TuiTestHarness(root, 100, 30);
        harness.Run();

        // The tree should show "Customers" as a visible child of Tables
        harness.ScreenContains("Customers").Should().BeTrue(
            "when Northwind > Tables is expanded, 'Customers' should be visible on screen");
    }

    [Fact]
    public void DatabaseBrowser_DataGrid_DownArrow_ChangesSelection()
    {
        var (root, _, grid, _) = BuildDatabaseBrowserUI();

        using var harness = new TuiTestHarness(root, 100, 30);

        // Tab to grid and navigate
        harness
            .Tab(2) // past menu and tree to grid
            .PressKey(ConsoleKey.DownArrow)
            .PressKey(ConsoleKey.DownArrow)
            .Run();

        grid.SelectedRow.Should().Be(2,
            "two DownArrow presses should move DataGrid selection to row 2");
    }

    [Fact]
    public void DatabaseBrowser_TextBox_AcceptsInput()
    {
        var (root, _, _, queryBox) = BuildDatabaseBrowserUI();

        using var harness = new TuiTestHarness(root, 100, 30);

        // Tab to query box and type
        harness
            .Tab(3) // past menu, tree, grid to textbox
            .Type("SELECT")
            .Run();

        queryBox.Text.Should().Be("SELECT",
            "typing characters while TextBox is focused should insert them");
    }

    [Fact]
    public void DatabaseBrowser_F5_Execute_Fires()
    {
        var executeCalled = false;
        var (root, _, _, _) = BuildDatabaseBrowserUI(onF5Execute: () => executeCalled = true);

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .PressKey(ConsoleKey.F5)
            .Run();

        executeCalled.Should().BeTrue(
            "F5 is displayed as 'Execute' — pressing F5 must invoke it. " +
            "Currently F-keys are not routed to StatusBar actions.");
    }

    [Fact]
    public void DatabaseBrowser_F10_Quit_Fires()
    {
        var quitCalled = false;
        var (root, _, _, _) = BuildDatabaseBrowserUI(onF10Quit: () => quitCalled = true);

        using var harness = new TuiTestHarness(root, 100, 30);

        harness
            .PressKey(ConsoleKey.F10)
            .Run();

        quitCalled.Should().BeTrue(
            "F10 is displayed as 'Quit' — pressing F10 must invoke it");
    }

    [Fact]
    public void DatabaseBrowser_Renders_AllSections()
    {
        var (root, _, _, _) = BuildDatabaseBrowserUI();

        using var harness = new TuiTestHarness(root, 100, 30);
        harness.Run();

        harness.ScreenContains("Connection").Should().BeTrue("menu bar must render");
        harness.ScreenContains("Objects").Should().BeTrue("tree panel must render");
        harness.ScreenContains("Data:").Should().BeTrue("data panel must render");
        harness.ScreenContains("Query").Should().BeTrue("query panel must render");
        harness.ScreenContains("F5").Should().BeTrue("status bar must render");
    }

    // ════════════════════════════════════════════════════════════════════
    // CROSS-CUTTING BEHAVIORAL TESTS
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void App_CtrlC_QuitsApplication()
    {
        using var harness = new TuiTestHarness(new Label("test"), 80, 24);

        harness
            .PressKey(ConsoleKey.C, '\x03', control: true)
            .Run();

        harness.Driver.IsShutdown.Should().BeTrue(
            "Ctrl+C should trigger clean shutdown");
    }

    [Fact]
    public void App_Tab_CyclesFocusThroughAllFocusableWidgets()
    {
        var stack = new VStack();
        var btn1 = new Button("A");
        var btn2 = new Button("B");
        var btn3 = new Button("C");
        stack.Add(btn1);
        stack.Add(btn2);
        stack.Add(btn3);

        using var harness = new TuiTestHarness(stack, 40, 10);

        // First focusable widget gets focus automatically
        harness.Run();
        btn1.HasFocus.Should().BeTrue("first widget gets auto-focus");

        // Tab through all three
        using var h2 = new TuiTestHarness(stack, 40, 10);
        h2.Tab(1).Run();
        btn2.HasFocus.Should().BeTrue("one Tab should focus btn2");

        using var h3 = new TuiTestHarness(stack, 40, 10);
        h3.Tab(2).Run();
        btn3.HasFocus.Should().BeTrue("two Tabs should focus btn3");
    }

    [Fact]
    public void App_MouseClick_FocusesWidget()
    {
        var stack = new VStack();
        var btn1 = new Button("A");
        var btn2 = new Button("B");
        stack.Add(btn1);
        stack.Add(btn2);

        using var harness = new TuiTestHarness(stack, 40, 10);

        // Click on btn2's row
        harness
            .Click(2, 1)
            .Run();

        btn2.HasFocus.Should().BeTrue(
            "clicking on a widget should give it focus");
    }

    [Fact]
    public void App_Button_Enter_FiresClicked()
    {
        var clicked = false;
        var btn = new Button("OK");
        btn.Clicked += (_, _) => clicked = true;

        using var harness = new TuiTestHarness(btn, 40, 5);

        harness
            .PressKey(ConsoleKey.Enter)
            .Run();

        clicked.Should().BeTrue(
            "pressing Enter on a focused Button must fire Clicked");
    }

    [Fact]
    public void App_CheckBox_Space_TogglesChecked()
    {
        var cb = new CheckBox("Option");

        using var harness = new TuiTestHarness(cb, 40, 5);

        harness
            .PressKey(ConsoleKey.Spacebar)
            .Run();

        cb.IsChecked.Should().BeTrue(
            "pressing Space on a focused CheckBox must toggle it");
    }

    // ════════════════════════════════════════════════════════════════════
    // WIDGET TREE BUILDERS
    // ════════════════════════════════════════════════════════════════════
    // These mirror the actual demo Program.cs files but with deterministic
    // test data and injectable callbacks. Any mismatch between these and
    // the real demos is itself a bug.

    private static (VStack root, Application? app) BuildFileManagerUI(
        Action? onQuit = null,
        Action? onEdit = null,
        Action? onF5Copy = null,
        Action? onF10Quit = null)
    {
        var menuBar = new MenuBar();

        var fileMenu = new MenuItem("File");
        if (onQuit != null)
        {
            fileMenu.Activated += (_, _) => onQuit();
        }

        menuBar.AddItem(fileMenu);

        var editMenu = new MenuItem("Edit");
        if (onEdit != null)
        {
            editMenu.Activated += (_, _) => onEdit();
        }

        menuBar.AddItem(editMenu);
        menuBar.AddItem(new MenuItem("View"));
        menuBar.AddItem(new MenuItem("Help"));

        var leftList = new ListBox();
        leftList.AddItem("/..");
        leftList.AddItem("/Documents");
        leftList.AddItem("/Downloads");
        leftList.AddItem(" readme.md      4K");
        leftList.AddItem(" config.json    2K");

        var leftPanel = new TuiPanel
        {
            Title = "Left: /home",
            Content = leftList,
            BorderStyle = new Style(Color.Blue),
        };

        var rightList = new ListBox();
        rightList.AddItem("/..");
        rightList.AddItem("/Projects");
        rightList.AddItem(" notes.txt     1K");

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
        statusBar.AddItem("F3", "View ", null);
        statusBar.AddItem("F5", "Copy ", onF5Copy);
        statusBar.AddItem("F6", "Move ", null);
        statusBar.AddItem("F10", "Quit", onF10Quit);

        var root = new VStack();
        root.Add(menuBar);
        splitter.HeightConstraint = Constraint.Fill();
        root.Add(splitter);
        root.Add(statusBar);

        return (root, null);
    }

    private static (VStack root, DataGrid processGrid) BuildSystemMonitorUI(
        Action? onQuit = null)
    {
        var menuBar = new MenuBar();
        var fileMenu = new MenuItem("File");
        if (onQuit != null)
        {
            fileMenu.Activated += (_, _) => onQuit();
        }

        menuBar.AddItem(fileMenu);
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

        return (root, processGrid);
    }

    private static (VStack root, Spectre.Console.Tui.Widgets.Controls.TreeView tree,
        DataGrid grid, TextBox queryBox) BuildDatabaseBrowserUI(
        Action? onQuit = null,
        Action? onQuery = null,
        Action? onF5Execute = null,
        Action? onF10Quit = null)
    {
        var menuBar = new MenuBar();

        var connMenu = new MenuItem("Connection");
        if (onQuit != null)
        {
            connMenu.Activated += (_, _) => onQuit();
        }

        menuBar.AddItem(connMenu);

        var queryMenu = new MenuItem("Query");
        if (onQuery != null)
        {
            queryMenu.Activated += (_, _) => onQuery();
        }

        menuBar.AddItem(queryMenu);
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
        statusBar.AddItem("F5", "Execute", onF5Execute);
        statusBar.AddItem("F9", "Connect", null);
        statusBar.AddItem("F10", "Quit  ", onF10Quit);
        root.Add(statusBar);

        return (root, treeView, dataGrid, queryBox);
    }

    // ════════════════════════════════════════════════════════════════════
    // WIDGET TREE TRAVERSAL HELPERS
    // ════════════════════════════════════════════════════════════════════

    private static T? FindWidget<T>(Widget root) where T : Widget
    {
        if (root is T match)
        {
            return match;
        }

        foreach (var child in root.GetChildren())
        {
            var found = FindWidget<T>(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static List<T> FindAllWidgets<T>(Widget root) where T : Widget
    {
        var results = new List<T>();
        CollectWidgets(root, results);
        return results;
    }

    private static void CollectWidgets<T>(Widget widget, List<T> results) where T : Widget
    {
        if (widget is T match)
        {
            results.Add(match);
        }

        foreach (var child in widget.GetChildren())
        {
            CollectWidgets(child, results);
        }
    }
}
