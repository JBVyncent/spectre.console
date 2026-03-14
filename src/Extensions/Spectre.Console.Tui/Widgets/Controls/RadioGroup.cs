namespace Spectre.Console.Tui.Widgets.Controls;

/// <summary>
/// Groups radio buttons for mutual exclusion.
/// </summary>
public class RadioGroup
{
    private readonly List<RadioButton> _buttons = new();

    public IReadOnlyList<RadioButton> Buttons => _buttons;

    public RadioButton? Selected => _buttons.Find(b => b.IsSelected);

    public event EventHandler<RadioButton>? SelectionChanged;

    public void Add(RadioButton button)
    {
        ArgumentNullException.ThrowIfNull(button);

        button.Group = this;
        _buttons.Add(button);
    }

    public void Remove(RadioButton button)
    {
        ArgumentNullException.ThrowIfNull(button);

        button.Group = null;
        _buttons.Remove(button);
    }

    internal void Select(RadioButton button)
    {
        for (var i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != button)
            {
                _buttons[i].IsSelected = false;
            }
        }

        button.IsSelected = true;
        SelectionChanged?.Invoke(this, button);
    }
}

