using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="IAttributeMetadata"/>.
/// </summary>
public class CliAttributeMetadata : IAttributeMetadata
{
    private readonly INamedTypeSymbol? _symbol;
    private readonly Settings _settings;
    private readonly string _name;
    private readonly string _value;

    private CliAttributeMetadata(AttributeData attribute, Settings settings)
    {
        _symbol = attribute.AttributeClass;
        _settings = settings;

        var declaration = attribute.ToString() ?? string.Empty;
        var index = declaration.IndexOf("(", StringComparison.Ordinal);

        _name = _symbol?.Name ?? string.Empty;

        if (index > -1)
        {
            _value = declaration.Substring(index + 1, declaration.Length - index - 2);

            // Trim {} from params
            if (_value.EndsWith("\"}", StringComparison.OrdinalIgnoreCase))
            {
                _value = _value.Remove(_value.LastIndexOf("{\"", StringComparison.Ordinal), 1);
                _value = _value.TrimEnd('}');
            }
            else if (_value.EndsWith("}", StringComparison.OrdinalIgnoreCase))
            {
                _value = _value.Remove(_value.LastIndexOf("{", StringComparison.Ordinal), 1);
                _value = _value.TrimEnd('}');
            }
        }
        else
        {
            _value = string.Empty;
        }

        if (_name.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
        {
            _name = _name.Substring(0, _name.Length - 9);
        }

        Arguments = attribute.ConstructorArguments.Concat(attribute.NamedArguments.Select(p => p.Value))
            .Select(p => new CliAttributeArgumentMetadata(p, settings));
    }

    public string DocComment => string.Empty;
    public string Name => _name;
    public string FullName => _symbol?.ToDisplayString() ?? string.Empty;
    public string AssemblyName => _symbol?.ContainingAssembly?.Name ?? string.Empty;
    public string Value => _value;

    public ITypeMetadata Type => _symbol != null ? CliTypeMetadata.FromTypeSymbol(_symbol, _settings) : null!;
    public IEnumerable<IAttributeArgumentMetadata> Arguments { get; }

    internal static IEnumerable<IAttributeMetadata> FromAttributeData(IEnumerable<AttributeData> attributes, Settings settings)
    {
        return attributes
            .Where(a => a.AttributeClass?.DeclaredAccessibility == Accessibility.Public)
            .Select(a => new CliAttributeMetadata(a, settings));
    }
}

/// <summary>
/// CLI implementation of <see cref="IAttributeArgumentMetadata"/>.
/// </summary>
public class CliAttributeArgumentMetadata : IAttributeArgumentMetadata
{
    private readonly TypedConstant _typeConstant;
    private readonly Settings _settings;

    internal CliAttributeArgumentMetadata(TypedConstant typeConstant, Settings settings)
    {
        _typeConstant = typeConstant;
        _settings = settings;
    }

    public ITypeMetadata Type => _typeConstant.Type != null
        ? CliTypeMetadata.FromTypeSymbol(_typeConstant.Type, _settings)
        : null!;

    public ITypeMetadata? TypeValue => _typeConstant.Kind == TypedConstantKind.Type && _typeConstant.Value is INamedTypeSymbol namedSymbol
        ? CliTypeMetadata.FromTypeSymbol(namedSymbol, _settings)
        : null;

    public object? GetValue() => _typeConstant.Kind == TypedConstantKind.Array
        ? _typeConstant.Values.Select(prop => prop.Value).ToArray()
        : _typeConstant.Value;
}
