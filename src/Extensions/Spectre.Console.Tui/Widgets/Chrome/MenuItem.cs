namespace Spectre.Console.Tui.Widgets.Chrome;

/// <summary>
/// Represents an item in a menu.
/// </summary>
public class MenuItem
{
    public string Text { get; }
    public string? Shortcut { get; }
    public bool IsSeparator { get; }
    public bool Enabled { get; set; } = true;
    public List<MenuItem> SubItems { get; } = new();

    public event EventHandler? Activated;

    public MenuItem(string text, string? shortcut = null)
    {
        Text = text ?? string.Empty;
        Shortcut = shortcut;
    }

    private MenuItem()
    {
        Text = string.Empty;
        IsSeparator = true;
    }

    public static MenuItem Separator() => new MenuItem();

    internal void RaiseActivated()
    {
        Activated?.Invoke(this, EventArgs.Empty);
    }
}
