# Research: CLI Template Engine Parity Gap Analysis

**Date**: 2026-01-26
**Branch**: `002-cli-template-parity`

## Executive Summary

This document captures the systematic comparison between the VS extension template engine and the CLI implementation, identifying gaps that could cause templates to behave differently.

## 1. Parser Method Resolution

### VS Extension Behavior (Parser.cs)

```csharp
// TryGetIdentifier - lines 176-212
var property = type.GetProperty(identifier);
if (property != null) { ... }

var extension = extensions.Select(e => e.GetMethod(identifier, new[] { type })).FirstOrDefault(m => m != null);
if (extension != null) { ... }
```

**Key observations:**
- Only looks for properties on context type
- Only looks for static extension methods
- Uses default GetMethod (public only)

### CLI Behavior (CliParser.cs) - AFTER FIXES

```csharp
// TryGetIdentifier - now includes:
var methods = templateType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    .Where(m => m.Name == identifier)
    .ToList();
```

**Status: FIXED** - CLI now finds instance methods with proper BindingFlags

### Remaining Gap: None Identified

The CLI implementation is now MORE capable than VS extension (finds instance methods that VS doesn't explicitly search for).

## 2. Settings Configuration

### Base Settings Class Properties

| Property | Type | VS Default | CLI Default | Status |
|----------|------|------------|-------------|--------|
| OutputExtension | string | ".ts" | ".ts" | MATCH |
| OutputFilenameFactory | Func<File, string> | null | null | MATCH |
| PartialRenderingMode | enum | Partial | Partial | MATCH |
| OutputDirectory | string | null | null | MATCH |
| SkipAddingGeneratedFilesToProject | bool | false | false | N/A (CLI always skips) |

### SettingsImpl Properties (VS-specific)

| Property | Type | VS Behavior | CLI Equivalent | Status |
|----------|------|-------------|----------------|--------|
| IsSingleFileMode | bool | Set by SingleFileMode() | Set by SingleFileMode() | MATCH |
| SingleFileName | string | Set by SingleFileMode() | Set by SingleFileMode() | MATCH |
| StringLiteralCharacter | char | Default '"' | Default '"' | NEEDS VERIFICATION |
| StrictNullGeneration | bool | Default true | Default true | FIXED |
| Utf8BomGeneration | bool | Default true | Default true | MATCH |
| TemplatePath | string | From constructor | From constructor | MATCH |
| IncludedProjects | ICollection | Via methods | Via methods | MATCH |

### Settings Methods

| Method | VS Behavior | CLI Behavior | Status |
|--------|-------------|--------------|--------|
| IncludeProject(name) | Adds to list | Adds to list | MATCH |
| SingleFileMode(filename) | Sets mode + name | Sets mode + name | MATCH |
| IncludeCurrentProject() | Adds current | No-op (includes all) | ACCEPTABLE |
| IncludeReferencedProjects() | Adds referenced | No-op (includes all) | ACCEPTABLE |
| IncludeAllProjects() | Adds all | No-op (includes all) | ACCEPTABLE |
| UseStringLiteralCharacter(ch) | Sets char | Sets char | NEEDS VERIFICATION |
| DisableStrictNullGeneration() | Sets false | Sets false | FIXED |
| DisableUtf8BomGeneration() | Sets false | Sets false | MATCH |

### Gap: StringLiteralCharacter Usage

**Investigation needed:** Where is `StringLiteralCharacter` actually used in template rendering?

Searching codebase... Used in `Helpers.cs`:
```csharp
public static string GetDefaultValue(..., Settings settings)
{
    // Uses settings.StringLiteralCharacter for string default values
}
```

**Status:** NEEDS VERIFICATION - Ensure CLI passes settings to Helpers when rendering default values.

## 3. Type Resolution

### CliTypeMetadata vs RoslynTypeMetadata

| Property | RoslynTypeMetadata | CliTypeMetadata | Status |
|----------|-------------------|-----------------|--------|
| Name | ✓ | ✓ | MATCH |
| FullName | ✓ | ✓ | MATCH |
| IsArray | ✓ | ✓ | MATCH |
| IsEnumerable | Complex check | Complex check | NEEDS VERIFICATION |
| IsDictionary | Checks original definition | Checks generic type | NEEDS VERIFICATION |
| IsDynamic | ✓ | ✓ | MATCH |
| IsEnum | ✓ | ✓ | MATCH |
| IsGeneric | ✓ | ✓ | MATCH |
| IsGuid | ✓ | ✓ | MATCH |
| IsNullable | Complex check | Complex check | MATCH |
| IsPrimitive | ✓ | ✓ | MATCH |
| IsTask | Name check | Name check | NEEDS VERIFICATION |
| IsTimeSpan | ✓ | ✓ | MATCH |
| IsString | ✓ | ✓ | MATCH |
| IsValueTuple | Complex check | Complex check | NEEDS VERIFICATION |
| TypeArguments | Handles arrays | Handles arrays | FIXED |
| ElementType | ✓ | ✓ | MATCH |
| TupleElements | Reflection-based | Direct property | NEEDS VERIFICATION |

### Gap: IsDictionary Implementation

**RoslynTypeMetadata:**
```csharp
public bool IsDictionary => _symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType
    && (namedTypeSymbol.OriginalDefinition.Name.Equals("Dictionary", StringComparison.OrdinalIgnoreCase) ||
        namedTypeSymbol.OriginalDefinition.Name.Equals("IDictionary", StringComparison.OrdinalIgnoreCase))
    && namedTypeSymbol.OriginalDefinition.ContainingNamespace.ToDisplayString().Equals(
        "System.Collections.Generic",
        StringComparison.OrdinalIgnoreCase);
```

**CliTypeMetadata:**
```csharp
public bool IsDictionary => IsGenericType("System.Collections.Generic.IDictionary",
    "System.Collections.Generic.Dictionary",
    "System.Collections.Generic.IReadOnlyDictionary");
```

**Status:** MATCH - Both check for Dictionary types correctly, CLI also includes IReadOnlyDictionary.

### Gap: IsValueTuple Implementation

**RoslynTypeMetadata:**
```csharp
public bool IsValueTuple => _symbol.Name.Equals(string.Empty, StringComparison.OrdinalIgnoreCase) &&
    string.Equals(_symbol.BaseType?.Name, "ValueType", StringComparison.OrdinalIgnoreCase) &&
    string.Equals(_symbol.BaseType.ContainingNamespace.Name, "System", StringComparison.OrdinalIgnoreCase);
```

**CliTypeMetadata:**
```csharp
public bool IsValueTuple => _symbol.IsTupleType;
```

**Status:** CLI uses Roslyn's built-in `IsTupleType` which is MORE accurate than the VS extension's heuristic.

### Gap: TupleElements Implementation

**RoslynTypeMetadata:** Uses reflection to access TupleElements property
**CliTypeMetadata:** Directly accesses `namedType.TupleElements`

**Status:** CLI implementation is BETTER (direct access vs reflection).

### Gap: Task<T> Handling

**RoslynTypeMetadata.FromTypeSymbol:**
```csharp
else if (symbol.Name.Equals("Task", StringComparison.OrdinalIgnoreCase) &&
         symbol.ContainingNamespace.GetFullName().Equals("System.Threading.Tasks", StringComparison.OrdinalIgnoreCase))
{
    var type = symbol as INamedTypeSymbol;
    var argument = type?.TypeArguments.FirstOrDefault();
    if (argument != null)
    {
        return new RoslynTypeMetadata(argument, false, true, settings);  // Returns INNER type
    }
    return new RoslynVoidTaskMetadata();
}
```

**CliTypeMetadata.FromTypeSymbol:**
```csharp
// Does NOT unwrap Task<T> - returns Task<T> as-is
```

**Status:** POTENTIAL GAP - CLI may not unwrap Task<T> types. Needs investigation.

## 4. Filter Syntax

### ItemFilter.cs (VS) vs CliItemFilter.cs (CLI)

Both implementations should be identical or very similar since filtering is core functionality.

**Comparison:**

| Feature | VS ItemFilter | CLI CliItemFilter | Status |
|---------|---------------|-------------------|--------|
| Name pattern (*) | ✓ | ✓ | NEEDS VERIFICATION |
| Attribute filter ([]) | ✓ | ✓ | NEEDS VERIFICATION |
| Inheritance filter (:) | ✓ | ✓ | NEEDS VERIFICATION |
| IFilterable interface | Uses it | Uses it | MATCH |

**Status:** Both use the same IFilterable interface from shared CodeModel. Implementation should be equivalent.

## 5. Code Model Properties

### Property Availability by Type

This requires detailed comparison of each *Impl class in VS with Cli*Metadata in CLI.

**High-risk areas:**
- Lazy-loaded properties that might not be initialized
- Parent references that might be null
- Settings propagation through property chains

**To verify:**
1. Class: All collection properties (Properties, Methods, Fields, etc.)
2. Property: Type, DefaultValue, HasGetter, HasSetter
3. Method: ReturnType, Parameters, TypeParameters
4. Attribute: Arguments (named and positional)

## 6. Template Syntax

### Boolean Conditionals

**VS Parser.ParseDollar:**
```csharp
else if (value is bool)
{
    var trueBlock = ParseBlock(stream, '[', ']');
    var falseBlock = ParseBlock(stream, '[', ']');
    output.Append(ParseTemplate(..., (bool)value ? trueBlock : falseBlock, context));
}
```

**CLI CliParser.ParseDollar:**
```csharp
else if (value is bool boolValue)
{
    var trueBlock = ParseBlock(stream, '[', ']');
    var falseBlock = ParseBlock(stream, '[', ']');
    output.Append(ParseTemplate(..., boolValue ? trueBlock : falseBlock, context));
}
```

**Status:** MATCH - Identical logic.

### Collection Iteration

Both parsers handle `$Collection[block][separator]` identically.

**Status:** MATCH

## Summary of Gaps

### Already Fixed (in previous session)
1. ✅ StrictNullGeneration not respected
2. ✅ Array TypeArguments not returned
3. ✅ Instance methods not found
4. ✅ Private constructor not invoked

### Needs Verification
1. ⚠️ StringLiteralCharacter propagation
2. ⚠️ Task<T> unwrapping behavior
3. ⚠️ Filter syntax edge cases
4. ⚠️ PartialRenderingMode effect

### Low Risk (likely fine)
1. Dictionary type resolution
2. Tuple type resolution
3. Boolean conditionals
4. Code model completeness

## Recommendations

1. **Priority 1:** Create test for Task<T> unwrapping - this is a likely gap
2. **Priority 2:** Verify StringLiteralCharacter is used when rendering defaults
3. **Priority 3:** Add filter syntax tests for edge cases
4. **Priority 4:** Add code model property completeness tests

## Decision Log

| Decision | Rationale | Alternatives Considered |
|----------|-----------|------------------------|
| CLI includes IReadOnlyDictionary in IsDictionary | More complete than VS | Match VS exactly (rejected - less useful) |
| CLI uses IsTupleType instead of heuristic | More accurate | Match VS heuristic (rejected - less accurate) |
| CLI methods search includes NonPublic | Matches expected behavior | Public only (rejected - breaks templates) |
