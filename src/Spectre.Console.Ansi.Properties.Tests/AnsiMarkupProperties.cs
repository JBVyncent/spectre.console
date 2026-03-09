namespace Spectre.Console.Ansi.Tests.Properties;

public sealed class AnsiMarkupProperties
{
    [Property]
    public bool Escape_ThenRemove_ReturnsOriginal(NonNull<string> input)
    {
        var escaped = AnsiMarkup.Escape(input.Get);
        var removed = AnsiMarkup.Remove(escaped);
        return removed == input.Get;
    }

    [Property]
    public bool Escape_NeverProducesSingleOpenBracket(NonNull<string> input)
    {
        var escaped = AnsiMarkup.Escape(input.Get);
        for (var i = 0; i < escaped.Length; i++)
        {
            if (escaped[i] == '[')
            {
                if (i + 1 >= escaped.Length || escaped[i + 1] != '[') return false;
                i++;
            }
        }
        return true;
    }

    [Property]
    public bool Escape_NeverProducesSingleCloseBracket(NonNull<string> input)
    {
        var escaped = AnsiMarkup.Escape(input.Get);
        for (var i = 0; i < escaped.Length; i++)
        {
            if (escaped[i] == ']')
            {
                if (i + 1 >= escaped.Length || escaped[i + 1] != ']') return false;
                i++;
            }
        }
        return true;
    }

    [Property]
    public bool Escape_OnStringWithNoBrackets_IsIdentity(NonNull<string> input)
    {
        var noBrackets = new string(input.Get.Where(c => c != '[' && c != ']').ToArray());
        return AnsiMarkup.Escape(noBrackets) == noBrackets;
    }

    [Property]
    public bool Escape_LengthAtLeastOriginalLength(NonNull<string> input)
    {
        return AnsiMarkup.Escape(input.Get).Length >= input.Get.Length;
    }

    [Fact]
    public void Escape_NullInput_ReturnsEmpty()
    {
        AnsiMarkup.Escape(null).Should().BeEmpty();
    }

    [Fact]
    public void Escape_EmptyInput_ReturnsEmpty()
    {
        AnsiMarkup.Escape(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Escape_DoublesBrackets()
    {
        AnsiMarkup.Escape("[red]Hello[/]").Should().Be("[[red]]Hello[[/]]");
    }

    [Fact]
    public void Remove_NullInput_ReturnsEmpty()
    {
        AnsiMarkup.Remove(null).Should().BeEmpty();
    }

    [Fact]
    public void Remove_WhitespaceOnly_ReturnsEmpty()
    {
        AnsiMarkup.Remove("   ").Should().BeEmpty();
    }

    [Fact]
    public void Remove_StripsMarkupTags()
    {
        AnsiMarkup.Remove("[bold]Hello[/] [red]World[/]").Should().Be("Hello World");
    }

    [Fact]
    public void Remove_EscapedBrackets_BecomeLiteralBrackets()
    {
        AnsiMarkup.Remove("[[foo]]").Should().Be("[foo]");
    }

    [Fact]
    public void Parse_BalancedMarkup_ReturnsSegments()
    {
        var segments = AnsiMarkup.Parse("[bold]Hello[/] World").ToList();
        var text = string.Concat(segments.Select(s => s.Text));
        text.Should().Be("Hello World");
    }

    [Fact]
    public void Parse_PlainText_ReturnsSingleSegmentWithPlainStyle()
    {
        var segments = AnsiMarkup.Parse("Hello World").ToList();
        segments.Should().ContainSingle();
        segments[0].Style.Should().Be(Style.Plain);
    }
}
