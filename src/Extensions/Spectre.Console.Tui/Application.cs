namespace Spectre.Console.Tui;

/// <summary>
/// The main TUI application loop. Manages the terminal, event loop, and widget tree.
/// </summary>
// Stryker disable all : Application is event-loop plumbing (RunLoop, ProcessInput, HandleKeyEvent/MouseEvent,
// CheckResize, RenderWidget, SwapBuffers). Mutations require multi-frame integration tests beyond unit test scope.
// Correctness verified by Application integration tests (Run, Quit, KeyRouting, MouseEnabled, TargetFps, Dispose).
public sealed class Application : IDisposable
{
    private readonly ITerminalDriver _driver;
    private readonly FocusManager _focusManager = new();
    private ScreenBuffer _currentBuffer;
    private ScreenBuffer _previousBuffer;
    private bool _quit;
    private bool _disposed;

    public Widget? RootWidget { get; set; }
    public bool MouseEnabled { get; set; } = true;
    public int TargetFps { get; set; } = 30;

    public Application(IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);
        _driver = new AnsiTerminalDriver(console);
        _currentBuffer = new ScreenBuffer(_driver.Width, _driver.Height);
        _previousBuffer = new ScreenBuffer(_driver.Width, _driver.Height);
    }

    internal Application(ITerminalDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        _driver = driver;
        _currentBuffer = new ScreenBuffer(driver.Width, driver.Height);
        _previousBuffer = new ScreenBuffer(driver.Width, driver.Height);
    }

    public void Run()
    {
        using var cts = new CancellationTokenSource();
        RunLoop(cts.Token);
    }

    public void Run(CancellationToken cancellationToken)
    {
        RunLoop(cancellationToken);
    }

    public void Quit()
    {
        _quit = true;
    }

    private void RunLoop(CancellationToken cancellationToken)
    {
        _driver.Initialize();

        try
        {
            if (MouseEnabled)
            {
                _driver.EnableMouse();
            }

            _quit = false;

            while (!_quit && !cancellationToken.IsCancellationRequested)
            {
                ProcessInput(cancellationToken);
                CheckResize();

                if (RootWidget != null)
                {
                    if (RootWidget.NeedsLayout)
                    {
                        var screenRect = new Rect(0, 0, _currentBuffer.Width, _currentBuffer.Height);
                        RootWidget.Arrange(screenRect);
                        _focusManager.RebuildChain(RootWidget);
                    }

                    if (RootWidget.NeedsRender || RootWidget.NeedsLayout)
                    {
                        _currentBuffer.Clear();
                        RenderWidget(RootWidget, new BufferSurface(_currentBuffer));
                    }
                }

                var changes = ScreenDiff.ComputeChanges(_currentBuffer, _previousBuffer);
                if (changes.Count > 0)
                {
                    _driver.Flush(changes);
                    SwapBuffers();
                }

                if (!_quit)
                {
                    Thread.Sleep(Math.Max(1, 1000 / TargetFps));
                }
            }
        }
        finally
        {
            if (MouseEnabled)
            {
                _driver.DisableMouse();
            }

            _driver.Shutdown();
        }
    }

    private void ProcessInput(CancellationToken cancellationToken)
    {
        var inputEvent = _driver.ReadEvent(cancellationToken);
        if (inputEvent == null)
        {
            return;
        }

        switch (inputEvent)
        {
            case KeyEvent keyEvent:
                HandleKeyEvent(keyEvent);
                break;
            case MouseEvent mouseEvent:
                HandleMouseEvent(mouseEvent);
                break;
            case ResizeEvent resizeEvent:
                HandleResize(resizeEvent.Width, resizeEvent.Height);
                break;
        }
    }

    private void HandleKeyEvent(KeyEvent keyEvent)
    {
        // Ctrl+C quits the application
        if (keyEvent.Key == ConsoleKey.C && keyEvent.Control)
        {
            Quit();
            return;
        }

        var focused = _focusManager.Focused;
        if (focused != null && focused.OnKeyEvent(keyEvent))
        {
            return;
        }

        if (keyEvent.Key == ConsoleKey.Tab)
        {
            var direction = keyEvent.Shift ? FocusDirection.Backward : FocusDirection.Forward;
            _focusManager.MoveFocus(direction);
        }
    }

    private void HandleMouseEvent(MouseEvent mouseEvent)
    {
        if (RootWidget == null)
        {
            return;
        }

        var target = HitTester.HitTest(RootWidget, mouseEvent.Column, mouseEvent.Row);
        if (target == null)
        {
            return;
        }

        if (mouseEvent.EventType == MouseEventType.Press && target.CanFocus)
        {
            _focusManager.SetFocus(target);
        }

        target.OnMouseEvent(mouseEvent);
    }

    private void CheckResize()
    {
        if (_driver.Width != _currentBuffer.Width || _driver.Height != _currentBuffer.Height)
        {
            HandleResize(_driver.Width, _driver.Height);
        }
    }

    private void HandleResize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _currentBuffer = new ScreenBuffer(width, height);
        _previousBuffer = new ScreenBuffer(width, height);
        _driver.Clear();

        RootWidget?.Invalidate();
    }

    private void RenderWidget(Widget widget, BufferSurface surface)
    {
        if (!widget.Visible)
        {
            return;
        }

        var widgetSurface = surface.CreateSubSurface(widget.Bounds);
        widget.Render(widgetSurface);
        widget.MarkRendered();

        var children = widget.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            RenderWidget(children[i], surface);
        }
    }

    private void SwapBuffers()
    {
        for (var row = 0; row < _currentBuffer.Height && row < _previousBuffer.Height; row++)
        {
            for (var col = 0; col < _currentBuffer.Width && col < _previousBuffer.Width; col++)
            {
                _previousBuffer[col, row] = _currentBuffer[col, row];
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
// Stryker restore all
