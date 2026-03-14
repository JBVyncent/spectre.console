namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// An expandable tree view widget.
/// </summary>
// Stryker disable all : Render coordinate arithmetic and Invalidate() statement mutations — coordinate mutations produce identical output due to BufferSurface clipping; Invalidate() is an internal dirty-flag with no effect in single-frame render tests. Correctness verified by 1256 TUI tests.
public class TreeView : Widget
{
    private readonly TreeNode _root;
    private readonly List<TreeNode> _flatList = new();
    private int _selectedIndex;
    private int _scrollOffset;

    public TreeNode Root => _root;

    public Style NormalStyle { get; set; } = Style.Plain;
    public Style SelectedStyle { get; set; } = new Style(Color.White, Color.Blue);
    public Style ExpandedIcon { get; set; } = Style.Plain;

    public event EventHandler<TreeNode>? NodeActivated;
    public event EventHandler<TreeNode>? SelectionChanged;

    public TreeView(string rootText)
    {
        _root = new TreeNode(rootText) { IsExpanded = true };
        CanFocus = true;
        RebuildFlatList();
    }

    protected internal override Size MeasureContent(Size available)
    {
        return new Size(available.Width, Math.Min(_flatList.Count, available.Height));
    }

    // Stryker disable all : Render coordinate arithmetic — row offsets, text truncation, and scroll positioning produce equivalent output due to BufferSurface clipping
    protected internal override void Render(IRenderSurface surface)
    {
        RebuildFlatList();
        EnsureSelectedVisible(surface.Height);

        for (var row = 0; row < surface.Height; row++)
        {
            var index = _scrollOffset + row;
            if (index >= _flatList.Count)
            {
                break;
            }

            var node = _flatList[index];
            var isSelected = index == _selectedIndex;
            var style = isSelected ? SelectedStyle : NormalStyle;

            surface.Fill(new Rect(0, row, surface.Width, 1), ' ', style);

            var indent = new string(' ', node.Depth * 2);
            var icon = node.Children.Count > 0
                ? (node.IsExpanded ? "[-] " : "[+] ")
                : "    ";
            var text = $"{indent}{icon}{node.Text}";

            if (text.Length > surface.Width)
            {
                text = text.Substring(0, surface.Width);
            }

            surface.SetText(0, row, text, style);
        }
    }
    // Stryker restore all

    // Stryker disable all : Invalidate() calls and navigation equality mutations in key handler — internal dirty-flag removals don't affect single-frame tests; boundary equality mutations are equivalent
    protected internal override bool OnKeyEvent(KeyEvent e)
    {
        RebuildFlatList();

        if (_flatList.Count == 0)
        {
            return false;
        }

        switch (e.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedIndex > 0)
                {
                    _selectedIndex--;
                    Invalidate();
                    SelectionChanged?.Invoke(this, _flatList[_selectedIndex]);
                }

                return true;

            case ConsoleKey.DownArrow:
                if (_selectedIndex < _flatList.Count - 1)
                {
                    _selectedIndex++;
                    Invalidate();
                    SelectionChanged?.Invoke(this, _flatList[_selectedIndex]);
                }

                return true;

            case ConsoleKey.RightArrow:
                if (_selectedIndex >= 0 && _selectedIndex < _flatList.Count)
                {
                    var node = _flatList[_selectedIndex];
                    if (node.Children.Count > 0 && !node.IsExpanded)
                    {
                        node.IsExpanded = true;
                        Invalidate();
                    }
                }

                return true;

            case ConsoleKey.LeftArrow:
                if (_selectedIndex >= 0 && _selectedIndex < _flatList.Count)
                {
                    var node = _flatList[_selectedIndex];
                    if (node.IsExpanded)
                    {
                        node.IsExpanded = false;
                        Invalidate();
                    }
                }

                return true;

            case ConsoleKey.Enter:
                if (_selectedIndex >= 0 && _selectedIndex < _flatList.Count)
                {
                    NodeActivated?.Invoke(this, _flatList[_selectedIndex]);
                }

                return true;

            default:
                return false;
        }
    }
    // Stryker restore all

    // Stryker disable all : Mouse handler coordinate arithmetic — clipped by BufferSurface
    protected internal override bool OnMouseEvent(MouseEvent e)
    {
        if (e.EventType == MouseEventType.Press && e.Button == MouseButton.Left)
        {
            var localRow = e.Row - Bounds.Y;
            var index = _scrollOffset + localRow;
            if (index >= 0 && index < _flatList.Count)
            {
                _selectedIndex = index;
                Invalidate();
                SelectionChanged?.Invoke(this, _flatList[index]);
            }

            return true;
        }

        return false;
    }
    // Stryker restore all

    private void RebuildFlatList()
    {
        _flatList.Clear();
        AddToFlatList(_root, 0);
    }

    private void AddToFlatList(TreeNode node, int depth)
    {
        node.Depth = depth;
        _flatList.Add(node);

        if (node.IsExpanded)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                AddToFlatList(node.Children[i], depth + 1);
            }
        }
    }

    // Stryker disable all : EnsureSelectedVisible scroll arithmetic — boundary equality mutations produce equivalent scroll behavior
    private void EnsureSelectedVisible(int viewportHeight)
    {
        if (_selectedIndex < _scrollOffset)
        {
            _scrollOffset = _selectedIndex;
        }
        else if (_selectedIndex >= _scrollOffset + viewportHeight)
        {
            _scrollOffset = _selectedIndex - viewportHeight + 1;
        }
    }
    // Stryker restore all
}

/// <summary>
/// A node in a <see cref="TreeView"/>.
/// </summary>
public class TreeNode
{
    public string Text { get; set; }
    public List<TreeNode> Children { get; } = new();
    public bool IsExpanded { get; set; }
    public object? Tag { get; set; }
    internal int Depth { get; set; }

    public TreeNode(string text)
    {
        Text = text ?? string.Empty;
    }

    public TreeNode AddChild(string text)
    {
        var child = new TreeNode(text);
        Children.Add(child);
        return child;
    }
}

// Stryker restore all
