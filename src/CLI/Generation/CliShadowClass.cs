using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Typewriter.CodeModel;
using Typewriter.Core.Abstractions;
using Type = System.Type;
using RoslynDiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;

namespace Typewriter.CLI.Generation;

/// <summary>
/// Manages the compilation of template code blocks into a runtime assembly.
/// This is a CLI-specific implementation without VS dependencies.
/// </summary>
public class CliShadowClass
{
    #region Constants

    private const string StartTemplate = @"namespace __Typewriter
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Typewriter.CodeModel;
    using Typewriter.Configuration;
    using Attribute = Typewriter.CodeModel.Attribute;
    using Enum = Typewriter.CodeModel.Enum;
    using Type = Typewriter.CodeModel.Type;
    ";

    private const string ClassTemplate = @"
    public class Template
    {
";

    private const string EndClassTemplate = @"
    }";

    private const string EndTemplate = @"
}
";
    #endregion

    private readonly List<Snippet> _snippets = new();
    private readonly HashSet<Assembly> _referencedAssemblies = new();
    private readonly IErrorReporter _errorReporter;
    private int _offset;
    private bool _classAdded;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliShadowClass"/> class.
    /// </summary>
    /// <param name="errorReporter">The error reporter for logging.</param>
    public CliShadowClass(IErrorReporter errorReporter)
    {
        _errorReporter = errorReporter;
        AddDefaultReferencedAssemblies();
        Clear();
    }

    /// <summary>
    /// Gets the code snippets.
    /// </summary>
    public IEnumerable<Snippet> Snippets => _snippets;

    /// <summary>
    /// Gets the referenced assemblies.
    /// </summary>
    public IEnumerable<Assembly> ReferencedAssemblies => _referencedAssemblies;

    private void AddDefaultReferencedAssemblies()
    {
        // Add the CodeModel assembly
        _referencedAssemblies.Add(typeof(Class).Assembly);

        // Add common system assemblies
        _referencedAssemblies.Add(typeof(object).Assembly); // mscorlib/System.Private.CoreLib
        _referencedAssemblies.Add(typeof(Enumerable).Assembly); // System.Linq
        _referencedAssemblies.Add(typeof(List<>).Assembly); // System.Collections.Generic
    }

    /// <summary>
    /// Adds a reference to an assembly.
    /// </summary>
    /// <param name="pathOrName">The path or name of the assembly.</param>
    public void AddReference(string pathOrName)
    {
        try
        {
            var asm = pathOrName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? Assembly.LoadFrom(pathOrName)
                : Assembly.Load(pathOrName);

            _referencedAssemblies.Add(asm);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportWarning($"Failed to load reference '{pathOrName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a using statement.
    /// </summary>
    /// <param name="code">The using statement code.</param>
    /// <param name="startIndex">The start index in the template.</param>
    public void AddUsing(string code, int startIndex)
    {
        _snippets.Add(Snippet.Create(SnippetType.Using, code, _offset, startIndex, startIndex + code.Length));
        _offset += code.Length;
    }

    /// <summary>
    /// Adds a code block.
    /// </summary>
    /// <param name="code">The code block.</param>
    /// <param name="startIndex">The start index in the template.</param>
    public void AddBlock(string code, int startIndex)
    {
        if (!_classAdded)
        {
            _snippets.Add(Snippet.Create(SnippetType.Class, ClassTemplate));
            _offset += ClassTemplate.Length;
            _classAdded = true;
        }

        _snippets.Add(Snippet.Create(SnippetType.Code, code, _offset, startIndex, startIndex + code.Length));
        _offset += code.Length;
    }

    /// <summary>
    /// Adds a lambda filter method.
    /// </summary>
    /// <param name="code">The lambda code.</param>
    /// <param name="type">The type parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="startIndex">The method index.</param>
    public void AddLambda(string code, string type, string name, int startIndex)
    {
        if (!_classAdded)
        {
            _snippets.Add(Snippet.Create(SnippetType.Class, ClassTemplate));
            _offset += ClassTemplate.Length;
            _classAdded = true;
        }

        var method = $"public static bool __{startIndex} ({type} {name}) {{ return ";
        var index = code.IndexOf("=>", StringComparison.Ordinal) + 2;
        code = code.Remove(0, index);

        _snippets.Add(Snippet.Create(SnippetType.Class, method));
        _offset += method.Length;

        _snippets.Add(Snippet.Create(SnippetType.Lambda, code, _offset, startIndex, startIndex + code.Length, index));
        _offset += code.Length;

        _snippets.Add(Snippet.Create(SnippetType.Class, ";}"));
        _offset += 2;
    }

    /// <summary>
    /// Clears all snippets and resets the state.
    /// </summary>
    public void Clear()
    {
        _snippets.Clear();
        _snippets.Add(Snippet.Create(SnippetType.Class, StartTemplate));
        _offset = StartTemplate.Length;
        _classAdded = false;
    }

    /// <summary>
    /// Finalizes the shadow class code.
    /// </summary>
    public void Parse()
    {
        if (!_classAdded)
        {
            _snippets.Add(Snippet.Create(SnippetType.Class, ClassTemplate));
            _offset += ClassTemplate.Length;
            _classAdded = true;
        }

        _snippets.Add(Snippet.Create(SnippetType.Class, EndClassTemplate));
        _snippets.Add(Snippet.Create(SnippetType.Class, EndTemplate));
    }

    /// <summary>
    /// Compiles the shadow class and returns the Template type.
    /// </summary>
    /// <param name="templatePath">The template path for error reporting.</param>
    /// <returns>The compiled Template type, or null if compilation failed.</returns>
    public Type? Compile(string templatePath)
    {
        var code = string.Join(string.Empty, _snippets.Select(s => s.Code));

        // If there's no actual code (just the template wrapper), return null
        if (!_snippets.Any(s => s.Type == SnippetType.Code || s.Type == SnippetType.Lambda))
        {
            return null;
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = GetMetadataReferences();

        var compilation = CSharpCompilation.Create(
            $"Typewriter_{Guid.NewGuid():N}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == RoslynDiagnosticSeverity.Error || d.Severity == RoslynDiagnosticSeverity.Warning);

            foreach (var error in errors)
            {
                var message = error.GetMessage();
                message = message.Replace("__Typewriter.", string.Empty);
                message = message.Replace("publicstatic", string.Empty);

                if (error.Severity == RoslynDiagnosticSeverity.Error || error.IsWarningAsError)
                {
                    _errorReporter.ReportError($"Template error: {error.Id} {message}", templatePath);
                }
                else
                {
                    _errorReporter.ReportWarning($"Template warning: {error.Id} {message}", templatePath);
                }
            }

            if (result.Diagnostics.Any(d => d.Severity == RoslynDiagnosticSeverity.Error))
            {
                return null;
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        return assembly.GetType("__Typewriter.Template");
    }

    private List<MetadataReference> GetMetadataReferences()
    {
        var references = new List<MetadataReference>();

        // Add references from loaded assemblies
        foreach (var assembly in _referencedAssemblies)
        {
            try
            {
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
            catch
            {
                // Ignore assemblies that can't be referenced
            }
        }

        // Add essential runtime references
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrEmpty(trustedAssemblies))
        {
            var paths = trustedAssemblies.Split(Path.PathSeparator);
            var essentialAssemblies = new[]
            {
                "System.Runtime.dll",
                "System.Collections.dll",
                "System.Linq.dll",
                "System.Linq.Expressions.dll",
                "System.Text.RegularExpressions.dll",
                "netstandard.dll"
            };

            foreach (var path in paths)
            {
                var fileName = Path.GetFileName(path);
                if (essentialAssemblies.Any(a => string.Equals(fileName, a, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        if (!references.Any(r => r.Display?.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) == true))
                        {
                            references.Add(MetadataReference.CreateFromFile(path));
                        }
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
            }
        }

        return references;
    }
}
