using Spectre.Console;
using Spectre.Console.Network;
using Spectre.Console.Network.Protocol;
using Spectre.Console.Network.Transport;

namespace Gallery.Demos.NetworkConsole;

public sealed class NetworkConsoleDemo : IDemoModule
{
    public string Name => "Network Console";
    public string Description => "IAnsiConsole over TCP — any widget works transparently over the network";

    public void Run()
    {
        // Demonstrate the concept with a local loopback using stream transport
        AnsiConsole.MarkupLine("[bold cyan]Network Console — Remote Terminal Rendering[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[grey]The Network Console extension lets you send Spectre.Console output[/]");
        AnsiConsole.MarkupLine("[grey]over a network connection. Tables, Charts, Progress bars, and all[/]");
        AnsiConsole.MarkupLine("[grey]widgets work transparently — the server renders ANSI output and[/]");
        AnsiConsole.MarkupLine("[grey]the client displays it in its local terminal.[/]");
        AnsiConsole.WriteLine();

        // Show protocol structure
        AnsiConsole.Write(new Rule("[bold yellow]Protocol Messages[/]").RuleStyle(Style.Parse("grey")));
        AnsiConsole.WriteLine();

        var protocolTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Message Type[/]").Centered())
            .AddColumn(new TableColumn("[bold]Direction[/]").Centered())
            .AddColumn(new TableColumn("[bold]Description[/]"));

        protocolTable.AddRow("[cyan]Handshake[/]", "[green]Client → Server[/]", "Terminal dimensions & capabilities");
        protocolTable.AddRow("[cyan]HandshakeAck[/]", "[blue]Server → Client[/]", "Connection accepted");
        protocolTable.AddRow("[cyan]Output[/]", "[blue]Server → Client[/]", "Rendered ANSI text stream");
        protocolTable.AddRow("[cyan]KeyPress[/]", "[green]Client → Server[/]", "Keyboard input forwarding");
        protocolTable.AddRow("[cyan]Resize[/]", "[green]Client → Server[/]", "Terminal resize notification");
        protocolTable.AddRow("[cyan]Disconnect[/]", "[grey]Either[/]", "Connection termination");

        AnsiConsole.Write(protocolTable);
        AnsiConsole.WriteLine();

        // Demonstrate loopback rendering
        AnsiConsole.Write(new Rule("[bold yellow]Loopback Demo[/]").RuleStyle(Style.Parse("grey")));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Creating a NetworkConsole with StreamTransport and rendering widgets...[/]");
        AnsiConsole.WriteLine();

        using var stream = new MemoryStream();
        using var transport = new StreamTransport(stream);
        using var console = Spectre.Console.Network.NetworkConsole.Create(transport, 60, 24);

        // Render a table through the network console
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Feature[/]")
            .AddColumn("[bold]Status[/]");

        table.AddRow("Tables", "[green]✓[/]");
        table.AddRow("Markup", "[green]✓[/]");
        table.AddRow("Rules", "[green]✓[/]");
        table.AddRow("Charts", "[green]✓[/]");
        table.AddRow("Progress", "[green]✓[/]");
        table.AddRow("Prompts", "[green]✓[/]");

        console.Write(table);

        // Read back and display what was sent
        stream.Position = 0;
        var msg = transport.ReceiveAsync().GetAwaiter().GetResult();
        if (msg != null)
        {
            var output = NetworkMessageSerializer.ReadOutput(msg);
            AnsiConsole.MarkupLine("[bold green]Received from network transport:[/]");
            AnsiConsole.Profile.Out.Writer.Write(output);
            AnsiConsole.Profile.Out.Writer.Flush();
        }

        AnsiConsole.WriteLine();

        // Show API usage
        AnsiConsole.Write(new Rule("[bold yellow]Usage Example[/]").RuleStyle(Style.Parse("grey")));
        AnsiConsole.WriteLine();

        var panel = new Panel(
            new Rows(
                new Text("// Server side"),
                new Text("var server = new NetworkConsoleServer(12345);"),
                new Text("server.Start();"),
                new Text("var console = await server.AcceptAsync();"),
                new Text("console.Write(new Table().AddColumn(\"Name\"));"),
                new Text(""),
                new Text("// Client side"),
                new Text("var client = await NetworkConsoleClient.ConnectAsync(\"localhost\", 12345);"),
                new Text("await client.RunAsync(AnsiConsole.Console);")))
            .Header("[bold cyan]Server / Client API[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("grey"));

        AnsiConsole.Write(panel);
    }
}
