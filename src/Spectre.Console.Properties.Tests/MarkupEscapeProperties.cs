namespace Spectre.Console.Tests.Properties;

public sealed class MarkupEscapeProperties
{
    [Property]
    public bool Escape_ThenRemove_ReturnsOriginal(NonNull<string> input)
    {
        // Any text, when escaped and then markup-removed, equals itself.
        var escaped = Markup.Escape(input.Get);
        var removed = Markup.Remove(escaped);
        return removed == input.Get;
    }

    [Property]
    public bool Escape_NeverProducesSingleOpenBracket(NonNull<string> input)
    {
        var escaped = Markup.Escape(input.Get);
        // Every '[' must be doubled as '[['.
        for (var i = 0; i < escaped.Length; i++)
        {
            if (escaped[i] == '[')
            {
                if (i + 1 >= escaped.Length || escaped[i + 1] != '[') return false;
                i++; // skip the paired second bracket
            }
        }
        return true;
    }

    [Property]
    public bool Escape_NeverProducesSingleCloseBracket(NonNull<string> input)
    {
        var escaped = Markup.Escape(input.Get);
        // Every ']' must be doubled as ']]'.
        for (var i = 0; i < escaped.Length; i++)
        {
            if (escaped[i] == ']')
            {
                if (i + 1 >= escaped.Length || escaped[i + 1] != ']') return false;
                i++; // skip the paired second bracket
            }
        }
        return true;
    }

    [Property]
    public bool Escape_LengthAtLeastAsLongAsOriginal(NonNull<string> input)
    {
        return Markup.Escape(input.Get).Length >= input.Get.Length;
    }

    [Property]
    public bool Escape_OnStringWithNoBrackets_IsIdentity(NonNull<string> input)
    {
        var noBrackets = new string(input.Get.Where(c => c != '[' && c != ']').ToArray());
        return Markup.Escape(noBrackets) == noBrackets;
    }

    [Fact]
    public void Escape_EmptyString_ReturnsEmptyString()
    {
        Markup.Escape(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Escape_DoublesBrackets()
    {
        Markup.Escape("[bold]Hello[/]").Should().Be("[[bold]]Hello[[/]]");
    }

    [Fact]
    public void Remove_EmptyString_ReturnsEmptyString()
    {
        Markup.Remove(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Remove_StripsMarkupTags()
    {
        Markup.Remove("[red]Hello[/] [bold]World[/]").Should().Be("Hello World");
    }

    [Fact]
    public void Remove_EscapedBrackets_UnescapesCorrectly()
    {
        // [[text]] in markup → [text] in plain text
        Markup.Remove("[[foo]]").Should().Be("[foo]");
    }
}
