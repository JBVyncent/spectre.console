namespace Spectre.Console.Ansi.Tests;

public sealed class AnsiMarkupTests
{
    public sealed class TheParseMethod
    {
        [Theory]
        [InlineData("[yellow]Hello[", "Encountered malformed markup tag at position 14.")]
        [InlineData("[yellow]Hello[/", "Encountered malformed markup tag at position 15.")]
        [InlineData("[yellow]Hello[/foo", "Encountered malformed markup tag at position 15.")]
        [InlineData("[yellow Hello", "Encountered malformed markup tag at position 13.")]
        [InlineData("[yellow[green]]Hello", "Encountered malformed markup tag at position 7.")]
        public void Should_Throw_If_Encounters_Malformed_Tag(string markup, string expected)
        {
            // Given, When
            var result = Record.Exception(() => AnsiMarkup.Parse(markup));

            // Then
            result.Should().BeOfType<InvalidOperationException>()
                    .Which.Message.Should().Be(expected);
        }

        [Fact]
        public void Should_Throw_If_Tags_Are_Unbalanced()
        {
            // Given, When
            var result = Record.Exception(() => AnsiMarkup.Parse("[yellow][blue]Hello[/]"));

            // Then
            result.Should().BeOfType<InvalidOperationException>()
                    .Which.Message.Should().Be("Unbalanced markup stack. Did you forget to close a tag?");
        }

        [Fact]
        public void Should_Throw_If_Encounters_Closing_Tag()
        {
            // Given, When
            var result = Record.Exception(() => AnsiMarkup.Parse("Hello[/]World"));

            // Then
            result.Should().BeOfType<InvalidOperationException>()
                    .Which.Message.Should().Be("Encountered closing tag when none was expected near position 5.");
        }
    }

    public sealed class TheParseLinkScopeMethod
    {
        [Fact]
        public void Link_Should_Not_Leak_Past_Closing_Tag()
        {
            // [link=a]x[/]y — 'y' should have no link
            var segments = AnsiMarkup.Parse("[link=https://example.com]click[/]plain").ToList();
            segments.Should().HaveCount(2);
            segments[0].Link.Should().NotBeNull();
            segments[0].Text.Should().Be("click");
            segments[1].Link.Should().BeNull();
            segments[1].Text.Should().Be("plain");
        }

        [Fact]
        public void Nested_Link_Should_Override_Outer_Link()
        {
            // [link=a][link=b]inner[/]outer[/] — inner should use b, outer should use a
            var segments = AnsiMarkup.Parse("[link=https://a.com][link=https://b.com]inner[/]outer[/]").ToList();
            segments.Should().HaveCount(2);
            segments[0].Link!.Url.Should().Be("https://b.com");
            segments[0].Text.Should().Be("inner");
            segments[1].Link!.Url.Should().Be("https://a.com");
            segments[1].Text.Should().Be("outer");
        }

        [Fact]
        public void Link_Should_Restore_To_Null_After_Close()
        {
            // "before" = plain+null link, "linked" = plain+link, "after" = plain+null link
            // "after" can't merge with "linked" (different link), so 3 segments
            var segments = AnsiMarkup.Parse("before[link=https://x.com]linked[/]after").ToList();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("before");
            segments[0].Link.Should().BeNull();
            segments[1].Text.Should().Be("linked");
            segments[1].Link.Should().NotBeNull();
            segments[2].Text.Should().Be("after");
            segments[2].Link.Should().BeNull();
        }

        [Fact]
        public void Link_With_Style_Should_Scope_Both()
        {
            var segments = AnsiMarkup.Parse("[red link=https://x.com]styled[/]plain").ToList();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("styled");
            segments[0].Link.Should().NotBeNull();
            segments[0].Style.Foreground.Should().Be(Color.Red);
            segments[1].Text.Should().Be("plain");
            segments[1].Link.Should().BeNull();
        }
    }

    public sealed class TheSegmentToStringMethod
    {
        [Fact]
        public void Should_Include_Link_In_Markup()
        {
            var segment = new AnsiMarkupSegment("click", Style.Plain, new Link("https://example.com"));
            segment.ToString().Should().Be("[link=https://example.com]click[/]");
        }

        [Fact]
        public void Should_Include_Both_Style_And_Link()
        {
            var segment = new AnsiMarkupSegment("click", new Style(Color.Red), new Link("https://example.com"));
            segment.ToString().Should().Be("[red link=https://example.com]click[/]");
        }

        [Fact]
        public void Should_Output_Plain_Text_When_No_Style_Or_Link()
        {
            var segment = new AnsiMarkupSegment("plain", Style.Plain, null);
            segment.ToString().Should().Be("plain");
        }

        [Fact]
        public void Should_Output_Style_Only_When_No_Link()
        {
            var segment = new AnsiMarkupSegment("styled", new Style(Color.Blue), null);
            segment.ToString().Should().Be("[blue]styled[/]");
        }

        [Fact]
        public void Should_Output_Link_Keyword_For_EmptyLink()
        {
            var segment = new AnsiMarkupSegment("auto", Style.Plain, new Link(Constants.EmptyLink));
            segment.ToString().Should().Be("[link]auto[/]");
        }

        [Fact]
        public void Should_Escape_Markup_In_Text_With_Link()
        {
            var segment = new AnsiMarkupSegment("[test]", Style.Plain, new Link("https://x.com"));
            segment.ToString().Should().Be("[link=https://x.com][[test]][/]");
        }
    }

    public sealed class TheEscapeMethod
    {
        [Theory]
        [InlineData("Hello World", "Hello World")]
        [InlineData("Hello World [", "Hello World [[")]
        [InlineData("Hello World ]", "Hello World ]]")]
        [InlineData("Hello [World]", "Hello [[World]]")]
        [InlineData("Hello [[World]]", "Hello [[[[World]]]]")]
        public void Should_Escape_Markup_As_Expected(string input, string expected)
        {
            // Given, When
            var result = AnsiMarkup.Escape(input);

            // Then
            result.Should().Be(expected);
        }
    }

    public sealed class TheRemoveMethod
    {
        [Theory]
        [InlineData("Hello World", "Hello World")]
        [InlineData("Hello [blue]World", "Hello World")]
        [InlineData("Hello [blue]World[/]", "Hello World")]
        [InlineData("[grey][[grey]][/][white][[white]][/]", "[grey][white]")]
        public void Should_Remove_Markup_From_Text(string input, string expected)
        {
            // Given, When
            var result = AnsiMarkup.Remove(input);

            // Then
            result.Should().Be(expected);
        }
    }

    public sealed class TheHighlightMethod
    {
        private readonly Style _highlightStyle =
            new Style(foreground: Color.Default, background: Color.Yellow, Decoration.Bold);

        [Fact]
        public void Should_Return_Same_Value_When_SearchText_Is_Empty()
        {
            // Given
            var value = "Sample text";
            var searchText = string.Empty;
            var highlightStyle = new Style();

            // When
            var result = AnsiMarkup.Highlight(value, searchText, highlightStyle);

            // Then
            result.Should().Be(value);
        }

        [Fact]
        public void Should_Highlight_Matched_Text()
        {
            // Given
            var value = "Sample text with test word";
            var searchText = "test";
            var highlightStyle = _highlightStyle;

            // When
            var result = AnsiMarkup.Highlight(value, searchText, highlightStyle);

            // Then
            result.Should().Be("Sample text with [bold on yellow]test[/] word");
        }

        [Fact]
        public void Should_Match_Text_Across_Tokens()
        {
            // Given
            var value = "[red]Sample text[/] with test word";
            var searchText = "text with";
            var highlightStyle = _highlightStyle;

            // When
            var result = AnsiMarkup.Highlight(value, searchText, highlightStyle);

            // Then
            result.Should().Be("[red]Sample [/][bold on yellow]text with[/] test word");
        }

        [Fact]
        public void Should_Highlight_Only_First_Matched_Text()
        {
            // Given
            var value = "Sample text with test word";
            var searchText = "te";
            var highlightStyle = _highlightStyle;

            // When
            var result = AnsiMarkup.Highlight(value, searchText, highlightStyle);

            // Then
            result.Should().Be("Sample [bold on yellow]te[/]xt with test word");
        }

        [Fact]
        public void Should_Not_Match_Text_Outside_Of_Text_Tokens()
        {
            // Given
            var value = "[red]Sample text with test word[/]";
            var searchText = "red";
            var highlightStyle = _highlightStyle;

            // When
            var result = AnsiMarkup.Highlight(value, searchText, highlightStyle);

            // Then
            result.Should().Be(value);
        }

        [Fact]
        public void Should_Highlight_Case_Insensitive_When_Specified()
        {
            // Given
            var value = "Hello World";
            var searchText = "hello";

            // When
            var result = AnsiMarkup.Highlight(value, searchText, _highlightStyle, StringComparison.OrdinalIgnoreCase);

            // Then
            result.Should().Be("[bold on yellow]Hello[/] World");
        }

        [Fact]
        public void Should_Not_Highlight_Case_Insensitive_By_Default()
        {
            // Given
            var value = "Hello World";
            var searchText = "hello";

            // When
            var result = AnsiMarkup.Highlight(value, searchText, _highlightStyle);

            // Then — Ordinal (default) does not match
            result.Should().Be("Hello World");
        }

        [Fact]
        public void Should_Highlight_Text_With_Escaped_Brackets()
        {
            // Given — text like "[[01]] My first item" (escaped brackets in markup)
            var value = "[[[[01]]]] My first item";
            var searchText = "first";

            // When
            var result = AnsiMarkup.Highlight(value, searchText, _highlightStyle);

            // Then — the brackets should be preserved and "first" highlighted
            result.Should().Be("[[[[01]]]] My [bold on yellow]first[/] item");
        }

        [Fact]
        public void Should_Highlight_Case_Insensitive_With_Escaped_Brackets()
        {
            // Given — text with escaped brackets and case-insensitive search
            var value = "[[[[01]]]] My First Item";
            var searchText = "first";

            // When
            var result = AnsiMarkup.Highlight(value, searchText, _highlightStyle, StringComparison.OrdinalIgnoreCase);

            // Then
            result.Should().Be("[[[[01]]]] My [bold on yellow]First[/] Item");
        }

        [Fact]
        public void Should_Highlight_Bracket_Character_In_Escaped_Text()
        {
            // Given — text with escaped brackets, searching for bracket char
            var value = "[[01]] My item";
            var searchText = "[";

            // When — search for literal [ in the plain text
            var result = AnsiMarkup.Highlight(value, searchText, _highlightStyle);

            // Then — the [ should be highlighted while preserving valid markup
            // Parse the result to ensure it's valid markup
            var parsed = AnsiMarkup.Parse(result);
            var plainText = string.Concat(parsed.Select(s => s.Text));
            plainText.Should().Be("[01] My item");
        }
    }

    public sealed class TheWriteMethod
    {
        [Fact]
        public void Should_Escape_Markup_Blocks_As_Expected()
        {
            // Given
            var fixture = new AnsiFixture();

            // When
            fixture.Markup.Write("Hello [[ World ]] !");

            // Then
            fixture.Output
                .Should().Be("Hello [ World ] !");
        }

        [Theory]
        [InlineData("[yellow]Hello[/]", "[93mHello[0m")]
        [InlineData("[yellow]Hello [italic]World[/]![/]", "[93mHello [0m[3;93mWorld[0m[93m![0m")]
        public void Should_Output_Expected_Ansi_For_Markup(string text, string expected)
        {
            // Given
            var fixture = new AnsiFixture();
            fixture.Capabilities.ColorSystem = ColorSystem.Standard;

            // When
            fixture.Markup.Write(text);

            // Then
            fixture.Output
                .Should().Be(expected);
        }

        [Fact]
        public void Should_Output_Expected_Ansi_For_Link_With_Url_And_Text()
        {
            // Given
            var fixture = new AnsiFixture();

            // When
            fixture.Markup.Write("[link=https://patriksvensson.se]Click to visit my blog[/]");

            // Then
            fixture.Output.Should().MatchRegex(
                "]8;id=[0-9]*;https:\\/\\/patriksvensson\\.se\\\\Click to visit my blog]8;;\\\\");
        }

        [Fact]
        public void Should_Output_Expected_Ansi_For_Link_With_Only_Url()
        {
            // Given
            var fixture = new AnsiFixture();

            // When
            fixture.Markup.Write("[link]https://patriksvensson.se[/]");

            // Then
            fixture.Output.Should().MatchRegex(
                "]8;id=[0-9]*;https:\\/\\/patriksvensson\\.se\\\\https:\\/\\/patriksvensson\\.se]8;;\\\\");
        }

        [Fact]
        public void Should_Output_Expected_Ansi_For_Link_With_Bracket_In_Url_Only()
        {
            // Given
            var fixture = new AnsiFixture();

            // When
            const string Path = "file://c:/temp/[x].txt";
            fixture.Markup.Write($"[link]{AnsiMarkup.Escape(Path)}[/]");

            // Then
            fixture.Output.Should().MatchRegex(
                "]8;id=[0-9]*;file:\\/\\/c:\\/temp\\/\\[x\\].txt\\\\file:\\/\\/c:\\/temp\\/\\[x\\].txt]8;;\\\\");
        }

        [Fact]
        public void Should_Output_Expected_Ansi_For_Link_With_Bracket_In_Url()
        {
            // Given
            var fixture = new AnsiFixture();
            const string Path = "file://c:/temp/[x].txt";
            var escapedPath = AnsiMarkup.Escape(Path);

            // When
            fixture.Markup.Write($"[link={escapedPath}]{escapedPath}[/]");

            // Then
            fixture.Output.Should().MatchRegex(
                "]8;id=[0-9]*;file:\\/\\/c:\\/temp\\/\\[x\\].txt\\\\file:\\/\\/c:\\/temp\\/\\[x\\].txt]8;;\\\\");
        }

        [Theory]
        [InlineData("[yellow]Hello [[ World[/]", "\e[93mHello [ World\e[0m")]
        public void Should_Be_Able_To_Escape_Tags(string text, string expected)
        {
            // Given
            var fixture = new AnsiFixture();
            fixture.Capabilities.ColorSystem = ColorSystem.Standard;

            // When
            fixture.Markup.Write(text);

            // Then
            fixture.Output
                .Should().Be(expected);
        }
    }
}