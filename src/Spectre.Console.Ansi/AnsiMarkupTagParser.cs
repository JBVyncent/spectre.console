namespace Spectre.Console;

internal static class AnsiMarkupTagParser
{
    public static (Style Style, Link? Link) Parse(string text)
    {
        var result = Parse(text, out var error);
        if (error != null)
        {
            throw new InvalidOperationException(error);
        }

        // Stryker disable once NullCoalescing,String : Parse(text, out error) always sets error when returning null;
        // the ?? throw is unreachable defensive code — NullCoalescing and String mutations are both dead code
        return result ?? throw new InvalidOperationException("Could not parse style.");
    }

    public static bool TryParse(string text, [NotNullWhen(true)] out (Style Style, Link?)? result)
    {
        result = Parse(text, out _);
        return result != null;
    }

    private static (Style Style, Link? Link)? Parse(string text, out string? error)
    {
        var effectiveDecoration = (Decoration?)null;
        var effectiveForeground = (Color?)null;
        var effectiveBackground = (Color?)null;
        var effectiveLink = (string?)null;

        var parts = text.Split([' ']);
        var foreground = true;
        foreach (var part in parts)
        {
            if (part.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (part.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                foreground = false;
                continue;
            }

            if (part.StartsWith("link=", StringComparison.OrdinalIgnoreCase))
            {
                if (effectiveLink != null)
                {
                    error = "A link has already been set.";
                    return null;
                }

                effectiveLink = part.Substring(5);
                continue;
            }
            else if (part.StartsWith("link", StringComparison.OrdinalIgnoreCase))
            {
                effectiveLink = Constants.EmptyLink;
                continue;
            }

            var decoration = DecorationTable.GetDecoration(part);
            if (decoration != null)
            {
                effectiveDecoration ??= Decoration.None;

                effectiveDecoration |= decoration.Value;
            }
            else
            {
                var color = ColorTable.GetColor(part);
                if (color == null)
                {
                    if (part.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                    {
                        color = ParseHexColor(part, out error);
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            return null;
                        }
                    }
                    else if (part.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                    {
                        color = ParseRgbColor(part, out error);
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            return null;
                        }
                    }
                    else if (int.TryParse(part, out var number))
                    {
                        if (number < 0)
                        {
                            error = $"Color number must be greater than or equal to 0 (was {number})";
                            return null;
                        }
                        else if (number > 255)
                        {
                            error = $"Color number must be less than or equal to 255 (was {number})";
                            return null;
                        }

                        color = number;
                    }
                    else
                    {
                        error = !foreground
                            ? $"Could not find color '{part}'."
                            : $"Could not find color or style '{part}'.";

                        return null;
                    }
                }

                if (foreground)
                {
                    if (effectiveForeground != null)
                    {
                        error = "A foreground color has already been set.";
                        return null;
                    }

                    effectiveForeground = color;
                }
                else
                {
                    if (effectiveBackground != null)
                    {
                        error = "A background color has already been set.";
                        return null;
                    }

                    effectiveBackground = color;
                }
            }
        }

        var link = effectiveLink != null
            ? new Link(effectiveLink)
            : null;

        error = null;
        return (new Style(
                effectiveForeground,
                effectiveBackground,
                effectiveDecoration),
            link);
    }

    private static Color? ParseHexColor(string hex, out string? error)
    {
        error = null;

        // Caller guarantees hex starts with '#' (checked via StartsWith("#")).
        // Strip the leading '#' with Substring(1) instead of ReplaceExact + Trim,
        // avoiding 2 intermediate string allocations.
        // Stryker disable once NullCoalescing,String : hex is never null (caller passes part from Split); dead-code branch
        hex ??= string.Empty;
        // Stryker disable once Conditional,Logical,Equality : hex always starts with '#' (caller checks
        // StartsWith("#")), so Length > 0 && hex[0] == '#' is always true; all conditional/logical/equality
        // mutations on this ternary condition are semantically equivalent.
        var digits = hex.Length > 0 && hex[0] == '#' ? hex.Substring(1) : hex;

        // Use TryParse instead of try/catch for flow control — avoids exception
        // allocation on invalid input and is faster on the error path.
        if (digits.Length == 6)
        {
            if (byte.TryParse(digits.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
                byte.TryParse(digits.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
                byte.TryParse(digits.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                return new Color(r, g, b);
            }
        }
        else if (digits.Length == 3)
        {
            if (TryParseHexNibble(digits[0], out var r) &&
                TryParseHexNibble(digits[1], out var g) &&
                TryParseHexNibble(digits[2], out var b))
            {
                // Expand single hex digit to double: 0xF -> 0xFF (F * 17 = 255)
                return new Color((byte)(r * 17), (byte)(g * 17), (byte)(b * 17));
            }
        }

        error = $"Invalid hex color '{hex}'.";
        return null;
    }

    private static bool TryParseHexNibble(char c, out int value)
    {
        if (c >= '0' && c <= '9')
        {
            value = c - '0';
            return true;
        }

        if (c >= 'a' && c <= 'f')
        {
            value = c - 'a' + 10;
            return true;
        }

        if (c >= 'A' && c <= 'F')
        {
            value = c - 'A' + 10;
            return true;
        }

        value = 0;
        return false;
    }

    private static Color? ParseRgbColor(string rgb, out string? error)
    {
        error = null;

        // Stryker disable once NullCoalescing,String : rgb is never null in practice (caller validates); dead-code branch
        var normalized = rgb ?? string.Empty;
        // Stryker disable once Equality : length==3 means exactly "rgb" which has no parenthesized values;
        // >= 3 and > 3 both fail to parse it (Substring(3) is empty, StartsWith("(") is false)
        if (normalized.Length >= 3)
        {
            // Strip "rgb" prefix — use Substring(3) once instead of chaining Trim calls.
            normalized = normalized.Substring(3).Trim();

            // Stryker disable once String : mutating "(" to "" makes StartsWith always true, but EndsWith(")")
            // still gates the code path; all invalid inputs produce the same null result via a different code path
            if (normalized.StartsWith("(", StringComparison.OrdinalIgnoreCase) &&
               normalized.EndsWith(")", StringComparison.OrdinalIgnoreCase))
            {
                // Strip parentheses with a single Substring instead of chained Trim('(').Trim(')').
                normalized = normalized.Substring(1, normalized.Length - 2);

                var parts = normalized.Split([','], StringSplitOptions.RemoveEmptyEntries);
                // Use TryParse instead of try/catch for flow control — avoids exception
                // allocation on invalid input (e.g., "rgb(foo,bar,baz)").
                if (parts.Length == 3 &&
                    int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) &&
                    int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var g) &&
                    int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) &&
                    r is >= 0 and <= 255 && g is >= 0 and <= 255 && b is >= 0 and <= 255)
                {
                    return new Color((byte)r, (byte)g, (byte)b);
                }
            }
        }

        error = $"Invalid RGB color '{rgb}'.";
        return null;
    }
}