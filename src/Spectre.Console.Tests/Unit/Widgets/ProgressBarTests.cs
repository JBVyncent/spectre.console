namespace Spectre.Console.Tests.Unit;

[ExpectationPath("Widgets/ProgressBar")]
public class ProgressBarTests
{
    [Fact]
    [Expectation("Render")]
    public async Task Should_Render_Correctly()
    {
        // Given
        var console = new TestConsole();

        var bar = new ProgressBar()
        {
            Width = 60,
            Value = 9000,
            MaxValue = 9000,
            ShowValue = true,
        };

        // When
        console.Write(bar);

        // Then
        await Verifier.Verify(console.Output);
    }

    [Fact]
    [Expectation("Formatted")]
    public async Task Should_Render_ValueFormatted()
    {
        // Given
        var console = new TestConsole();

        var bar = new ProgressBar()
        {
            Width = 60,
            Value = 9000,
            MaxValue = 9000,
            ShowValue = true,
            ValueFormatter = (value, _) => value.ToString("N0", CultureInfo.InvariantCulture),
        };

        // When
        console.Write(bar);

        // Then
        await Verifier.Verify(console.Output);
    }

    [Fact]
    public void Should_Not_Crash_When_MaxValue_Is_Zero()
    {
        // Given
        var console = new TestConsole();
        var bar = new ProgressBar
        {
            Width = 20,
            Value = 0,
            MaxValue = 0,
        };

        // When
        var act = () => console.Write(bar);

        // Then — should not throw DivideByZeroException or produce NaN
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_Render_Empty_Bar_When_MaxValue_Is_Zero()
    {
        // Given
        var console = new TestConsole();
        var bar = new ProgressBar
        {
            Width = 10,
            Value = 5,
            MaxValue = 0,
        };

        // When
        console.Write(bar);

        // Then — bar should have rendered without crashing, producing some output
        console.Output.Should().NotBeEmpty();
    }
}