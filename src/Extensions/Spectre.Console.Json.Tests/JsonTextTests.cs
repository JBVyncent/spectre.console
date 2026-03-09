namespace Spectre.Console.Json.Tests;

public sealed class JsonTextTests
{
    private static string Render(string json, Action<JsonText>? configure = null)
    {
        using var console = new TestConsole();
        var jt = new JsonText(json);
        configure?.Invoke(jt);
        console.Write(jt);
        return console.Output;
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Null_Throws()
    {
        var act = () => new JsonText(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Basic rendering ───────────────────────────────────────────────────────

    [Fact]
    public void Render_Number_OutputContainsNumber()
    {
        var output = Render("42");
        output.Should().Contain("42");
    }

    [Fact]
    public void Render_String_OutputContainsString()
    {
        var output = Render("\"hello\"");
        output.Should().Contain("\"hello\"");
    }

    [Fact]
    public void Render_BooleanTrue_OutputContainsTrue()
    {
        var output = Render("true");
        output.Should().Contain("true");
    }

    [Fact]
    public void Render_BooleanFalse_OutputContainsFalse()
    {
        var output = Render("false");
        output.Should().Contain("false");
    }

    [Fact]
    public void Render_Null_OutputContainsNull()
    {
        var output = Render("null");
        output.Should().Contain("null");
    }

    [Fact]
    public void Render_EmptyObject_OutputContainsBraces()
    {
        var output = Render("{}");
        output.Should().Contain("{");
        output.Should().Contain("}");
    }

    [Fact]
    public void Render_EmptyArray_OutputContainsBrackets()
    {
        var output = Render("[]");
        output.Should().Contain("[");
        output.Should().Contain("]");
    }

    [Fact]
    public void Render_ObjectWithMember_OutputContainsMemberNameAndValue()
    {
        var output = Render("{\"name\":\"Alice\"}");
        output.Should().Contain("\"name\"");
        output.Should().Contain("\"Alice\"");
    }

    [Fact]
    public void Render_ObjectWithMultipleMembers_OutputContainsComma()
    {
        var output = Render("{\"a\":1,\"b\":2}");
        output.Should().Contain(",");
    }

    [Fact]
    public void Render_ObjectLastMember_NoTrailingComma()
    {
        var output = Render("{\"a\":1}");
        // After the value, there should be a newline but no comma
        output.Should().NotMatchRegex(@"1,\s*}");
    }

    [Fact]
    public void Render_ArrayWithMultipleItems_OutputContainsComma()
    {
        var output = Render("[1,2,3]");
        output.Should().Contain(",");
    }

    [Fact]
    public void Render_ArrayLastItem_NoTrailingComma()
    {
        var output = Render("[1,2]");
        // Last item (2) should not be followed by a comma before ]
        output.Should().NotMatchRegex(@"2,\s*]");
    }

    // ── Parser property ───────────────────────────────────────────────────────

    [Fact]
    public void Parser_DefaultIsNull_UsesSharedParser()
    {
        var jt = new JsonText("42");
        jt.Parser.Should().BeNull();
        // Should render without throwing
        using var console = new TestConsole();
        console.Write(jt);
        console.Output.Should().Contain("42");
    }

    [Fact]
    public void Parser_WhenSet_UsesCustomParser()
    {
        var customParser = new CustomParser("99");
        using var console = new TestConsole();
        var jt = new JsonText("ignored") { Parser = customParser };
        console.Write(jt);
        console.Output.Should().Contain("99");
    }

    [Fact]
    public void Parser_SettingToNull_ResetsToSharedParser()
    {
        using var console = new TestConsole();
        var jt = new JsonText("42");
        jt.Parser = new CustomParser("99");
        jt.Parser = null; // reset
        console.Write(jt);
        console.Output.Should().Contain("42");
    }

    [Fact]
    public void Parser_Setter_UpdatesParserProperty()
    {
        // Verifies that the Parser property reflects the newly set parser.
        // Note: JustInTimeRenderable caches the rendered result; the new parser
        // is used only on the FIRST render after setting it (before any render).
        var jt = new JsonText("42");
        var customParser = new CustomParser("77");
        jt.Parser = customParser;
        jt.Parser.Should().BeSameAs(customParser);
    }

    // ── Style defaults ────────────────────────────────────────────────────────

    [Fact]
    public void Styles_DefaultNull_RendersWithDefaultColors()
    {
        // All styles are null by default; rendering should still work
        var jt = new JsonText("{\"k\":true}");
        jt.BracesStyle.Should().BeNull();
        jt.BracketsStyle.Should().BeNull();
        jt.MemberStyle.Should().BeNull();
        jt.ColonStyle.Should().BeNull();
        jt.CommaStyle.Should().BeNull();
        jt.StringStyle.Should().BeNull();
        jt.NumberStyle.Should().BeNull();
        jt.BooleanStyle.Should().BeNull();
        jt.NullStyle.Should().BeNull();
    }

    [Fact]
    public void Styles_CustomStyles_RendersWithoutThrowing()
    {
        var output = Render("{\"k\":1}", jt =>
        {
            jt.BracesStyle = new Style(Color.Red);
            jt.BracketsStyle = new Style(Color.Blue);
            jt.MemberStyle = new Style(Color.Green);
            jt.ColonStyle = new Style(Color.Yellow);
            jt.CommaStyle = new Style(Color.Magenta);
            jt.StringStyle = new Style(Color.Cyan);
            jt.NumberStyle = new Style(Color.White);
            jt.BooleanStyle = new Style(Color.Orange1);
            jt.NullStyle = new Style(Color.Grey);
        });
        output.Should().Contain("\"k\"");
        output.Should().Contain("1");
    }

    // ── Invalid JSON ──────────────────────────────────────────────────────────

    [Fact]
    public void Render_InvalidJson_Throws()
    {
        var jt = new JsonText("not json");
        using var console = new TestConsole();
        var act = () => console.Write(jt);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Style ANSI output (NullCoalescing + Object initializer mutations) ──────

    private static string RenderAnsi(string json, Action<JsonText>? configure = null)
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = true;
        var jt = new JsonText(json);
        configure?.Invoke(jt);
        console.Write(jt);
        return console.Output;
    }

    [Fact]
    public void Render_DefaultStyles_AnsiOutputContainsEscapeCodes()
    {
        // Object initializer mutation: new JsonTextStyles {} (all null) → no ANSI codes emitted
        var output = RenderAnsi("42");
        output.Should().Contain("\u001b[");
    }

    [Fact]
    public void BracesStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        // NullCoalescing mutation: BracesStyle ?? Color.Grey → Color.Grey (ignores custom style)
        var defaultOutput = RenderAnsi("{}");
        var customOutput = RenderAnsi("{}", jt => jt.BracesStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void BracketsStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        var defaultOutput = RenderAnsi("[]");
        var customOutput = RenderAnsi("[]", jt => jt.BracketsStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void MemberStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        var defaultOutput = RenderAnsi("{\"k\":1}");
        var customOutput = RenderAnsi("{\"k\":1}", jt => jt.MemberStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void ColonStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        var defaultOutput = RenderAnsi("{\"k\":1}");
        var customOutput = RenderAnsi("{\"k\":1}", jt => jt.ColonStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void CommaStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        var defaultOutput = RenderAnsi("{\"a\":1,\"b\":2}");
        var customOutput = RenderAnsi("{\"a\":1,\"b\":2}", jt => jt.CommaStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void StringStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        // Default StringStyle is Color.Red; use a different color to detect the NullCoalescing mutation.
        var defaultOutput = RenderAnsi("\"hello\"");
        var customOutput = RenderAnsi("\"hello\"", jt => jt.StringStyle = new Style(Color.Blue));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void NumberStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        var defaultOutput = RenderAnsi("42");
        var customOutput = RenderAnsi("42", jt => jt.NumberStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void BooleanStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        var defaultOutput = RenderAnsi("true");
        var customOutput = RenderAnsi("true", jt => jt.BooleanStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }

    [Fact]
    public void NullStyle_CustomColor_AnsiOutputDiffersFromDefault()
    {
        var defaultOutput = RenderAnsi("null");
        var customOutput = RenderAnsi("null", jt => jt.NullStyle = new Style(Color.Red));
        customOutput.Should().NotBe(defaultOutput);
    }
}

file sealed class CustomParser : IJsonParser
{
    private readonly string _lexeme;

    public CustomParser(string lexeme) => _lexeme = lexeme;

    public JsonSyntax Parse(string json) => new JsonNumber(_lexeme);
}
