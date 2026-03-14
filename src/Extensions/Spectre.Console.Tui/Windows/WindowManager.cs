namespace Spectre.Console.Tui.Windows;

/// <summary>
/// Manages z-ordered overlapping windows.
/// </summary>
internal sealed class WindowManager
{
    private readonly List<Window> _windows = new();

    public IReadOnlyList<Window> Windows => _windows;

    public Window? ActiveWindow => _windows.Count > 0 ? _windows[_windows.Count - 1] : null;

    public void AddWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.ZOrder = _windows.Count;
        _windows.Add(window);
    }

    public void RemoveWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _windows.Remove(window);
        RecalculateZOrder();
    }

    public void BringToFront(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_windows.Remove(window))
        {
            _windows.Add(window);
            RecalculateZOrder();
        }
    }

    public void SendToBack(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_windows.Remove(window))
        {
            _windows.Insert(0, window);
            RecalculateZOrder();
        }
    }

    public Window? GetWindowAt(int col, int row)
    {
        // Search from top (last) to bottom (first)
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i].Visible && _windows[i].Bounds.Contains(col, row))
            {
                return _windows[i];
            }
        }

        return null;
    }

    private void RecalculateZOrder()
    {
        for (var i = 0; i < _windows.Count; i++)
        {
            _windows[i].ZOrder = i;
        }
    }
}
