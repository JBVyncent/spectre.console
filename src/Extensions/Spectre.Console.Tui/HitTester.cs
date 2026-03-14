namespace Spectre.Console.Tui;

/// <summary>
/// Performs hit testing to find the widget at a given screen coordinate.
/// </summary>
internal static class HitTester
{
    public static Widget? HitTest(Widget root, int screenCol, int screenRow)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (!root.Visible || !root.Bounds.Contains(screenCol, screenRow))
        {
            return null;
        }

        // Check children in reverse order (top-most first)
        var children = root.GetChildren();
        for (var i = children.Count - 1; i >= 0; i--)
        {
            var result = HitTest(children[i], screenCol, screenRow);
            if (result != null)
            {
                return result;
            }
        }

        return root;
    }
}

