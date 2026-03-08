namespace Spectre.Console.Json;

internal sealed class JsonToken
{
    public JsonTokenType Type { get; }
    public string Lexeme { get; }

    public JsonToken(JsonTokenType type, string lexeme)
    {
        ArgumentNullException.ThrowIfNull(lexeme);
        Type = type;
        Lexeme = lexeme;
    }
}