using System.Text;

namespace Typewriter.CLI.Generation;

/// <summary>
/// Stream class for parsing template strings character by character.
/// This is a port of the VS extension's Stream class.
/// </summary>
internal class TemplateStream
{
    private readonly int _offset;
    private readonly string _template;
    private int _position = -1;
    private char _current = char.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateStream"/> class.
    /// </summary>
    /// <param name="template">The template string to parse.</param>
    /// <param name="offset">The offset for position tracking.</param>
    public TemplateStream(string template, int offset = 0)
    {
        _offset = offset;
        _template = template ?? string.Empty;
    }

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public int Position => _position + _offset;

    /// <summary>
    /// Gets the current character.
    /// </summary>
    public char Current => _current;

    /// <summary>
    /// Advances the stream by the specified offset.
    /// </summary>
    /// <param name="offset">The number of characters to advance.</param>
    /// <returns>True if the advance was successful; false if end of stream reached.</returns>
    public bool Advance(int offset = 1)
    {
        for (var i = 0; i < offset; i++)
        {
            _position++;

            if (_position >= _template.Length)
            {
                _current = char.MinValue;
                return false;
            }

            _current = _template[_position];
        }

        return true;
    }

    /// <summary>
    /// Peeks at a character at the specified offset from current position.
    /// </summary>
    /// <param name="offset">The offset from current position.</param>
    /// <returns>The character at the offset, or char.MinValue if out of bounds.</returns>
    public char Peek(int offset = 1)
    {
        var index = _position + offset;

        if (index > -1 && index < _template.Length)
        {
            return _template[index];
        }

        return char.MinValue;
    }

    /// <summary>
    /// Peeks at a word starting from the specified offset.
    /// </summary>
    /// <param name="start">The starting offset.</param>
    /// <returns>The word, or null if no word found.</returns>
    public string? PeekWord(int start = 0)
    {
        if (!char.IsLetter(Peek(start)))
        {
            return null;
        }

        var identifier = new StringBuilder();
        var i = start;
        while (char.IsLetterOrDigit(Peek(i)))
        {
            identifier.Append(Peek(i));
            i++;
        }

        return identifier.ToString();
    }

    /// <summary>
    /// Peeks at a line starting from the specified offset.
    /// </summary>
    /// <param name="start">The starting offset.</param>
    /// <returns>The line including the newline character.</returns>
    public string PeekLine(int start = 0)
    {
        var line = new StringBuilder();
        var i = start;
        do
        {
            line.Append(Peek(i));
            i++;
        } while (Peek(i) != '\n' && i + _position < _template.Length);

        line.Append('\n');

        return line.ToString();
    }

    /// <summary>
    /// Peeks at a block delimited by open/close characters.
    /// </summary>
    /// <param name="start">The starting offset.</param>
    /// <param name="open">The opening delimiter character.</param>
    /// <param name="close">The closing delimiter character.</param>
    /// <returns>The block content without delimiters.</returns>
    public string PeekBlock(int start, char open, char close)
    {
        var i = start;
        var depth = 1;
        var identifier = new StringBuilder();

        while (depth > 0)
        {
            var letter = Peek(i);

            if (letter == char.MinValue)
            {
                break;
            }

            if (IsMatch(i, letter, close))
            {
                depth--;
            }

            if (depth > 0)
            {
                identifier.Append(letter);
                if (IsMatch(i, letter, open))
                {
                    depth++;
                }

                i++;

                if (letter != open && (letter == '"' || letter == '\''))
                {
                    var block = PeekBlock(i, letter, letter);
                    identifier.Append(block);
                    i += block.Length;

                    if (letter == Peek(i))
                    {
                        identifier.Append(letter);
                        i++;
                    }
                }
            }
        }

        return identifier.ToString();
    }

    private bool IsMatch(int index, char letter, char match)
    {
        if (letter == match)
        {
            var isString = match == '"' || match == '\'';
            if (isString)
            {
                if (Peek(index - 1) == '\\' && Peek(index - 2) != '\\')
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Skips whitespace characters.
    /// </summary>
    /// <returns>True if there are more characters to read; otherwise, false.</returns>
    public bool SkipWhitespace()
    {
        if (_position < 0)
        {
            Advance();
        }

        while (char.IsWhiteSpace(Current))
        {
            Advance();
        }

        return _position < _template.Length;
    }
}
