namespace Spectre.Console.Tui.Integration;

using Spectre.Console.Tui.Widgets.Containers;
using Spectre.Console.Tui.Widgets.Controls;
using Spectre.Console.Tui.Windows;

/// <summary>
/// Provides simple message dialog functionality.
/// </summary>
public static class MessageBox
{
    /// <summary>
    /// Creates a message box dialog with the specified buttons.
    /// </summary>
    public static Dialog Create(string title, string message, MessageBoxButtons buttons = MessageBoxButtons.Ok)
    {
        var dialog = new Dialog(title);

        var vstack = new VStack { Spacing = 1 };
        vstack.Add(new Label(message));

        var buttonRow = new HStack { Spacing = 2 };

        if (buttons == MessageBoxButtons.Ok || buttons == MessageBoxButtons.OkCancel)
        {
            var okButton = new Button("OK");
            okButton.Clicked += (_, _) => dialog.Close(DialogResult.Ok);
            buttonRow.Add(okButton);
        }

        if (buttons == MessageBoxButtons.OkCancel)
        {
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += (_, _) => dialog.Close(DialogResult.Cancel);
            buttonRow.Add(cancelButton);
        }

        if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
        {
            var yesButton = new Button("Yes");
            yesButton.Clicked += (_, _) => dialog.Close(DialogResult.Yes);
            buttonRow.Add(yesButton);

            var noButton = new Button("No");
            noButton.Clicked += (_, _) => dialog.Close(DialogResult.No);
            buttonRow.Add(noButton);
        }

        if (buttons == MessageBoxButtons.YesNoCancel)
        {
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += (_, _) => dialog.Close(DialogResult.Cancel);
            buttonRow.Add(cancelButton);
        }

        vstack.Add(buttonRow);
        dialog.Add(vstack);

        return dialog;
    }
}

/// <summary>
/// Specifies which buttons to display in a message box.
/// </summary>
public enum MessageBoxButtons
{
    Ok,
    OkCancel,
    YesNo,
    YesNoCancel,
}
