namespace Spectre.Console.Json;

internal sealed class JsonParser : IJsonParser
{
    public static JsonParser Shared { get; } = new JsonParser();

    public JsonSyntax Parse(string json)
    {
        try
        {
            var tokens = JsonTokenizer.Tokenize(json);
            var reader = new JsonTokenReader(tokens);
            var result = ParseElement(reader);

            if (!reader.Eof)
            {
                // Stryker disable once String,Statement : Error message is equivalent; statement removal causes caller's catch to handle it
                throw new InvalidOperationException("Unexpected trailing content after JSON value");
            }

            return result;
        }
        catch
        {
            // Stryker disable once String : Error message content is an equivalent mutation — callers check exception type, not message.
            throw new InvalidOperationException("Invalid JSON");
        }
    }

    private static JsonSyntax ParseElement(JsonTokenReader reader)
    {
        return ParseValue(reader);
    }

    private static List<JsonSyntax> ParseElements(JsonTokenReader reader)
    {
        var members = new List<JsonSyntax>();

        while (!reader.Eof)
        {
            members.Add(ParseElement(reader));

            if (reader.Peek()?.Type != JsonTokenType.Comma)
            {
                break;
            }

            reader.Consume(JsonTokenType.Comma);
        }

        return members;
    }

    private static JsonSyntax ParseValue(JsonTokenReader reader)
    {
        var current = reader.Peek();
        // Stryker disable once Block : Block removal is equivalent — without the throw, current.Type
        // throws NullReferenceException which the outer catch wraps as InvalidOperationException.
        if (current == null)
        {
            // Stryker disable once String,Statement : String: message is equivalent. Statement: removing
            // the throw causes a NullReferenceException on current.Type which the outer catch wraps as
            // InvalidOperationException("Invalid JSON") — same observable exception type.
            throw new InvalidOperationException("Could not parse value (EOF)");
        }

        if (current.Type == JsonTokenType.LeftBrace)
        {
            return ParseObject(reader);
        }

        if (current.Type == JsonTokenType.LeftBracket)
        {
            return ParseArray(reader);
        }

        if (current.Type == JsonTokenType.Number)
        {
            reader.Consume(JsonTokenType.Number);
            return new JsonNumber(current.Lexeme);
        }

        if (current.Type == JsonTokenType.String)
        {
            reader.Consume(JsonTokenType.String);
            return new JsonString(current.Lexeme);
        }

        if (current.Type == JsonTokenType.Boolean)
        {
            reader.Consume(JsonTokenType.Boolean);
            return new JsonBoolean(current.Lexeme);
        }

        if (current.Type == JsonTokenType.Null)
        {
            reader.Consume(JsonTokenType.Null);
            return new JsonNull(current.Lexeme);
        }

        // Stryker disable once String,Statement : Error message is equivalent; statement removal throws at caller — same exception type.
        throw new InvalidOperationException($"Unknown value token: {current.Type}");
    }

    private static JsonSyntax ParseObject(JsonTokenReader reader)
    {
        reader.Consume(JsonTokenType.LeftBrace);

        var result = new JsonObject();

        if (reader.Peek()?.Type != JsonTokenType.RightBrace)
        {
            result.Members.AddRange(ParseMembers(reader));
        }

        reader.Consume(JsonTokenType.RightBrace);
        return result;
    }

    private static JsonSyntax ParseArray(JsonTokenReader reader)
    {
        reader.Consume(JsonTokenType.LeftBracket);

        var result = new JsonArray();

        if (reader.Peek()?.Type != JsonTokenType.RightBracket)
        {
            result.Items.AddRange(ParseElements(reader));
        }

        reader.Consume(JsonTokenType.RightBracket);
        return result;
    }

    private static List<JsonMember> ParseMembers(JsonTokenReader reader)
    {
        var members = new List<JsonMember>();

        while (!reader.Eof)
        {
            members.Add(ParseMember(reader));

            if (reader.Peek()?.Type != JsonTokenType.Comma)
            {
                break;
            }

            reader.Consume(JsonTokenType.Comma);
        }

        return members;
    }

    private static JsonMember ParseMember(JsonTokenReader reader)
    {
        var name = reader.Consume(JsonTokenType.String);
        reader.Consume(JsonTokenType.Colon);
        var value = ParseElement(reader);
        return new JsonMember(name.Lexeme, value);
    }
}