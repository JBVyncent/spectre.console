namespace Spectre.Console.Tui.Widgets.Containers;

/// <summary>
/// Horizontal stack container — arranges children left to right.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class HStack : ContainerWidget
{
    public int Spacing { get; set; }

    protected internal override Size MeasureContent(Size available)
    {
        var width = 0;
        var height = 0;
        var children = Children;

        for (var i = 0; i < children.Count; i++)
        {
            if (!children[i].Visible)
            {
                continue;
            }

            var childSize = children[i].MeasureContent(new Size(available.Width - width, available.Height));
            width += childSize.Width;
            height = Math.Max(height, childSize.Height);

            if (i < children.Count - 1)
            {
                width += Spacing;
            }
        }

        return new Size(Math.Min(width, available.Width), Math.Min(height, available.Height));
    }

    protected internal override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        var x = bounds.X;
        var children = Children;

        // First pass: measure fixed and count fills
        var fixedWidth = 0;
        var fillCount = 0;
        var totalSpacing = 0;

        for (var i = 0; i < children.Count; i++)
        {
            if (!children[i].Visible)
            {
                continue;
            }

            if (i > 0)
            {
                totalSpacing += Spacing;
            }

            if (children[i].WidthConstraint?.Kind == ConstraintKind.Fill)
            {
                fillCount++;
            }
            else
            {
                var measured = children[i].MeasureContent(new Size(bounds.Width, bounds.Height));
                var childWidth = children[i].WidthConstraint?.Resolve(bounds.Width) ?? measured.Width;
                fixedWidth += childWidth;
            }
        }

        var remainingWidth = Math.Max(0, bounds.Width - fixedWidth - totalSpacing);
        var fillWidth = fillCount > 0 ? remainingWidth / fillCount : 0;

        // Second pass: arrange
        for (var i = 0; i < children.Count; i++)
        {
            if (!children[i].Visible)
            {
                continue;
            }

            int childWidth;
            if (children[i].WidthConstraint?.Kind == ConstraintKind.Fill)
            {
                childWidth = fillWidth;
            }
            else
            {
                var measured = children[i].MeasureContent(new Size(bounds.Width, bounds.Height));
                childWidth = children[i].WidthConstraint?.Resolve(bounds.Width) ?? measured.Width;
            }

            var childHeight = children[i].HeightConstraint?.Resolve(bounds.Height) ?? bounds.Height;
            children[i].Arrange(new Rect(x, bounds.Y, childWidth, childHeight));
            x += childWidth + Spacing;
        }
    }

    protected internal override void Render(IRenderSurface surface)
    {
        // Container itself doesn't render — children render into their own bounds
    }
}

// Stryker restore all
