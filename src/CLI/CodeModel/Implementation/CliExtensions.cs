using Microsoft.CodeAnalysis;

namespace Typewriter.CLI.CodeModel.Implementation;

/// <summary>
/// Extension methods for Roslyn symbols used by CLI metadata implementations.
/// </summary>
public static class CliExtensions
{
    /// <summary>
    /// Gets the name of a symbol, handling array types.
    /// </summary>
    public static string GetName(this ISymbol symbol)
    {
        return symbol is IArrayTypeSymbol array ? $"{array.ElementType}[]" : symbol.Name;
    }

    /// <summary>
    /// Gets the full name of a symbol including namespace.
    /// </summary>
    public static string GetFullName(this ISymbol symbol)
    {
        if (symbol is ITypeParameterSymbol)
        {
            return symbol.Name;
        }

        if (symbol is IDynamicTypeSymbol)
        {
            return symbol.Name;
        }

        var name = (symbol is INamedTypeSymbol type) ? GetFullTypeName(type) : symbol.Name;

        var namespaceSymbol = symbol.ContainingSymbol as INamespaceSymbol;
        if (namespaceSymbol?.IsGlobalNamespace == true)
        {
            return name;
        }

        if (symbol is IArrayTypeSymbol array)
        {
            return "System.Collections.Generic.ICollection<" + GetFullName(array.ElementType) + ">";
        }

        return GetFullName(symbol.ContainingSymbol) + "." + name;
    }

    /// <summary>
    /// Gets the namespace of a symbol.
    /// </summary>
    public static string? GetNamespace(this ISymbol symbol)
    {
        if (string.IsNullOrEmpty(symbol.ContainingNamespace?.Name))
        {
            return null;
        }

        var restOfResult = GetNamespace(symbol.ContainingNamespace);
        var result = symbol.ContainingNamespace.Name;

        if (restOfResult != null)
        {
            result = restOfResult + '.' + result;
        }

        return result;
    }

    /// <summary>
    /// Gets the full type name including generic arguments.
    /// </summary>
    public static string GetFullTypeName(this INamedTypeSymbol type)
    {
        var sb = new System.Text.StringBuilder(type.Name);

        if (type.Name.Equals("Nullable", StringComparison.OrdinalIgnoreCase) &&
            type.ContainingNamespace.Name.Equals("System", StringComparison.OrdinalIgnoreCase) &&
            type.TypeArguments.FirstOrDefault() is INamedTypeSymbol typeSymbol)
        {
            return GetFullTypeName(typeSymbol) + "?";
        }

        if (type.TypeArguments.Any())
        {
            sb.Append('<');
            sb.Append(string.Join(
                ", ",
                type.TypeArguments.Select(
                    t => t is not INamedTypeSymbol typeSymbol2 ? t.Name : GetFullName(typeSymbol2))));
            sb.Append('>');
        }
        else if (type.TypeParameters.Any())
        {
            sb.Append('<');
            sb.Append(string.Join(", ", type.TypeParameters.Select(t => t.Name)));
            sb.Append('>');
        }

        return sb.ToString();
    }
}
