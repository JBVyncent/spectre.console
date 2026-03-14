namespace Spectre.Console.Tui.Integration;

/// <summary>
/// A theme for the TUI framework with styles for all widget types.
/// </summary>
public sealed class TuiTheme
{
    public Style WindowTitle { get; set; } = new Style(Color.White, Color.Blue);
    public Style WindowBorder { get; set; } = new Style(Color.Grey);
    public Style WindowBackground { get; set; } = Style.Plain;
    public Style FocusedBorder { get; set; } = new Style(Color.Cyan1);

    public Style ButtonNormal { get; set; } = new Style(Color.White, Color.Blue);
    public Style ButtonFocused { get; set; } = new Style(Color.White, Color.Cyan1);

    public Style TextBoxNormal { get; set; } = new Style(Color.White, Color.Grey);
    public Style TextBoxFocused { get; set; } = new Style(Color.White, Color.DarkBlue);

    public Style MenuBarNormal { get; set; } = new Style(Color.Black, Color.Grey);
    public Style MenuBarSelected { get; set; } = new Style(Color.White, Color.Blue);

    public Style StatusBarBackground { get; set; } = new Style(Color.Black, Color.Grey);
    public Style StatusBarKey { get; set; } = new Style(Color.White, Color.DarkBlue);

    public Style ListItemNormal { get; set; } = Style.Plain;
    public Style ListItemSelected { get; set; } = new Style(Color.White, Color.Blue);

    public Style CheckBoxNormal { get; set; } = Style.Plain;
    public Style CheckBoxFocused { get; set; } = new Style(Color.Cyan1);

    public Style ProgressFilled { get; set; } = new Style(Color.Green);
    public Style ProgressEmpty { get; set; } = new Style(Color.Grey);

    public Style DataGridHeader { get; set; } = new Style(Color.White, Color.DarkBlue);

    public static TuiTheme Default { get; } = new TuiTheme();

    public static TuiTheme Dark { get; } = new TuiTheme
    {
        WindowTitle = new Style(Color.White, Color.DarkSlateGray1),
        WindowBorder = new Style(Color.DarkSlateGray1),
        FocusedBorder = new Style(Color.Green),
        ButtonNormal = new Style(Color.White, Color.DarkSlateGray1),
        ButtonFocused = new Style(Color.White, Color.Green),
        MenuBarNormal = new Style(Color.White, Color.Grey11),
        MenuBarSelected = new Style(Color.White, Color.DarkSlateGray1),
        StatusBarBackground = new Style(Color.White, Color.Grey11),
        StatusBarKey = new Style(Color.White, Color.DarkSlateGray1),
        ListItemSelected = new Style(Color.White, Color.DarkSlateGray1),
    };

    public static TuiTheme Blue { get; } = new TuiTheme
    {
        WindowTitle = new Style(Color.White, Color.Blue),
        WindowBorder = new Style(Color.Blue),
        FocusedBorder = new Style(Color.Cyan1),
        WindowBackground = new Style(Color.White, Color.DarkBlue),
        ButtonNormal = new Style(Color.White, Color.Blue),
        ButtonFocused = new Style(Color.Yellow, Color.Blue),
        TextBoxNormal = new Style(Color.Black, Color.Cyan1),
        TextBoxFocused = new Style(Color.Black, Color.White),
        MenuBarNormal = new Style(Color.White, Color.Blue),
        MenuBarSelected = new Style(Color.Blue, Color.White),
        StatusBarBackground = new Style(Color.White, Color.Blue),
        StatusBarKey = new Style(Color.Yellow, Color.Blue),
        ListItemNormal = new Style(Color.White, Color.DarkBlue),
        ListItemSelected = new Style(Color.Yellow, Color.Blue),
        DataGridHeader = new Style(Color.White, Color.Blue),
    };
}
