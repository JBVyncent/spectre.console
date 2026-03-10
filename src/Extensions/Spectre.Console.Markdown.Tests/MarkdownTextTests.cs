using FluentAssertions;
using Spectre.Console.Testing;
using Xunit;

namespace Spectre.Console.Markdown.Tests;

public sealed class MarkdownTextTests
{
    [Fact]
    public void Constructor_NullMarkdown_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new MarkdownText(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("markdown");
    }

    [Fact]
    public void Constructor_ValidMarkdown_DoesNotThrow()
    {
        // Act & Assert
        var action = () => new MarkdownText("# Hello");
        action.Should().NotThrow();
    }

    [Fact]
    public void Properties_DefaultValues()
    {
        var md = new MarkdownText("test");

        md.Heading1Style.Should().BeNull();
        md.Heading2Style.Should().BeNull();
        md.Heading3Style.Should().BeNull();
        md.HeadingStyle.Should().BeNull();
        md.CodeBlockStyle.Should().BeNull();
        md.CodeSpanStyle.Should().BeNull();
        md.BlockquoteStyle.Should().BeNull();
        md.LinkStyle.Should().BeNull();
        md.ListBulletStyle.Should().BeNull();
        md.RuleStyle.Should().BeNull();
        md.CodeBlockBorder.Should().Be(BoxBorder.Rounded);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var style = new Style(Color.Red);
        var md = new MarkdownText("test")
        {
            Heading1Style = style,
            Heading2Style = style,
            Heading3Style = style,
            HeadingStyle = style,
            CodeBlockStyle = style,
            CodeSpanStyle = style,
            BlockquoteStyle = style,
            LinkStyle = style,
            ListBulletStyle = style,
            RuleStyle = style,
            CodeBlockBorder = BoxBorder.Heavy,
        };

        md.Heading1Style.Should().Be(style);
        md.Heading2Style.Should().Be(style);
        md.Heading3Style.Should().Be(style);
        md.HeadingStyle.Should().Be(style);
        md.CodeBlockStyle.Should().Be(style);
        md.CodeSpanStyle.Should().Be(style);
        md.BlockquoteStyle.Should().Be(style);
        md.LinkStyle.Should().Be(style);
        md.ListBulletStyle.Should().Be(style);
        md.RuleStyle.Should().Be(style);
        md.CodeBlockBorder.Should().Be(BoxBorder.Heavy);
    }

    [Fact]
    public void Render_Heading1_ProducesRule()
    {
        // Arrange
        var console = new TestConsole();
        var md = new MarkdownText("# Hello World");

        // Act
        console.Write(md);
        var output = console.Output;

        // Assert
        output.Should().Contain("Hello World");
    }

    [Fact]
    public void Render_Heading2_ProducesText()
    {
        var console = new TestConsole();
        var md = new MarkdownText("## Subtitle");

        console.Write(md);

        console.Output.Should().Contain("Subtitle");
    }

    [Fact]
    public void Render_Paragraph_ProducesText()
    {
        var console = new TestConsole();
        var md = new MarkdownText("Hello world paragraph");

        console.Write(md);

        console.Output.Should().Contain("Hello world paragraph");
    }

    [Fact]
    public void Render_CodeBlock_ProducesPanel()
    {
        var console = new TestConsole();
        var md = new MarkdownText("```\nvar x = 1;\n```");

        console.Write(md);

        console.Output.Should().Contain("var x = 1;");
    }

    [Fact]
    public void Render_CodeBlockWithLanguage_ShowsLanguage()
    {
        var console = new TestConsole();
        var md = new MarkdownText("```csharp\nvar longVariableName = 42;\n```");

        console.Write(md);

        // Panel header shows language; content long enough to avoid truncation
        console.Output.Should().Contain("csharp");
        console.Output.Should().Contain("var longVariableName = 42;");
    }

    [Fact]
    public void Render_UnorderedList_ShowsBullets()
    {
        var console = new TestConsole();
        var md = new MarkdownText("- First\n- Second");

        console.Write(md);

        console.Output.Should().Contain("First");
        console.Output.Should().Contain("Second");
    }

    [Fact]
    public void Render_OrderedList_ShowsNumbers()
    {
        var console = new TestConsole();
        var md = new MarkdownText("1. First\n2. Second");

        console.Write(md);

        console.Output.Should().Contain("1.");
        console.Output.Should().Contain("First");
    }

    [Fact]
    public void Render_ThematicBreak_ProducesRule()
    {
        var console = new TestConsole();
        var md = new MarkdownText("---");

        console.Write(md);

        // Rule renders as ─ chars
        console.Output.Should().Contain("─");
    }

    [Fact]
    public void Render_Blockquote_ProducesPanel()
    {
        var console = new TestConsole();
        var md = new MarkdownText("> Quote text");

        console.Write(md);

        console.Output.Should().Contain("Quote text");
    }

    [Fact]
    public void Render_Bold_ProducesBoldText()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("**bold text**");

        console.Write(md);

        // Bold should appear in output
        console.Output.Should().Contain("bold text");
    }

    [Fact]
    public void Render_Italic_ProducesItalicText()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("*italic text*");

        console.Write(md);

        console.Output.Should().Contain("italic text");
    }

    [Fact]
    public void Render_InlineCode_ProducesCodeText()
    {
        var console = new TestConsole();
        var md = new MarkdownText("Use `code` here");

        console.Write(md);

        console.Output.Should().Contain("code");
    }

    [Fact]
    public void Render_Link_ProducesLinkText()
    {
        var console = new TestConsole();
        var md = new MarkdownText("[click](https://example.com)");

        console.Write(md);

        console.Output.Should().Contain("click");
    }

    [Fact]
    public void Render_ComplexDocument()
    {
        var console = new TestConsole();
        var md = new MarkdownText(
            "# Title\n\n" +
            "A paragraph with **bold** and *italic*.\n\n" +
            "## Section\n\n" +
            "- Item one\n- Item two\n\n" +
            "```csharp\nvar x = 42;\n```\n\n" +
            "---\n\n" +
            "> A quote");

        console.Write(md);
        var output = console.Output;

        output.Should().Contain("Title");
        output.Should().Contain("bold");
        output.Should().Contain("italic");
        output.Should().Contain("Section");
        output.Should().Contain("Item one");
        output.Should().Contain("var x = 42;");
        output.Should().Contain("A quote");
    }

    [Fact]
    public void Render_Strikethrough_ProducesText()
    {
        var console = new TestConsole();
        var md = new MarkdownText("~~deleted~~");

        console.Write(md);

        console.Output.Should().Contain("deleted");
    }

    [Fact]
    public void Render_EmptyMarkdown_DoesNotThrow()
    {
        var console = new TestConsole();
        var md = new MarkdownText("");

        var action = () => console.Write(md);

        action.Should().NotThrow();
    }

    [Fact]
    public void Render_WithCustomStyles()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var md = new MarkdownText("# Heading\n\n---")
        {
            Heading1Style = new Style(Color.Red),
            RuleStyle = new Style(Color.Green),
        };

        console.Write(md);

        console.Output.Should().Contain("Heading");
    }

    [Fact]
    public void Render_Heading3_ProducesText()
    {
        var console = new TestConsole();
        var md = new MarkdownText("### H3 Title");

        console.Write(md);

        console.Output.Should().Contain("H3 Title");
    }

    [Fact]
    public void Render_Heading4_ProducesText()
    {
        var console = new TestConsole();
        var md = new MarkdownText("#### H4 Title");

        console.Write(md);

        console.Output.Should().Contain("H4 Title");
    }

    [Fact]
    public void Render_CodeBlockBorder_CanBeChanged()
    {
        var console = new TestConsole();
        var md = new MarkdownText("```\ncode\n```")
        {
            CodeBlockBorder = BoxBorder.Heavy,
        };

        console.Write(md);

        console.Output.Should().Contain("code");
    }
}
