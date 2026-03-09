namespace Spectre.Console.Json.Tests;

public sealed class JsonParserTests
{
    private static readonly IJsonParser Parser = JsonParser.Shared;

    // ── Primitives ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Null_ReturnsJsonNull()
    {
        var result = Parser.Parse("null");
        result.Should().BeOfType<JsonNull>();
        ((JsonNull)result).Lexeme.Should().Be("null");
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void Parse_Boolean_ReturnsJsonBoolean(string json)
    {
        var result = Parser.Parse(json);
        result.Should().BeOfType<JsonBoolean>();
        ((JsonBoolean)result).Lexeme.Should().Be(json);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("42")]
    [InlineData("-5")]
    [InlineData("3.14")]
    [InlineData("1e10")]
    public void Parse_Number_ReturnsJsonNumber(string json)
    {
        var result = Parser.Parse(json);
        result.Should().BeOfType<JsonNumber>();
        ((JsonNumber)result).Lexeme.Should().Be(json);
    }

    [Fact]
    public void Parse_String_ReturnsJsonString()
    {
        var result = Parser.Parse("\"hello\"");
        result.Should().BeOfType<JsonString>();
        ((JsonString)result).Lexeme.Should().Be("\"hello\"");
    }

    // ── Object ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyObject_ReturnsJsonObjectWithNoMembers()
    {
        var result = Parser.Parse("{}");
        var obj = result.Should().BeOfType<JsonObject>().Subject;
        obj.Members.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ObjectWithOneMember_ReturnsMemberWithCorrectNameAndValue()
    {
        var result = Parser.Parse("{\"key\":42}");
        var obj = result.Should().BeOfType<JsonObject>().Subject;
        obj.Members.Should().HaveCount(1);
        obj.Members[0].Name.Should().Be("\"key\"");
        obj.Members[0].Value.Should().BeOfType<JsonNumber>();
    }

    [Fact]
    public void Parse_ObjectWithMultipleMembers_ReturnsAllMembers()
    {
        var result = Parser.Parse("{\"a\":1,\"b\":2,\"c\":3}");
        var obj = result.Should().BeOfType<JsonObject>().Subject;
        obj.Members.Should().HaveCount(3);
        obj.Members.Select(m => m.Name).Should().Equal("\"a\"", "\"b\"", "\"c\"");
    }

    [Fact]
    public void Parse_ObjectWithStringValue_ReturnsJsonString()
    {
        var result = Parser.Parse("{\"k\":\"v\"}");
        var obj = result.Should().BeOfType<JsonObject>().Subject;
        obj.Members[0].Value.Should().BeOfType<JsonString>();
    }

    [Fact]
    public void Parse_ObjectWithBooleanValue_ReturnsJsonBoolean()
    {
        var result = Parser.Parse("{\"k\":true}");
        var obj = result.Should().BeOfType<JsonObject>().Subject;
        obj.Members[0].Value.Should().BeOfType<JsonBoolean>();
    }

    [Fact]
    public void Parse_ObjectWithNullValue_ReturnsJsonNull()
    {
        var result = Parser.Parse("{\"k\":null}");
        var obj = result.Should().BeOfType<JsonObject>().Subject;
        obj.Members[0].Value.Should().BeOfType<JsonNull>();
    }

    // ── Array ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyArray_ReturnsJsonArrayWithNoItems()
    {
        var result = Parser.Parse("[]");
        var arr = result.Should().BeOfType<JsonArray>().Subject;
        arr.Items.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ArrayWithOneItem_ReturnsOneItem()
    {
        var result = Parser.Parse("[42]");
        var arr = result.Should().BeOfType<JsonArray>().Subject;
        arr.Items.Should().HaveCount(1);
        arr.Items[0].Should().BeOfType<JsonNumber>();
    }

    [Fact]
    public void Parse_ArrayWithMultipleItems_ReturnsAllItems()
    {
        var result = Parser.Parse("[1,2,3]");
        var arr = result.Should().BeOfType<JsonArray>().Subject;
        arr.Items.Should().HaveCount(3);
        arr.Items.Should().AllBeOfType<JsonNumber>();
    }

    [Fact]
    public void Parse_ArrayWithMixedTypes_ReturnsMixedItems()
    {
        var result = Parser.Parse("[1,\"s\",true,null]");
        var arr = result.Should().BeOfType<JsonArray>().Subject;
        arr.Items.Should().HaveCount(4);
        arr.Items[0].Should().BeOfType<JsonNumber>();
        arr.Items[1].Should().BeOfType<JsonString>();
        arr.Items[2].Should().BeOfType<JsonBoolean>();
        arr.Items[3].Should().BeOfType<JsonNull>();
    }

    // ── Nested ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_NestedObject_ReturnsNestedStructure()
    {
        var result = Parser.Parse("{\"outer\":{\"inner\":1}}");
        var outer = result.Should().BeOfType<JsonObject>().Subject;
        outer.Members[0].Value.Should().BeOfType<JsonObject>();
    }

    [Fact]
    public void Parse_NestedArray_ReturnsNestedStructure()
    {
        var result = Parser.Parse("[[1,2],[3,4]]");
        var outer = result.Should().BeOfType<JsonArray>().Subject;
        outer.Items[0].Should().BeOfType<JsonArray>();
        outer.Items[1].Should().BeOfType<JsonArray>();
    }

    [Fact]
    public void Parse_ObjectWithArrayValue_ReturnsArrayValue()
    {
        var result = Parser.Parse("{\"k\":[1,2]}");
        var obj = result.Should().BeOfType<JsonObject>().Subject;
        obj.Members[0].Value.Should().BeOfType<JsonArray>();
    }

    // ── Error cases ───────────────────────────────────────────────────────────

    [Fact]
    public void Parse_InvalidJson_ThrowsInvalidOperationException()
    {
        var act = () => Parser.Parse("not valid json");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_EmptyInput_ThrowsInvalidOperationException()
    {
        var act = () => Parser.Parse("");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Shared instance ───────────────────────────────────────────────────────

    [Fact]
    public void Shared_IsNotNull()
    {
        JsonParser.Shared.Should().NotBeNull();
    }

    [Fact]
    public void Shared_CanParseMultipleTimesWithoutStateLeakage()
    {
        var r1 = JsonParser.Shared.Parse("1");
        var r2 = JsonParser.Shared.Parse("2");
        ((JsonNumber)r1).Lexeme.Should().Be("1");
        ((JsonNumber)r2).Lexeme.Should().Be("2");
    }

    // ── Trailing tokens ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("true false")]
    [InlineData("{\"a\":1}junk")]
    [InlineData("null null")]
    [InlineData("1 2")]
    [InlineData("\"a\" \"b\"")]
    [InlineData("[1] [2]")]
    [InlineData("{} {}")]
    public void Parse_TrailingTokens_ThrowsInvalidOperationException(string json)
    {
        var act = () => Parser.Parse(json);
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("\"hello\"")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"a\":1}")]
    [InlineData("[1,2,3]")]
    public void Parse_SingleValue_DoesNotThrow(string json)
    {
        var act = () => Parser.Parse(json);
        act.Should().NotThrow();
    }

    // ── Unknown token type coverage ───────────────────────────────────────────

    [Fact]
    public void Parse_CommaAtTopLevel_ThrowsInvalidOperationException()
    {
        // Covers ParseValue's fallthrough throw — comma token is not a valid JSON value.
        // The outer try-catch in Parse() wraps it as InvalidOperationException("Invalid JSON").
        var act = () => Parser.Parse(",");
        act.Should().Throw<InvalidOperationException>();
    }
}
