namespace Spectre.Console;

internal static class StringUriExtensions
{
    public static bool TryGetUri(this string path, [NotNullWhen(true)] out Uri? result)
    {
        try
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                result = null;
                return false;
            }

            if (uri.Scheme == "file")
            {
                // Use empty host for file URIs to produce file:///path format.
                // Windows Terminal and most other terminals require this format;
                // using Dns.GetHostName() produces file://HOSTNAME/path which is
                // not recognized by Windows Terminal (see GitHub #1592).
                var builder = new UriBuilder(uri)
                {
                    Host = string.Empty,
                };

                uri = builder.Uri;
            }

            result = uri;
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}