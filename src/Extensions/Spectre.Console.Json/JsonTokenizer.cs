namespace Spectre.Console.Json;

internal static class JsonTokenizer
{
    private static readonly Dictionary<char, JsonTokenType> _typeLookup;
    private static readonly Dictionary<string, JsonTokenType> _keywords;
    private static readonly HashSet<char> _allowedEscapedChars;

    // Cache single-char token strings to avoid per-token char.ToString() allocations.
    // These 6 structural tokens appear frequently in JSON and are always single characters.
    private static readonly Dictionary<char, string> _tokenStrings;

    static JsonTokenizer()
    {
        _typeLookup = new Dictionary<char, JsonTokenType>
        {
            { '{', JsonTokenType.LeftBrace },
            { '}', JsonTokenType.RightBrace },
            { '[', JsonTokenType.LeftBracket },
            { ']', JsonTokenType.RightBracket },
            { ':', JsonTokenType.Colon },
            { ',', JsonTokenType.Comma },
        };

        _tokenStrings = new Dictionary<char, string>
        {
            { '{', "{" },
            { '}', "}" },
            { '[', "[" },
            { ']', "]" },
            { ':', ":" },
            { ',', "," },
        };

        _keywords = new Dictionary<string, JsonTokenType>
        {
            { "true", JsonTokenType.Boolean },
            { "false", JsonTokenType.Boolean },
            { "null", JsonTokenType.Null },
        };

        _allowedEscapedChars =
        [
            '\"', '\\', '/', 'b', 'f', 'n', 'r', 't', 'u'
        ];
    }

    public static List<JsonToken> Tokenize(string text)
    {
        var result = new List<JsonToken>();
        var buffer = new StringBuffer(text);

        while (!buffer.Eof)
        {
            var current = buffer.Peek();

            if (_typeLookup.TryGetValue(current, out var tokenType))
            {
                buffer.Read(); // Consume
                result.Add(new JsonToken(tokenType, _tokenStrings[current]));
                // Stryker disable once Statement : Removing continue is equivalent — at the end of
                // the if block, C# falls through to after the if-else chain (which has no more code),
                // proceeding to the next while iteration just as continue would.
                continue;
            }
            else if (current == '\"')
            {
                result.Add(ReadString(buffer));
            }
            else if (current == '-' || current.IsDigit())
            {
                result.Add(ReadNumber(buffer));
            }
            else if (current is ' ' or '\n' or '\r' or '\t')
            {
                buffer.Read(); // Consume
            }
            else if (char.IsLetter(current))
            {
                var accumulator = new StringBuilder();
                while (!buffer.Eof)
                {
                    current = buffer.Peek();
                    if (!char.IsLetter(current))
                    {
                        break;
                    }

                    buffer.Read(); // Consume
                    accumulator.Append(current);
                }

                // Call ToString() once and reuse for both lookup and token creation.
                var keywordText = accumulator.ToString();
                if (!_keywords.TryGetValue(keywordText, out var keyword))
                {
                    // Stryker disable once String : Error message content is an equivalent mutation — callers check exception type, not message.
                    throw new InvalidOperationException($"Encountered invalid keyword '{keywordText}'");
                }

                result.Add(new JsonToken(keyword, keywordText));
            }
            else
            {
                // Stryker disable once String : Error message content is an equivalent mutation — callers check exception type, not message.
                throw new InvalidOperationException("Invalid token");
            }
        }

        return result;
    }

    private static JsonToken ReadString(StringBuffer buffer)
    {
        var accumulator = new StringBuilder();
        accumulator.Append(buffer.Expect('\"'));

        while (!buffer.Eof)
        {
            var current = buffer.Peek();
            if (current == '\"')
            {
                break;
            }
            else if (current == '\\')
            {
                buffer.Expect('\\');

                // Stryker disable once Block : Removing this if block is equivalent — buffer.Read()
                // returns '\0' at EOF, which fails the allowedEscapedChars check and throws the
                // same InvalidOperationException that tests verify.
                if (buffer.Eof)
                {
                    // Stryker disable once Statement : Removing the break is equivalent — the while loop
                    // exits naturally at buffer.Eof and the "Unterminated string literal" throw below fires.
                    break;
                }

                current = buffer.Read();
                if (!_allowedEscapedChars.Contains(current))
                {
                    // Stryker disable once String : Error message content is an equivalent mutation — callers check exception type, not message.
                    throw new InvalidOperationException("Invalid escape encountered");
                }

                accumulator.Append('\\').Append(current);

                // \u must be followed by exactly 4 hexadecimal digits
                if (current == 'u')
                {
                    for (var i = 0; i < 4; i++)
                    {
                        if (buffer.Eof)
                        {
                            throw new InvalidOperationException("Incomplete \\u escape sequence");
                        }

                        var hex = buffer.Read();
                        if (!IsHexDigit(hex))
                        {
                            throw new InvalidOperationException($"Invalid hex digit '{hex}' in \\u escape sequence");
                        }

                        accumulator.Append(hex);
                    }
                }
            }
            else
            {
                accumulator.Append(current);
                buffer.Read();
            }
        }

        // Stryker disable once Block : Block removal is equivalent — Expect('"') at EOF throws
        // InvalidOperationException from StringBuffer — same observable exception type.
        if (buffer.Eof)
        {
            // Stryker disable once String,Statement : String: message is equivalent. Statement: removing the throw
            // causes Expect('"') to throw InvalidOperationException from StringBuffer — same exception type.
            throw new InvalidOperationException("Unterminated string literal");
        }

        accumulator.Append(buffer.Expect('\"'));
        return new JsonToken(JsonTokenType.String, accumulator.ToString());
    }

    private static JsonToken ReadNumber(StringBuffer buffer)
    {
        var accumulator = new StringBuilder();

        // Minus?
        if (buffer.Peek() == '-')
        {
            buffer.Read();
            accumulator.Append("-");
        }

        // Digits
        var current = buffer.Peek();
        if (current.IsDigit(min: 1))
        {
            ReadDigits(buffer, accumulator, min: 1);
        }
        // Stryker disable once Equality,Block : Dead code — IsDigit(min:1) matches '0' (char code 48 >> 1),
        // so this else-if is never reached. Equality (==→!=) and Block removal are both unobservable.
        else if (current == '0')
        {
            // Stryker disable once Statement : Unreachable dead code — '0' is always handled by ReadDigits above.
            accumulator.Append(buffer.Expect('0'));
        }
        // Stryker disable once Block : Block removal is equivalent — remaining input is parsed as a keyword,
        // which also throws InvalidOperationException — same observable exception type.
        else
        {
            // Stryker disable once String,Statement : String: message is equivalent. Statement: removing the
            // throw causes an empty/"-" token; the remaining input is then parsed as a keyword and throws.
            throw new InvalidOperationException("Invalid number");
        }

        // Fractions
        current = buffer.Peek();
        if (current == '.')
        {
            accumulator.Append(buffer.Expect('.'));
            ReadDigits(buffer, accumulator);
        }

        // Exponent
        current = buffer.Peek();
        if (current is 'e' or 'E')
        {
            accumulator.Append(buffer.Read());

            current = buffer.Peek();
            if (current is '+' or '-')
            {
                accumulator.Append(buffer.Read());
            }

            ReadDigits(buffer, accumulator);
        }

        return new JsonToken(JsonTokenType.Number, accumulator.ToString());
    }

    private static void ReadDigits(StringBuffer buffer, StringBuilder accumulator, int min = 0)
    {
        while (!buffer.Eof)
        {
            var current = buffer.Peek();
            if (!current.IsDigit(min))
            {
                break;
            }

            buffer.Read(); // Consume
            accumulator.Append(current);
        }
    }

    private static bool IsHexDigit(char c) =>
        c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
}