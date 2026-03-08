namespace Spectre.Console.Tests.Unit;

[ExpectationPath("Widgets/BarChart")]
public sealed class BarChartTests
{
    [Fact]
    [Expectation("Mutated_Values")]
    public async Task Should_Render_Updated_Values_After_Mutation()
    {
        // Given — items created and added by reference
        var apple = new BarChartItem("Apple", 12.0);
        var orange = new BarChartItem("Orange", 54.0);
        var banana = new BarChartItem("Banana", 33.0);

        var console = new TestConsole();
        var chart = new BarChart()
            .Width(60)
            .Label("Number of fruits")
            .AddItem(apple)
            .AddItem(orange)
            .AddItem(banana);

        // When — mutate all three properties on each item
        apple.Value = 45.0;
        apple.Label = "Pear";
        orange.Value = 20.0;
        orange.Color = Color.Orange1;
        banana.Value = 45.0;

        console.Write(chart);

        // Then
        await Verifier.Verify(console.Output);
    }


    [Fact]
    [Expectation("Render")]
    public async Task Should_Render_Correctly()
    {
        // Given
        var console = new TestConsole();

        // When
        console.Write(new BarChart()
            .Width(60)
            .Label("Number of fruits")
            .AddItem("Apple", 12)
            .AddItem("Orange", 54)
            .AddItem("Banana", 33));

        // Then
        await Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("Zero_Value")]
    public async Task Should_Render_Correctly_2()
    {
        // Given
        var console = new TestConsole();

        // When
        console.Write(new BarChart()
            .Width(60)
            .Label("Number of fruits")
            .AddItem("Apple", 0)
            .AddItem("Orange", 54)
            .AddItem("Banana", 33));

        // Then
        await Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("Fixed_Max_Value")]
    public async Task Should_Render_Correctly_3()
    {
        // Given
        var console = new TestConsole();

        // When
        console.Write(new BarChart()
            .Width(60)
            .WithMaxValue(100)
            .Label("Number of fruits")
            .AddItem("Apple", 12)
            .AddItem("Orange", 54)
            .AddItem("Banana", 33));

        // Then
        await Verifier.Verify(console.Output);
    }
}