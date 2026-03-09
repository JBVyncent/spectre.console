namespace Spectre.Console.Tests.Unit;

[ExpectationPath("Widgets/Columns")]
public sealed class ColumnsTests
{
    private sealed class User
    {
        public required string Name { get; init; }
        public required string Country { get; init; }
    }

    [Fact]
    [Expectation("Render")]
    public Task Should_Render_Columns_Correctly()
    {
        // Given
        var console = new TestConsole().Width(61);
        var users = new[]
        {
            new User { Name = "Savannah Thompson", Country = "Australia" },
            new User { Name = "Sophie Ramos", Country = "United States" },
            new User { Name = "Katrin Goldberg", Country = "Germany" },
        };

        var cards = new List<Panel>();
        foreach (var user in users)
        {
            cards.Add(
                new Panel($"[b]{user.Name}[/]\n[yellow]{user.Country}[/]")
                    .RoundedBorder().Expand());
        }

        // When
        console.Write(new Columns(cards));

        // Then
        return Verifier.Verify(console.Output);
    }

    [Fact]
    public void Measure_Should_Evaluate_All_Rows()
    {
        // Given — 6 items in a wide console (will produce ~2-3 columns)
        // First row items are narrow, later row items are wider
        // Bug: with row += columnCount, only the first row was measured
        var items = new IRenderable[]
        {
            new Text("A"),     // narrow
            new Text("B"),     // narrow
            new Text("C"),     // narrow
            new Text("WideItemRow2A"), // wider — in row 2
            new Text("WideItemRow2B"), // wider — in row 2
            new Text("WideItemRow2C"), // wider — in row 2
        };

        var columns = new Columns(items) { Expand = false };
        var console = new TestConsole().Width(80);

        // When — render to trigger measurement
        console.Write(columns);
        var output = console.Output;

        // Then — the wider items should be present in output
        // (if measurement was wrong, layout could truncate them)
        output.Should().Contain("WideItemRow2A");
        output.Should().Contain("WideItemRow2B");
        output.Should().Contain("WideItemRow2C");
    }

    [Fact]
    public void Measure_Should_Handle_Partial_Last_Row()
    {
        // Given — 7 items, where the last row has fewer items than columnCount
        var items = new IRenderable[]
        {
            new Text("A"), new Text("B"), new Text("C"),
            new Text("D"), new Text("E"), new Text("F"),
            new Text("LongLastRowItem"), // partial last row
        };

        var columns = new Columns(items) { Expand = false };
        var console = new TestConsole().Width(80);

        // When
        console.Write(columns);

        // Then — partial row item should be rendered
        console.Output.Should().Contain("LongLastRowItem");
    }
}