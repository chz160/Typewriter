# Feature Specification: CLI Template Engine Parity

**Feature Branch**: `002-cli-template-parity`
**Created**: 2026-01-26
**Status**: Draft
**Input**: User description: "Investigate and remediate CLI template engine parity gaps with the VS extension"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Template Author Uses Custom Methods (Priority: P1)

A template author writes a .tst file with custom C# methods (defined in `${ }` code blocks) that filter, transform, or generate content. These methods may be instance methods on the template class or static extension methods. The CLI must invoke these methods correctly during template rendering, matching VS extension behavior.

**Why this priority**: Custom methods are fundamental to template functionality. Without them, users cannot define filters like `$Classes($IncludeClass)` or transformation functions. This was one of the bugs discovered during testing.

**Independent Test**: Can be fully tested by creating a template with custom instance and static methods, then verifying the CLI invokes them correctly and produces expected output.

**Acceptance Scenarios**:

1. **Given** a template with a private instance method `bool IncludeClass(Class c)`, **When** used as `$Classes($IncludeClass)[...]`, **Then** the method is found via reflection and invoked to filter classes
2. **Given** a template with a public static extension method, **When** referenced as `$MethodName` in the template, **Then** the method is found and invoked correctly
3. **Given** a template with parameterless methods, **When** referenced as `$MethodName`, **Then** the method is invoked without arguments
4. **Given** a method that throws an exception, **When** invoked during rendering, **Then** the error is logged and rendering continues gracefully

---

### User Story 2 - Template Author Uses Settings Configuration (Priority: P1)

A template author configures generation settings in the template constructor (e.g., `settings.DisableStrictNullGeneration()`, `settings.OutputFilenameFactory = ...`). The CLI must respect all settings modifications made during template initialization.

**Why this priority**: Settings control critical output behavior including file naming, null handling, and output encoding. Without proper settings support, generated files will have wrong names or content.

**Independent Test**: Create templates that modify each available setting and verify the CLI respects each configuration.

**Acceptance Scenarios**:

1. **Given** a template that calls `settings.DisableStrictNullGeneration()`, **When** rendering nullable types, **Then** output shows `type` instead of `type | null`
2. **Given** a template that sets `settings.OutputFilenameFactory`, **When** generating files, **Then** output filenames match the factory's return value
3. **Given** a template that sets `settings.OutputExtension`, **When** generating files, **Then** files have the configured extension
4. **Given** a template that calls `settings.UseStringLiteralCharacter('\'')`, **When** rendering string literals, **Then** single quotes are used instead of double quotes
5. **Given** a template that calls `settings.SingleFileMode("output.ts")`, **When** generating, **Then** all output goes to a single file
6. **Given** a template that calls `settings.DisableUtf8BomGeneration()`, **When** writing files, **Then** no UTF-8 BOM is included

---

### User Story 3 - Template Author Uses Type Resolution Features (Priority: P1)

A template author accesses type information (arrays, generics, dictionaries, nullable types, tuples) and expects correct TypeScript type mappings. The CLI must resolve all type metadata correctly.

**Why this priority**: Type resolution is core to code generation. Incorrect type handling (like `any[]` instead of `number[]`) produces broken TypeScript output.

**Independent Test**: Create C# files with various type combinations and verify the CLI generates correct TypeScript types.

**Acceptance Scenarios**:

1. **Given** a C# property of type `int[]`, **When** rendered, **Then** TypeScript type is `number[]` (not `any[]`)
2. **Given** a C# property of type `List<string>`, **When** rendered, **Then** TypeScript type is `string[]`
3. **Given** a C# property of type `Dictionary<string, int>`, **When** rendered, **Then** TypeScript type is `{ [key: string]: number }`
4. **Given** a C# property of type `int?`, **When** StrictNullGeneration is enabled, **Then** TypeScript type is `number | null`
5. **Given** a C# property of type `(string Name, int Age)` (ValueTuple), **When** rendered, **Then** TypeScript type is `{ Name: string; Age: number }` (named tuple becomes interface-like object)
6. **Given** a C# property of type `Task<string>`, **When** rendered, **Then** TypeScript type unwraps to `string`

---

### User Story 4 - Template Author Uses Filter Syntax (Priority: P2)

A template author uses various filter syntaxes to select specific code elements (name patterns, attribute filters, inheritance filters). The CLI must support all VS extension filter syntaxes.

**Why this priority**: Filters are commonly used but well-defined. The ItemFilter class is shared code, reducing risk.

**Independent Test**: Create templates using each filter syntax variation and verify correct filtering behavior.

**Acceptance Scenarios**:

1. **Given** a filter `$Classes(*Model)`, **When** evaluated, **Then** only classes ending in "Model" are included
2. **Given** a filter `$Classes([Serializable])`, **When** evaluated, **Then** only classes with `[Serializable]` attribute are included
3. **Given** a filter `$Classes(:BaseClass)`, **When** evaluated, **Then** only classes inheriting from `BaseClass` are included
4. **Given** a predicate filter `$Classes($CustomFilter)`, **When** evaluated, **Then** the custom method is called to filter classes

---

### User Story 5 - Template Author Uses Code Model Properties (Priority: P2)

A template author accesses code model properties (Class, Property, Method, Enum, Interface, Attribute, etc.) and expects all properties to be available and return correct values.

**Why this priority**: Code model completeness ensures templates can access any C# metadata they need.

**Independent Test**: Create a comprehensive test that accesses every code model property and verifies correct values.

**Acceptance Scenarios**:

1. **Given** a class with attributes, **When** accessing `$Attributes`, **Then** all attributes are available with Name, Value, and Arguments
2. **Given** a class with generic type parameters, **When** accessing `$TypeParameters`, **Then** all type parameters are available
3. **Given** a method with parameters, **When** accessing `$Parameters`, **Then** all parameters with types and default values are available
4. **Given** a property with documentation comments, **When** accessing `$DocComment`, **Then** the XML documentation is available
5. **Given** an interface implementation, **When** accessing `$Interfaces`, **Then** all implemented interfaces are listed

---

### User Story 6 - Template Author Uses Boolean Conditionals (Priority: P2)

A template author uses boolean properties with conditional blocks like `$IsEnum[true block][false block]`. The CLI must handle boolean conditionals correctly.

**Why this priority**: Boolean conditionals are a core template syntax feature.

**Independent Test**: Create templates with boolean conditionals for various boolean properties.

**Acceptance Scenarios**:

1. **Given** a template with `$IsEnum[enum code][class code]`, **When** context is an enum, **Then** "enum code" is output
2. **Given** a template with `$HasGetter[has getter][]`, **When** property has a getter, **Then** "has getter" is output
3. **Given** a template with nested boolean conditionals, **When** evaluated, **Then** correct nesting behavior is maintained

---

### User Story 7 - Template Author Uses PartialRenderingMode (Priority: P3)

A template author sets `PartialRenderingMode` to control how partial classes are rendered. The CLI must support this setting.

**Why this priority**: Partial class handling is important but affects fewer users.

**Independent Test**: Create partial classes and verify rendering with different modes.

**Acceptance Scenarios**:

1. **Given** `PartialRenderingMode.Partial` (default), **When** rendering partial classes, **Then** only members from the current file are included
2. **Given** `PartialRenderingMode.Combined`, **When** rendering partial classes, **Then** members from all parts are combined

---

### Edge Cases

- What happens when a custom method has multiple overloads? The CLI should match on parameter type compatibility.
- How does the CLI handle circular type references? It should not infinite loop.
- What happens when OutputFilenameFactory returns invalid characters? They should be sanitized (replaced with `-`).
- How are generic types with multiple type arguments handled (e.g., `Func<T1, T2, TResult>`)? All arguments should be available.
- What happens when a template references a method that doesn't exist? It should fail gracefully and log an error.

## Requirements *(mandatory)*

### Functional Requirements

**Parser & Method Resolution**

- **FR-001**: CLI MUST find and invoke private instance methods on compiled template classes using reflection with `BindingFlags.Instance | BindingFlags.NonPublic`
- **FR-002**: CLI MUST find and invoke public static extension methods from template extension types
- **FR-003**: CLI MUST support parameterless methods invoked as `$MethodName`
- **FR-004**: CLI MUST support methods with single parameter matching context type
- **FR-005**: CLI MUST handle method invocation exceptions gracefully, logging errors without crashing

**Settings Configuration**

- **FR-006**: CLI MUST use `template.Settings` (modified by constructor) instead of creating new settings instances
- **FR-007**: CLI MUST respect `OutputFilenameFactory` for determining output file names
- **FR-008**: CLI MUST respect `OutputExtension` setting
- **FR-009**: CLI MUST respect `OutputDirectory` setting
- **FR-010**: CLI MUST respect `StringLiteralCharacter` setting
- **FR-011**: CLI MUST respect `StrictNullGeneration` setting (already fixed)
- **FR-012**: CLI MUST respect `Utf8BomGeneration` setting
- **FR-013**: CLI MUST respect `SingleFileMode` setting
- **FR-014**: CLI MUST respect `PartialRenderingMode` setting
- **FR-015**: CLI MUST respect `SkipAddingGeneratedFilesToProject` setting (CLI always skips, verify no conflict)

**Type Resolution**

- **FR-016**: CLI MUST return `ElementType` as `TypeArguments` for array types (already fixed)
- **FR-017**: CLI MUST correctly resolve generic type arguments for all collection types
- **FR-018**: CLI MUST correctly resolve nullable value types
- **FR-019**: CLI MUST correctly resolve dictionary types with key and value type arguments
- **FR-020**: CLI MUST correctly resolve tuple types with element types and names
- **FR-021**: CLI MUST unwrap `Task<T>` to return inner type

**Filter Syntax**

- **FR-022**: CLI MUST support name pattern filters with wildcards (`*Model`, `Base*`, `*`)
- **FR-023**: CLI MUST support attribute filters with bracket syntax (`[AttributeName]`)
- **FR-024**: CLI MUST support inheritance filters with colon syntax (`:BaseClass`)
- **FR-025**: CLI MUST support predicate filters with dollar syntax (`$CustomFilter`)

**Code Model Completeness**

- **FR-026**: CLI MUST provide all code model properties matching VS extension (Classes, Interfaces, Enums, Records, Properties, Methods, Fields, etc.)
- **FR-027**: CLI MUST provide attribute metadata including Name, FullName, Value, Arguments
- **FR-028**: CLI MUST provide type metadata including all boolean flags (IsArray, IsEnum, IsGeneric, IsNullable, etc.)
- **FR-029**: CLI MUST provide DocComment for documented code elements

**Template Syntax**

- **FR-030**: CLI MUST support boolean conditionals `$BoolProp[true][false]`
- **FR-031**: CLI MUST support collection iteration `$Items[block][separator]`
- **FR-032**: CLI MUST support nested template expressions

### Key Entities

- **Template**: Compiled .tst file with custom code blocks and template text
- **Settings**: Configuration object modified in template constructor
- **CodeModel**: Metadata representation of C# code (File, Class, Property, Method, etc.)
- **Parser**: Engine that evaluates template expressions against code model context
- **Filter**: Predicate that selects subsets of code model collections

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: CLI produces identical output to VS extension for all test templates (100% parity)
- **SC-002**: All existing AcciClaim templates generate files matching VS extension output
- **SC-003**: Comprehensive parity test suite covers all 32 functional requirements
- **SC-004**: No regressions in existing CLI test suite (357+ tests continue passing)
- **SC-005**: Users can migrate from VS extension to CLI without template modifications
- **SC-006**: All supported Settings properties are exercised in tests
- **SC-007**: Type resolution tests cover arrays, generics, nullable, dictionaries, tuples, and Task types

## Assumptions

1. The VS extension's behavior is the source of truth for expected output
2. Templates compiled by the CLI use the same Roslyn compiler as the VS extension
3. The CliSettings class can be extended without breaking changes
4. Performance is not a primary concern for this feature (correctness first)
5. The ItemFilter class (shared code) works correctly and doesn't need changes
6. Template syntax is well-documented and stable (no new syntax features needed)
