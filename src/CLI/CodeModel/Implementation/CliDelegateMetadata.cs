using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IDelegateMetadata"/>.
/// </summary>
public class CliDelegateMetadata : IDelegateMetadata
{
    private readonly INamedTypeSymbol _symbol;
    private readonly IMethodSymbol? _methodSymbol;
    private readonly Settings _settings;

    private CliDelegateMetadata(INamedTypeSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _methodSymbol = symbol.DelegateInvokeMethod;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string FullName => _symbol.GetFullName();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public bool IsAbstract => false;
    public bool IsGeneric => _symbol.TypeParameters.Any();

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public ITypeMetadata? Type => _methodSymbol == null ? null : CliTypeMetadata.FromTypeSymbol(_methodSymbol.ReturnType, _settings);
    public IEnumerable<ITypeParameterMetadata> TypeParameters => CliTypeParameterMetadata.FromTypeParameterSymbols(_symbol.TypeParameters);
    public IEnumerable<IParameterMetadata> Parameters => _methodSymbol == null
        ? Array.Empty<IParameterMetadata>()
        : CliParameterMetadata.FromParameterSymbols(_methodSymbol.Parameters, _settings);

    internal static IEnumerable<IDelegateMetadata> FromNamedTypeSymbols(IEnumerable<INamedTypeSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public)
            .Select(s => new CliDelegateMetadata(s, settings));
    }
}
