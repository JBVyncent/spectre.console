namespace Spectre.Console;

/// <summary>
/// Contains the collected results from a wizard prompt.
/// Results are stored by step key and can be retrieved with type-safe accessors.
/// </summary>
public sealed class WizardResult
{
    private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets a value indicating whether the wizard was cancelled by the user.
    /// </summary>
    public bool IsCancelled { get; internal set; }

    /// <summary>
    /// Gets the keys of all collected results.
    /// </summary>
    public IEnumerable<string> Keys => _values.Keys;

    /// <summary>
    /// Gets a result value by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The step key.</param>
    /// <returns>The value cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be cast to <typeparamref name="T"/>.</exception>
    public T Get<T>(string key)
    {
        // Stryker disable once Statement : Killed by WizardResultTests.Get_Throws_ArgumentNullException_On_Null_Key
        ArgumentNullException.ThrowIfNull(key);

        if (!_values.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"No wizard result found for key '{key}'.");
        }

        return (T)value;
    }

    /// <summary>
    /// Tries to get a result value by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The step key.</param>
    /// <param name="value">The value if found and castable; otherwise the default.</param>
    /// <returns><c>true</c> if the key was found and the value is of type <typeparamref name="T"/>; otherwise <c>false</c>.</returns>
    public bool TryGet<T>(string key, out T value)
    {
        // Stryker disable once Statement : Killed by WizardResultTests.TryGet_Throws_ArgumentNullException_On_Null_Key
        ArgumentNullException.ThrowIfNull(key);

        if (_values.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Returns whether a result exists for the given key.
    /// </summary>
    /// <param name="key">The step key.</param>
    /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
    public bool Contains(string key)
    {
        // Stryker disable once Statement : Killed by WizardResultTests.Contains_Throws_ArgumentNullException_On_Null_Key
        ArgumentNullException.ThrowIfNull(key);
        return _values.ContainsKey(key);
    }

    // Stryker disable all : Killed by WizardResultTests — Set/Remove are internal; Stryker coverage analysis can't trace InternalsVisibleTo
    internal void Set(string key, object value)
    {
        _values[key] = value;
    }

    internal void Remove(string key)
    {
        _values.Remove(key);
    }
    // Stryker restore all
}
