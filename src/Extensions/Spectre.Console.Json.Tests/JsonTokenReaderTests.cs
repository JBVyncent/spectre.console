namespace Spectre.Console.Json.Tests;

public sealed class JsonTokenReaderTests
{
    private static JsonToken MakeToken(JsonTokenType type = JsonTokenType.Number) =>
        new JsonToken(type, "x");

    [Fact]
    public void Position_StartsAtZero()
    {
        var reader = new JsonTokenReader([MakeToken()]);
        reader.Position.Should().Be(0);
    }

    [Fact]
    public void Eof_EmptyList_IsTrue()
    {
        var reader = new JsonTokenReader([]);
        reader.Eof.Should().BeTrue();
    }

    [Fact]
    public void Eof_NonEmptyList_IsFalse()
    {
        var reader = new JsonTokenReader([MakeToken()]);
        reader.Eof.Should().BeFalse();
    }

    [Fact]
    public void Peek_ReturnsFirstToken_WithoutAdvancing()
    {
        var token = MakeToken(JsonTokenType.String);
        var reader = new JsonTokenReader([token]);
        var result = reader.Peek();
        result.Should().BeSameAs(token);
        reader.Position.Should().Be(0);
    }

    [Fact]
    public void Peek_WhenEof_ReturnsNull()
    {
        var reader = new JsonTokenReader([]);
        reader.Peek().Should().BeNull();
    }

    [Fact]
    public void Read_AdvancesPosition_AndReturnsToken()
    {
        var token = MakeToken(JsonTokenType.Colon);
        var reader = new JsonTokenReader([token]);
        var result = reader.Read();
        result.Should().BeSameAs(token);
        reader.Position.Should().Be(1);
        reader.Eof.Should().BeTrue();
    }

    [Fact]
    public void Read_WhenEof_ReturnsNull()
    {
        var reader = new JsonTokenReader([]);
        reader.Read().Should().BeNull();
    }

    [Fact]
    public void Read_MultipleTokens_AdvancesSequentially()
    {
        var t1 = new JsonToken(JsonTokenType.LeftBrace, "{");
        var t2 = new JsonToken(JsonTokenType.RightBrace, "}");
        var reader = new JsonTokenReader([t1, t2]);

        reader.Read().Should().BeSameAs(t1);
        reader.Position.Should().Be(1);
        reader.Read().Should().BeSameAs(t2);
        reader.Position.Should().Be(2);
        reader.Eof.Should().BeTrue();
    }

    [Fact]
    public void Consume_CorrectType_ReturnsToken()
    {
        var token = new JsonToken(JsonTokenType.LeftBrace, "{");
        var reader = new JsonTokenReader([token]);
        var result = reader.Consume(JsonTokenType.LeftBrace);
        result.Should().BeSameAs(token);
    }

    [Fact]
    public void Consume_WrongType_Throws()
    {
        var token = new JsonToken(JsonTokenType.LeftBrace, "{");
        var reader = new JsonTokenReader([token]);
        var act = () => reader.Consume(JsonTokenType.RightBrace);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Consume_WhenEof_Throws()
    {
        var reader = new JsonTokenReader([]);
        var act = () => reader.Consume(JsonTokenType.LeftBrace);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── JsonToken constructor ─────────────────────────────────────────────────

    [Fact]
    public void JsonToken_Constructor_NullLexeme_Throws()
    {
        // Kills Statement mutation that removes ArgumentNullException.ThrowIfNull(lexeme)
        var act = () => new JsonToken(JsonTokenType.Number, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
