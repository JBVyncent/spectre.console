namespace Spectre.Console.Tui;

/// <summary>
/// Base class for all TUI widgets.
/// </summary>
public abstract class Widget
{
    private bool _needsLayout = true;
    private bool _needsRender = true;

    public string? Name { get; set; }

    // Layout
    public Rect Bounds { get; internal set; }
    public Constraint? WidthConstraint { get; set; }
    public Constraint? HeightConstraint { get; set; }
    public Margin Margin { get; set; }
    public Margin Padding { get; set; }

    // Visibility and focus
    public bool Visible { get; set; } = true;
    public bool CanFocus { get; set; }
    public bool HasFocus { get; internal set; }
    public int TabIndex { get; set; }

    // Tree
    public Widget? Parent { get; internal set; }

    internal bool NeedsLayout => _needsLayout;
    internal bool NeedsRender => _needsRender;

    // Lifecycle
    protected internal virtual void OnMount()
    {
    }

    protected internal virtual void OnUnmount()
    {
    }

    // Layout
    protected internal abstract Size MeasureContent(Size available);

    protected internal virtual void Arrange(Rect bounds)
    {
        Bounds = bounds;
        _needsLayout = false;
    }

    // Rendering
    protected internal abstract void Render(IRenderSurface surface);

    // Input
    protected internal virtual bool OnKeyEvent(KeyEvent e) => false;
    protected internal virtual bool OnMouseEvent(MouseEvent e) => false;

    protected internal virtual void OnFocusGained()
    {
    }

    protected internal virtual void OnFocusLost()
    {
    }

    // Invalidation
    public void Invalidate()
    {
        _needsRender = true;
        _needsLayout = true;
    }

    internal void MarkRendered()
    {
        _needsRender = false;
    }

    // Children (overridden by containers)
    protected internal virtual IReadOnlyList<Widget> GetChildren()
    {
        return Array.Empty<Widget>();
    }
}

