namespace Spectre.Console.Json.Tests;

public sealed class JsonTokenizerTests
{
    // ── Structural tokens ────────────────────────────────────────────────────

    [Fact] public void Tokenize_LeftBrace() => AssertSingleToken("{", JsonTokenType.LeftBrace);
    [Fact] public void Tokenize_RightBrace() => AssertSingleToken("}", JsonTokenType.RightBrace);
    [Fact] public void Tokenize_LeftBracket() => AssertSingleToken("[", JsonTokenType.LeftBracket);
    [Fact] public void Tokenize_RightBracket() => AssertSingleToken("]", JsonTokenType.RightBracket);
    [Fact] public void Tokenize_Colon() => AssertSingleToken(":", JsonTokenType.Colon);
    [Fact] public void Tokenize_Comma() => AssertSingleToken(",", JsonTokenType.Comma);

    private static void AssertSingleToken(string input, JsonTokenType expected)
    {
        var tokens = JsonTokenizer.Tokenize(input);
        tokens.Should().HaveCount(1);
        tokens[0].Type.Should().Be(expected);
        tokens[0].Lexeme.Should().Be(input);
    }

    // ── Keyword tokens ────────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_True_ReturnsBooleanToken()
    {
        var tokens = JsonTokenizer.Tokenize("true");
        tokens.Should().HaveCount(1);
        tokens[0].Type.Should().Be(JsonTokenType.Boolean);
        tokens[0].Lexeme.Should().Be("true");
    }

    [Fact]
    public void Tokenize_False_ReturnsBooleanToken()
    {
        var tokens = JsonTokenizer.Tokenize("false");
        tokens[0].Type.Should().Be(JsonTokenType.Boolean);
        tokens[0].Lexeme.Should().Be("false");
    }

    [Fact]
    public void Tokenize_Null_ReturnsNullToken()
    {
        var tokens = JsonTokenizer.Tokenize("null");
        tokens[0].Type.Should().Be(JsonTokenType.Null);
        tokens[0].Lexeme.Should().Be("null");
    }

    // ── String tokens ─────────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_SimpleString_ReturnsStringToken()
    {
        var tokens = JsonTokenizer.Tokenize("\"hello\"");
        tokens.Should().HaveCount(1);
        tokens[0].Type.Should().Be(JsonTokenType.String);
        tokens[0].Lexeme.Should().Be("\"hello\"");
    }

    [Fact]
    public void Tokenize_EmptyString_ReturnsStringToken()
    {
        var tokens = JsonTokenizer.Tokenize("\"\"");
        tokens[0].Type.Should().Be(JsonTokenType.String);
        tokens[0].Lexeme.Should().Be("\"\"");
    }

    [Fact]
    public void Tokenize_EscapedQuote_ParsesCorrectly()
    {
        var input = "\"\\\"\"";
        var tokens = JsonTokenizer.Tokenize(input);
        tokens[0].Type.Should().Be(JsonTokenType.String);
        tokens[0].Lexeme.Should().Be(input);
    }

    [Fact]
    public void Tokenize_EscapedBackslash_ParsesCorrectly()
    {
        var input = "\"\\\\\"";
        var tokens = JsonTokenizer.Tokenize(input);
        tokens[0].Type.Should().Be(JsonTokenType.String);
        tokens[0].Lexeme.Should().Be(input);
    }

    [Fact] public void Tokenize_EscapedSlash() { var t = JsonTokenizer.Tokenize("\"\\/\""); t[0].Type.Should().Be(JsonTokenType.String); }
    [Fact] public void Tokenize_EscapedNewline() { var t = JsonTokenizer.Tokenize("\"\\n\""); t[0].Type.Should().Be(JsonTokenType.String); }
    [Fact] public void Tokenize_EscapedTab() { var t = JsonTokenizer.Tokenize("\"\\t\""); t[0].Type.Should().Be(JsonTokenType.String); }
    [Fact] public void Tokenize_EscapedFormFeed() { var t = JsonTokenizer.Tokenize("\"\\f\""); t[0].Type.Should().Be(JsonTokenType.String); }
    [Fact] public void Tokenize_EscapedBackspace() { var t = JsonTokenizer.Tokenize("\"\\b\""); t[0].Type.Should().Be(JsonTokenType.String); }
    [Fact] public void Tokenize_EscapedCR() { var t = JsonTokenizer.Tokenize("\"\\r\""); t[0].Type.Should().Be(JsonTokenType.String); }

    [Fact]
    public void Tokenize_EscapedUnicode_ParsesCorrectly()
    {
        var tokens = JsonTokenizer.Tokenize("\"\\u0041\"");
        tokens[0].Type.Should().Be(JsonTokenType.String);
        tokens[0].Lexeme.Should().Contain("\\u0041");
    }

    [Fact]
    public void Tokenize_InvalidEscape_Throws()
    {
        var act = () => JsonTokenizer.Tokenize("\"\\x\"");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Unicode escape validation ────────────────────────────────────────────

    [Theory]
    [InlineData("\"\\u0041\"")]   // \uXXXX with uppercase hex
    [InlineData("\"\\u00e9\"")]   // \uXXXX with lowercase hex
    [InlineData("\"\\uFFFF\"")]   // \uXXXX max value
    [InlineData("\"\\u0000\"")]   // \uXXXX null char
    public void Tokenize_ValidUnicodeEscape_Succeeds(string input)
    {
        var tokens = JsonTokenizer.Tokenize(input);
        tokens.Should().HaveCount(1);
        tokens[0].Type.Should().Be(JsonTokenType.String);
        tokens[0].Lexeme.Should().Be(input);
    }

    [Theory]
    [InlineData("\"\\u\"")]       // \u with no hex digits
    [InlineData("\"\\u0\"")]      // \u with 1 hex digit
    [InlineData("\"\\u00\"")]     // \u with 2 hex digits
    [InlineData("\"\\u004\"")]    // \u with 3 hex digits
    [InlineData("\"\\uGGGG\"")]   // \u with non-hex chars
    [InlineData("\"\\u00G0\"")]   // \u with non-hex in middle
    public void Tokenize_InvalidUnicodeEscape_Throws(string input)
    {
        var act = () => JsonTokenizer.Tokenize(input);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Tokenize_UnicodeEscape_PreservesHexDigitsInLexeme()
    {
        var tokens = JsonTokenizer.Tokenize("\"abc\\u0041def\"");
        tokens[0].Lexeme.Should().Be("\"abc\\u0041def\"");
    }

    [Fact]
    public void Tokenize_MultipleUnicodeEscapes_AllValidated()
    {
        var tokens = JsonTokenizer.Tokenize("\"\\u0041\\u0042\"");
        tokens[0].Lexeme.Should().Be("\"\\u0041\\u0042\"");
    }

    [Fact]
    public void Tokenize_UnterminatedString_Throws()
    {
        var act = () => JsonTokenizer.Tokenize("\"unterminated");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Number tokens ─────────────────────────────────────────────────────────

    [Fact] public void Tokenize_Zero() { var t = JsonTokenizer.Tokenize("0"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("0"); }
    [Fact] public void Tokenize_PositiveInt() { var t = JsonTokenizer.Tokenize("42"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("42"); }
    [Fact] public void Tokenize_NegativeInt() { var t = JsonTokenizer.Tokenize("-5"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("-5"); }
    [Fact] public void Tokenize_Decimal() { var t = JsonTokenizer.Tokenize("3.14"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("3.14"); }
    [Fact] public void Tokenize_SciLower() { var t = JsonTokenizer.Tokenize("1e10"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("1e10"); }
    [Fact] public void Tokenize_SciUpper() { var t = JsonTokenizer.Tokenize("1E10"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("1E10"); }
    [Fact] public void Tokenize_SciPlus() { var t = JsonTokenizer.Tokenize("1e+10"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("1e+10"); }
    [Fact] public void Tokenize_SciMinus() { var t = JsonTokenizer.Tokenize("1e-10"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("1e-10"); }
    [Fact] public void Tokenize_DecimalSci() { var t = JsonTokenizer.Tokenize("1.5e2"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("1.5e2"); }
    [Fact] public void Tokenize_NegDecimal() { var t = JsonTokenizer.Tokenize("-0.5"); t[0].Type.Should().Be(JsonTokenType.Number); t[0].Lexeme.Should().Be("-0.5"); }

    [Fact]
    public void Tokenize_NegativeWithNoDigits_Throws()
    {
        var act = () => JsonTokenizer.Tokenize("-abc");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Whitespace handling ───────────────────────────────────────────────────

    [Fact] public void Tokenize_LeadingSpace_IsSkipped() { var t = JsonTokenizer.Tokenize(" 42"); t.Should().HaveCount(1); t[0].Type.Should().Be(JsonTokenType.Number); }
    [Fact] public void Tokenize_LeadingTab_IsSkipped() { var t = JsonTokenizer.Tokenize("\t42"); t.Should().HaveCount(1); }
    [Fact] public void Tokenize_LeadingNewline_IsSkipped() { var t = JsonTokenizer.Tokenize("\n42"); t.Should().HaveCount(1); }
    [Fact] public void Tokenize_LeadingCR_IsSkipped() { var t = JsonTokenizer.Tokenize("\r42"); t.Should().HaveCount(1); }
    [Fact] public void Tokenize_EmptyInput_ReturnsNoTokens() { JsonTokenizer.Tokenize("").Should().BeEmpty(); }

    [Fact]
    public void Tokenize_StringWithTrailingBackslash_Throws()
    {
        // Covers the buffer.Eof early-exit path in ReadString (backslash at end of input)
        var act = () => JsonTokenizer.Tokenize("\"\\");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Invalid input ─────────────────────────────────────────────────────────

    [Fact] public void Tokenize_InvalidKeyword_Throws() { ((Action)(() => JsonTokenizer.Tokenize("invalid"))).Should().Throw<InvalidOperationException>(); }
    [Fact] public void Tokenize_InvalidCharacter_Throws() { ((Action)(() => JsonTokenizer.Tokenize("@"))).Should().Throw<InvalidOperationException>(); }

    // ── Multi-token sequences ─────────────────────────────────────────────────

    [Fact]
    public void Tokenize_SimpleObject_ReturnsCorrectTokenSequence()
    {
        var tokens = JsonTokenizer.Tokenize("{\"k\":1}");
        tokens.Should().HaveCount(5);
        tokens[0].Type.Should().Be(JsonTokenType.LeftBrace);
        tokens[1].Type.Should().Be(JsonTokenType.String);
        tokens[2].Type.Should().Be(JsonTokenType.Colon);
        tokens[3].Type.Should().Be(JsonTokenType.Number);
        tokens[4].Type.Should().Be(JsonTokenType.RightBrace);
    }

    [Fact]
    public void Tokenize_SimpleArray_ReturnsCorrectTokenSequence()
    {
        var tokens = JsonTokenizer.Tokenize("[1,2]");
        tokens.Should().HaveCount(5);
        tokens[0].Type.Should().Be(JsonTokenType.LeftBracket);
        tokens[1].Type.Should().Be(JsonTokenType.Number);
        tokens[2].Type.Should().Be(JsonTokenType.Comma);
        tokens[3].Type.Should().Be(JsonTokenType.Number);
        tokens[4].Type.Should().Be(JsonTokenType.RightBracket);
    }

    [Fact]
    public void Tokenize_AllValueTypes_AllPresent()
    {
        var tokens = JsonTokenizer.Tokenize("[\"s\",42,true,false,null]");
        tokens.Select(t => t.Type).Should().Contain(JsonTokenType.String);
        tokens.Select(t => t.Type).Should().Contain(JsonTokenType.Number);
        tokens.Select(t => t.Type).Should().Contain(JsonTokenType.Boolean);
        tokens.Select(t => t.Type).Should().Contain(JsonTokenType.Null);
    }
}
