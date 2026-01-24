using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Typewriter.Core.Abstractions;

namespace Typewriter.CLI.Generation;

/// <summary>
/// Parses template code blocks and compiles custom extensions.
/// This is a port of the VS extension's TemplateCodeParser without VS dependencies.
/// </summary>
public static class CliTemplateCodeParser
{
    private static int _counter;

    /// <summary>
    /// Parses a template, extracting and compiling code blocks.
    /// </summary>
    /// <param name="templatePath">The path to the template file.</param>
    /// <param name="template">The template content.</param>
    /// <param name="extensions">List to populate with compiled extension types.</param>
    /// <param name="errorReporter">The error reporter for logging.</param>
    /// <returns>The processed template with code blocks handled.</returns>
    public static string Parse(
        string templatePath,
        string template,
        List<Type> extensions,
        IErrorReporter errorReporter)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var output = string.Empty;
        var stream = new TemplateStream(template);
        var shadowClass = new CliShadowClass(errorReporter);

        while (stream.Advance())
        {
            if (ParseReference(stream, shadowClass, templatePath, errorReporter))
            {
                continue;
            }

            if (ParseCodeBlock(stream, shadowClass))
            {
                continue;
            }

            if (ParseLambda(stream, shadowClass, ref output))
            {
                continue;
            }

            output += stream.Current;
        }

        // Compile the shadow class
        shadowClass.Parse();

        extensions.Clear();

        try
        {
            var compiledType = shadowClass.Compile(templatePath);
            if (compiledType != null)
            {
                extensions.Add(compiledType);
            }

            // Add extension classes from referenced assemblies
            extensions.AddRange(FindExtensionClasses(shadowClass));
        }
        catch (Exception ex)
        {
            errorReporter.ReportError($"Failed to compile template code: {ex.Message}", templatePath);
        }

        return output;
    }

    private static IEnumerable<Type> FindExtensionClasses(CliShadowClass shadowClass)
    {
        var types = new List<Type>();

        var usings = shadowClass.Snippets
            .Where(s => s.Type == SnippetType.Using && s.Code.StartsWith("using", StringComparison.OrdinalIgnoreCase));

        foreach (var usingStatement in usings.Select(u => u.Code))
        {
            var ns = usingStatement.Remove(0, 5).Trim().Trim(';');

            foreach (var assembly in shadowClass.ReferencedAssemblies)
            {
                try
                {
                    types.AddRange(assembly.GetExportedTypes().Where(t =>
                        string.Equals(t.Namespace, ns, StringComparison.OrdinalIgnoreCase) &&
                        t.GetMethods(BindingFlags.Static | BindingFlags.Public).Any(m =>
                            m.IsDefined(typeof(ExtensionAttribute), false) &&
                            m.GetParameters().FirstOrDefault()?.ParameterType.Namespace?.Equals("Typewriter.CodeModel", StringComparison.OrdinalIgnoreCase) == true)));
                }
                catch
                {
                    // Ignore assemblies that can't be inspected
                }
            }
        }

        return types;
    }

    private static bool ParseCodeBlock(TemplateStream stream, CliShadowClass shadowClass)
    {
        if (stream.Current == '$' && stream.Peek() == '{')
        {
            // Check if this is commented out
            for (var i = 0; ; i--)
            {
                var current = stream.Peek(i);
                if (current == '`' || (current == '/' && stream.Peek(i - 1) == '/'))
                {
                    return false;
                }

                if (current == '\n' || current == char.MinValue)
                {
                    break;
                }
            }

            stream.Advance();

            var block = stream.PeekBlock(1, '{', '}');
            var codeStream = new TemplateStream(block, stream.Position + 1);

            ParseUsings(codeStream, shadowClass);
            ParseCode(codeStream, shadowClass);

            stream.Advance(block.Length + 1);

            return true;
        }

        return false;
    }

    private static void ParseUsings(TemplateStream stream, CliShadowClass shadowClass)
    {
        stream.Advance();

        while (true)
        {
            stream.SkipWhitespace();

            if ((stream.Current == 'u' && string.Equals(stream.PeekWord(), "using", StringComparison.OrdinalIgnoreCase)) ||
                (stream.Current == '/' && stream.Peek() == '/'))
            {
                var line = stream.PeekLine();
                shadowClass.AddUsing(line, stream.Position);
                stream.Advance(line.Length);

                continue;
            }

            break;
        }
    }

    private static void ParseCode(TemplateStream stream, CliShadowClass shadowClass)
    {
        var code = new StringBuilder();

        do
        {
            if (stream.Current != char.MinValue)
            {
                code.Append(stream.Current);
            }
        }
        while (stream.Advance());

        shadowClass.AddBlock(code.ToString(), 0);
    }

    private static bool ParseLambda(TemplateStream stream, CliShadowClass shadowClass, ref string template)
    {
        if (stream.Current == '$')
        {
            var identifier = stream.PeekWord(1);
            if (identifier != null)
            {
                var filter = stream.PeekBlock(identifier.Length + 2, '(', ')');
                if (filter != null && stream.Peek(filter.Length + 2 + identifier.Length + 1) == '[')
                {
                    try
                    {
                        var index = filter.IndexOf("=>", StringComparison.Ordinal);

                        if (index > 0)
                        {
                            var name = filter.Substring(0, index);

                            var contextName = identifier;

                            // Map context names
                            if (string.Equals(contextName, "TypeArguments", StringComparison.OrdinalIgnoreCase))
                            {
                                contextName = "Types";
                            }
                            else if (contextName.StartsWith("Nested", StringComparison.OrdinalIgnoreCase))
                            {
                                contextName = contextName.Remove(0, 6);
                            }

                            var type = GetContextType(contextName);

                            if (type == null)
                            {
                                return false;
                            }

                            var methodIndex = _counter++;

                            shadowClass.AddLambda(filter, type, name, methodIndex);

                            stream.Advance(filter.Length + 2 + identifier.Length);
                            template += $"${identifier}($__{methodIndex})";

                            return true;
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                }
            }
        }

        return false;
    }

    private static string? GetContextType(string contextName)
    {
        // Map common context names to their full type names
        return contextName switch
        {
            "Classes" => "Typewriter.CodeModel.Class",
            "Interfaces" => "Typewriter.CodeModel.Interface",
            "Enums" => "Typewriter.CodeModel.Enum",
            "Delegates" => "Typewriter.CodeModel.Delegate",
            "Records" => "Typewriter.CodeModel.Record",
            "Properties" => "Typewriter.CodeModel.Property",
            "Methods" => "Typewriter.CodeModel.Method",
            "Parameters" => "Typewriter.CodeModel.Parameter",
            "Fields" => "Typewriter.CodeModel.Field",
            "Events" => "Typewriter.CodeModel.Event",
            "Attributes" => "Typewriter.CodeModel.Attribute",
            "Types" => "Typewriter.CodeModel.Type",
            "Constants" => "Typewriter.CodeModel.Constant",
            "TypeParameters" => "Typewriter.CodeModel.TypeParameter",
            _ => null
        };
    }

    private static bool ParseReference(
        TemplateStream stream,
        CliShadowClass shadowClass,
        string templatePath,
        IErrorReporter errorReporter)
    {
        const string keyword = "reference";

        if (stream.Current == '#' && stream.Peek() == keyword[0] &&
            string.Equals(stream.PeekWord(1), keyword, StringComparison.OrdinalIgnoreCase))
        {
            var reference = stream.PeekLine(keyword.Length + 1);
            if (reference != null)
            {
                var len = reference.Length + keyword.Length + 1;
                reference = reference.Trim('"', ' ', '\n', '\r');
                try
                {
                    if (reference.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resolve relative path
                        var templateDir = Path.GetDirectoryName(templatePath);
                        if (!Path.IsPathRooted(reference) && !string.IsNullOrEmpty(templateDir))
                        {
                            reference = Path.Combine(templateDir, reference);
                        }
                    }

                    shadowClass.AddReference(reference);
                    return true;
                }
                catch (Exception ex)
                {
                    errorReporter.ReportWarning($"Reference Error: {ex.Message}", templatePath);
                }
                finally
                {
                    stream.Advance(len - 1);
                }
            }
        }

        return false;
    }
}

/// <summary>
/// Represents a code snippet in the shadow class.
/// </summary>
public class Snippet
{
    public SnippetType Type { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public int Offset { get; private set; }
    public int StartIndex { get; private set; }
    public int EndIndex { get; private set; }
    public int Length => Code.Length;

    public static Snippet Create(SnippetType type, string code)
    {
        return new Snippet { Type = type, Code = code };
    }

    public static Snippet Create(SnippetType type, string code, int offset, int startIndex, int endIndex, int lambdaOffset = 0)
    {
        return new Snippet
        {
            Type = type,
            Code = code,
            Offset = offset,
            StartIndex = startIndex,
            EndIndex = endIndex
        };
    }
}

/// <summary>
/// Type of code snippet.
/// </summary>
public enum SnippetType
{
    Class,
    Using,
    Code,
    Lambda
}
