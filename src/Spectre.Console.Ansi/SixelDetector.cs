namespace Spectre.Console;

/// <summary>
/// Detects whether the current terminal supports Sixel image encoding.
/// </summary>
/// <remarks>
/// Sixel support cannot be probed via a non-blocking environment variable query alone;
/// the most authoritative method is the DA1 response (ESC[c → ESC[?...c) which requires
/// a synchronous stdin/stdout roundtrip. This class uses a heuristic instead:
/// it checks known terminal identifiers via environment variables.
///
/// <list type="bullet">
///   <item><description>
///     <c>TERM_PROGRAM</c> = <c>WezTerm</c>, <c>mintty</c>, <c>contour</c>, <c>mlterm</c>
///   </description></item>
///   <item><description>
///     <c>TERM</c> starts with <c>mlterm</c> or equals <c>foot</c> / <c>foot-extra</c>
///   </description></item>
///   <item><description>
///     <c>MLTERM</c> environment variable is set (mlterm sets this)
///   </description></item>
/// </list>
///
/// Users can override the auto-detected value by setting
/// <c>console.Profile.Capabilities.SupportsSixel = true/false</c>.
/// </remarks>
internal static class SixelDetector
{
    private static readonly HashSet<string> _sixelTermPrograms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "WezTerm",
            "mintty",
            "contour",
            "mlterm",
        };

    private static readonly HashSet<string> _sixelTermNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "foot",
            "foot-extra",
        };

    /// <summary>
    /// Returns <c>true</c> when the current environment indicates a Sixel-capable terminal.
    /// </summary>
    public static bool Detect()
    {
        // mlterm sets MLTERM in the environment
        var mlterm = Environment.GetEnvironmentVariable("MLTERM");
        if (!string.IsNullOrEmpty(mlterm))
        {
            return true;
        }

        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        if (!string.IsNullOrEmpty(termProgram) && _sixelTermPrograms.Contains(termProgram))
        {
            return true;
        }

        var term = Environment.GetEnvironmentVariable("TERM");
        if (!string.IsNullOrEmpty(term))
        {
            if (_sixelTermNames.Contains(term))
            {
                return true;
            }

            if (term.StartsWith("mlterm", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
