using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IPropertyMetadata"/>.
/// </summary>
public class CliPropertyMetadata : IPropertyMetadata
{
    private readonly IPropertySymbol _symbol;
    private readonly Settings _settings;

    private CliPropertyMetadata(IPropertySymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string FullName => _symbol.ToDisplayString();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public bool IsAbstract => _symbol.IsAbstract;
    public bool IsVirtual => _symbol.IsVirtual;
    public bool HasGetter => _symbol.GetMethod != null && _symbol.GetMethod.DeclaredAccessibility == Accessibility.Public;
    public bool HasSetter => _symbol.SetMethod != null && _symbol.SetMethod.DeclaredAccessibility == Accessibility.Public;

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol.Type, _settings);

    internal static IEnumerable<IPropertyMetadata> FromPropertySymbols(IEnumerable<IPropertySymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public && !s.IsStatic && !s.IsIndexer)
            .Select(s => new CliPropertyMetadata(s, settings));
    }
}
