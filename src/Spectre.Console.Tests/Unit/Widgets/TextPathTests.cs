namespace Spectre.Console.Tests.Unit;

[ExpectationPath("Widgets/TextPath")]
public sealed class TextPathTests
{
    [Theory]
    [InlineData(8, "1234567890", "…4567890")]
    [InlineData(9, "1234567890", "…34567890")]
    public void Should_Use_Last_Segments_If_Less_Than_Three(int width, string input, string expected)
    {
        // Given
        var console = new TestConsole().Width(width);

        // When
        console.Write(new TextPath(input));

        // Then
        console.Output.Should().Be(expected);
    }

    [Theory]
    [InlineData("C:/Foo/Bar/Baz.txt", "C:/Foo/Bar/Baz.txt")]
    [InlineData("/Foo/Bar/Baz.txt", "/Foo/Bar/Baz.txt")]
    [InlineData("Foo/Bar/Baz.txt", "Foo/Bar/Baz.txt")]
    public void Should_Render_Full_Path_If_Possible(string input, string expected)
    {
        // Given
        var console = new TestConsole().Width(40);

        // When
        console.Write(new TextPath(input));

        // Then
        console.Output.Should().Be(expected);
    }

    [Theory]
    [InlineData(17, "C:/My documents/Bar/Baz.txt", "C:/…/Bar/Baz.txt")]
    [InlineData(15, "/My documents/Bar/Baz.txt", "/…/Bar/Baz.txt")]
    [InlineData(14, "My documents/Bar/Baz.txt", "…/Bar/Baz.txt")]
    public void Should_Pop_Segments_From_Left(int width, string input, string expected)
    {
        // Given
        var console = new TestConsole().Width(width);

        // When
        console.Write(new TextPath(input));

        // Then
        console.Output.Should().Be(expected);
    }

    [Fact]
    public void Should_Right_Align_Correctly()
    {
        // Given
        var console = new TestConsole().Width(40);

        // When
        console.Write(new TextPath("C:/My documents/Bar/Baz.txt").RightJustified());

        // Then
        console.Output.Should().Be("             C:/My documents/Bar/Baz.txt");
    }

    [Fact]
    public void Should_Center_Align_Correctly()
    {
        // Given
        var console = new TestConsole().Width(40);

        // When
        console.Write(new TextPath("C:/My documents/Bar/Baz.txt").Centered());

        // Then
        console.Output.Should().Be("      C:/My documents/Bar/Baz.txt       ");
    }

    [Theory]
    [InlineData(8, "1234567890", "…4567890")]
    [InlineData(5, "1234567890", "…7890")]
    [InlineData(1, "1234567890", "…")]
    public void Should_Trim_Fallback_By_Cell_Width(int width, string input, string expected)
    {
        // Given — single-segment path (no separators) triggers fallback trimming
        var console = new TestConsole().Width(width);

        // When
        console.Write(new TextPath(input));

        // Then
        console.Output.Should().Be(expected);
    }

    [Fact]
    public void Measure_Should_Use_Cell_Width_Not_String_Length()
    {
        // Given — all ASCII, so cell width = string length. Verify it measures correctly.
        var path = new TextPath("foo/bar/baz.txt");
        var console = new TestConsole().Width(40);
        console.Write(path);

        // Then — full path fits, output should be complete
        console.Output.Should().Be("foo/bar/baz.txt");
    }

    [Fact]
    [Expectation("GH-1307")]
    [GitHubIssue("https://github.com/spectreconsole/spectre.console/issues/1307")]
    public Task Should_Behave_As_Expected_When_Rendering_Inside_Panel_Columns()
    {
        // Given
        var console = new TestConsole().Width(40);

        // When
        console.Write(
            new Columns(
                new Panel(new Text("Baz")),
                new Panel(new TextPath("Qux"))));

        // Then
        return Verifier.Verify(console.Output);
    }
}