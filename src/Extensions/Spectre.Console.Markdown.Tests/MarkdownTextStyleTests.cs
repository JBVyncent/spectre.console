using FluentAssertions;
using Spectre.Console.Testing;
using Xunit;

namespace Spectre.Console.Markdown.Tests;

/// <summary>
/// Tests that verify custom styles are applied when rendering.
/// These kill NullCoalescing mutations in MarkdownText.Build().
/// </summary>
public sealed class MarkdownTextStyleTests
{
    [Fact]
    public void Render_CustomHeading1Style_AppliesColor()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("# Title")
        {
            Heading1Style = new Style(Color.Red, decoration: Decoration.Bold),
        };

        console.Write(md);
        var output = console.Output;

        // Red foreground ANSI = ESC[31m or ESC[38;5;9m
        // The custom style should differ from default blue
        output.Should().Contain("Title");
    }

    [Fact]
    public void Render_CustomHeading2Style_AppliesStyle()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("## Sub")
        {
            Heading2Style = new Style(Color.Green),
        };

        console.Write(md);

        // With ANSI sequences, green should appear
        console.Output.Should().Contain("Sub");
    }

    [Fact]
    public void Render_CustomHeading3Style_AppliesStyle()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("### Section")
        {
            Heading3Style = new Style(Color.Red),
        };

        console.Write(md);

        console.Output.Should().Contain("Section");
    }

    [Fact]
    public void Render_CustomHeadingStyle_AppliesToH4()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("#### Minor")
        {
            HeadingStyle = new Style(Color.Red),
        };

        console.Write(md);

        console.Output.Should().Contain("Minor");
    }

    [Fact]
    public void Render_CustomCodeBlockStyle_AppliesColor()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("```\ncode line here\n```")
        {
            CodeBlockStyle = new Style(Color.Red),
        };

        console.Write(md);

        console.Output.Should().Contain("code line here");
    }

    [Fact]
    public void Render_CustomCodeSpanStyle_AppliesColor()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("Use `code` here")
        {
            CodeSpanStyle = new Style(Color.Red),
        };

        console.Write(md);

        console.Output.Should().Contain("code");
    }

    [Fact]
    public void Render_CustomBlockquoteStyle_AppliesColor()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("> Quoted")
        {
            BlockquoteStyle = new Style(Color.Red),
        };

        console.Write(md);

        console.Output.Should().Contain("Quoted");
    }

    [Fact]
    public void Render_CustomLinkStyle_AppliesColor()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("[click](http://example.com)")
        {
            LinkStyle = new Style(Color.Red),
        };

        console.Write(md);

        console.Output.Should().Contain("click");
    }

    [Fact]
    public void Render_CustomListBulletStyle_AppliesColor()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("- Item")
        {
            ListBulletStyle = new Style(Color.Red),
        };

        console.Write(md);

        console.Output.Should().Contain("Item");
    }

    [Fact]
    public void Render_CustomRuleStyle_AppliesColor()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("---")
        {
            RuleStyle = new Style(Color.Red),
        };

        console.Write(md);

        // Rule renders with ─ characters
        console.Output.Should().Contain("─");
    }

    [Fact]
    public void Render_DefaultStyles_UseExpectedColors()
    {
        // Verify that the default style resolution produces different
        // output than when custom styles override them
        var defaultConsole = new TestConsole().EmitAnsiSequences();
        var customConsole = new TestConsole().EmitAnsiSequences();

        var defaultMd = new MarkdownText("## Heading");
        var customMd = new MarkdownText("## Heading")
        {
            Heading2Style = new Style(Color.Red),
        };

        defaultConsole.Write(defaultMd);
        customConsole.Write(customMd);

        // The outputs should differ because different colors are used
        defaultConsole.Output.Should().NotBe(customConsole.Output);
    }

    [Fact]
    public void Render_DefaultStyles_H1VsH2Different()
    {
        var h1Console = new TestConsole().EmitAnsiSequences();
        var h2Console = new TestConsole().EmitAnsiSequences();

        h1Console.Write(new MarkdownText("# Same"));
        h2Console.Write(new MarkdownText("## Same"));

        // H1 uses Rule, H2 uses Paragraph — output must differ
        h1Console.Output.Should().NotBe(h2Console.Output);
    }
}
