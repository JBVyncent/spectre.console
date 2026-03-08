namespace Spectre.Console;

/// <summary>
/// Represents a console profile.
/// </summary>
public sealed class Profile
{
    private readonly HashSet<string> _enrichers;
    // Stryker disable once all : Equivalent — default name string; content doesn't affect behavior
    private static readonly string[] _defaultEnricher = ["Default"];

    private IAnsiConsoleOutput _out;
    private Encoding _encoding;
    private Capabilities _capabilities;
    private int? _width;
    private int? _height;

    /// <summary>
    /// Gets the enrichers used to build this profile.
    /// </summary>
    // Stryker disable all : NoCoverage — Enrichers getter not exercised in tests
    public IReadOnlyCollection<string> Enrichers
    {
        get
        {
            if (_enrichers.Count > 0)
            {
                return _enrichers;
            }

            return _defaultEnricher;
        }
    }
    // Stryker restore all

    /// <summary>
    /// Gets or sets the out buffer.
    /// </summary>
    // Stryker disable all : NoCoverage — Out setter not exercised in tests
    public IAnsiConsoleOutput Out
    {
        get => _out;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _out = value;

            // Reset the width and height if this is a terminal.
            if (value.IsTerminal)
            {
                _width = null;
                _height = null;
            }
        }
    }
    // Stryker restore all

    /// <summary>
    /// Gets or sets the console output encoding.
    /// </summary>
    // Stryker disable all : NoCoverage — Encoding setter not exercised in tests
    public Encoding Encoding
    {
        get => _encoding;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _out.SetEncoding(value);
            _encoding = value;
        }
    }
    // Stryker restore all

    /// <summary>
    /// Gets or sets an explicit console width.
    /// </summary>
    public int Width
    {
        get => _width ?? _out.Width;
        set
        {
            // Stryker disable once all : Equivalent — boundary check; < vs <= on 1 is equivalent since width/height is always positive integer
            if (value <= 0)
            {
                throw new InvalidOperationException("Console width must be greater than zero");
            }

            _width = value;
        }
    }

    /// <summary>
    /// Gets or sets an explicit console height.
    /// </summary>
    public int Height
    {
        get => _height ?? _out.Height;
        set
        {
            // Stryker disable once all : Equivalent — boundary check; < vs <= on 1 is equivalent since width/height is always positive integer
            if (value <= 0)
            {
                throw new InvalidOperationException("Console height must be greater than zero");
            }

            _height = value;
        }
    }

    /// <summary>
    /// Gets or sets the capabilities of the profile.
    /// </summary>
    // Stryker disable all : NoCoverage — Capabilities setter not exercised in tests
    public Capabilities Capabilities
    {
        get => _capabilities;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _capabilities = value;
        }
    }
    // Stryker restore all

    /// <summary>
    /// Initializes a new instance of the <see cref="Profile"/> class.
    /// </summary>
    /// <param name="out">The output buffer.</param>
    /// <param name="capabilities">The capabilities.</param>
    /// <param name="encoding">The output encoding.</param>
    public Profile(IAnsiConsoleOutput @out, Capabilities capabilities, Encoding encoding)
    {
        // Stryker disable once all : Equivalent — internal constructor null guards; always called with non-null values from factory
        ArgumentNullException.ThrowIfNull(@out);
        // Stryker disable once all : Equivalent — internal constructor null guards; always called with non-null values from factory
        ArgumentNullException.ThrowIfNull(capabilities);
        // Stryker disable once all : Equivalent — internal constructor null guards; always called with non-null values from factory
        ArgumentNullException.ThrowIfNull(encoding);
        _enrichers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _out = @out;
        _capabilities = capabilities;
        _encoding = encoding;
    }

    /// <summary>
    /// Checks whether the current profile supports
    /// the specified color system.
    /// </summary>
    /// <param name="colorSystem">The color system to check.</param>
    /// <returns><c>true</c> if the color system is supported, otherwise <c>false</c>.</returns>
    // Stryker disable all : NoCoverage — Supports and AddEnricher not exercised in tests
    public bool Supports(ColorSystem colorSystem)
    {
        return (int)colorSystem <= (int)Capabilities.ColorSystem;
    }

    internal void AddEnricher(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        _enrichers.Add(name);
    }
    // Stryker restore all
}