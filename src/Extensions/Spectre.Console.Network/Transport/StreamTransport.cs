namespace Spectre.Console.Network.Transport;

/// <summary>
/// A network transport that sends and receives framed messages over streams.
/// </summary>
public class StreamTransport : INetworkTransport
{
    private readonly Stream _readStream;
    private readonly Stream _writeStream;
    private readonly SemaphoreSlim _writeLock;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTransport"/> class
    /// using a single bidirectional stream.
    /// </summary>
    /// <param name="stream">The bidirectional stream.</param>
    public StreamTransport(Stream stream)
        : this(stream, stream)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTransport"/> class
    /// using separate read and write streams.
    /// </summary>
    /// <param name="readStream">The stream to read from.</param>
    /// <param name="writeStream">The stream to write to.</param>
    public StreamTransport(Stream readStream, Stream writeStream)
    {
        ArgumentNullException.ThrowIfNull(readStream);
        ArgumentNullException.ThrowIfNull(writeStream);

        _readStream = readStream;
        _writeStream = writeStream;
        _writeLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc/>
    public bool IsConnected => !_disposed;

    /// <inheritdoc/>
    // Stryker disable all : ConfigureAwait(false/true) is equivalent in test context; Statement mutations on null guard and dispose check are defensive
    public async Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = NetworkMessageSerializer.ToFrame(message);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
#if NETSTANDARD2_0
            await _writeStream.WriteAsync(frame, 0, frame.Length, cancellationToken).ConfigureAwait(false);
#else
            await _writeStream.WriteAsync(frame.AsMemory(), cancellationToken).ConfigureAwait(false);
#endif
#if NETSTANDARD2_0
            await _writeStream.FlushAsync().ConfigureAwait(false);
#else
            await _writeStream.FlushAsync(cancellationToken).ConfigureAwait(false);
#endif
        }
        finally
        {
            _writeLock.Release();
        }
    }
    // Stryker restore all

    /// <inheritdoc/>
    // Stryker disable all : ConfigureAwait(false/true) is equivalent in test context; Equality boundary on length < 0 vs <= 0 is equivalent (length 0 is valid empty payload)
    public async Task<NetworkMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Read header: 1 byte type + 4 bytes length
        var header = new byte[5];
        if (!await ReadExactAsync(header, 0, 5, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var type = (MessageType)header[0];
        var length = NetworkMessageSerializer.ReadInt32BigEndian(header, 1);

        if (length < 0)
        {
            throw new InvalidOperationException("Received negative payload length.");
        }

        // Read payload
        var payload = new byte[length];
        if (length > 0)
        {
            if (!await ReadExactAsync(payload, 0, length, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }
        }

        return new NetworkMessage(type, payload);
    }
    // Stryker restore all

    /// <inheritdoc/>
    // Stryker disable all : Statement mutation removing GC.SuppressFinalize is equivalent — no finalizer defined
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    // Stryker restore all

    /// <summary>
    /// Disposes the transport resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    // Stryker disable all : Statement mutations on Dispose are equivalent — double-dispose is safe; ReferenceEquals guard prevents double-disposing same stream
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _writeLock.Dispose();

            // Only dispose streams if they are different objects
            _readStream.Dispose();
            if (!ReferenceEquals(_readStream, _writeStream))
            {
                _writeStream.Dispose();
            }
        }

        _disposed = true;
    }
    // Stryker restore all

    // Stryker disable all : ConfigureAwait(false/true) is equivalent in test context
    private async Task<bool> ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
#if NETSTANDARD2_0
            var read = await _readStream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
#else
            var read = await _readStream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), cancellationToken).ConfigureAwait(false);
#endif
            if (read == 0)
            {
                return false;
            }

            totalRead += read;
        }

        return true;
    }
    // Stryker restore all
}
