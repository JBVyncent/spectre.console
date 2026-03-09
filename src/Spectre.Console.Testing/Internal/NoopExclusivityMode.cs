namespace Spectre.Console.Testing;

internal sealed class NoopExclusivityMode : IExclusivityMode
{
    public T Run<T>(Func<T> func)
    {
        return func();
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> func)
    {
        // Stryker disable once Boolean : ConfigureAwait(false) vs ConfigureAwait(true) is
        // equivalent in test environments — both resume on the same thread context; the
        // Boolean mutation produces identical observable behaviour in xUnit tests.
        return await func().ConfigureAwait(false);
    }
}