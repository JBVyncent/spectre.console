using Spectre.Console;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Chrome;
using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;

// Database Browser — tabular data browser with tree navigation
var app = new Application(AnsiConsole.Console);

// Menu bar
var menuBar = new MenuBar();
var connMenu = new MenuItem("Connection");
connMenu.Activated += (_, _) => app.Quit();
menuBar.AddItem(connMenu);
menuBar.AddItem(new MenuItem("Query"));
menuBar.AddItem(new MenuItem("Tools"));
menuBar.AddItem(new MenuItem("Help"));

// Database tree
var treeView = new Spectre.Console.Tui.Widgets.Controls.TreeView("Databases");
var northwind = treeView.Root.AddChild("Northwind");
var customers = northwind.AddChild("Tables");
customers.AddChild("Customers");
customers.AddChild("Orders");
customers.AddChild("Products");
customers.AddChild("Employees");
customers.AddChild("Suppliers");
var views = northwind.AddChild("Views");
views.AddChild("CustomerOrders");
views.AddChild("ProductSales");

var adventureWorks = treeView.Root.AddChild("AdventureWorks");
var awTables = adventureWorks.AddChild("Tables");
awTables.AddChild("Person");
awTables.AddChild("Address");
awTables.AddChild("Product");
awTables.AddChild("SalesOrder");

// Data grid
var dataGrid = new DataGrid();
dataGrid.AddColumns("ID", "Name", "Contact", "City", "Country");
dataGrid.AddRow("ALFKI", "Alfreds Futterkiste", "Maria Anders", "Berlin", "Germany");
dataGrid.AddRow("ANATR", "Ana Trujillo", "Ana Trujillo", "Mexico City", "Mexico");
dataGrid.AddRow("ANTON", "Antonio Moreno", "Antonio Moreno", "Mexico City", "Mexico");
dataGrid.AddRow("AROUT", "Around the Horn", "Thomas Hardy", "London", "UK");
dataGrid.AddRow("BERGS", "Berglunds snabbkop", "Christina Berglund", "Lulea", "Sweden");
dataGrid.AddRow("BLAUS", "Blauer See Delikatessen", "Hanna Moos", "Mannheim", "Germany");
dataGrid.AddRow("BLONP", "Blondesddsl pere", "Frederique Citeaux", "Strasbourg", "France");
dataGrid.AddRow("BOLID", "Bolido Comidas", "Martin Sommer", "Madrid", "Spain");
dataGrid.AddRow("BONAP", "Bon app'", "Laurence Lebihan", "Marseille", "France");
dataGrid.AddRow("BOTTM", "Bottom-Dollar Markets", "Elizabeth Lincoln", "Tsawassen", "Canada");

// Query input
var queryBox = new TextBox { Placeholder = "Enter SQL query..." };

// Layout
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

app.RootWidget = root;
app.Run();

AnsiConsole.MarkupLine("[bold cyan]Database Browser closed.[/]");
