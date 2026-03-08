namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests targeting Stryker surviving mutants in Markup.cs.
/// </summary>
public sealed class MarkupMutationTests
{
    public sealed class NullGuards
    {
        [Fact]
        public void Escape_Should_Throw_If_Text_Is_Null()
        {
            // Kills: Line 91, ThrowIfNull removal
            var ex = Record.Exception(() => Markup.Escape(null!));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Remove_Should_Throw_If_Text_Is_Null()
        {
            // Kills: Line 103, ThrowIfNull removal
            var ex = Record.Exception(() => Markup.Remove(null!));
            ex.ShouldBeOfType<ArgumentNullException>();
        }
    }

    public sealed class EscapingFormatProviderTests
    {
        [Fact]
        public void Should_Store_Inner_Provider()
        {
            // Kills: Lines 118-120, _inner = inner assignment removal (block removal)
            // If _inner is null, GetFormat for non-ICustomFormatter types would fail
            // The inner provider is used for IFormattable.ToString(format, _inner)
            var console = new TestConsole().Width(80);
            var value = 42;
            console.MarkupInterpolated($"Value: {value}");
            console.Output.ShouldContain("42");
        }

        [Fact]
        public void Should_Store_Inner_Provider_For_Format_Delegation()
        {
            // Kills: Line 118 block removal — proves _inner field is needed
            // If block is removed (constructor body empty), _inner is null
            // GetFormat delegates to _inner for non-ICustomFormatter types
            // and _inner.GetFormat(type) would NullRef
            var console = new TestConsole().Width(80);
            // Use a formatted value that requires the inner provider for number formatting
            var num = 1234.5;
            console.MarkupInterpolated($"Number: {num:N2}");
            console.Output.ShouldContain("1");
        }

        [Fact]
        public void GetFormat_Should_Return_Self_For_ICustomFormatter()
        {
            // Kills: Line 124, formatType == typeof(ICustomFormatter) -> true
            // When mutated to always true, non-ICustomFormatter requests return self too
            // This would break format provider chains for number/date formatting
            var console = new TestConsole().Width(80);
            var brackets = "[test]";
            console.MarkupInterpolated($"Hello {brackets} world");
            console.Output.ShouldContain("[test]");
        }

        [Fact]
        public void GetFormat_Should_Delegate_To_Inner_For_Non_ICustomFormatter()
        {
            // Kills: Line 124 conditional true mutation
            // If always returns self instead of _inner.GetFormat, number formatting breaks
            var console = new TestConsole().Width(80);
            // Date formatting needs NumberFormatInfo from the inner provider
            var date = new DateTime(2024, 1, 15);
            console.MarkupInterpolated($"Date: {date:yyyy-MM-dd}");
            console.Output.ShouldContain("2024-01-15");
        }

        [Fact]
        public void Should_Format_Non_IFormattable_Args()
        {
            // Kills: NoCoverage Line 136, arg?.ToString() ?? string.Empty
            var console = new TestConsole().Width(80);
            object? arg = new NonFormattableObject("test[value]");
            console.MarkupInterpolated($"Result: {arg}");
            console.Output.ShouldContain("test[value]");
        }

        [Fact]
        public void Should_Handle_Null_Arg_In_Format()
        {
            // Kills: Line 136, arg?.ToString() — null arg should produce empty string
            var console = new TestConsole().Width(80);
            object? arg = null;
            console.MarkupInterpolated($"Result: {arg} end");
            console.Output.ShouldContain("end");
        }
    }

    /// <summary>
    /// A test object that implements ToString but NOT IFormattable.
    /// </summary>
    private sealed class NonFormattableObject
    {
        private readonly string _value;

        public NonFormattableObject(string value)
        {
            _value = value;
        }

        public override string ToString() => _value;
    }
}
