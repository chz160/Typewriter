using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IEnumMetadata"/>.
/// </summary>
public class CliEnumMetadata : IEnumMetadata
{
    private readonly INamedTypeSymbol _symbol;
    private readonly Settings _settings;

    private CliEnumMetadata(INamedTypeSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public string FullName => _symbol.ToDisplayString();
    public string Namespace => _symbol.GetNamespace() ?? string.Empty;
    public bool IsFlags => _symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "FlagsAttribute");

    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol, _settings);
    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public IEnumerable<IEnumValueMetadata> Values => CliEnumValueMetadata.FromFieldSymbols(_symbol.GetMembers().OfType<IFieldSymbol>(), _settings);
    public IClassMetadata? ContainingClass => CliClassMetadata.FromNamedTypeSymbol(_symbol.ContainingType, _settings);

    internal static IEnumerable<IEnumMetadata> FromNamedTypeSymbols(IEnumerable<INamedTypeSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public && s.TypeKind == TypeKind.Enum)
            .Select(s => new CliEnumMetadata(s, settings));
    }
}
