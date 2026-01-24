using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IInterfaceMetadata"/>.
/// </summary>
public class CliInterfaceMetadata : IInterfaceMetadata
{
    private readonly INamedTypeSymbol _symbol;
    private readonly CliFileMetadata? _file;
    private readonly Settings _settings;
    private IReadOnlyCollection<ISymbol>? _members;

    private CliInterfaceMetadata(INamedTypeSymbol symbol, CliFileMetadata? file, Settings settings)
    {
        _symbol = symbol;
        _file = file;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public string FullName => _symbol.ToDisplayString();
    public bool IsGeneric => _symbol.TypeParameters.Any();
    public string Namespace => _symbol.GetNamespace() ?? string.Empty;

    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol, _settings);
    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public IClassMetadata? ContainingClass => CliClassMetadata.FromNamedTypeSymbol(_symbol.ContainingType, _settings);
    public IEnumerable<IEventMetadata> Events => CliEventMetadata.FromEventSymbols(Members.OfType<IEventSymbol>(), _settings);
    public IEnumerable<IInterfaceMetadata> Interfaces => FromNamedTypeSymbols(_symbol.Interfaces, null, _settings);
    public IEnumerable<IMethodMetadata> Methods => CliMethodMetadata.FromMethodSymbols(Members.OfType<IMethodSymbol>(), _settings);
    public IEnumerable<IPropertyMetadata> Properties => CliPropertyMetadata.FromPropertySymbols(Members.OfType<IPropertySymbol>(), _settings);
    public IEnumerable<ITypeParameterMetadata> TypeParameters => CliTypeParameterMetadata.FromTypeParameterSymbols(_symbol.TypeParameters);
    public IEnumerable<ITypeMetadata> TypeArguments => CliTypeMetadata.FromTypeSymbols(_symbol.TypeArguments, _settings);

    private IReadOnlyCollection<ISymbol> Members
    {
        get
        {
            if (_members == null)
            {
                if (_file?.Settings.PartialRenderingMode == PartialRenderingMode.Partial && _symbol.Locations.Length > 1)
                {
                    _members = _symbol.GetMembers()
                        .Where(m => m.Locations.Any(l => string.Equals(l.SourceTree?.FilePath, _file.FullName, StringComparison.OrdinalIgnoreCase)))
                        .ToArray();
                }
                else
                {
                    _members = _symbol.GetMembers().ToArray();
                }
            }
            return _members;
        }
    }

    internal static IEnumerable<IInterfaceMetadata> FromNamedTypeSymbols(IEnumerable<INamedTypeSymbol> symbols, CliFileMetadata? file, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public)
            .Select(s => new CliInterfaceMetadata(s, file, settings));
    }
}
