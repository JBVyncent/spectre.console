using System.Text;

namespace Spectre.Console.Phantom.Runner;

/// <summary>
/// Continuously reads from a ConPTY output stream on a background thread,
/// decodes UTF-8, feeds the text into a <see cref="PhantomTerminal"/>,
/// and evaluates registered waiter predicates on each chunk.
/// </summary>
internal sealed class OutputReader : IDisposable
{
    private readonly PhantomTerminal _terminal;
    private readonly Stream _outputStream;
    private readonly Task _readLoop;
    private readonly CancellationTokenSource _cts;
    private readonly Lock _lock = new();
    private readonly List<Waiter> _waiters = [];
    private readonly List<string> _rawChunks = [];
    private Exception? _readException;
    private bool _streamEnded;

    public bool HasError => _readException != null;
    public Exception? ReadException => _readException;
    public bool StreamEnded => _streamEnded;

    /// <summary>
    /// All raw text chunks received from the process (for diagnostics).
    /// </summary>
    public IReadOnlyList<string> RawChunks
    {
        get
        {
            lock (_lock)
            {
                return [.. _rawChunks];
            }
        }
    }

    public OutputReader(PhantomTerminal terminal, Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(outputStream);

        _terminal = terminal;
        _outputStream = outputStream;
        _cts = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoopAsync(_cts.Token));
    }

    private Task ReadLoopAsync(CancellationToken ct)
    {
        // Use synchronous reads on this background thread.
        // CreatePipe handles are synchronous (no FILE_FLAG_OVERLAPPED),
        // so ReadAsync would just post to the thread pool anyway.
        var buffer = new byte[4096];
        var decoder = Encoding.UTF8.GetDecoder();
        var charBuffer = new char[4096];

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = _outputStream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    // Pipe broken — process exited
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                var charsDecoded = decoder.GetChars(buffer.AsSpan(0, bytesRead), charBuffer, false);
                var text = new string(charBuffer, 0, charsDecoded);

                lock (_lock)
                {
                    _rawChunks.Add(text);
                    _terminal.Write(text);
                    EvaluateWaiters();
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _readException = ex;
        }
        finally
        {
            _streamEnded = true;
            lock (_lock)
            {
                // Signal all remaining waiters that the stream ended
                EvaluateWaiters();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Register a condition to wait for. Returns true when the condition is met,
    /// false on timeout.
    /// </summary>
    public async Task<bool> WaitForConditionAsync(
        Func<PhantomTerminal, bool> predicate,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        lock (_lock)
        {
            // Check immediately — condition may already be met
            if (predicate(_terminal))
            {
                return true;
            }
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = new Waiter(predicate, tcs);

        lock (_lock)
        {
            _waiters.Add(waiter);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

        // Race: condition met vs timeout vs cancellation
        var timeoutTask = Task.Delay(timeout, timeoutCts.Token)
            .ContinueWith(
                _ =>
                {
                    tcs.TrySetResult(false);
                    lock (_lock)
                    {
                        _waiters.Remove(waiter);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

        try
        {
            return await tcs.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            lock (_lock)
            {
                _waiters.Remove(waiter);
            }

            throw;
        }
    }

    private void EvaluateWaiters()
    {
        for (var i = _waiters.Count - 1; i >= 0; i--)
        {
            var waiter = _waiters[i];
            if (waiter.Predicate(_terminal))
            {
                waiter.Tcs.TrySetResult(true);
                _waiters.RemoveAt(i);
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();

        // Complete any outstanding waiters
        lock (_lock)
        {
            foreach (var waiter in _waiters)
            {
                waiter.Tcs.TrySetResult(false);
            }

            _waiters.Clear();
        }

        _cts.Dispose();
    }

    private sealed record Waiter(Func<PhantomTerminal, bool> Predicate, TaskCompletionSource<bool> Tcs);
}
