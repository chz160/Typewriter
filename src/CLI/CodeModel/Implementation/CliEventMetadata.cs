using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IEventMetadata"/>.
/// </summary>
public class CliEventMetadata : IEventMetadata
{
    private readonly IEventSymbol _symbol;
    private readonly Settings _settings;

    private CliEventMetadata(IEventSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string FullName => _symbol.ToDisplayString();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol.Type, _settings);

    internal static IEnumerable<IEventMetadata> FromEventSymbols(IEnumerable<IEventSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public && !s.IsStatic)
            .Select(s => new CliEventMetadata(s, settings));
    }
}
