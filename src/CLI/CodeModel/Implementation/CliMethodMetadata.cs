using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IMethodMetadata"/>.
/// </summary>
public class CliMethodMetadata : IMethodMetadata
{
    private readonly IMethodSymbol _symbol;
    private readonly Settings _settings;

    private CliMethodMetadata(IMethodSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string FullName => _symbol.ToDisplayString();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public bool IsAbstract => _symbol.IsAbstract;
    public bool IsGeneric => _symbol.TypeParameters.Any();
    public bool IsVirtual => _symbol.IsVirtual;

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public IEnumerable<IParameterMetadata> Parameters => CliParameterMetadata.FromParameterSymbols(_symbol.Parameters, _settings);
    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol.ReturnType, _settings);
    public IEnumerable<ITypeParameterMetadata> TypeParameters => CliTypeParameterMetadata.FromTypeParameterSymbols(_symbol.TypeParameters);

    internal static IEnumerable<IMethodMetadata> FromMethodSymbols(IEnumerable<IMethodSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.DeclaredAccessibility == Accessibility.Public &&
                       s.MethodKind == MethodKind.Ordinary &&
                       !s.IsStatic)
            .Select(s => new CliMethodMetadata(s, settings));
    }
}
