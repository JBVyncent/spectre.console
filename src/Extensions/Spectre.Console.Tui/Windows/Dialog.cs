namespace Spectre.Console.Tui.Windows;

/// <summary>
/// A modal dialog window.
/// </summary>
public class Dialog : Window
{
    private DialogResult _result = DialogResult.None;

    public DialogResult Result => _result;
    public bool IsModal { get; } = true;

    public Dialog(string title)
        : base(title)
    {
        Closable = true;
        Resizable = false;
        Movable = true;
    }

    public void Close(DialogResult result)
    {
        _result = result;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public new event EventHandler? Closed;
}

/// <summary>
/// Standard dialog result values.
/// </summary>
public enum DialogResult
{
    None,
    Ok,
    Cancel,
    Yes,
    No,
}
