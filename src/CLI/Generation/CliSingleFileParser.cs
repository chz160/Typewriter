using System.Reflection;
using System.Text;
using Typewriter.CodeModel;
using Typewriter.Core.Abstractions;
using File = Typewriter.CodeModel.File;
using Type = System.Type;

namespace Typewriter.CLI.Generation;

/// <summary>
/// CLI-specific parser for single-file mode that combines multiple files into one output.
/// This is a port of the VS extension's SingleFileParser class without VS dependencies.
/// </summary>
public class CliSingleFileParser
{
    /// <summary>
    /// Parses a template with multiple files in single-file mode.
    /// </summary>
    /// <param name="templatePath">The path to the template file (for error reporting).</param>
    /// <param name="files">The files to process.</param>
    /// <param name="template">The template string to parse.</param>
    /// <param name="extensions">The list of extension types for custom methods.</param>
    /// <param name="errorReporter">The error reporter for logging.</param>
    /// <param name="success">Output parameter indicating whether parsing succeeded.</param>
    /// <returns>The rendered output, or null if no match was found.</returns>
    public static string? Parse(
        string templatePath,
        File[] files,
        string template,
        List<Type> extensions,
        IErrorReporter errorReporter,
        out bool success)
    {
        var instance = new CliSingleFileParser(extensions, errorReporter, templatePath);
        var output = instance.ParseTemplate(template, files);
        success = !instance._hasError;

        return instance._matchFound ? output : null;
    }

    private readonly List<Type> _extensions;
    private readonly IErrorReporter _errorReporter;
    private readonly string _templatePath;
    private bool _matchFound;
    private bool _hasError;

    private CliSingleFileParser(List<Type> extensions, IErrorReporter errorReporter, string templatePath)
    {
        _extensions = extensions;
        _errorReporter = errorReporter;
        _templatePath = templatePath;
    }

    private string? ParseTemplate(string template, File[] files, object? context = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            return null;
        }

        var output = new StringBuilder();
        var stream = new TemplateStream(template);

        while (stream.Advance())
        {
            if (ParseDollar(template, files, stream, context, output))
            {
                continue;
            }

            output.Append(stream.Current);
        }

        return output.ToString();
    }

    private bool ParseDollar(string template, File[] files, TemplateStream stream, object? context, StringBuilder output)
    {
        if (stream.Current == '$')
        {
            var identifier = stream.PeekWord(1);

            if (identifier == null)
            {
                return false;
            }

            stream.Advance(identifier.Length);
            var advance = 0;
            var offset = stream.Position + 1;
            var index = 0;

            foreach (var f in files)
            {
                // Create new stream for each file
                var innerStream = new TemplateStream(template);
                innerStream.Advance(offset);

                var sourcePath = f.FullName;
                context = f;

                if (TryGetIdentifier(identifier, context, sourcePath, out var value))
                {
                    if (value is IEnumerable<Item> collection)
                    {
                        var filter = ParseBlock(innerStream, '(', ')');
                        var block = ParseBlock(innerStream, '[', ']');
                        var separator = ParseBlock(innerStream, '[', ']');

                        if (filter == null && block == null && separator == null)
                        {
                            var stringValue = value.ToString();

                            if (stringValue != null && !string.Equals(stringValue, value.GetType().FullName, StringComparison.OrdinalIgnoreCase))
                            {
                                output.Append(stringValue);
                            }
                            else
                            {
                                output.Append("$").Append(identifier);
                            }
                        }
                        else
                        {
                            var items = ApplyFilter(collection, filter, identifier, sourcePath);

                            output.Append(string.Join(
                                ParseTemplate(sourcePath, separator, context),
                                items.Select(item => ParseTemplate(sourcePath, block, item))));

                            // Check if we need a separator before the next file's items
                            for (var i = index + 1; i < files.Length; i++)
                            {
                                var next = files[i];

                                if (next != null)
                                {
                                    if (TryGetIdentifier(identifier, next, next.FullName, out var vnext))
                                    {
                                        if (vnext is IEnumerable<Item> colnext)
                                        {
                                            colnext = ApplyFilter(colnext, filter, identifier, next.FullName);

                                            if (colnext.Any())
                                            {
                                                output.Append(ParseTemplate(sourcePath, separator, context));
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (value is bool boolValue)
                    {
                        var trueBlock = ParseBlock(innerStream, '[', ']');
                        var falseBlock = ParseBlock(innerStream, '[', ']');

                        output.Append(ParseTemplate(sourcePath, boolValue ? trueBlock : falseBlock, context));
                    }
                    else
                    {
                        var block = ParseBlock(innerStream, '[', ']');
                        if (value != null)
                        {
                            if (block != null)
                            {
                                output.Append(ParseTemplate(sourcePath, block, value));
                            }
                            else
                            {
                                output.Append(value.ToString());
                            }
                        }
                    }
                }

                advance = innerStream.Position + 1;
                index++;
            }

            stream.Advance(advance - offset);

            return true;
        }

        return false;
    }

    private IEnumerable<Item> ApplyFilter(
        IEnumerable<Item> collection,
        string? filter,
        string identifier,
        string sourcePath)
    {
        IEnumerable<Item> items;

        if (filter != null && filter.StartsWith("$", StringComparison.OrdinalIgnoreCase))
        {
            var predicate = filter.Remove(0, 1);
            if (_extensions != null)
            {
                var method = _extensions.FirstOrDefault()?.GetMethod(predicate);
                if (method != null)
                {
                    try
                    {
                        items = collection.Where(x => (bool)method.Invoke(null, new object[] { x })!).ToList();
                        _matchFound = _matchFound || items.Any();
                    }
                    catch (Exception e)
                    {
                        items = Array.Empty<Item>();
                        _hasError = true;

                        var message = $"Error rendering template. Cannot apply filter to identifier '{identifier}'.";
                        LogException(e, message, sourcePath);
                    }
                }
                else
                {
                    items = Array.Empty<Item>();
                }
            }
            else
            {
                items = Array.Empty<Item>();
            }
        }
        else
        {
            items = CliItemFilter.Apply(collection, filter, ref _matchFound);
        }

        return items;
    }

    private static string? ParseBlock(TemplateStream stream, char open, char close, bool onlyPeek = false)
    {
        if (stream.Peek() == open)
        {
            var block = stream.PeekBlock(2, open, close);

            if (!onlyPeek)
            {
                stream.Advance(block.Length);
                stream.Advance(stream.Peek(2) == close ? 2 : 1);
            }

            return block;
        }

        return null;
    }

    private bool TryGetIdentifier(string? identifier, object context, string sourcePath, out object? value)
    {
        value = null;

        if (identifier == null)
        {
            return false;
        }

        var type = context.GetType();

        try
        {
            var property = type.GetProperty(identifier);
            if (property != null)
            {
                value = property.GetValue(context);
                return true;
            }

            var extension = _extensions
                .Select(e => e.GetMethod(identifier, new[] { type }))
                .FirstOrDefault(m => m != null);

            if (extension != null)
            {
                value = extension.Invoke(null, new[] { context });
                return true;
            }
        }
        catch (Exception e)
        {
            _hasError = true;

            var message = $"Error rendering template. Cannot get identifier '{identifier}'.";
            LogException(e, message, sourcePath);
        }

        return false;
    }

    private void LogException(Exception exception, string message, string sourcePath)
    {
        if (exception is TargetInvocationException && exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        var fullMessage = $"{message} Error: {exception.Message}. Source path: {sourcePath}";
        _errorReporter.ReportError(fullMessage, _templatePath);
    }

    private string? ParseTemplate(string sourcePath, string? template, object context)
    {
        if (string.IsNullOrEmpty(template))
        {
            return null;
        }

        var output = new StringBuilder();
        var stream = new TemplateStream(template);

        while (stream.Advance())
        {
            if (ParseDollar(sourcePath, stream, context, output))
            {
                continue;
            }

            output.Append(stream.Current);
        }

        return output.ToString();
    }

    private bool ParseDollar(string sourcePath, TemplateStream stream, object context, StringBuilder output)
    {
        if (stream.Current == '$')
        {
            var identifier = stream.PeekWord(1);

            if (TryGetIdentifier(identifier, context, sourcePath, out var value))
            {
                stream.Advance(identifier!.Length);

                if (value is IEnumerable<Item> collection)
                {
                    var filter = ParseBlock(stream, '(', ')');
                    var block = ParseBlock(stream, '[', ']');
                    var separator = ParseBlock(stream, '[', ']');

                    if (filter == null && block == null && separator == null)
                    {
                        var stringValue = value.ToString();

                        if (stringValue != null && !string.Equals(stringValue, value.GetType().FullName, StringComparison.OrdinalIgnoreCase))
                        {
                            output.Append(stringValue);
                        }
                        else
                        {
                            output.Append("$").Append(identifier);
                        }
                    }
                    else
                    {
                        var items = ApplyFilter(collection, filter, identifier, sourcePath);

                        output.Append(string.Join(
                            ParseTemplate(sourcePath, separator, context),
                            items.Select(item => ParseTemplate(sourcePath, block, item))));
                    }
                }
                else if (value is bool boolValue)
                {
                    var trueBlock = ParseBlock(stream, '[', ']');
                    var falseBlock = ParseBlock(stream, '[', ']');

                    output.Append(ParseTemplate(sourcePath, boolValue ? trueBlock : falseBlock, context));
                }
                else
                {
                    var block = ParseBlock(stream, '[', ']');
                    if (value != null)
                    {
                        if (block != null)
                        {
                            output.Append(ParseTemplate(sourcePath, block, value));
                        }
                        else
                        {
                            output.Append(value.ToString());
                        }
                    }
                }

                return true;
            }
        }

        return false;
    }
}
