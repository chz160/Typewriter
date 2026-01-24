using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IParameterMetadata"/>.
/// </summary>
public class CliParameterMetadata : IParameterMetadata
{
    private readonly IParameterSymbol _symbol;
    private readonly Settings _settings;

    private CliParameterMetadata(IParameterSymbol symbol, Settings settings)
    {
        _symbol = symbol;
        _settings = settings;
    }

    public string Name => _symbol.Name;
    public string FullName => _symbol.ToDisplayString();
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public bool HasDefaultValue => _symbol.HasExplicitDefaultValue;
    public string DefaultValue => GetDefaultValue();

    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData(_symbol.GetAttributes(), _settings);
    public ITypeMetadata Type => CliTypeMetadata.FromTypeSymbol(_symbol.Type, _settings);

    internal static IEnumerable<IParameterMetadata> FromParameterSymbols(IEnumerable<IParameterSymbol> symbols, Settings settings)
    {
        return symbols.Select(s => new CliParameterMetadata(s, settings));
    }

    private string GetDefaultValue()
    {
        if (!_symbol.HasExplicitDefaultValue)
        {
            return string.Empty;
        }

        if (_symbol.ExplicitDefaultValue == null)
        {
            return "null";
        }

        if (_symbol.ExplicitDefaultValue is string stringValue)
        {
            return $"\"{stringValue.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }

        if (_symbol.ExplicitDefaultValue is bool v)
        {
            return v ? "true" : "false";
        }

        return _symbol.ExplicitDefaultValue.ToString() ?? string.Empty;
    }
}
