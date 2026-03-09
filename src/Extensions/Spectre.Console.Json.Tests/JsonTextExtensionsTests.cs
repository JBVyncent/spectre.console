namespace Spectre.Console.Json.Tests;

public sealed class JsonTextExtensionsTests
{
    // ── Null guards ───────────────────────────────────────────────────────────

    [Fact] public void BracesStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).BracesStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void BracketStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).BracketStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void MemberStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).MemberStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void ColonStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).ColonStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void CommaStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).CommaStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void StringStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).StringStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void NumberStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).NumberStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void BooleanStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).BooleanStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void NullStyle_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).NullStyle(Style.Plain))).Should().Throw<ArgumentNullException>();

    [Fact] public void BracesColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).BracesColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void BracketColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).BracketColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void MemberColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).MemberColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void ColonColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).ColonColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void CommaColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).CommaColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void StringColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).StringColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void NumberColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).NumberColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void BooleanColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).BooleanColor(Color.Red))).Should().Throw<ArgumentNullException>();

    [Fact] public void NullColor_Null_Throws() =>
        ((Action)(() => ((JsonText)null!).NullColor(Color.Red))).Should().Throw<ArgumentNullException>();

    // ── Style setters — property assignment and self-return ───────────────────

    [Fact]
    public void BracesStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("{}");
        var result = jt.BracesStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.BracesStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void BracketStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("[]");
        var result = jt.BracketStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.BracketsStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void MemberStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("{\"k\":1}");
        var result = jt.MemberStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.MemberStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void ColonStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("{\"k\":1}");
        var result = jt.ColonStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.ColonStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void CommaStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("[1,2]");
        var result = jt.CommaStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.CommaStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void StringStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("\"s\"");
        var result = jt.StringStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.StringStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void NumberStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("1");
        var result = jt.NumberStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.NumberStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void BooleanStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("true");
        var result = jt.BooleanStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.BooleanStyle.Should().Be(Style.Plain);
    }

    [Fact]
    public void NullStyle_SetsProperty_ReturnsSelf()
    {
        var jt = new JsonText("null");
        var result = jt.NullStyle(Style.Plain);
        result.Should().BeSameAs(jt);
        jt.NullStyle.Should().Be(Style.Plain);
    }

    // ── Color setters — creates Style with correct color ──────────────────────

    [Fact]
    public void BracesColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("{}");
        var result = jt.BracesColor(Color.Red);
        result.Should().BeSameAs(jt);
        jt.BracesStyle.Should().Be(new Style(Color.Red));
    }

    [Fact]
    public void BracketColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("[]");
        var result = jt.BracketColor(Color.Blue);
        result.Should().BeSameAs(jt);
        jt.BracketsStyle.Should().Be(new Style(Color.Blue));
    }

    [Fact]
    public void MemberColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("{\"k\":1}");
        var result = jt.MemberColor(Color.Green);
        result.Should().BeSameAs(jt);
        jt.MemberStyle.Should().Be(new Style(Color.Green));
    }

    [Fact]
    public void ColonColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("{\"k\":1}");
        var result = jt.ColonColor(Color.Yellow);
        result.Should().BeSameAs(jt);
        jt.ColonStyle.Should().Be(new Style(Color.Yellow));
    }

    [Fact]
    public void CommaColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("[1,2]");
        var result = jt.CommaColor(Color.Magenta);
        result.Should().BeSameAs(jt);
        jt.CommaStyle.Should().Be(new Style(Color.Magenta));
    }

    [Fact]
    public void StringColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("\"s\"");
        var result = jt.StringColor(Color.Cyan);
        result.Should().BeSameAs(jt);
        jt.StringStyle.Should().Be(new Style(Color.Cyan));
    }

    [Fact]
    public void NumberColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("1");
        var result = jt.NumberColor(Color.White);
        result.Should().BeSameAs(jt);
        jt.NumberStyle.Should().Be(new Style(Color.White));
    }

    [Fact]
    public void BooleanColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("true");
        var result = jt.BooleanColor(Color.Orange1);
        result.Should().BeSameAs(jt);
        jt.BooleanStyle.Should().Be(new Style(Color.Orange1));
    }

    [Fact]
    public void NullColor_SetsStyleWithColor_ReturnsSelf()
    {
        var jt = new JsonText("null");
        var result = jt.NullColor(Color.Grey);
        result.Should().BeSameAs(jt);
        jt.NullStyle.Should().Be(new Style(Color.Grey));
    }

    // ── Fluent chaining ───────────────────────────────────────────────────────

    [Fact]
    public void FluentChaining_MultipleMethods_AllApplied()
    {
        var jt = new JsonText("{}")
            .BracesColor(Color.Red)
            .BracketColor(Color.Blue)
            .MemberColor(Color.Green);

        jt.BracesStyle.Should().Be(new Style(Color.Red));
        jt.BracketsStyle.Should().Be(new Style(Color.Blue));
        jt.MemberStyle.Should().Be(new Style(Color.Green));
    }

    // ── Null style is allowed ─────────────────────────────────────────────────

    [Fact]
    public void BracesStyle_NullValue_SetsToNull()
    {
        var jt = new JsonText("{}");
        jt.BracesStyle(new Style(Color.Red));
        jt.BracesStyle((Style?)null);
        jt.BracesStyle.Should().BeNull();
    }
}
