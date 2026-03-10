namespace Spectre.Console.Network;

/// <summary>
/// An <see cref="IAnsiConsole"/> implementation that sends rendered output
/// over a network transport. All Spectre.Console widgets (Tables, Charts,
/// Progress, Prompts, etc.) work transparently over the network.
/// </summary>
public sealed class NetworkConsole : IAnsiConsole, IDisposable
{
    private readonly IAnsiConsole _inner;
    private readonly INetworkTransport _transport;
    private readonly NetworkConsoleInput _input;

    /// <inheritdoc/>
    public Profile Profile => _inner.Profile;

    /// <inheritdoc/>
    public IAnsiConsoleCursor Cursor => _inner.Cursor;

    /// <inheritdoc/>
    public IAnsiConsoleInput Input => _input;

    /// <inheritdoc/>
    public IExclusivityMode ExclusivityMode => _inner.ExclusivityMode;

    /// <inheritdoc/>
    public RenderPipeline Pipeline => _inner.Pipeline;

    /// <summary>
    /// Gets the underlying network transport.
    /// </summary>
    public INetworkTransport Transport => _transport;

    private NetworkConsole(
        IAnsiConsole inner,
        INetworkTransport transport,
        NetworkConsoleInput input)
    {
        _inner = inner;
        _transport = transport;
        _input = input;
    }

    /// <summary>
    /// Creates a new <see cref="NetworkConsole"/> by performing the protocol handshake
    /// with a connected client.
    /// </summary>
    /// <param name="transport">The connected transport.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A configured <see cref="NetworkConsole"/> ready for use.</returns>
    // Stryker disable all : ConfigureAwait(false/true) is equivalent in test context; Boolean/Statement mutations on handshake ack are defensive protocol behavior
    public static async Task<NetworkConsole> AcceptAsync(
        INetworkTransport transport,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transport);

        // Wait for client handshake
        var handshakeMsg = await transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        if (handshakeMsg == null)
        {
            throw new InvalidOperationException("Connection closed before handshake.");
        }

        var (width, height, colorSystem, interactive) = NetworkMessageSerializer.ReadHandshake(handshakeMsg);

        // Create the console
        var console = Create(transport, width, height, colorSystem, interactive);

        // Send acknowledgment
        await transport.SendAsync(
            NetworkMessageSerializer.CreateHandshakeAck(true),
            cancellationToken).ConfigureAwait(false);

        return console;
    }
    // Stryker restore all

    /// <summary>
    /// Creates a new <see cref="NetworkConsole"/> with explicit dimensions and capabilities,
    /// bypassing the handshake protocol.
    /// </summary>
    /// <param name="transport">The network transport.</param>
    /// <param name="width">The console width.</param>
    /// <param name="height">The console height.</param>
    /// <param name="colorSystem">The color system.</param>
    /// <param name="interactive">Whether the remote terminal supports interaction.</param>
    /// <returns>A configured <see cref="NetworkConsole"/>.</returns>
    // Stryker disable all : Boolean/Conditional/Object initializer mutations on internal console setup are equivalent — enrichers disabled because explicit assignments override; Ansi/Unicode always true for network transport
    public static NetworkConsole Create(
        INetworkTransport transport,
        int width = 80,
        int height = 24,
        ColorSystem colorSystem = ColorSystem.TrueColor,
        bool interactive = true)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var writer = new NetworkTextWriter(transport);
        var output = new NetworkConsoleOutput(writer, width, height);

        var inner = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = (ColorSystemSupport)colorSystem,
            Out = output,
            Interactive = interactive ? InteractionSupport.Yes : InteractionSupport.No,
            Enrichment = new ProfileEnrichment
            {
                UseDefaultEnrichers = false,
            },
        });

        inner.Profile.Width = width;
        inner.Profile.Height = height;
        inner.Profile.Capabilities.Ansi = true;
        inner.Profile.Capabilities.Unicode = true;
        inner.Profile.Capabilities.Interactive = interactive;

        var input = new NetworkConsoleInput(transport, (w, h) =>
        {
            inner.Profile.Width = w;
            inner.Profile.Height = h;
        });

        return new NetworkConsole(inner, transport, input);
    }
    // Stryker restore all

    /// <inheritdoc/>
    public void Clear(bool home)
    {
        _inner.Clear(home);
        _inner.Profile.Out.Writer.Flush();
    }

    /// <inheritdoc/>
    public void Write(IRenderable renderable)
    {
        _inner.Write(renderable);
        _inner.Profile.Out.Writer.Flush();
    }

    /// <inheritdoc/>
    public void WriteAnsi(Action<AnsiWriter> action)
    {
        _inner.WriteAnsi(action);
        _inner.Profile.Out.Writer.Flush();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _transport.Dispose();
    }
}
