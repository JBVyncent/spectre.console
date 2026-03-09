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
        screen.ContainsText("Name").Should().BeTrue();
        screen.ContainsText("Value").Should().BeTrue();
        screen.ContainsText("Key").Should().BeTrue();
        screen.ContainsText("42").Should().BeTrue();
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
        screen.ContainsText("A B C").Should().BeTrue();
        // Verify no raw tab characters in output
        var fullText = screen.GetScreenText();
        fullText.Should().NotContain("\t");
    }

    [Fact]
    public void Should_Render_Markup()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[bold red]Error:[/] Something went wrong");

        var screen = output.Terminal;
        screen.ContainsText("Error:").Should().BeTrue();
        screen.ContainsText("Something went wrong").Should().BeTrue();

        // Verify the "Error:" text has bold+red styling
        var pos = screen.FindText("Error:");
        pos.Should().NotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
        cell.Foreground.Should().NotBeNull();
    }

    [Fact]
    public void Should_Render_MarkupInterpolated_With_NonString_Args()
    {
        // Bug #1: Non-string arguments should be properly escaped
        var (console, output) = PhantomConsole.Create(80, 24);

        var count = 42;
        console.MarkupInterpolated($"[green]Count:[/] {count}");

        var screen = output.Terminal;
        screen.ContainsText("Count:").Should().BeTrue();
        screen.ContainsText("42").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Rule()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule("[yellow]Title[/]"));

        var screen = output.Terminal;
        screen.ContainsText("Title").Should().BeTrue();
        // The rule should contain the line character
        var rowText = screen.GetRowText(0);
        rowText.Length.Should().BeGreaterThan(5);
    }

    [Fact]
    public void Should_Render_Rule_At_Narrow_Width()
    {
        // Bug #7: Rule should not overflow at narrow widths
        var (console, output) = PhantomConsole.Create(5, 24);

        // This should not throw
        FluentActions.Invoking(() => console.Write(new Rule())).Should().NotThrow();

        // It should produce some output
        var screen = output.Terminal;
        screen.GetScreenText().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Should_Render_Panel()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Panel("Hello World").Header("[blue]Info[/]"));

        var screen = output.Terminal;
        screen.ContainsText("Hello World").Should().BeTrue();
        screen.ContainsText("Info").Should().BeTrue();
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
        screen.ContainsText("Data").Should().BeTrue();
        screen.ContainsText("1").Should().BeTrue();
        screen.ContainsText("2").Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Write_With_Curly_Braces()
    {
        // Bug #6: Writing strings with { or } without format args should not throw
        var (console, output) = PhantomConsole.Create(80, 24);

        FluentActions.Invoking(() => console.WriteLine("JSON: { \"key\": \"value\" }")).Should().NotThrow();

        var screen = output.Terminal;
        screen.ContainsText("JSON:").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_FigletText()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Write(new FigletText("Hi").Color(Color.Cyan1));

        var screen = output.Terminal;
        // Figlet text spans multiple rows
        screen.GetScreenText().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Should_Render_Multiple_Writes_On_Same_Line()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Markup("[green]Hello[/] ");
        console.Markup("[blue]World[/]");

        var screen = output.Terminal;
        var row = screen.GetRowText(0);
        row.Should().Contain("Hello");
        row.Should().Contain("World");
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

        terminal.CursorRow.Should().Be(0);
        terminal.CursorCol.Should().Be(8);

        // "Status: " should still be intact on row 0
        terminal.GetRowText(0).Should().StartWith("Status:");
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

        terminal.GetRowText(0).Should().Be("Prefix New");
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
        terminal.ContainsText("Done!").Should().BeTrue();
        terminal.ContainsText("Connecting").Should().BeFalse(); // Old content erased
        terminal.ContainsText("Processing").Should().BeFalse(); // Old content erased
        terminal.CursorVisible.Should().BeTrue();
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
        terminal.GetRowText(0).Should().StartWith("User content:");
        terminal.CursorRow.Should().Be(0);
        terminal.CursorCol.Should().Be(userContentCol);
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
        screen.ContainsText("Col1").Should().BeTrue();
        screen.ContainsText("A").Should().BeTrue();
        screen.ContainsText("B").Should().BeTrue();
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
        screen.ContainsText("1").Should().BeTrue();
        screen.ContainsText("2").Should().BeTrue();
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
        screen.ContainsText("Alice").Should().BeTrue();
        screen.ContainsText("Bob").Should().BeTrue();
        screen.ContainsText("Charlie").Should().BeTrue();
        screen.ContainsText("30").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Paragraph()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Paragraph("This is a paragraph of text that will wrap across multiple lines in a narrow terminal."));

        var screen = output.Terminal;
        screen.ContainsText("This is a paragraph").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Rule_Left_Aligned()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule("[cyan]Left[/]").LeftJustified());

        var screen = output.Terminal;
        screen.ContainsText("Left").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Rule_Right_Aligned()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule("[cyan]Right[/]").RightJustified());

        var screen = output.Terminal;
        screen.ContainsText("Right").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Rule_Without_Title()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Rule());

        var screen = output.Terminal;
        var rowText = screen.GetRowText(0);
        rowText.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Render_Panel_With_Padding()
    {
        var (console, output) = PhantomConsole.Create(60, 24);

        console.Write(new Panel("Padded").Padding(2, 1));

        var screen = output.Terminal;
        screen.ContainsText("Padded").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Panel_Without_Header()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(new Panel("Content only"));

        var screen = output.Terminal;
        screen.ContainsText("Content only").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Nested_Styles()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[bold][red]Bold Red[/][/] [italic]Italic[/]");

        var screen = output.Terminal;
        screen.ContainsText("Bold Red").Should().BeTrue();
        screen.ContainsText("Italic").Should().BeTrue();

        // Verify "Bold Red" has both bold and foreground color
        var pos = screen.FindText("Bold Red");
        pos.Should().NotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Strikethrough()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[strikethrough]Deleted[/]");

        var screen = output.Terminal;
        screen.ContainsText("Deleted").Should().BeTrue();

        var pos = screen.FindText("Deleted");
        pos.Should().NotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Strikethrough).Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Underline()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[underline]Underlined[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Underlined");
        pos.Should().NotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Underline).Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Markup_With_Dim()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[dim]Dimmed[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Dimmed");
        pos.Should().NotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Decoration.HasFlag(CellDecoration.Dim).Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Multiple_Markup_Lines()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[green]Line 1[/]");
        console.MarkupLine("[blue]Line 2[/]");
        console.MarkupLine("[red]Line 3[/]");

        var screen = output.Terminal;
        screen.ContainsText("Line 1").Should().BeTrue();
        screen.ContainsText("Line 2").Should().BeTrue();
        screen.ContainsText("Line 3").Should().BeTrue();
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
        screen.ContainsText("Inner").Should().BeTrue();
        screen.ContainsText("Data").Should().BeTrue();
        screen.ContainsText("Outer").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_FigletText_With_Color()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Write(new FigletText("AB").Color(Color.Red));

        var screen = output.Terminal;
        // FigletText renders characters using ASCII art patterns
        screen.GetScreenText().Should().NotBeNullOrEmpty();
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
        screen.ContainsText("Row 1").Should().BeTrue();
        screen.ContainsText("Row 2").Should().BeTrue();
        screen.ContainsText("Row 3").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_Align_Center()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(Align.Center(new Markup("[cyan]Centered[/]")));

        var screen = output.Terminal;
        screen.ContainsText("Centered").Should().BeTrue();

        // The text should have leading spaces (centered in 40 columns)
        var pos = screen.FindText("Centered");
        pos.Should().NotBeNull();
        pos!.Value.Col.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Render_Align_Right()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        console.Write(Align.Right(new Markup("[cyan]Right[/]")));

        var screen = output.Terminal;
        screen.ContainsText("Right").Should().BeTrue();

        var pos = screen.FindText("Right");
        pos.Should().NotBeNull();
        pos!.Value.Col.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Should_Render_WriteLn_Plain_Text()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.WriteLine("Line A");
        console.WriteLine("Line B");

        var screen = output.Terminal;
        screen.ContainsText("Line A").Should().BeTrue();
        screen.ContainsText("Line B").Should().BeTrue();
    }

    [Fact]
    public void Should_Render_MarkupLine_With_Emoji()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[yellow]:warning: Warning![/]");

        var screen = output.Terminal;
        screen.ContainsText("Warning!").Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Empty_Table()
    {
        var (console, output) = PhantomConsole.Create(40, 24);

        var table = new Table();
        table.AddColumn("Empty");

        console.Write(table);

        var screen = output.Terminal;
        screen.ContainsText("Empty").Should().BeTrue();
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
        screen.ContainsText("Short").Should().BeTrue();
        screen.ContainsText("This is").Should().BeTrue();
    }

    // ── PhantomConsoleOutput integration ──────────────────────────────

    [Fact]
    public void Should_Capture_Raw_ANSI_Output()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[bold]Test[/]");

        // Raw output should contain ANSI sequences
        output.RawOutput.Should().Contain("\x1b[");
        output.RawOutput.Should().Contain("Test");
    }

    [Fact]
    public void Should_Reset_And_Reuse_Output()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.WriteLine("First");
        output.Terminal.ContainsText("First").Should().BeTrue();

        output.Reset();
        output.Terminal.ContainsText("First").Should().BeFalse();
        output.RawOutput.Should().BeEmpty();

        console.WriteLine("Second");
        output.Terminal.ContainsText("Second").Should().BeTrue();
    }

    // ── Style verification ───────────────────────────────────────────

    [Fact]
    public void Should_Verify_Green_Foreground_Color()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[green]Green text[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Green text");
        pos.Should().NotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Foreground.Should().NotBeNull();
    }

    [Fact]
    public void Should_Verify_Background_Color_In_Markup()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.MarkupLine("[white on blue]Highlighted[/]");

        var screen = output.Terminal;
        var pos = screen.FindText("Highlighted");
        pos.Should().NotBeNull();
        var cell = screen.GetCell(pos!.Value.Row, pos!.Value.Col);
        cell.Foreground.Should().NotBeNull();
        cell.Background.Should().NotBeNull();
    }

    [Fact]
    public void Should_Verify_Style_Transitions()
    {
        var (console, output) = PhantomConsole.Create(80, 24);

        console.Markup("[bold]Bold[/][italic]Italic[/]");

        var screen = output.Terminal;
        var boldPos = screen.FindText("Bold");
        var italicPos = screen.FindText("Italic");
        boldPos.Should().NotBeNull();
        italicPos.Should().NotBeNull();

        screen.GetCell(boldPos!.Value.Row, boldPos!.Value.Col)
            .Decoration.HasFlag(CellDecoration.Bold).Should().BeTrue();
        screen.GetCell(italicPos!.Value.Row, italicPos!.Value.Col)
            .Decoration.HasFlag(CellDecoration.Italic).Should().BeTrue();
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
        pos.Should().NotBeNull();

        output.Terminal.Screen.AssertCellDecoration(pos!.Value.Row, pos!.Value.Col, CellDecoration.Bold);
        output.Terminal.Screen.AssertCellDecoration(pos!.Value.Row, pos!.Value.Col, CellDecoration.Underline);
    }
}
