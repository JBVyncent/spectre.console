using Spectre.Console;
using Spectre.Console.Network;
using Spectre.Console.Network.Protocol;
using Spectre.Console.Network.Transport;

AnsiConsole.Write(new FigletText("Network Console")
    .Color(Color.Cyan1)
    .Centered());
AnsiConsole.Write(new Rule("[grey]Spectre.Console over the network[/]").RuleStyle(Style.Parse("cyan")));
AnsiConsole.WriteLine();

// === Loopback Demo: Render widgets through a NetworkConsole and capture output ===

AnsiConsole.MarkupLine("[bold yellow]1. Loopback Demo — Rendering through StreamTransport[/]");
AnsiConsole.WriteLine();

using (var stream = new MemoryStream())
using (var transport = new StreamTransport(stream))
using (var networkConsole = NetworkConsole.Create(transport, 70, 24))
{
    // Render rich widgets through the network console
    networkConsole.Write(new Rule("[bold cyan]Remote Table[/]").RuleStyle(Style.Parse("grey")));
    networkConsole.Write(new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("[bold]Widget[/]")
        .AddColumn("[bold]Supported[/]")
        .AddRow("Table", "[green]Yes[/]")
        .AddRow("Markup", "[green]Yes[/]")
        .AddRow("Rule", "[green]Yes[/]")
        .AddRow("FigletText", "[green]Yes[/]")
        .AddRow("BarChart", "[green]Yes[/]")
        .AddRow("Panel", "[green]Yes[/]"));

    // Read back the captured ANSI output
    stream.Position = 0;
    var messages = new List<string>();
    while (true)
    {
        var msg = await transport.ReceiveAsync();
        if (msg == null)
        {
            break;
        }

        if (msg.Type == MessageType.Output)
        {
            messages.Add(NetworkMessageSerializer.ReadOutput(msg));
        }
    }

    AnsiConsole.MarkupLine("[green]Received {0} output messages from network transport:[/]", messages.Count);
    AnsiConsole.WriteLine();

    // Display the captured output directly (it contains ANSI sequences)
    foreach (var text in messages)
    {
        AnsiConsole.Profile.Out.Writer.Write(text);
    }

    AnsiConsole.Profile.Out.Writer.Flush();
}

AnsiConsole.WriteLine();

// === Handshake Demo: Show the protocol handshake flow ===

AnsiConsole.MarkupLine("[bold yellow]2. Protocol Handshake Demo[/]");
AnsiConsole.WriteLine();

using (var stream = new MemoryStream())
using (var transport = new StreamTransport(stream))
{
    // Simulate client handshake
    var handshake = NetworkMessageSerializer.CreateHandshake(120, 40, ColorSystem.TrueColor, true);
    await transport.SendAsync(handshake);

    stream.Position = 0;
    var received = await transport.ReceiveAsync();
    var (width, height, colorSystem, interactive) = NetworkMessageSerializer.ReadHandshake(received!);

    var handshakeTable = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("[bold]Property[/]")
        .AddColumn("[bold]Value[/]");

    handshakeTable.AddRow("Width", width.ToString());
    handshakeTable.AddRow("Height", height.ToString());
    handshakeTable.AddRow("Color System", colorSystem.ToString());
    handshakeTable.AddRow("Interactive", interactive.ToString());

    AnsiConsole.MarkupLine("[grey]Handshake message decoded:[/]");
    AnsiConsole.Write(handshakeTable);
}

AnsiConsole.WriteLine();

// === KeyPress Serialization Demo ===

AnsiConsole.MarkupLine("[bold yellow]3. KeyPress Serialization Demo[/]");
AnsiConsole.WriteLine();

var keys = new[]
{
    new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false),
    new ConsoleKeyInfo('A', ConsoleKey.A, true, false, false),
    new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false),
    new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false),
    new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false),
    new ConsoleKeyInfo('\u0100', ConsoleKey.A, false, true, true), // Unicode + modifiers
};

var keyTable = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("[bold]KeyChar[/]")
    .AddColumn("[bold]Key[/]")
    .AddColumn("[bold]Modifiers[/]")
    .AddColumn("[bold]Payload Size[/]");

foreach (var key in keys)
{
    var msg = NetworkMessageSerializer.CreateKeyPress(key);
    var restored = NetworkMessageSerializer.ReadKeyPress(msg);

    var charDisplay = key.KeyChar switch
    {
        '\r' => "\\r",
        '\t' => "\\t",
        '\0' => "\\0",
        _ when key.KeyChar < ' ' => $"\\x{(int)key.KeyChar:X2}",
        _ => key.KeyChar.ToString(),
    };

    keyTable.AddRow(
        $"[cyan]{charDisplay}[/]",
        restored.Key.ToString(),
        restored.Modifiers.ToString(),
        $"{msg.Payload.Length} bytes");
}

AnsiConsole.Write(keyTable);
AnsiConsole.WriteLine();

AnsiConsole.Write(new Rule("[grey]Demo complete[/]").RuleStyle(Style.Parse("grey")));
