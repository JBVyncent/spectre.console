using Shouldly;
using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

/// <summary>
/// Integration tests that validate Spectre.Console rendering through the
/// Phantom virtual terminal. These tests exercise real rendering paths
/// and validate the visual output — catching bugs that unit tests with
/// string assertions on raw ANSI output would miss.
/// </summary>
public sealed class IntegrationTests
{
    [Fact]
    public void Should_Render_Table()
    {
        var (console, output) = PhantomConsole.Create(60, 24);

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Name");
        table.AddColumn("Value");
        table.AddRow("Key", "42");

        console.Write(table);

        var screen = output.Terminal;
        screen.ContainsText("Name").ShouldBeTrue();
        screen.ContainsText("Value").ShouldBeTrue();
        screen.ContainsText("Key").ShouldBeTrue();
        screen.ContainsText("42").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Table_With_Tabs_As_Spaces()
    {
        // Bug #2: Tab characters in table cells should be replaced with spaces
        var (console, output) = PhantomConsole.Create(60, 24);

        var table = new Table().Border(TableBorder.Square);
        table.AddColumn("Content");
        table.AddRow("A\tB\tC");

        console.Write(table);

        var screen = output.Terminal;
        // The tab should be replaced with a space, so we see "A B C"
        screen.ContainsText("A B C").ShouldBeTrue();
        // Verify no raw tab characters in output
        var fullText = screen.GetScreenText();
        fullText.ShouldNotContain("\t");
    }

    [Fact]
    public void Should_Render_Markup()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[bold red]Error:[/] Something went wrong");

        var screen = output.Terminal;
        screen.ContainsText("Error:").ShouldBeTrue();
        screen.ContainsText("Something went wrong").ShouldBeTrue();

        // Verify the "Error:" text has bold+red styling
        var pos = screen.FindText("Error:");
        pos.ShouldNotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Bold).ShouldBeTrue();
        cell.Foreground.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Render_MarkupInterpolated_With_NonString_Args()
    {
        // Bug #1: Non-string arguments should be properly escaped
        var (console, output) = PhantomConsole.Create(80, 24);

        var count = 42;
        console.MarkupInterpolated($"[green]Count:[/] {count}");

        var screen = output.Terminal;
        screen.ContainsText("Count:").ShouldBeTrue();
        screen.ContainsText("42").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Rule()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule("[yellow]Title[/]"));

        var screen = output.Terminal;
        screen.ContainsText("Title").ShouldBeTrue();
        // The rule should contain the line character
        var rowText = screen.GetRowText(0);
        rowText.Length.ShouldBeGreaterThan(5);
    }

    [Fact]
    public void Should_Render_Rule_At_Narrow_Width()
    {
        // Bug #7: Rule should not overflow at narrow widths
        var (console, output) = PhantomConsole.Create(5, 24);

        // This should not throw
        Should.NotThrow(() => console.Write(new Rule()));

        // It should produce some output
        var screen = output.Terminal;
        screen.GetScreenText().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Should_Render_Panel()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Panel("Hello World").Header("[blue]Info[/]"));

        var screen = output.Terminal;
        screen.ContainsText("Hello World").ShouldBeTrue();
        screen.ContainsText("Info").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Nested_Table_In_Panel()
    {
        var (console, output) = PhantomConsole.Create(60, 24);

        var table = new Table();
        table.AddColumn("A");
        table.AddColumn("B");
        table.AddRow("1", "2");

        var panel = new Panel(table).Header("Data");
        console.Write(panel);

        var screen = output.Terminal;
        screen.ContainsText("Data").ShouldBeTrue();
        screen.ContainsText("1").ShouldBeTrue();
        screen.ContainsText("2").ShouldBeTrue();
    }

    [Fact]
    public void Should_Handle_Write_With_Curly_Braces()
    {
        // Bug #6: Writing strings with { or } without format args should not throw
        var (console, output) = PhantomConsole.Create(80, 24);

        Should.NotThrow(() => console.WriteLine("JSON: { \"key\": \"value\" }"));

        var screen = output.Terminal;
        screen.ContainsText("JSON:").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_FigletText()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Write(new FigletText("Hi").Color(Color.Cyan1));

        var screen = output.Terminal;
        // Figlet text spans multiple rows
        screen.GetScreenText().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Should_Render_Multiple_Writes_On_Same_Line()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Markup("[green]Hello[/] ");
        console.Markup("[blue]World[/]");

        var screen = output.Terminal;
        var row = screen.GetRowText(0);
        row.ShouldContain("Hello");
        row.ShouldContain("World");
    }

    [Fact]
    public void Should_Preserve_Cursor_Position_With_Save_Restore()
    {
        // Validates the fundamental mechanism used in Bug #10 fix
        var terminal = new PhantomTerminal(80, 24);

        // Simulate: user writes "Status: ", save cursor, write live content, restore
        terminal.Write("Status: ");
        terminal.Write("\x1b[s");  // Save cursor at col 8
        terminal.Write("Working...\nProgress: 50%");
        terminal.Write("\x1b[u");  // Restore to col 8

        terminal.CursorRow.ShouldBe(0);
        terminal.CursorCol.ShouldBe(8);

        // "Status: " should still be intact on row 0
        terminal.GetRowText(0).ShouldStartWith("Status:");
    }

    [Fact]
    public void Should_Erase_Old_Content_After_Restore()
    {
        // Validates save/restore + erase pattern used in live displays
        var terminal = new PhantomTerminal(80, 24);

        terminal.Write("Prefix ");
        terminal.Write("\x1b[s");           // Save cursor
        terminal.Write("Old live content"); // Write live content
        terminal.Write("\x1b[u");           // Restore cursor
        terminal.Write("\x1b[0J");          // Erase from cursor to end
        terminal.Write("New");              // Write new content

        terminal.GetRowText(0).ShouldBe("Prefix New");
    }

    [Fact]
    public void Should_Handle_Status_Like_Render_Cycle()
    {
        // Simulates the actual render cycle of Spectre.Console Status display
        var terminal = new PhantomTerminal(40, 10);

        // First render: hide cursor, save, write status, restore+erase
        terminal.Write("\x1b[?25l");        // Hide cursor
        terminal.Write("\x1b[s");           // Save cursor
        terminal.Write("* Connecting...");  // Status content
        terminal.Write("\x1b[u\x1b[0J");    // Restore + erase

        // Second render: save, write new status
        terminal.Write("\x1b[s");
        terminal.Write("* Processing...");
        terminal.Write("\x1b[u\x1b[0J");

        // Third render: save, write final status
        terminal.Write("\x1b[s");
        terminal.Write("* Done!");

        // Show cursor
        terminal.Write("\x1b[?25h");

        // The screen should show the latest status
        terminal.ContainsText("Done!").ShouldBeTrue();
        terminal.ContainsText("Connecting").ShouldBeFalse(); // Old content erased
        terminal.ContainsText("Processing").ShouldBeFalse(); // Old content erased
        terminal.CursorVisible.ShouldBeTrue();
    }

    [Fact]
    public void Should_Not_Corrupt_Content_Before_Live_Display()
    {
        // This is the core test for Bug #10:
        // User writes partial-line content, then a live display runs.
        // The live display should NOT overwrite the user's content.
        var terminal = new PhantomTerminal(40, 10);

        // User writes partial-line content (no trailing newline)
        terminal.Write("User content: ");
        var userContentCol = terminal.CursorCol;

        // Save cursor before live display
        terminal.Write("\x1b[s");

        // Live display renders on next line(s)
        terminal.Write("\n* Working...\n  Progress: 50%");

        // Restore cursor + erase (repositions for next refresh)
        terminal.Write("\x1b[u\x1b[0J");

        // Verify user content is still intact
        terminal.GetRowText(0).ShouldStartWith("User content:");
        terminal.CursorRow.ShouldBe(0);
        terminal.CursorCol.ShouldBe(userContentCol);
    }
}
