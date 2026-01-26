using System.Reflection;
using System.Text;
using Typewriter.CodeModel;
using Typewriter.Core.Abstractions;
using Type = System.Type;

namespace Typewriter.CLI.Generation;

/// <summary>
/// CLI-specific parser that processes template strings and generates output.
/// This is a port of the VS extension's Parser class without VS dependencies.
/// </summary>
public class CliParser
{
    /// <summary>
    /// Parses a template with the given context and returns the rendered output.
    /// </summary>
    /// <param name="templatePath">The path to the template file (for error reporting).</param>
    /// <param name="sourcePath">The path to the source file being processed.</param>
    /// <param name="template">The template string to parse.</param>
    /// <param name="extensions">The list of extension types for custom methods.</param>
    /// <param name="templateInstance">The compiled template instance for invoking custom methods.</param>
    /// <param name="context">The context object (e.g., File) for property resolution.</param>
    /// <param name="errorReporter">The error reporter for logging.</param>
    /// <param name="success">Output parameter indicating whether parsing succeeded.</param>
    /// <returns>The rendered output, or null if no match was found.</returns>
    public static string? Parse(
        string templatePath,
        string sourcePath,
        string template,
        List<Type> extensions,
        object? templateInstance,
        object context,
        IErrorReporter errorReporter,
        out bool success)
    {
        var instance = new CliParser(extensions, templateInstance, errorReporter, templatePath, sourcePath);
        var output = instance.ParseTemplate(template, context);
        success = !instance._hasError;

        return instance._matchFound ? output : null;
    }

    private readonly List<Type> _extensions;
    private readonly object? _templateInstance;
    private readonly IErrorReporter _errorReporter;
    private readonly string _templatePath;
    private readonly string _sourcePath;
    private bool _matchFound;
    private bool _hasError;

    private CliParser(List<Type> extensions, object? templateInstance, IErrorReporter errorReporter, string templatePath, string sourcePath)
    {
        _extensions = extensions;
        _templateInstance = templateInstance;
        _errorReporter = errorReporter;
        _templatePath = templatePath;
        _sourcePath = sourcePath;
    }

    private string? ParseTemplate(string template, object context)
    {
        if (string.IsNullOrEmpty(template))
        {
            return null;
        }

        var output = new StringBuilder();
        var stream = new TemplateStream(template);

        while (stream.Advance())
        {
            if (ParseDollar(stream, context, output))
            {
                continue;
            }

            output.Append(stream.Current);
        }

        return output.ToString();
    }

    private bool ParseDollar(TemplateStream stream, object context, StringBuilder output)
    {
        if (stream.Current == '$')
        {
            var identifier = stream.PeekWord(1);

            if (TryGetIdentifier(identifier, context, out var value))
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

                        if (stringValue != null && !stringValue.Equals(value.GetType().FullName, StringComparison.OrdinalIgnoreCase))
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
                        IEnumerable<Item> items;
                        if (filter != null && filter.StartsWith("$", StringComparison.OrdinalIgnoreCase))
                        {
                            var predicate = filter.Remove(0, 1);
                            items = ApplyPredicateFilter(collection, predicate, identifier);
                        }
                        else
                        {
                            items = CliItemFilter.Apply(collection, filter, ref _matchFound);
                        }

                        output.Append(string.Join(
                            ParseTemplate(separator, context),
                            items.Select(item => ParseTemplate(block, item))));
                    }
                }
                else if (value is bool boolValue)
                {
                    var trueBlock = ParseBlock(stream, '[', ']');
                    var falseBlock = ParseBlock(stream, '[', ']');

                    output.Append(ParseTemplate(boolValue ? trueBlock : falseBlock, context));
                }
                else
                {
                    var block = ParseBlock(stream, '[', ']');
                    if (value != null)
                    {
                        if (block != null)
                        {
                            output.Append(ParseTemplate(block, value));
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

    private static string? ParseBlock(TemplateStream stream, char open, char close)
    {
        if (stream.Peek() == open)
        {
            var block = stream.PeekBlock(2, open, close);

            stream.Advance(block.Length);
            stream.Advance(stream.Peek(2) == close ? 2 : 1);

            return block;
        }

        return null;
    }

    private bool TryGetIdentifier(string? identifier, object context, out object? value)
    {
        value = null;

        if (identifier == null)
        {
            return false;
        }

        var type = context.GetType();

        try
        {
            // First, try to find a property on the context object
            var property = type.GetProperty(identifier);
            if (property != null)
            {
                value = property.GetValue(context);
                return true;
            }

            // Try to find a method in the template instance or extensions
            // Include non-public methods since template methods default to private
            if (_templateInstance != null)
            {
                var templateType = _templateInstance.GetType();
                var methods = templateType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == identifier)
                    .ToList();

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(type))
                    {
                        value = method.Invoke(_templateInstance, new[] { context });
                        return true;
                    }
                    // Also try parameterless methods
                    if (parameters.Length == 0)
                    {
                        value = method.Invoke(_templateInstance, Array.Empty<object>());
                        return true;
                    }
                }
            }

            // Also try static extension methods from extension types
            foreach (var ext in _extensions)
            {
                var staticMethods = ext.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == identifier)
                    .ToList();

                foreach (var method in staticMethods)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(type))
                    {
                        value = method.Invoke(null, new[] { context });
                        return true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _hasError = true;

            var message = $"Error rendering template. Cannot get identifier '{identifier}'.";
            LogException(e, message);
        }

        return false;
    }

    private IEnumerable<Item> ApplyPredicateFilter(IEnumerable<Item> collection, string predicate, string identifier)
    {
        // First, try to find the method on the template instance
        if (_templateInstance != null)
        {
            var templateType = _templateInstance.GetType();
            var method = templateType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == predicate && m.GetParameters().Length == 1);

            if (method != null)
            {
                try
                {
                    var items = collection.Where(x => (bool)method.Invoke(_templateInstance, new object[] { x })!).ToList();
                    _matchFound = _matchFound || items.Any();
                    return items;
                }
                catch (Exception e)
                {
                    _hasError = true;
                    var message = $"Error rendering template. Cannot apply filter to identifier '{identifier}'.";
                    LogException(e, message);
                    return Array.Empty<Item>();
                }
            }
        }

        // Fall back to looking for static methods in extensions
        if (_extensions != null)
        {
            foreach (var ext in _extensions)
            {
                var method = ext.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == predicate && m.GetParameters().Length == 1);

                if (method != null)
                {
                    try
                    {
                        var items = collection.Where(x => (bool)method.Invoke(null, new object[] { x })!).ToList();
                        _matchFound = _matchFound || items.Any();
                        return items;
                    }
                    catch (Exception e)
                    {
                        _hasError = true;
                        var message = $"Error rendering template. Cannot apply filter to identifier '{identifier}'.";
                        LogException(e, message);
                        return Array.Empty<Item>();
                    }
                }
            }
        }

        return Array.Empty<Item>();
    }

    private void LogException(Exception exception, string message)
    {
        // Skip the target invocation exception, get the real exception instead
        if (exception is TargetInvocationException && exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        var fullMessage = $"{message} Error: {exception.Message}. Source path: {_sourcePath}";
        _errorReporter.ReportError(fullMessage, _templatePath);
    }
}
