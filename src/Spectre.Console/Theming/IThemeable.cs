namespace Spectre.Console;

/// <summary>
/// Represents a widget that supports theming.
/// Widgets implementing this interface resolve their styles through
/// the theme's style slots at render time.
/// </summary>
public interface IThemeable
{
    /// <summary>
    /// Gets or sets the theme applied to this widget.
    /// When <c>null</c>, the widget uses its own explicit styles or built-in defaults.
    /// </summary>
    Theme? Theme { get; set; }
}
