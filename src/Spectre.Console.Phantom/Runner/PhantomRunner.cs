using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace Spectre.Console.Phantom.Runner;

/// <summary>
/// End-to-end test runner for terminal applications.
/// Launches a real executable inside a Windows ConPTY pseudo-terminal
/// (via a host subprocess), tracks screen state via <see cref="PhantomTerminal"/>,
/// and provides a fluent async API for sending keystrokes and asserting on visible output.
/// </summary>
/// <example>
/// <code>
/// await using var runner = PhantomRunner.Launch("gallery.exe", width: 120, height: 40);
/// await runner.WaitForText("Select a demo");
/// await runner.NavigateToChoice("Tables");
/// await runner.WaitForText("Demo complete");
/// runner.AssertNoExceptions();
/// </code>
/// </example>
public sealed class PhantomRunner : IAsyncDisposable
{
    private readonly Process _hostProcess;
    private readonly OutputReader _reader;
    private readonly PhantomTerminal _terminal;
    private readonly NamedPipeServerStream _inputPipe;
    private readonly NamedPipeServerStream _outputPipe;
    private bool _disposed;

    /// <summary>
    /// The virtual terminal tracking the process output.
    /// Use this for detailed assertions on screen content, cell styles, etc.
    /// </summary>
    public PhantomTerminal Terminal => _terminal;

    /// <summary>
    /// Default timeout for wait operations. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Delay between keystrokes in navigation helpers. Defaults to 100ms.
    /// </summary>
    public TimeSpan NavigationDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Whether the process has exited.
    /// </summary>
    public bool HasExited => _hostProcess.HasExited;

    /// <summary>
    /// The process exit code (only valid after exit).
    /// </summary>
    public int ExitCode => _hostProcess.ExitCode;

    private PhantomRunner(
        Process hostProcess,
        OutputReader reader,
        PhantomTerminal terminal,
        NamedPipeServerStream inputPipe,
        NamedPipeServerStream outputPipe)
    {
        _hostProcess = hostProcess;
        _reader = reader;
        _terminal = terminal;
        _inputPipe = inputPipe;
        _outputPipe = outputPipe;
    }

    // ── Factory ─────────────────────────────────────────────────────

    /// <summary>
    /// Launch an executable inside a pseudo-terminal.
    /// </summary>
    /// <param name="commandLine">
    /// Full command line including executable path and arguments.
    /// Example: <c>"C:\\path\\to\\app.exe --flag"</c>
    /// </param>
    /// <param name="width">Terminal width in columns.</param>
    /// <param name="height">Terminal height in rows.</param>
    /// <param name="workingDirectory">Optional working directory for the process.</param>
    /// <returns>A <see cref="PhantomRunner"/> ready for interaction.</returns>
    public static PhantomRunner Launch(
        string commandLine,
        int width = 120,
        int height = 40,
        string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(commandLine);

        var terminal = new PhantomTerminal(width, height);
        var hostPath = FindHostExe();

        // Create named pipes for communication (avoids ConPTY corrupting std handles)
        var pipeId = Guid.NewGuid().ToString("N");
        var inputPipeName = $"phantom-in-{pipeId}";
        var outputPipeName = $"phantom-out-{pipeId}";

        // Input: test runner writes → host reads (we are the server, host is client)
        var inputPipe = new NamedPipeServerStream(inputPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        // Output: host writes → test runner reads
        var outputPipe = new NamedPipeServerStream(outputPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        var process = new Process();
        process.StartInfo.FileName = hostPath;

        // Build argument list: width height inputPipe outputPipe [workingDir] -- commandLine
        var argBuilder = new StringBuilder();
        argBuilder.Append($"{width} {height} {inputPipeName} {outputPipeName} ");
        if (workingDirectory != null)
        {
            argBuilder.Append($"\"{workingDirectory}\" ");
        }

        argBuilder.Append("-- ");
        argBuilder.Append(commandLine);

        process.StartInfo.Arguments = argBuilder.ToString();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        if (!process.Start())
        {
            inputPipe.Dispose();
            outputPipe.Dispose();
            throw new InvalidOperationException($"Failed to start Phantom host process: {hostPath}");
        }

        // Wait for host to connect to our pipes
        inputPipe.WaitForConnection();
        outputPipe.WaitForConnection();

        var reader = new OutputReader(terminal, outputPipe);

        var runner = new PhantomRunner(process, reader, terminal, inputPipe, outputPipe);
        return runner;
    }

    /// <summary>
    /// Find the Phantom host executable relative to the calling assembly.
    /// The host EXE requires its companion .dll in the same directory to run.
    /// </summary>
    private static string FindHostExe()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(PhantomRunner).Assembly.Location)
            ?? throw new InvalidOperationException("Cannot determine assembly location.");

        // Search upward for the host project and use its own build output.
        // We can't use the copy in the test output dir because the .dll isn't copied there.
        var searchDir = assemblyDir;
        while (searchDir != null)
        {
            var hostProject = Path.Combine(searchDir, "Spectre.Console.Phantom.Host",
                "Spectre.Console.Phantom.Host.csproj");
            if (File.Exists(hostProject))
            {
                var config = assemblyDir.Contains("Release") ? "Release" : "Debug";
                var hostExe = Path.Combine(searchDir, "Spectre.Console.Phantom.Host",
                    "bin", config, "net10.0", "Spectre.Console.Phantom.Host.exe");
                if (File.Exists(hostExe))
                {
                    return Path.GetFullPath(hostExe);
                }
            }

            searchDir = Path.GetDirectoryName(searchDir);
        }

        throw new FileNotFoundException(
            "Cannot find Spectre.Console.Phantom.Host.exe. " +
            "Ensure the Spectre.Console.Phantom.Host project is built. " +
            $"Searched near: {assemblyDir}");
    }

    // ── Input ────────────────────────────────────────────────────────

    /// <summary>
    /// Send a single key to the process.
    /// </summary>
    public void SendKey(ConsoleKey key, bool shift = false, bool ctrl = false, bool alt = false)
    {
        var sequence = KeyMap.ToVt100(key, shift, ctrl, alt);
        SendRaw(sequence);
    }

    /// <summary>
    /// Type a string of text (sent as raw bytes to the process stdin).
    /// </summary>
    public void Type(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        SendRaw(text);
    }

    /// <summary>
    /// Type text followed by Enter.
    /// </summary>
    public void TypeLine(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        SendRaw(text + KeyMap.ToVt100(ConsoleKey.Enter));
    }

    /// <summary>
    /// Send raw text to the process stdin.
    /// </summary>
    public void SendRaw(string data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(data);
        _inputPipe.Write(bytes);
        _inputPipe.Flush();
    }

    // ── Wait ─────────────────────────────────────────────────────────

    /// <summary>
    /// Wait until the screen contains the specified text.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown if the text does not appear within the timeout.</exception>
    public async Task WaitForText(string text, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result = await _reader.WaitForConditionAsync(
            t => t.ContainsText(text),
            timeout ?? DefaultTimeout).ConfigureAwait(false);

        if (!result)
        {
            throw new TimeoutException(
                $"Timed out waiting for text \"{text}\".\n\n" +
                $"Screen content:\n{_terminal.GetScreenText()}");
        }
    }

    /// <summary>
    /// Wait until the screen no longer contains the specified text.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown if the text still appears after the timeout.</exception>
    public async Task WaitForNoText(string text, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result = await _reader.WaitForConditionAsync(
            t => !t.ContainsText(text),
            timeout ?? DefaultTimeout).ConfigureAwait(false);

        if (!result)
        {
            throw new TimeoutException(
                $"Timed out waiting for text \"{text}\" to disappear.\n\n" +
                $"Screen content:\n{_terminal.GetScreenText()}");
        }
    }

    /// <summary>
    /// Wait until a custom condition is met on the terminal state.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown if the condition is not met within the timeout.</exception>
    public async Task WaitFor(
        Func<PhantomTerminal, bool> condition,
        TimeSpan? timeout = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var result = await _reader.WaitForConditionAsync(
            condition,
            timeout ?? DefaultTimeout).ConfigureAwait(false);

        if (!result)
        {
            throw new TimeoutException(
                $"Timed out waiting for condition" +
                $"{(description != null ? $": {description}" : "")}.\n\n" +
                $"Screen content:\n{_terminal.GetScreenText()}");
        }
    }

    /// <summary>
    /// Wait for text to appear, then send a key.
    /// Common pattern for "wait for prompt, then respond".
    /// </summary>
    public async Task WaitForTextThenSendKey(string text, ConsoleKey key, TimeSpan? timeout = null)
    {
        await WaitForText(text, timeout).ConfigureAwait(false);
        SendKey(key);
    }

    /// <summary>
    /// Wait for text to appear, then type a response and press Enter.
    /// </summary>
    public async Task WaitForTextThenTypeLine(string waitText, string response, TimeSpan? timeout = null)
    {
        await WaitForText(waitText, timeout).ConfigureAwait(false);
        TypeLine(response);
    }

    // ── Navigation Helpers ───────────────────────────────────────────

    /// <summary>
    /// Navigate to a choice in a <c>SelectionPrompt</c> by pressing DownArrow
    /// until the target text appears on the highlighted line, then press Enter.
    /// </summary>
    /// <param name="choiceText">Text to find in the highlighted choice.</param>
    /// <param name="maxAttempts">Maximum arrow key presses before giving up.</param>
    /// <param name="timeout">Timeout for each wait between key presses.</param>
    public async Task NavigateToChoice(string choiceText, int maxAttempts = 50, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(choiceText);

        // Brief initial delay to let the prompt render
        await Task.Delay(NavigationDelay).ConfigureAwait(false);

        for (var i = 0; i < maxAttempts; i++)
        {
            // Check if the choice text is on the currently highlighted line.
            // SelectionPrompt uses ">" as the highlight marker.
            var pos = _terminal.FindText(choiceText);
            if (pos != null)
            {
                var row = _terminal.GetRowText(pos.Value.Row);
                if (row.Contains('>') || row.Contains('\u203a'))
                {
                    SendKey(ConsoleKey.Enter);
                    return;
                }
            }

            SendKey(ConsoleKey.DownArrow);
            await Task.Delay(NavigationDelay).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Could not navigate to choice \"{choiceText}\" after {maxAttempts} attempts.\n\n" +
            $"Screen content:\n{_terminal.GetScreenText()}");
    }

    /// <summary>
    /// Answer a <c>[y/n]</c> confirmation prompt.
    /// Waits for the prompt text, then sends 'y' or 'n' followed by Enter.
    /// </summary>
    public async Task AnswerConfirmation(string promptText, bool yes, TimeSpan? timeout = null)
    {
        await WaitForText(promptText, timeout).ConfigureAwait(false);
        TypeLine(yes ? "y" : "n");
    }

    // ── Assertions ───────────────────────────────────────────────────

    /// <summary>
    /// Assert that the screen contains the specified text.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the text is not found.</exception>
    public void AssertScreenContains(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!_terminal.ContainsText(text))
        {
            throw new InvalidOperationException(
                $"Expected screen to contain \"{text}\" but it was not found.\n\n" +
                $"Screen content:\n{_terminal.GetScreenText()}");
        }
    }

    /// <summary>
    /// Assert that the screen does NOT contain the specified text.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the text is found.</exception>
    public void AssertScreenNotContains(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_terminal.ContainsText(text))
        {
            throw new InvalidOperationException(
                $"Expected screen NOT to contain \"{text}\" but it was found.\n\n" +
                $"Screen content:\n{_terminal.GetScreenText()}");
        }
    }

    /// <summary>
    /// Assert that no exceptions occurred in the output reader
    /// and no exception stack traces appear on screen.
    /// </summary>
    public void AssertNoExceptions()
    {
        if (_reader.HasError)
        {
            throw new InvalidOperationException(
                "An exception occurred while reading process output.",
                _reader.ReadException);
        }

        // Check for common .NET exception patterns on screen
        if (_terminal.ContainsText("Unhandled exception") ||
            _terminal.ContainsText("System.InvalidOperationException") ||
            _terminal.ContainsText("System.NullReferenceException") ||
            _terminal.ContainsText("System.ArgumentException"))
        {
            throw new InvalidOperationException(
                $"Exception detected on screen.\n\n" +
                $"Screen content:\n{_terminal.GetScreenText()}");
        }
    }

    /// <summary>
    /// Get a snapshot of the current screen content (for diagnostics/logging).
    /// </summary>
    public string GetScreenSnapshot() => _terminal.GetScreenText();

    /// <summary>
    /// Get raw output chunks received from the process (for diagnostics).
    /// </summary>
    public IReadOnlyList<string> GetRawChunks() => _reader.RawChunks;

    /// <summary>
    /// Whether the output reader encountered an error.
    /// </summary>
    public bool HasReaderError => _reader.HasError;

    /// <summary>
    /// The exception from the output reader, if any.
    /// </summary>
    public Exception? ReaderException => _reader.ReadException;

    // ── Process Control ──────────────────────────────────────────────

    /// <summary>
    /// Wait for the process to exit.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public async Task<int> WaitForExitAsync(TimeSpan? timeout = null)
    {
        var t = timeout ?? TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(t);
        try
        {
            await _hostProcess.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            return _hostProcess.ExitCode;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Process did not exit within {t.TotalSeconds}s.");
        }
    }

    /// <summary>
    /// Forcefully kill the process.
    /// </summary>
    public void Kill()
    {
        try
        {
            if (!_hostProcess.HasExited)
            {
                _hostProcess.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stop reading first
        _reader.Dispose();

        // Kill the process tree if still running
        try
        {
            if (!_hostProcess.HasExited)
            {
                _hostProcess.Kill(entireProcessTree: true);

                // Brief wait for cleanup
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await _hostProcess.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Process didn't exit gracefully — that's fine, we already killed it
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }

        _hostProcess.Dispose();
        _inputPipe.Dispose();
        _outputPipe.Dispose();
    }
}
