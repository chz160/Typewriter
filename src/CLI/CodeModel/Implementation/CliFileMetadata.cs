using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IFileMetadata"/> that provides metadata about a C# source file.
/// This implementation works without Visual Studio dependencies.
/// </summary>
public class CliFileMetadata : IFileMetadata
{
    private readonly Document _document;
    private readonly Settings _settings;
    private readonly Action<string[]>? _requestRender;
    private readonly SyntaxNode _root;
    private readonly SemanticModel _semanticModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliFileMetadata"/> class.
    /// </summary>
    /// <param name="document">The Roslyn document.</param>
    /// <param name="settings">The generation settings.</param>
    /// <param name="requestRender">Callback for requesting additional renders.</param>
    public CliFileMetadata(Document document, Settings settings, Action<string[]>? requestRender)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _requestRender = requestRender;

        // Load document synchronously (CLI doesn't have VS threading requirements)
        _semanticModel = document.GetSemanticModelAsync().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException($"Could not get semantic model for {document.FilePath}");
        _root = _semanticModel.SyntaxTree.GetRoot();
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string Name => _document.Name;

    /// <summary>
    /// Gets the full file path.
    /// </summary>
    public string FullName => _document.FilePath ?? _document.Name;

    /// <summary>
    /// Gets the classes defined in this file.
    /// </summary>
    public IEnumerable<IClassMetadata> Classes =>
        CliClassMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<ClassDeclarationSyntax>(), this, _settings);

    /// <summary>
    /// Gets the delegates defined in this file.
    /// </summary>
    public IEnumerable<IDelegateMetadata> Delegates =>
        CliDelegateMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<DelegateDeclarationSyntax>(), _settings);

    /// <summary>
    /// Gets the enums defined in this file.
    /// </summary>
    public IEnumerable<IEnumMetadata> Enums =>
        CliEnumMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<EnumDeclarationSyntax>(), _settings);

    /// <summary>
    /// Gets the interfaces defined in this file.
    /// </summary>
    public IEnumerable<IInterfaceMetadata> Interfaces =>
        CliInterfaceMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<InterfaceDeclarationSyntax>(), this, _settings);

    /// <summary>
    /// Gets the records defined in this file.
    /// </summary>
    public IEnumerable<IRecordMetadata> Records =>
        CliRecordMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<RecordDeclarationSyntax>(), this, _settings);

    /// <summary>
    /// Gets the settings for this file.
    /// </summary>
    public Settings Settings => _settings;

    private IEnumerable<INamedTypeSymbol> GetNamespaceChildNodes<T>() where T : SyntaxNode
    {
        var nodes = _root.ChildNodes().OfType<T>()
            .Concat(_root.ChildNodes().OfType<BaseNamespaceDeclarationSyntax>()
                .SelectMany(n => n.ChildNodes().OfType<T>()));

        var symbols = nodes
            .Select(c => _semanticModel.GetDeclaredSymbol(c) as INamedTypeSymbol)
            .Where(s => s != null)
            .Cast<INamedTypeSymbol>();

        if (_settings.PartialRenderingMode == PartialRenderingMode.Combined)
        {
            return symbols.Where(s =>
            {
                var locationToRender = s.Locations
                    .Select(l => l.SourceTree?.FilePath)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

                if (string.Equals(locationToRender, FullName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (locationToRender != null)
                {
                    _requestRender?.Invoke(new[] { locationToRender });
                }

                return false;
            }).ToList();
        }

        return symbols;
    }
}
