namespace Spectre.Console.Tests.Unit;

public partial class AnsiConsoleTests
{
    public sealed class MarkupInterpolated
    {
        [Fact]
        public void Should_Print_Simple_Interpolated_Strings()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            const string Path = "file://c:/temp/[x].txt";
            console.MarkupInterpolated($"[Green]{Path}[/]");

            // Then
            console.Output.ShouldBe($"[32m{Path}[0m");
        }

        [Fact]
        public void Should_Not_Throw_Error_On_Links_Brackets()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            const string Path = "file://c:/temp/[x].txt";
            console.MarkupInterpolated($"[link={Path}]{Path}[/]");

            // Then
            var pathAsRegEx = Regex.Replace(Path, "([/\\[\\]\\\\])", "\\$1", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            console.Output.ShouldMatch($"\\]8;id=[0-9]+;{pathAsRegEx}\\\\{pathAsRegEx}\\]8;;\\\\");
        }

        [Fact]
        public void Should_Not_Throw_When_Non_String_Object_Has_Square_Brackets_In_ToString()
        {
            // Given
            var console = new TestConsole();
            var thing = new ThingWithBracketsInToString();

            // When / Then (should not throw — regression for #1763/#1348)
            console.MarkupLineInterpolated($"I have a {thing}");
            console.Output.ShouldContain("This[contains, braces].");
        }

        [Fact]
        public void Should_Preserve_Format_Specifiers_On_Non_String_Interpolated_Values()
        {
            // Given
            var console = new TestConsole();
            var value = 1234.5;

            // When
            console.MarkupInterpolated(CultureInfo.InvariantCulture, $"Value: {value:F1}");

            // Then
            console.Output.ShouldBe("Value: 1234.5");
        }
    }
}

file sealed class ThingWithBracketsInToString
{
    public override string ToString() => "This[contains, braces].";
}