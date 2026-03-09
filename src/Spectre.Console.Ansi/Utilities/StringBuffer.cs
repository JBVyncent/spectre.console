namespace Spectre.Console;

internal sealed class StringBuffer : IDisposable
{
    private readonly StringReader _reader;
    private readonly int _length;

    public int Position { get; private set; }
    public bool Eof => Position >= _length;

    public StringBuffer(string text)
    {
        text ??= string.Empty;

        _reader = new StringReader(text);
        _length = text.Length;

        Position = 0;
    }

    // Stryker disable all : StringReader.Dispose has no observable effect in test environments; block and statement mutations are unobservable
    public void Dispose() { _reader.Dispose(); }
    // Stryker restore all

    public char Peek()
    {
        if (Eof)
        {
            return '\0';
        }

        return (char)_reader.Peek();
    }

    public char Read()
    {
        if (Eof)
        {
            return '\0';
        }

        Position++;
        return (char)_reader.Read();
    }
}