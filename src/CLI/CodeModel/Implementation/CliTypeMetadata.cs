using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// CLI implementation of <see cref="ITypeMetadata"/>.
/// </summary>
public class CliTypeMetadata : ITypeMetadata
{
    private readonly ITypeSymbol _symbol;
    private readonly Settings _settings;
    private readonly bool _isNullable;

    private CliTypeMetadata(ITypeSymbol symbol, bool isNullable, Settings settings)
    {
        _symbol = symbol;
        _isNullable = isNullable;
        _settings = settings;
    }

    public string DocComment => (_symbol as INamedTypeSymbol)?.GetDocumentationCommentXml() ?? string.Empty;
    public string Name => _symbol.GetName() + (_isNullable ? "?" : string.Empty);
    public string FullName => _symbol.GetFullName() + (_isNullable ? "?" : string.Empty);
    public string AssemblyName => _symbol.ContainingAssembly?.Name ?? string.Empty;
    public bool IsAbstract => (_symbol as INamedTypeSymbol)?.IsAbstract ?? false;
    public bool IsStatic => (_symbol as INamedTypeSymbol)?.IsStatic ?? false;
    public bool IsArray => _symbol.TypeKind == TypeKind.Array;
    public bool IsDate => IsType("System.DateTime", "System.DateTimeOffset", "System.DateOnly");
    public bool IsDefined => _symbol.Locations.Any(l => l.IsInSource);
    public bool IsDictionary => IsGenericType("System.Collections.Generic.IDictionary", "System.Collections.Generic.Dictionary", "System.Collections.Generic.IReadOnlyDictionary");
    public bool IsDynamic => _symbol is IDynamicTypeSymbol;
    public bool IsEnum => _symbol.TypeKind == TypeKind.Enum;
    public bool IsEnumerable => !IsPrimitive && !IsString && (IsArray || IsGenericType("System.Collections.Generic.IEnumerable", "System.Collections.Generic.ICollection", "System.Collections.Generic.IList", "System.Collections.Generic.List"));
    public bool IsGeneric => (_symbol as INamedTypeSymbol)?.TypeParameters.Any() ?? false;
    public bool IsGuid => IsType("System.Guid");
    public bool IsNullable => _isNullable;
    public bool IsPrimitive => _symbol.IsValueType && IsType("System.Boolean", "System.Byte", "System.SByte", "System.Int16", "System.Int32", "System.Int64", "System.UInt16", "System.UInt32", "System.UInt64", "System.Single", "System.Double", "System.Decimal", "System.Char");
    public bool IsTask => IsType("System.Threading.Tasks.Task") || IsGenericType("System.Threading.Tasks.Task");
    public bool IsTimeSpan => IsType("System.TimeSpan");
    public bool IsString => IsType("System.String");
    public bool IsValueTuple => _symbol.IsTupleType;
    public string Namespace => _symbol.GetNamespace() ?? string.Empty;
    public string DefaultValue { get; set; } = string.Empty;

    public ITypeMetadata Type => this;
    public IEnumerable<IAttributeMetadata> Attributes => CliAttributeMetadata.FromAttributeData((_symbol as INamedTypeSymbol)?.GetAttributes() ?? Enumerable.Empty<AttributeData>(), _settings);
    public IClassMetadata? BaseClass => CliClassMetadata.FromNamedTypeSymbol((_symbol as INamedTypeSymbol)?.BaseType, _settings);
    IClassMetadata? IClassMetadata.ContainingClass => CliClassMetadata.FromNamedTypeSymbol(_symbol.ContainingType, _settings);

    public IEnumerable<IConstantMetadata> Constants => CliConstantMetadata.FromFieldSymbols((_symbol as INamedTypeSymbol)?.GetMembers().OfType<IFieldSymbol>() ?? Enumerable.Empty<IFieldSymbol>(), _settings);
    public IEnumerable<IDelegateMetadata> Delegates => CliDelegateMetadata.FromNamedTypeSymbols((_symbol as INamedTypeSymbol)?.GetTypeMembers().Where(s => s.TypeKind == TypeKind.Delegate) ?? Enumerable.Empty<INamedTypeSymbol>(), _settings);
    public IEnumerable<IEventMetadata> Events => CliEventMetadata.FromEventSymbols((_symbol as INamedTypeSymbol)?.GetMembers().OfType<IEventSymbol>() ?? Enumerable.Empty<IEventSymbol>(), _settings);
    public IEnumerable<IFieldMetadata> Fields => CliFieldMetadata.FromFieldSymbols((_symbol as INamedTypeSymbol)?.GetMembers().OfType<IFieldSymbol>() ?? Enumerable.Empty<IFieldSymbol>(), _settings);
    public IEnumerable<IInterfaceMetadata> Interfaces => CliInterfaceMetadata.FromNamedTypeSymbols((_symbol as INamedTypeSymbol)?.Interfaces ?? Enumerable.Empty<INamedTypeSymbol>(), null, _settings);
    public IEnumerable<IMethodMetadata> Methods => CliMethodMetadata.FromMethodSymbols((_symbol as INamedTypeSymbol)?.GetMembers().OfType<IMethodSymbol>() ?? Enumerable.Empty<IMethodSymbol>(), _settings);
    public IEnumerable<IPropertyMetadata> Properties => CliPropertyMetadata.FromPropertySymbols((_symbol as INamedTypeSymbol)?.GetMembers().OfType<IPropertySymbol>() ?? Enumerable.Empty<IPropertySymbol>(), _settings);
    public IEnumerable<IStaticReadOnlyFieldMetadata> StaticReadOnlyFields => CliStaticReadOnlyFieldMetadata.FromFieldSymbols((_symbol as INamedTypeSymbol)?.GetMembers().OfType<IFieldSymbol>() ?? Enumerable.Empty<IFieldSymbol>(), _settings);
    public IEnumerable<ITypeParameterMetadata> TypeParameters => CliTypeParameterMetadata.FromTypeParameterSymbols((_symbol as INamedTypeSymbol)?.TypeParameters ?? Enumerable.Empty<ITypeParameterSymbol>());
    public IEnumerable<ITypeMetadata> TypeArguments => FromTypeSymbols((_symbol as INamedTypeSymbol)?.TypeArguments ?? Enumerable.Empty<ITypeSymbol>(), _settings);
    public IEnumerable<IFieldMetadata> TupleElements => GetTupleElements();
    public ITypeMetadata? ElementType => _symbol is IArrayTypeSymbol arrayTypeSymbol ? FromTypeSymbol(arrayTypeSymbol.ElementType, _settings) : null;
    public IEnumerable<string> FileLocations => _symbol.Locations.Where(l => l.SourceTree != null).Select(l => l.SourceTree!.FilePath);
    public IEnumerable<IClassMetadata> NestedClasses => CliClassMetadata.FromNamedTypeSymbols((_symbol as INamedTypeSymbol)?.GetTypeMembers().Where(s => s.TypeKind == TypeKind.Class) ?? Enumerable.Empty<INamedTypeSymbol>(), null, _settings);
    public IEnumerable<IEnumMetadata> NestedEnums => CliEnumMetadata.FromNamedTypeSymbols((_symbol as INamedTypeSymbol)?.GetTypeMembers().Where(s => s.TypeKind == TypeKind.Enum) ?? Enumerable.Empty<INamedTypeSymbol>(), _settings);
    public IEnumerable<IInterfaceMetadata> NestedInterfaces => CliInterfaceMetadata.FromNamedTypeSymbols((_symbol as INamedTypeSymbol)?.GetTypeMembers().Where(s => s.TypeKind == TypeKind.Interface) ?? Enumerable.Empty<INamedTypeSymbol>(), null, _settings);

    private bool IsType(params string[] typeNames) => typeNames.Any(n => FullName.Equals(n, StringComparison.OrdinalIgnoreCase) || FullName.Equals(n + "?", StringComparison.OrdinalIgnoreCase));

    private bool IsGenericType(params string[] typeNames)
    {
        var fullName = FullName;
        return typeNames.Any(n => fullName.StartsWith(n + "<", StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<IFieldMetadata> GetTupleElements()
    {
        if (_symbol is INamedTypeSymbol namedType && namedType.IsTupleType)
        {
            return CliFieldMetadata.FromFieldSymbols(namedType.TupleElements, _settings);
        }
        return Enumerable.Empty<IFieldMetadata>();
    }

    internal static ITypeMetadata FromTypeSymbol(ITypeSymbol symbol, Settings settings)
    {
        var isNullable = symbol.NullableAnnotation == NullableAnnotation.Annotated;

        if (symbol is INamedTypeSymbol namedType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            isNullable = true;
            symbol = namedType.TypeArguments.First();
        }

        return new CliTypeMetadata(symbol, isNullable, settings);
    }

    internal static IEnumerable<ITypeMetadata> FromTypeSymbols(IEnumerable<ITypeSymbol> symbols, Settings settings)
    {
        return symbols.Select(s => FromTypeSymbol(s, settings));
    }
}
