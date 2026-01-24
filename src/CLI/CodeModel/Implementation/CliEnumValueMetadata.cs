using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IEnumValueMetadata"/>.
/// </summary>
public class CliEnumValueMetadata : IEnumValueMetadata
{
    private readonly IFieldSymbol _symbol;
    private readonly Settings _settings;

    private CliEnumValueMetadata(IFieldSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string DocComment => _symbol.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.Name;
    public string FullName => _symbol.ToDisplayString();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public long Value => Convert.ToInt64(_symbol.ConstantValue ?? 0);

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);

    internal static IEnumerable<IEnumValueMetadata> FromFieldSymbols(IEnumerable<IFieldSymbol> symbols, Settings settings)
    {
        return symbols
            .Where(s => s.HasConstantValue)
            .Select(s => new CliEnumValueMetadata(s, settings));
    }
}
