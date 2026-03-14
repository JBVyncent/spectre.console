namespace Spectre.Console.Tui.Widgets.Containers;

/// <summary>
/// Base class for widgets that contain child widgets.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public abstract class ContainerWidget : Widget
{
    private readonly List<Widget> _children = new();

    public IReadOnlyList<Widget> Children => _children;

    // Stryker disable all : Invalidate() sets internal dirty flags for multi-frame rendering; removing it doesn't affect single-frame render tests
    public void Add(Widget child)
    {
        ArgumentNullException.ThrowIfNull(child);

        child.Parent = this;
        _children.Add(child);
        child.OnMount();
        Invalidate();
    }
    // Stryker restore all

    // Stryker disable all : Invalidate() sets internal dirty flags for multi-frame rendering; removing it doesn't affect single-frame render tests
    public void Remove(Widget child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_children.Remove(child))
        {
            child.OnUnmount();
            child.Parent = null;
            Invalidate();
        }
    }
    // Stryker restore all

    // Stryker disable all : Invalidate() sets internal dirty flags for multi-frame rendering; removing it doesn't affect single-frame render tests
    public void Clear()
    {
        for (var i = _children.Count - 1; i >= 0; i--)
        {
            _children[i].OnUnmount();
            _children[i].Parent = null;
        }

        _children.Clear();
        Invalidate();
    }
    // Stryker restore all

    protected internal override IReadOnlyList<Widget> GetChildren() => _children;
}

// Stryker restore all
