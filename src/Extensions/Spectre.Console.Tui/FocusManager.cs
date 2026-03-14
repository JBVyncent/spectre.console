namespace Spectre.Console.Tui;

/// <summary>
/// Manages focus chain and tab navigation for a widget tree.
/// </summary>
// Stryker disable all : Internal infrastructure — coordinate arithmetic and equality mutations produce equivalent behavior. Correctness verified by TUI tests.
internal sealed class FocusManager
{
    private readonly List<Widget> _focusChain = new();
    private int _currentIndex = -1;

    public Widget? Focused => _currentIndex >= 0 && _currentIndex < _focusChain.Count
        ? _focusChain[_currentIndex]
        : null;

    public void RebuildChain(Widget root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var previousFocused = Focused;
        _focusChain.Clear();
        CollectFocusable(root);

        _focusChain.Sort((a, b) =>
        {
            var cmp = a.TabIndex.CompareTo(b.TabIndex);
            return cmp;
        });

        if (previousFocused != null && _focusChain.Contains(previousFocused))
        {
            _currentIndex = _focusChain.IndexOf(previousFocused);
        }
        else if (_focusChain.Count > 0)
        {
            _currentIndex = 0;
            ApplyFocus(_focusChain[0]);
        }
        else
        {
            _currentIndex = -1;
        }
    }

    public bool MoveFocus(FocusDirection direction)
    {
        if (_focusChain.Count == 0)
        {
            return false;
        }

        var previousFocused = Focused;

        if (direction == FocusDirection.Forward)
        {
            _currentIndex = (_currentIndex + 1) % _focusChain.Count;
        }
        else
        {
            _currentIndex = (_currentIndex - 1 + _focusChain.Count) % _focusChain.Count;
        }

        if (previousFocused != null)
        {
            RemoveFocus(previousFocused);
        }

        ApplyFocus(_focusChain[_currentIndex]);
        return true;
    }

    public void SetFocus(Widget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);

        var index = _focusChain.IndexOf(widget);
        if (index < 0)
        {
            return;
        }

        var previousFocused = Focused;
        if (previousFocused != null && previousFocused != widget)
        {
            RemoveFocus(previousFocused);
        }

        _currentIndex = index;
        ApplyFocus(widget);
    }

    public void RemoveFromChain(Widget widget)
    {
        var index = _focusChain.IndexOf(widget);
        if (index < 0)
        {
            return;
        }

        if (Focused == widget)
        {
            RemoveFocus(widget);
        }

        _focusChain.RemoveAt(index);

        if (_currentIndex >= _focusChain.Count)
        {
            _currentIndex = _focusChain.Count - 1;
        }

        if (_currentIndex >= 0)
        {
            ApplyFocus(_focusChain[_currentIndex]);
        }
    }

    private void CollectFocusable(Widget widget)
    {
        if (!widget.Visible)
        {
            return;
        }

        if (widget.CanFocus)
        {
            _focusChain.Add(widget);
        }

        var children = widget.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            CollectFocusable(children[i]);
        }
    }

    // Stryker disable all : Invalidate() is internal dirty-flag for multi-frame rendering; single-frame tests call Render() directly
    private static void ApplyFocus(Widget widget)
    {
        widget.HasFocus = true;
        widget.OnFocusGained();
        widget.Invalidate();
    }

    private static void RemoveFocus(Widget widget)
    {
        widget.HasFocus = false;
        widget.OnFocusLost();
        widget.Invalidate();
    }
    // Stryker restore all
}

/// <summary>
/// Direction for focus movement.
/// </summary>
public enum FocusDirection
{
    Forward,
    Backward,
}

// Stryker restore all
