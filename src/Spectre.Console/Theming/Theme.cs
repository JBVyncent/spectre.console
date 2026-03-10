namespace Spectre.Console;

/// <summary>
/// Represents a visual theme that controls the styling of Spectre.Console widgets.
/// Themes provide semantic style slots that widgets resolve at render time.
/// Set a property to <c>null</c> to use the widget's own default.
/// </summary>
public sealed class Theme
{
    /// <summary>
    /// Gets or sets the style used for borders (Table, Panel).
    /// </summary>
    public Style? BorderStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for tree guide lines.
    /// </summary>
    public Style? TreeStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for horizontal rules and dividers.
    /// </summary>
    public Style? RuleStyle { get; init; }

    /// <summary>
    /// Gets or sets the accent style used for emphasis and highlights
    /// (FigletText, selection highlights).
    /// </summary>
    public Style? AccentStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for dimmed or muted content.
    /// </summary>
    public Style? DimStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for headers (table headers, panel headers).
    /// </summary>
    public Style? HeaderStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for prompt highlights (selection cursor).
    /// </summary>
    public Style? HighlightStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for completed progress bars.
    /// </summary>
    public Style? ProgressCompletedStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for finished progress bars.
    /// </summary>
    public Style? ProgressFinishedStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for remaining progress bar segments.
    /// </summary>
    public Style? ProgressRemainingStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for spinners (progress, status).
    /// </summary>
    public Style? SpinnerStyle { get; init; }

    /// <summary>
    /// Gets or sets the style used for links.
    /// </summary>
    public Style? LinkStyle { get; init; }

    /// <summary>
    /// Gets or sets the name of the theme for display purposes.
    /// </summary>
    public string Name { get; init; } = "Custom";

    /// <summary>
    /// Gets the default theme (no overrides — widgets use their built-in defaults).
    /// </summary>
    public static Theme Default { get; } = new Theme { Name = "Default" };

    /// <summary>
    /// Gets the Nord theme — an arctic, north-bluish color palette.
    /// </summary>
    public static Theme Nord { get; } = CreateNord();

    /// <summary>
    /// Gets the Dracula theme — a dark theme with vibrant colors.
    /// </summary>
    public static Theme Dracula { get; } = CreateDracula();

    /// <summary>
    /// Gets the Solarized Dark theme — precision colors for machines and people.
    /// </summary>
    public static Theme SolarizedDark { get; } = CreateSolarizedDark();

    /// <summary>
    /// Gets the Monokai theme — a warm, high-contrast dark theme.
    /// </summary>
    public static Theme Monokai { get; } = CreateMonokai();

    /// <summary>
    /// Resolves a widget-level style against a theme slot.
    /// Returns the widget's explicit style if set, otherwise the theme slot, otherwise the fallback.
    /// </summary>
    /// <param name="widgetStyle">The widget's explicitly set style (may be null).</param>
    /// <param name="themeStyle">The theme's style slot (may be null).</param>
    /// <param name="fallback">The fallback style if neither is set.</param>
    /// <returns>The resolved style.</returns>
    public static Style Resolve(Style? widgetStyle, Style? themeStyle, Style fallback)
    {
        return widgetStyle ?? themeStyle ?? fallback;
    }

    private static Theme CreateNord()
    {
        // Nord palette: https://www.nordtheme.com/
        var frost0 = new Color(143, 188, 187); // #8FBCBB — teal
        var frost1 = new Color(136, 192, 208); // #88C0D0 — light blue
        var frost2 = new Color(129, 161, 193); // #81A1C1 — blue
        var frost3 = new Color(94, 129, 172);  // #5E81AC — dark blue
        var snow0 = new Color(216, 222, 233);  // #D8DEE9
        var aurora0 = new Color(191, 97, 106); // #BF616A — red
        var aurora3 = new Color(163, 190, 140); // #A3BE8C — green

        return new Theme
        {
            Name = "Nord",
            BorderStyle = new Style(frost2),
            TreeStyle = new Style(frost0),
            RuleStyle = new Style(frost3),
            AccentStyle = new Style(frost1),
            DimStyle = new Style(snow0),
            HeaderStyle = new Style(frost1, decoration: Decoration.Bold),
            HighlightStyle = new Style(frost1, decoration: Decoration.Bold),
            ProgressCompletedStyle = new Style(frost1),
            ProgressFinishedStyle = new Style(aurora3),
            ProgressRemainingStyle = new Style(frost3),
            SpinnerStyle = new Style(frost1),
            LinkStyle = new Style(frost1, decoration: Decoration.Underline),
        };
    }

    private static Theme CreateDracula()
    {
        // Dracula palette: https://draculatheme.com/
        var purple = new Color(189, 147, 249);  // #BD93F9
        var pink = new Color(255, 121, 198);    // #FF79C6
        var green = new Color(80, 250, 123);    // #50FA7B
        var cyan = new Color(139, 233, 253);    // #8BE9FD
        var orange = new Color(255, 184, 108);  // #FFB86C
        var foreground = new Color(248, 248, 242); // #F8F8F2
        var comment = new Color(98, 114, 164);  // #6272A4

        return new Theme
        {
            Name = "Dracula",
            BorderStyle = new Style(purple),
            TreeStyle = new Style(cyan),
            RuleStyle = new Style(comment),
            AccentStyle = new Style(pink),
            DimStyle = new Style(comment),
            HeaderStyle = new Style(purple, decoration: Decoration.Bold),
            HighlightStyle = new Style(green, decoration: Decoration.Bold),
            ProgressCompletedStyle = new Style(purple),
            ProgressFinishedStyle = new Style(green),
            ProgressRemainingStyle = new Style(comment),
            SpinnerStyle = new Style(pink),
            LinkStyle = new Style(cyan, decoration: Decoration.Underline),
        };
    }

    private static Theme CreateSolarizedDark()
    {
        // Solarized palette: https://ethanschoonover.com/solarized/
        var blue = new Color(38, 139, 210);    // #268BD2
        var cyan = new Color(42, 161, 152);    // #2AA198
        var green = new Color(133, 153, 0);    // #859900
        var yellow = new Color(181, 137, 0);   // #B58900
        var base0 = new Color(131, 148, 150);  // #839496
        var base01 = new Color(88, 110, 117);  // #586E75
        var violet = new Color(108, 113, 196); // #6C71C4

        return new Theme
        {
            Name = "Solarized Dark",
            BorderStyle = new Style(blue),
            TreeStyle = new Style(cyan),
            RuleStyle = new Style(base01),
            AccentStyle = new Style(yellow),
            DimStyle = new Style(base01),
            HeaderStyle = new Style(blue, decoration: Decoration.Bold),
            HighlightStyle = new Style(green, decoration: Decoration.Bold),
            ProgressCompletedStyle = new Style(blue),
            ProgressFinishedStyle = new Style(green),
            ProgressRemainingStyle = new Style(base01),
            SpinnerStyle = new Style(cyan),
            LinkStyle = new Style(violet, decoration: Decoration.Underline),
        };
    }

    private static Theme CreateMonokai()
    {
        // Monokai palette
        var pink = new Color(249, 38, 114);    // #F92672
        var green = new Color(166, 226, 46);   // #A6E22E
        var blue = new Color(102, 217, 239);   // #66D9EF
        var orange = new Color(253, 151, 31);  // #FD971F
        var purple = new Color(174, 129, 255); // #AE81FF
        var grey = new Color(117, 113, 94);    // #75715E
        var white = new Color(248, 248, 242);  // #F8F8F2

        return new Theme
        {
            Name = "Monokai",
            BorderStyle = new Style(blue),
            TreeStyle = new Style(green),
            RuleStyle = new Style(grey),
            AccentStyle = new Style(pink),
            DimStyle = new Style(grey),
            HeaderStyle = new Style(orange, decoration: Decoration.Bold),
            HighlightStyle = new Style(green, decoration: Decoration.Bold),
            ProgressCompletedStyle = new Style(blue),
            ProgressFinishedStyle = new Style(green),
            ProgressRemainingStyle = new Style(grey),
            SpinnerStyle = new Style(pink),
            LinkStyle = new Style(purple, decoration: Decoration.Underline),
        };
    }
}
