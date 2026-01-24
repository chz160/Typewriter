using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IClassMetadata"/>.
/// </summary>
public class CliClassMetadata : IClassMetadata
{
    private readonly INamedTypeSymbol _symbol;
    private readonly CliFileMetadata? _file;
    private readonly Settings _settings;
    private IReadOnlyCollection<ISymbol>? _members;

    private CliClassMetadata(INamedTypeSymbol symbol, CliFileMetadata? file, Settings settings)
    {
        _symbol = symbol;
        _file = file;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public string FullName => _symbol.ToDisplayString();
    public bool IsAbstract => _symbol.IsAbstract;
    public bool IsGeneric => _symbol.TypeParameters.Any();
    public bool IsStatic => _symbol.IsStatic;
    public string Namespace => _symbol.GetNamespace() ?? string.Empty;

    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol, _settings);
    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public IClassMetadata? BaseClass => FromNamedTypeSymbol(_symbol.BaseType, _settings);
    public IClassMetadata? ContainingClass => FromNamedTypeSymbol(_symbol.ContainingType, _settings);

    public IEnumerable<IConstantMetadata> Constants => CliConstantMetadata.FromFieldSymbols(Members.OfType<IFieldSymbol>(), _settings);
    public IEnumerable<IDelegateMetadata> Delegates => CliDelegateMetadata.FromNamedTypeSymbols(Members.OfType<INamedTypeSymbol>().Where(s => s.TypeKind == TypeKind.Delegate), _settings);
    public IEnumerable<IEventMetadata> Events => CliEventMetadata.FromEventSymbols(Members.OfType<IEventSymbol>(), _settings);
    public IEnumerable<IFieldMetadata> Fields => CliFieldMetadata.FromFieldSymbols(Members.OfType<IFieldSymbol>(), _settings);
    public IEnumerable<IInterfaceMetadata> Interfaces => CliInterfaceMetadata.FromNamedTypeSymbols(_symbol.Interfaces, null, _settings);
    public IEnumerable<IMethodMetadata> Methods => CliMethodMetadata.FromMethodSymbols(Members.OfType<IMethodSymbol>(), _settings);
    public IEnumerable<IPropertyMetadata> Properties => CliPropertyMetadata.FromPropertySymbols(Members.OfType<IPropertySymbol>(), _settings);
    public IEnumerable<IStaticReadOnlyFieldMetadata> StaticReadOnlyFields => CliStaticReadOnlyFieldMetadata.FromFieldSymbols(Members.OfType<IFieldSymbol>(), _settings);
    public IEnumerable<ITypeParameterMetadata> TypeParameters => CliTypeParameterMetadata.FromTypeParameterSymbols(_symbol.TypeParameters);
    public IEnumerable<ITypeMetadata> TypeArguments => CliTypeMetadata.FromTypeSymbols(_symbol.TypeArguments, _settings);
    public IEnumerable<IClassMetadata> NestedClasses => FromNamedTypeSymbols(Members.OfType<INamedTypeSymbol>().Where(s => s.TypeKind == TypeKind.Class), null, _settings);
    public IEnumerable<IEnumMetadata> NestedEnums => CliEnumMetadata.FromNamedTypeSymbols(Members.OfType<INamedTypeSymbol>().Where(s => s.TypeKind == TypeKind.Enum), _settings);
    public IEnumerable<IInterfaceMetadata> NestedInterfaces => CliInterfaceMetadata.FromNamedTypeSymbols(Members.OfType<INamedTypeSymbol>().Where(s => s.TypeKind == TypeKind.Interface), null, _settings);

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

    internal static IClassMetadata? FromNamedTypeSymbol(INamedTypeSymbol? symbol, Settings settings)
    {
        if (symbol == null) return null;
        if (symbol.DeclaredAccessibility != Accessibility.Public) return null;
        if (symbol.ToDisplayString().Equals("object", StringComparison.OrdinalIgnoreCase)) return null;
        return new CliClassMetadata(symbol, null, settings);
    }

    internal static IEnumerable<IClassMetadata> FromNamedTypeSymbols(IEnumerable<INamedTypeSymbol> symbols, CliFileMetadata? file, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public && !s.ToDisplayString().Equals("object", StringComparison.OrdinalIgnoreCase))
            .Select(s => new CliClassMetadata(s, file, settings));
    }
}
