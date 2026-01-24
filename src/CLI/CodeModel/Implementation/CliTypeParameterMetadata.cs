using Microsoft.CodeAnalysis;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="ITypeParameterMetadata"/>.
/// </summary>
public class CliTypeParameterMetadata : ITypeParameterMetadata
{
    private readonly ITypeParameterSymbol _symbol;

    private CliTypeParameterMetadata(ITypeParameterSymbol symbol)
    {
        _symbol = symbol;
    }

    public string Name => _symbol.Name;

    internal static IEnumerable<ITypeParameterMetadata> FromTypeParameterSymbols(IEnumerable<ITypeParameterSymbol> symbols)
    {
        return symbols.Select(s => new CliTypeParameterMetadata(s));
    }
}
