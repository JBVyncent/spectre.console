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

    // ── Additional Widget Tests ──────────────────────────────────────

    [Fact]
    public void Should_Render_Table_With_Multiple_Borders()
    {
        var (console, output) = PhantomConsole.Create(60, 24);

        var table = new Table().Border(TableBorder.Ascii);
        table.AddColumn("Col1");
        table.AddColumn("Col2");
        table.AddRow("A", "B");

        console.Write(table);

        var screen = output.Terminal;
        screen.ContainsText("Col1").ShouldBeTrue();
        screen.ContainsText("A").ShouldBeTrue();
        screen.ContainsText("B").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Table_With_No_Border()
    {
        var (console, output) = PhantomConsole.Create(60, 24);

        var table = new Table().Border(TableBorder.None);
        table.AddColumn("X");
        table.AddColumn("Y");
        table.AddRow("1", "2");

        console.Write(table);

        var screen = output.Terminal;
        screen.ContainsText("1").ShouldBeTrue();
        screen.ContainsText("2").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Table_With_Multiple_Rows()
    {
        var (console, output) = PhantomConsole.Create(60, 24);

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Age");
        table.AddRow("Alice", "30");
        table.AddRow("Bob", "25");
        table.AddRow("Charlie", "35");

        console.Write(table);

        var screen = output.Terminal;
        screen.ContainsText("Alice").ShouldBeTrue();
        screen.ContainsText("Bob").ShouldBeTrue();
        screen.ContainsText("Charlie").ShouldBeTrue();
        screen.ContainsText("30").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Paragraph()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Paragraph("This is a paragraph of text that will wrap across multiple lines in a narrow terminal."));

        var screen = output.Terminal;
        screen.ContainsText("This is a paragraph").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Rule_Left_Aligned()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule("[cyan]Left[/]").LeftJustified());

        var screen = output.Terminal;
        screen.ContainsText("Left").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Rule_Right_Aligned()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule("[cyan]Right[/]").RightJustified());

        var screen = output.Terminal;
        screen.ContainsText("Right").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Rule_Without_Title()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule());

        var screen = output.Terminal;
        var rowText = screen.GetRowText(0);
        rowText.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Should_Render_Panel_With_Padding()
    {
        var (console, output) = PhantomConsole.Create(60, 24);

        console.Write(new Panel("Padded").Padding(2, 1));

        var screen = output.Terminal;
        screen.ContainsText("Padded").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Panel_Without_Header()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Panel("Content only"));

        var screen = output.Terminal;
        screen.ContainsText("Content only").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Nested_Styles()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[bold][red]Bold Red[/][/] [italic]Italic[/]");

        var screen = output.Terminal;
        screen.ContainsText("Bold Red").ShouldBeTrue();
        screen.ContainsText("Italic").ShouldBeTrue();

        // Verify "Bold Red" has both bold and foreground color
        var pos = screen.FindText("Bold Red");
        pos.ShouldNotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Bold).ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Strikethrough()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[strikethrough]Deleted[/]");

        var screen = output.Terminal;
        screen.ContainsText("Deleted").ShouldBeTrue();

        var pos = screen.FindText("Deleted");
        pos.ShouldNotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Strikethrough).ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Underline()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[underline]Underlined[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Underlined");
        pos.ShouldNotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Underline).ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Dim()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[dim]Dimmed[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Dimmed");
        pos.ShouldNotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Dim).ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Multiple_Markup_Lines()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[green]Line 1[/]");
        console.MarkupLine("[blue]Line 2[/]");
        console.MarkupLine("[red]Line 3[/]");

        var screen = output.Terminal;
        screen.ContainsText("Line 1").ShouldBeTrue();
        screen.ContainsText("Line 2").ShouldBeTrue();
        screen.ContainsText("Line 3").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Table_In_Table()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        var inner = new Table();
        inner.AddColumn("Inner");
        inner.AddRow("Data");

        var outer = new Table();
        outer.AddColumn("Outer");
        outer.AddRow(inner);

        console.Write(outer);

        var screen = output.Terminal;
        screen.ContainsText("Inner").ShouldBeTrue();
        screen.ContainsText("Data").ShouldBeTrue();
        screen.ContainsText("Outer").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_FigletText_With_Color()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Write(new FigletText("AB").Color(Color.Red));

        var screen = output.Terminal;
        // FigletText renders characters using ASCII art patterns
        screen.GetScreenText().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Should_Render_Rows()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Write(new Rows(
            new Markup("[green]Row 1[/]"),
            new Markup("[blue]Row 2[/]"),
            new Markup("[red]Row 3[/]")));

        var screen = output.Terminal;
        screen.ContainsText("Row 1").ShouldBeTrue();
        screen.ContainsText("Row 2").ShouldBeTrue();
        screen.ContainsText("Row 3").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_Align_Center()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(Align.Center(new Markup("[cyan]Centered[/]")));

        var screen = output.Terminal;
        screen.ContainsText("Centered").ShouldBeTrue();

        // The text should have leading spaces (centered in 40 columns)
        var pos = screen.FindText("Centered");
        pos.ShouldNotBeNull();
        pos!.Value.Col.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Should_Render_Align_Right()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(Align.Right(new Markup("[cyan]Right[/]")));

        var screen = output.Terminal;
        screen.ContainsText("Right").ShouldBeTrue();

        var pos = screen.FindText("Right");
        pos.ShouldNotBeNull();
        pos!.Value.Col.ShouldBeGreaterThan(10);
    }

    [Fact]
    public void Should_Render_WriteLn_Plain_Text()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.WriteLine("Line A");
        console.WriteLine("Line B");

        var screen = output.Terminal;
        screen.ContainsText("Line A").ShouldBeTrue();
        screen.ContainsText("Line B").ShouldBeTrue();
    }

    [Fact]
    public void Should_Render_MarkupLine_With_Emoji()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[yellow]:warning: Warning![/]");

        var screen = output.Terminal;
        screen.ContainsText("Warning!").ShouldBeTrue();
    }

    [Fact]
    public void Should_Handle_Empty_Table()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        var table = new Table();
        table.AddColumn("Empty");

        console.Write(table);

        var screen = output.Terminal;
        screen.ContainsText("Empty").ShouldBeTrue();
    }

    [Fact]
    public void Should_Handle_Wide_Table_With_Wrapping()
    {
        var (console, output) = PhantomConsole.Create(30, 24);

        var table = new Table();
        table.AddColumn("A");
        table.AddColumn("B");
        table.AddRow("Short", "This is a much longer text that should wrap within the column");

        console.Write(table);

        var screen = output.Terminal;
        screen.ContainsText("Short").ShouldBeTrue();
        screen.ContainsText("This is").ShouldBeTrue();
    }

    // ── PhantomConsoleOutput integration ──────────────────────────────

    [Fact]
    public void Should_Capture_Raw_ANSI_Output()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[bold]Test[/]");

        // Raw output should contain ANSI sequences
        output.RawOutput.ShouldContain("\x1b[");
        output.RawOutput.ShouldContain("Test");
    }

    [Fact]
    public void Should_Reset_And_Reuse_Output()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.WriteLine("First");
        output.Terminal.ContainsText("First").ShouldBeTrue();

        output.Reset();
        output.Terminal.ContainsText("First").ShouldBeFalse();
        output.RawOutput.ShouldBeEmpty();

        console.WriteLine("Second");
        output.Terminal.ContainsText("Second").ShouldBeTrue();
    }

    // ── Style verification ───────────────────────────────────────────

    [Fact]
    public void Should_Verify_Green_Foreground_Color()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[green]Green text[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Green text");
        pos.ShouldNotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Foreground.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Verify_Background_Color_In_Markup()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[white on blue]Highlighted[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Highlighted");
        pos.ShouldNotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Foreground.ShouldNotBeNull();
        cell.Background.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Verify_Style_Transitions()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Markup("[bold]Bold[/][italic]Italic[/]");

        var screen = output.Terminal;
        var boldPos = screen.FindText("Bold");
        var italicPos = screen.FindText("Italic");
        boldPos.ShouldNotBeNull();
        italicPos.ShouldNotBeNull();

        screen.GetCell(boldPos!.Value.Row, boldPos!.Value.Col)
            .Decoration.HasFlag(CellDecoration.Bold).ShouldBeTrue();
        screen.GetCell(italicPos!.Value.Row, italicPos!.Value.Col)
            .Decoration.HasFlag(CellDecoration.Italic).ShouldBeTrue();
    }

    // ── Using assertion helpers ──────────────────────────────────────

    [Fact]
    public void Should_Use_AssertContainsText_Helper()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[green]Success[/]");

        output.Terminal.Screen.AssertContainsText("Success");
        output.Terminal.Screen.AssertNotContainsText("Failure");
    }

    [Fact]
    public void Should_Use_AssertCellDecoration_Helper()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Markup("[bold underline]Styled[/]");

        var pos = output.Terminal.FindText("Styled");
        pos.ShouldNotBeNull();

        output.Terminal.Screen.AssertCellDecoration(pos!.Value.Row, pos!.Value.Col, CellDecoration.Bold);
        output.Terminal.Screen.AssertCellDecoration(pos!.Value.Row, pos!.Value.Col, CellDecoration.Underline);
    }
}
