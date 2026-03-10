namespace Spectre.Console;

/// <summary>
/// Contains extension methods for <see cref="IThemeable"/>.
/// </summary>
public static class ThemeExtensions
{
    /// <summary>
    /// Applies a theme to a themeable widget.
    /// </summary>
    /// <typeparam name="T">The type of the widget.</typeparam>
    /// <param name="widget">The widget to apply the theme to.</param>
    /// <param name="theme">The theme to apply.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static T UseTheme<T>(this T widget, Theme theme)
        where T : IThemeable
    {
        ArgumentNullException.ThrowIfNull(widget);
        ArgumentNullException.ThrowIfNull(theme);

        widget.Theme = theme;
        return widget;
    }
}
