# Implementation Plan: CLI Template Engine Parity

**Branch**: `002-cli-template-parity` | **Date**: 2026-01-26 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-cli-template-parity/spec.md`

## Summary

Systematically audit and remediate parity gaps between the CLI template engine and the VS extension to ensure any valid .tst template that works with the VS extension also works identically with the CLI. This involves comparing parser implementations, settings handling, type resolution, filter syntax, and code model properties.

## Technical Context

**Language/Version**: C# / .NET 8.0 (CLI), .NET Framework 4.7.2 (shared CodeModel)
**Primary Dependencies**: Microsoft.CodeAnalysis (Roslyn) 4.14.0, System.CommandLine
**Storage**: N/A (file-based code generation)
**Testing**: xUnit with Should fluent assertions, NSubstitute for mocking
**Target Platform**: Cross-platform CLI (.NET 8.0)
**Project Type**: CLI tool extending existing VS extension codebase
**Performance Goals**: Not primary concern; correctness over speed
**Constraints**: Must maintain backward compatibility with VS extension behavior
**Scale/Scope**: 32 functional requirements across 7 user stories

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Upstream Style Conformance | PASS | CLI code follows existing patterns (PascalCase, `Cli*` prefix for CLI-specific classes) |
| II. Test Coverage Requirements | PASS | Will add comprehensive parity tests using xUnit + Should assertions |
| III. Architectural Consistency | PASS | CLI reuses CodeModel layer, adds parallel metadata implementations |
| IV. Visual Studio Extension Compatibility | N/A | CLI is standalone; VS extension unchanged |
| V. Performance and Resource Efficiency | PASS | Lazy loading maintained; no blocking operations |

**Upstream Compatibility**: PASS - No changes to public API; template syntax unchanged

## Project Structure

### Documentation (this feature)

```text
specs/002-cli-template-parity/
├── plan.md              # This file
├── research.md          # Phase 0 output - parity gap analysis
├── quickstart.md        # Phase 1 output - testing guide
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── CLI/
│   ├── CodeModel/Implementation/     # CLI metadata implementations
│   │   ├── CliTypeMetadata.cs       # MODIFY - type resolution fixes
│   │   ├── CliClassMetadata.cs      # AUDIT - code model completeness
│   │   ├── CliPropertyMetadata.cs   # AUDIT - code model completeness
│   │   └── ...
│   ├── Configuration/
│   │   └── CliSettings.cs           # AUDIT - settings support completeness
│   ├── Generation/
│   │   ├── CliParser.cs             # ALREADY FIXED - method invocation
│   │   ├── CliSingleFileParser.cs   # ALREADY FIXED - method invocation
│   │   ├── CliTemplate.cs           # ALREADY FIXED - template instance
│   │   ├── CliItemFilter.cs         # AUDIT - filter syntax support
│   │   └── TemplateProcessor.cs     # ALREADY FIXED - settings propagation
│   └── Infrastructure/
│       └── CliMetadataProvider.cs   # AUDIT - metadata completeness
├── CodeModel/                        # Shared abstract types (no changes)
├── Metadata/                         # Shared interfaces (no changes)
├── Roslyn/                           # VS extension Roslyn impl (reference only)
└── Typewriter/
    ├── CodeModel/
    │   ├── Helpers.cs               # REFERENCE - type conversion logic
    │   └── Implementation/          # REFERENCE - VS implementation patterns
    └── Generation/
        ├── Parser.cs                # REFERENCE - VS parser behavior
        ├── SingleFileParser.cs      # REFERENCE - VS single-file behavior
        ├── Template.cs              # REFERENCE - VS template behavior
        └── ItemFilter.cs            # REFERENCE - filter syntax (shared)

src/CLI.Tests/
├── TemplateParityTests.cs           # EXPAND - comprehensive parity tests
├── CliSettingsTests.cs              # EXPAND - settings coverage
├── TypeResolutionParityTests.cs     # ADD - type resolution tests
├── FilterSyntaxTests.cs             # ADD - filter syntax tests
└── CodeModelCompletenessTests.cs    # ADD - code model property tests
```

**Structure Decision**: Existing CLI structure is maintained. Changes are primarily audits and targeted fixes to existing files, with new test files for comprehensive coverage.

## Phase 0: Research & Gap Analysis

### Research Tasks

1. **Parser Method Resolution Audit**
   - Compare `Parser.cs` (VS) vs `CliParser.cs` (CLI) line by line
   - Document differences in `TryGetIdentifier` method lookup
   - Verify BindingFlags usage matches VS behavior
   - Status: PARTIALLY COMPLETE (fixes applied for instance methods)

2. **Settings Property Audit**
   - Enumerate all Settings properties and methods
   - Verify each is implemented in CliSettings
   - Trace settings propagation through TemplateProcessor
   - Status: PARTIALLY COMPLETE (StrictNullGeneration fixed)

3. **Type Resolution Audit**
   - Compare `RoslynTypeMetadata` vs `CliTypeMetadata` property by property
   - Test each `Is*` boolean flag
   - Test TypeArguments for arrays, generics, dictionaries, tuples
   - Status: PARTIALLY COMPLETE (array TypeArguments fixed)

4. **Filter Syntax Audit**
   - Compare `ItemFilter.cs` (VS) vs `CliItemFilter.cs` (CLI)
   - Test name patterns, attribute filters, inheritance filters
   - Test predicate filters with custom methods
   - Status: NOT STARTED

5. **Code Model Completeness Audit**
   - List all properties on each code model type (Class, Property, Method, etc.)
   - Compare VS Implementation classes with CLI Metadata classes
   - Identify missing properties or different behavior
   - Status: NOT STARTED

### Known Issues (Already Fixed)

| Issue | Root Cause | Fix Applied |
|-------|------------|-------------|
| StrictNullGeneration not respected | TemplateProcessor created new settings | Use template.Settings |
| Array types resolved as `any[]` | CliTypeMetadata.TypeArguments didn't handle arrays | Added IArrayTypeSymbol check |
| Filter methods not found | Methods looked up without BindingFlags | Added instance method search |
| OutputFilenameFactory not called | Template constructor not invoked | Added private constructor invocation |

### Potential Issues (To Investigate)

| Area | Risk | Investigation |
|------|------|---------------|
| Dictionary type resolution | Medium | Verify key/value type arguments |
| Tuple type resolution | Medium | Verify TupleElements property |
| Task<T> unwrapping | Medium | Verify IsTask and inner type |
| DocComment access | Low | Verify XML documentation retrieval |
| Attribute Arguments | Low | Verify named/positional arguments |
| PartialRenderingMode | Low | Verify partial class handling |

## Phase 1: Implementation Plan

### Task Group 1: Type Resolution Completeness (P1)

**Files to Modify:**
- `src/CLI/CodeModel/Implementation/CliTypeMetadata.cs`

**Specific Changes:**

1. **Verify Dictionary Type Resolution (FR-019)**
   - Check `IsDictionary` property implementation
   - Verify `TypeArguments` returns both key and value types
   - Compare with `RoslynTypeMetadata.IsDictionary`

2. **Verify Tuple Type Resolution (FR-020)**
   - Check `IsValueTuple` property implementation
   - Verify `TupleElements` property returns field metadata
   - Compare with `RoslynTypeMetadata.TupleElements`

3. **Verify Task<T> Unwrapping (FR-021)**
   - Check `IsTask` property implementation
   - Verify inner type is returned (not Task itself)
   - Compare with `RoslynTypeMetadata.FromTypeSymbol` Task handling

**Tests to Add:**
- `TypeResolutionParityTests.Dictionary_TypeArguments_ReturnsKeyAndValue`
- `TypeResolutionParityTests.ValueTuple_TupleElements_ReturnsNamedFields`
- `TypeResolutionParityTests.Task_InnerType_IsUnwrapped`

### Task Group 2: Settings Completeness Audit (P1)

**Files to Audit:**
- `src/CLI/Configuration/CliSettings.cs`
- `src/CLI/Generation/TemplateProcessor.cs`

**Specific Checks:**

1. **Verify All Settings Properties**
   | Property | CliSettings | Used In TemplateProcessor |
   |----------|-------------|---------------------------|
   | OutputExtension | Yes | Yes |
   | OutputFilenameFactory | Yes | Yes |
   | OutputDirectory | Yes | Yes |
   | StringLiteralCharacter | Yes | ? (check Helpers.cs usage) |
   | StrictNullGeneration | Yes | Yes (fixed) |
   | Utf8BomGeneration | Yes | Yes |
   | SingleFileMode/SingleFileName | Yes | Yes |
   | PartialRenderingMode | Yes | ? (check usage) |
   | SkipAddingGeneratedFilesToProject | Yes | N/A (CLI always skips) |

2. **Verify StringLiteralCharacter Usage (FR-010)**
   - Trace where this setting is consumed
   - Ensure CLI passes it to Helpers or equivalent

3. **Verify PartialRenderingMode Usage (FR-014)**
   - Trace where this setting affects output
   - Ensure CLI respects partial class handling

**Tests to Add:**
- `CliSettingsTests.StringLiteralCharacter_AffectsOutput`
- `CliSettingsTests.PartialRenderingMode_AffectsPartialClasses`

### Task Group 3: Filter Syntax Verification (P2)

**Files to Audit:**
- `src/CLI/Generation/CliItemFilter.cs`
- `src/Typewriter/Generation/ItemFilter.cs`

**Comparison Points:**

1. **Name Pattern Filters (FR-022)**
   - Wildcard at start: `*Model`
   - Wildcard at end: `Base*`
   - Wildcard in middle: `I*Service`
   - Exact match: `MyClass`

2. **Attribute Filters (FR-023)**
   - Bracket syntax: `[Serializable]`
   - Attribute name matching (with/without "Attribute" suffix)

3. **Inheritance Filters (FR-024)**
   - Colon syntax: `:BaseClass`
   - Interface inheritance: `:IInterface`

**Tests to Add:**
- `FilterSyntaxTests.NamePattern_WildcardStart_MatchesCorrectly`
- `FilterSyntaxTests.NamePattern_WildcardEnd_MatchesCorrectly`
- `FilterSyntaxTests.AttributeFilter_BracketSyntax_MatchesCorrectly`
- `FilterSyntaxTests.InheritanceFilter_ColonSyntax_MatchesCorrectly`

### Task Group 4: Code Model Completeness (P2)

**Files to Audit:**
- All files in `src/CLI/CodeModel/Implementation/`
- Compare with `src/Typewriter/CodeModel/Implementation/`

**Property Checklist by Type:**

| Type | Properties to Verify |
|------|---------------------|
| Class | Name, FullName, Namespace, Attributes, Properties, Methods, Fields, Interfaces, BaseClass, TypeParameters, IsAbstract, IsStatic, etc. |
| Property | Name, Type, HasGetter, HasSetter, Attributes, DefaultValue, etc. |
| Method | Name, Type, Parameters, Attributes, IsAbstract, IsVirtual, etc. |
| Attribute | Name, FullName, Value, Arguments, Type |
| Type | All Is* flags, TypeArguments, ElementType, etc. |

**Tests to Add:**
- `CodeModelCompletenessTests.Class_AllProperties_MatchVsExtension`
- `CodeModelCompletenessTests.Property_AllProperties_MatchVsExtension`
- `CodeModelCompletenessTests.Attribute_Arguments_AvailableCorrectly`

### Task Group 5: Boolean Conditional Verification (P2)

**Files to Verify:**
- `src/CLI/Generation/CliParser.cs` - boolean handling in ParseDollar
- `src/CLI/Generation/CliSingleFileParser.cs` - boolean handling

**Verification Points:**
- `$BoolProp[true][false]` syntax works
- Nested conditionals work
- Empty blocks work (`$BoolProp[content][]`)

**Tests to Add:**
- `TemplateParityTests.BooleanConditional_TrueBlock_Rendered`
- `TemplateParityTests.BooleanConditional_FalseBlock_Rendered`
- `TemplateParityTests.BooleanConditional_Nested_WorksCorrectly`

## Phase 2: Testing Strategy

### Test Categories

1. **Unit Tests** (per-component)
   - CliTypeMetadata property tests
   - CliSettings method tests
   - CliItemFilter behavior tests

2. **Integration Tests** (end-to-end)
   - Template rendering with various features
   - Full generation pipeline tests

3. **Parity Tests** (comparison with VS extension)
   - Same input, same output verification
   - AcciClaim template verification (already done manually)

### Test Fixtures Needed

```csharp
// Test C# source files for type resolution
public class TypeResolutionTestSource
{
    public int[] ArrayProperty { get; set; }
    public List<string> ListProperty { get; set; }
    public Dictionary<string, int> DictionaryProperty { get; set; }
    public (string Name, int Age) TupleProperty { get; set; }
    public Task<string> TaskProperty { get; set; }
    public int? NullableProperty { get; set; }
}

// Test C# source files for filter testing
[Serializable]
public class SerializableModel { }

public class BaseModel { }
public class DerivedModel : BaseModel { }

[CustomAttribute("value")]
public class AttributedClass { }
```

### Success Metrics

- [ ] All 32 functional requirements have corresponding tests
- [ ] All 357+ existing tests continue to pass
- [ ] AcciClaim templates generate identical output
- [ ] Type resolution tests cover all 6 type categories
- [ ] Filter tests cover all 4 filter types
- [ ] Settings tests cover all 10 settings properties

## Complexity Tracking

> No constitution violations requiring justification.

| Area | Complexity | Justification |
|------|------------|---------------|
| Multiple metadata classes | Necessary | CLI needs Roslyn-based metadata without VS dependencies |
| Reflection-based method invocation | Necessary | Template methods are compiled at runtime |
| Dual parser implementations | Necessary | Single-file and multi-file modes have different semantics |

## Next Steps

1. Run `/speckit.tasks` to generate detailed task breakdown
2. Execute Task Group 1 (Type Resolution) first as highest risk
3. Execute Task Group 2 (Settings) to verify configuration
4. Execute remaining groups in priority order
5. Run full test suite after each group
6. Verify against AcciClaim templates after all changes
