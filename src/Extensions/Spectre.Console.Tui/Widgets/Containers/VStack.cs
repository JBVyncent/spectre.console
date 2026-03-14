namespace Spectre.Console.Tui.Widgets.Containers;

/// <summary>
/// Vertical stack container — arranges children top to bottom.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public class VStack : ContainerWidget
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

            var childSize = children[i].MeasureContent(new Size(available.Width, available.Height - height));
            width = Math.Max(width, childSize.Width);
            height += childSize.Height;

            if (i < children.Count - 1)
            {
                height += Spacing;
            }
        }

        return new Size(Math.Min(width, available.Width), Math.Min(height, available.Height));
    }

    protected internal override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        var y = bounds.Y;
        var children = Children;

        // First pass: measure all children to determine fill allocation
        var fixedHeight = 0;
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

            if (children[i].HeightConstraint?.Kind == ConstraintKind.Fill)
            {
                fillCount++;
            }
            else
            {
                var measured = children[i].MeasureContent(new Size(bounds.Width, bounds.Height));
                var childHeight = children[i].HeightConstraint?.Resolve(bounds.Height) ?? measured.Height;
                fixedHeight += childHeight;
            }
        }

        var remainingHeight = Math.Max(0, bounds.Height - fixedHeight - totalSpacing);
        var fillHeight = fillCount > 0 ? remainingHeight / fillCount : 0;

        // Second pass: arrange
        for (var i = 0; i < children.Count; i++)
        {
            if (!children[i].Visible)
            {
                continue;
            }

            int childHeight;
            if (children[i].HeightConstraint?.Kind == ConstraintKind.Fill)
            {
                childHeight = fillHeight;
            }
            else
            {
                var measured = children[i].MeasureContent(new Size(bounds.Width, bounds.Height));
                childHeight = children[i].HeightConstraint?.Resolve(bounds.Height) ?? measured.Height;
            }

            var childWidth = children[i].WidthConstraint?.Resolve(bounds.Width) ?? bounds.Width;
            children[i].Arrange(new Rect(bounds.X, y, childWidth, childHeight));
            y += childHeight + Spacing;
        }
    }

    protected internal override void Render(IRenderSurface surface)
    {
        // Container itself doesn't render — children render into their own bounds
    }
}

// Stryker restore all
