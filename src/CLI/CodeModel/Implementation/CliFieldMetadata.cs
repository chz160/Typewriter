using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IFieldMetadata"/>.
/// </summary>
public class CliFieldMetadata : IFieldMetadata
{
    private readonly IFieldSymbol _symbol;
    private readonly Settings _settings;

    private CliFieldMetadata(IFieldSymbol symbol, Settings settings)
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

    internal static IEnumerable<IFieldMetadata> FromFieldSymbols(IEnumerable<IFieldSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public && !s.IsConst && !s.IsStatic)
            .Select(s => new CliFieldMetadata(s, settings));
    }
}

/// <summary>
/// CLI implementation of <see cref="IConstantMetadata"/>.
/// </summary>
public class CliConstantMetadata : IConstantMetadata
{
    private readonly IFieldSymbol _symbol;
    private readonly Settings _settings;

    private CliConstantMetadata(IFieldSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string FullName => _symbol.ToDisplayString();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public string Value => _symbol.ConstantValue?.ToString() ?? string.Empty;

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol.Type, _settings);

    internal static IEnumerable<IConstantMetadata> FromFieldSymbols(IEnumerable<IFieldSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public && s.IsConst)
            .Select(s => new CliConstantMetadata(s, settings));
    }
}

/// <summary>
/// CLI implementation of <see cref="IStaticReadOnlyFieldMetadata"/>.
/// </summary>
public class CliStaticReadOnlyFieldMetadata : IStaticReadOnlyFieldMetadata
{
    private readonly IFieldSymbol _symbol;
    private readonly Settings _settings;

    private CliStaticReadOnlyFieldMetadata(IFieldSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string FullName => _symbol.ToDisplayString();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public string Value => _symbol.ConstantValue?.ToString() ?? string.Empty;

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol.Type, _settings);

    internal static IEnumerable<IStaticReadOnlyFieldMetadata> FromFieldSymbols(IEnumerable<IFieldSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public && s.IsStatic && s.IsReadOnly && !s.IsConst)
            .Select(s => new CliStaticReadOnlyFieldMetadata(s, settings));
    }
}
