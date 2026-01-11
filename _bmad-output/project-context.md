---
project_name: 'Typewriter CLI'
user_name: 'Noah'
date: '2026-01-10'
sections_completed: ['discovery', 'technology_stack', 'implementation_rules', 'testing_rules', 'critical_rules']
status: 'complete'
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

| Technology | Version | Notes |
|------------|---------|-------|
| C# | .NET Framework 4.7.2 | Required for VS extension compatibility |
| Microsoft.CodeAnalysis | 4.14.0 | Roslyn - must match VS extension |
| System.CommandLine | Latest stable | CLI argument parsing |
| Newtonsoft.Json | Existing version | Configuration parsing |
| Buildalyzer | Submodule | Project analysis outside VS |
| xUnit | - | Test framework |
| Should | - | Fluent assertions |
| NSubstitute | - | Mocking framework |

**Version Constraints:**
- .NET Framework 4.7.2 is MANDATORY - no .NET Core/5/6/7/8 features
- Roslyn version must exactly match VS extension to ensure output parity

## Critical Implementation Rules

### C# / .NET Framework 4.7.2 Rules

- NO C# 8+ features (no nullable reference types, no default interface implementations, no switch expressions)
- NO `async Main` - use `.GetAwaiter().GetResult()` for sync-over-async in entry point
- Use `string.Empty` instead of `""` for empty strings
- Prefer `?? string.Empty` for null-coalescing on strings
- Use lazy initialization pattern: `_field ?? (_field = ...)`

### Codebase Pattern Rules

**Naming Conventions (MUST follow):**
- Implementation wrappers: `*Impl.cs` suffix (e.g., `ClassImpl.cs`)
- Metadata interfaces: `I*Metadata` prefix (e.g., `IClassMetadata`)
- Collections: `*Collection` suffix with lazy loading
- CLI Commands: `*Command` suffix (e.g., `GenerateCommand.cs`)
- CLI Providers: `Cli*Provider` (e.g., `CliMetadataProvider`)

**Static Factory Pattern:**
- Use `FromMetadata()` static methods for creating implementation objects
- Example: `ClassImpl.FromMetadata(metadata, settings)`

**Provider Pattern:**
- All metadata access goes through `IMetadataProvider` contract
- CLI uses `CliMetadataProvider`, VS extension uses `RoslynMetadataProvider`
- NEVER bypass provider abstraction

### Testing Rules

- Tests go in `src/Tests/CLI/` subfolder (co-located with existing tests)
- Use xUnit `[Fact]` and `[Theory]` attributes
- Use `Should` for fluent assertions: `result.ShouldBe(expected)`
- Use `NSubstitute` for mocks: `Substitute.For<IInterface>()`
- Test file naming: `{ClassName}Tests.cs`

### Console Output Rules

- ALWAYS use `ConsoleOutput` class for user-facing messages
- NEVER use `Console.WriteLine` directly
- Errors go to stderr, normal output to stdout
- Use ANSI colors: Green=success, Red=error, Yellow=warning
- Error format: `File:Line:Column: Message`

### Critical Don't-Miss Rules

**NEVER do these:**
- Use MSBuild-specific naming (use generic terms like "standalone workspace")
- Create separate test projects for CLI (use existing `src/Tests/`)
- Mix stdout and stderr streams
- Use `Console.WriteLine` instead of `ConsoleOutput`
- Add dependencies without checking .NET Framework 4.7.2 compatibility

**ALWAYS do these:**
- Follow existing `*Impl` pattern for any new wrappers
- Use compiler-style error format (File:Line:Column)
- Return proper exit codes (0=success, 1=failure, 2=invalid args)
- Preserve byte-identical output to VS extension
- Reference existing assemblies, don't duplicate code

---

_Generated from architecture decisions on 2026-01-10_
