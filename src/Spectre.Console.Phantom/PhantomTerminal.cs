namespace Spectre.Console.Phantom;

/// <summary>
/// A virtual terminal that processes ANSI output and maintains screen state.
/// This is the core of the Phantom test harness — it simulates what a real
/// terminal would display without needing an actual terminal.
/// </summary>
public sealed class PhantomTerminal
{
    private readonly ScreenBuffer _mainBuffer;
    private ScreenBuffer? _alternateBuffer;
    private bool _useAlternateBuffer;

    private int _cursorRow;
    private int _cursorCol;
    private int _savedCursorRow;
    private int _savedCursorCol;
    private int _mainCursorRow;
    private int _mainCursorCol;
    private bool _cursorVisible = true;

    // Current style state (applied to next character written)
    private readonly ScreenCell _currentStyle = new();
    private string? _currentHyperlinkUrl;

    // History of all sequences processed (for debugging/assertions)
    private readonly List<AnsiSequence> _sequenceHistory = [];

    /// <summary>
    /// The active screen buffer.
    /// </summary>
    public ScreenBuffer Screen => _useAlternateBuffer && _alternateBuffer != null
        ? _alternateBuffer
        : _mainBuffer;

    /// <summary>
    /// Current cursor row (0-indexed).
    /// </summary>
    public int CursorRow => _cursorRow;

    /// <summary>
    /// Current cursor column (0-indexed).
    /// </summary>
    public int CursorCol => _cursorCol;

    /// <summary>
    /// Whether the cursor is currently visible.
    /// </summary>
    public bool CursorVisible => _cursorVisible;

    /// <summary>
    /// Whether the alternate screen buffer is active.
    /// </summary>
    public bool IsAlternateScreen => _useAlternateBuffer;

    /// <summary>
    /// Width of the terminal in columns.
    /// </summary>
    public int Width => Screen.Width;

    /// <summary>
    /// Height of the terminal in rows.
    /// </summary>
    public int Height => Screen.Height;

    /// <summary>
    /// All ANSI sequences processed, in order (for debugging and assertions).
    /// </summary>
    public IReadOnlyList<AnsiSequence> SequenceHistory => _sequenceHistory;

    /// <summary>
    /// Create a new virtual terminal with the specified dimensions.
    /// </summary>
    public PhantomTerminal(int width = 80, int height = 24)
    {
        _mainBuffer = new ScreenBuffer(width, height);
    }

    /// <summary>
    /// Process raw ANSI output and update the terminal state.
    /// </summary>
    public void Write(string output)
    {
        // Stryker disable once all : Equivalent — ArgumentNullException still propagates through AnsiParser.Parse(output) if null; defensive guard only
        ArgumentNullException.ThrowIfNull(output);

        var sequences = AnsiParser.Parse(output);
        foreach (var seq in sequences)
        {
            _sequenceHistory.Add(seq);
            ProcessSequence(seq);
        }
    }

    /// <summary>
    /// Get the visible text content of the screen.
    /// </summary>
    public string GetScreenText()
    {
        return Screen.GetText();
    }

    /// <summary>
    /// Get the text content of a specific row.
    /// </summary>
    public string GetRowText(int row)
    {
        return Screen.GetRowText(row);
    }

    /// <summary>
    /// Check if the screen contains the specified text.
    /// </summary>
    public bool ContainsText(string text)
    {
        return Screen.ContainsText(text);
    }

    /// <summary>
    /// Find the position of text on the screen.
    /// </summary>
    public (int Row, int Col)? FindText(string text)
    {
        return Screen.FindText(text);
    }

    /// <summary>
    /// Get the cell at the specified position for style assertions.
    /// </summary>
    public ScreenCell GetCell(int row, int col)
    {
        return Screen[row, col];
    }

    /// <summary>
    /// Reset the terminal to its initial state.
    /// </summary>
    public void Reset()
    {
        _cursorRow = 0;
        _cursorCol = 0;
        _savedCursorRow = 0;
        _savedCursorCol = 0;
        _cursorVisible = true;
        _useAlternateBuffer = false;
        _alternateBuffer = null;
        _currentStyle.Reset();
        _currentHyperlinkUrl = null;
        _mainBuffer.EraseAll();
        _sequenceHistory.Clear();
    }

    private void ProcessSequence(AnsiSequence seq)
    {
        switch (seq)
        {
            case AnsiSequence.Text text:
                WriteText(text.Content);
                break;
            case AnsiSequence.NewLine:
                NewLine();
                break;
            case AnsiSequence.CarriageReturn:
                _cursorCol = 0;
                break;
            case AnsiSequence.Backspace:
                if (_cursorCol > 0)
                {
                    _cursorCol--;
                }
                break;
            case AnsiSequence.Sgr sgr:
                ApplySgr(sgr.Parameters);
                break;
            case AnsiSequence.CursorMove move:
                MoveCursor(move.Direction, move.Count);
                break;
            case AnsiSequence.CursorPosition pos:
                // ANSI positions are 1-indexed
                _cursorRow = Math.Clamp(pos.Row - 1, 0, Height - 1);
                _cursorCol = Math.Clamp(pos.Column - 1, 0, Width - 1);
                break;
            case AnsiSequence.CursorHorizontalAbsolute cha:
                _cursorCol = Math.Clamp(cha.Column - 1, 0, Width - 1);
                break;
            case AnsiSequence.SaveCursor:
                _savedCursorRow = _cursorRow;
                _savedCursorCol = _cursorCol;
                break;
            case AnsiSequence.RestoreCursor:
                _cursorRow = _savedCursorRow;
                _cursorCol = _savedCursorCol;
                break;
            case AnsiSequence.CursorVisibility vis:
                _cursorVisible = vis.Visible;
                break;
            case AnsiSequence.EraseInDisplay eid:
                ProcessEraseInDisplay(eid.Mode);
                break;
            case AnsiSequence.EraseInLine eil:
                ProcessEraseInLine(eil.Mode);
                break;
            case AnsiSequence.AlternateScreen alt:
                ProcessAlternateScreen(alt.Enter);
                break;
            case AnsiSequence.Hyperlink link:
                _currentHyperlinkUrl = string.IsNullOrEmpty(link.Url) ? null : link.Url;
                break;
        }
    }

    private void WriteText(string text)
    {
        foreach (var ch in text)
        {
            if (_cursorCol >= Width)
            {
                // Line wrap
                _cursorCol = 0;
                _cursorRow++;
                if (_cursorRow >= Height)
                {
                    Screen.ScrollUp();
                    _cursorRow = Height - 1;
                }
            }

            _currentStyle.HyperlinkUrl = _currentHyperlinkUrl;
            Screen.WriteChar(_cursorRow, _cursorCol, ch, _currentStyle);
            _cursorCol++;
        }
    }

    private void NewLine()
    {
        _cursorRow++;
        _cursorCol = 0;
        if (_cursorRow >= Height)
        {
            Screen.ScrollUp();
            _cursorRow = Height - 1;
        }
    }

    private void MoveCursor(CursorDirection direction, int count)
    {
        switch (direction)
        {
            case CursorDirection.Up:
                _cursorRow = Math.Max(0, _cursorRow - count);
                break;
            case CursorDirection.Down:
                _cursorRow = Math.Min(Height - 1, _cursorRow + count);
                break;
            case CursorDirection.Right:
                _cursorCol = Math.Min(Width - 1, _cursorCol + count);
                break;
            case CursorDirection.Left:
                _cursorCol = Math.Max(0, _cursorCol - count);
                break;
        }
    }

    private void ProcessEraseInDisplay(EraseMode mode)
    {
        switch (mode)
        {
            case EraseMode.ToEnd:
                Screen.EraseToEnd(_cursorRow, _cursorCol);
                break;
            case EraseMode.ToStart:
                Screen.EraseToStart(_cursorRow, _cursorCol);
                break;
            case EraseMode.All:
            case EraseMode.Scrollback:
                Screen.EraseAll();
                break;
        }
    }

    private void ProcessEraseInLine(EraseMode mode)
    {
        switch (mode)
        {
            case EraseMode.ToEnd:
                Screen.EraseLineToEnd(_cursorRow, _cursorCol);
                break;
            case EraseMode.ToStart:
                Screen.EraseLineToStart(_cursorRow, _cursorCol);
                break;
            case EraseMode.All:
                Screen.EraseLine(_cursorRow);
                break;
        }
    }

    private void ProcessAlternateScreen(bool enter)
    {
        if (enter)
        {
            // Save main screen cursor position
            _mainCursorRow = _cursorRow;
            _mainCursorCol = _cursorCol;
            _alternateBuffer ??= new ScreenBuffer(Width, Height);
            _useAlternateBuffer = true;
            // Reset cursor for alternate screen
            _cursorRow = 0;
            _cursorCol = 0;
        }
        else
        {
            _useAlternateBuffer = false;
            // Restore main screen cursor position
            _cursorRow = _mainCursorRow;
            _cursorCol = _mainCursorCol;
        }
    }

    private void ApplySgr(int[] parameters)
    {
        var i = 0;
        while (i < parameters.Length)
        {
            var p = parameters[i];
            switch (p)
            {
                case 0: // Reset
                    _currentStyle.Foreground = null;
                    _currentStyle.Background = null;
                    _currentStyle.Decoration = CellDecoration.None;
                    break;

                // Decorations
                case 1:
                    _currentStyle.Decoration |= CellDecoration.Bold;
                    break;
                case 2:
                    _currentStyle.Decoration |= CellDecoration.Dim;
                    break;
                case 3:
                    _currentStyle.Decoration |= CellDecoration.Italic;
                    break;
                case 4:
                    _currentStyle.Decoration |= CellDecoration.Underline;
                    break;
                case 5:
                    _currentStyle.Decoration |= CellDecoration.SlowBlink;
                    break;
                case 6:
                    _currentStyle.Decoration |= CellDecoration.RapidBlink;
                    break;
                case 7:
                    _currentStyle.Decoration |= CellDecoration.Reverse;
                    break;
                case 8:
                    _currentStyle.Decoration |= CellDecoration.Conceal;
                    break;
                case 9:
                    _currentStyle.Decoration |= CellDecoration.Strikethrough;
                    break;

                // Foreground colors (4-bit)
                case >= 30 and <= 37:
                case >= 90 and <= 97:
                    _currentStyle.Foreground = CellColor.FromLegacy(p);
                    break;

                // Background colors (4-bit)
                case >= 40 and <= 47:
                case >= 100 and <= 107:
                    _currentStyle.Background = CellColor.FromLegacy(p);
                    break;

                // Extended foreground color
                case 38:
                    i = ParseExtendedColor(parameters, i, isForeground: true);
                    break;

                // Extended background color
                case 48:
                    i = ParseExtendedColor(parameters, i, isForeground: false);
                    break;

                // Default foreground
                case 39:
                    _currentStyle.Foreground = null;
                    break;

                // Default background
                case 49:
                    _currentStyle.Background = null;
                    break;
            }

            i++;
        }
    }

    private int ParseExtendedColor(int[] parameters, int index, bool isForeground)
    {
        if (index + 1 >= parameters.Length)
        {
            return index;
        }

        var mode = parameters[index + 1];
        switch (mode)
        {
            case 5 when index + 2 < parameters.Length:
                // 8-bit color: 38;5;n or 48;5;n
                var color8 = CellColor.FromEightBit(parameters[index + 2]);
                if (isForeground)
                {
                    _currentStyle.Foreground = color8;
                }
                else
                {
                    _currentStyle.Background = color8;
                }
                return index + 2;

            case 2 when index + 4 < parameters.Length:
                // 24-bit RGB: 38;2;r;g;b or 48;2;r;g;b
                var colorRgb = CellColor.FromRgb(
                    (byte)parameters[index + 2],
                    (byte)parameters[index + 3],
                    (byte)parameters[index + 4]);
                if (isForeground)
                {
                    _currentStyle.Foreground = colorRgb;
                }
                else
                {
                    _currentStyle.Background = colorRgb;
                }
                return index + 4;

            default:
                return index + 1;
        }
    }
}
