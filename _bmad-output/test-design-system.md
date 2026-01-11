# System-Level Test Design - Typewriter CLI

**Date:** 2026-01-10
**Author:** Noah (via TEA Agent - Murat)
**Status:** Draft
**Project:** Typewriter CLI Extension

---

## Executive Summary

This document provides a **system-level testability review** for the Typewriter CLI architecture before implementation begins. It assesses the architecture's testability across controllability, observability, and reliability dimensions, identifies architecturally significant requirements (ASRs), recommends test levels strategy, and defines NFR testing approaches.

**Key Findings:**
- Architecture is **highly testable** due to provider pattern abstraction and clean project boundaries
- Existing test infrastructure (xUnit, Should, NSubstitute) can be fully reused
- ~90% code reuse from existing codebase minimizes test surface for new code
- CLI-specific testing requires focus on workspace provider, command handling, and console output

---

## Testability Assessment

### Controllability: PASS

**Can we control system state for testing?**

| Aspect | Assessment | Evidence |
|--------|------------|----------|
| **API Seeding** | PASS | CLI accepts solution/project paths as arguments; test fixtures can provide controlled .sln/.csproj files |
| **External Dependencies Mockable** | PASS | `IMetadataProvider` interface enables mocking; Buildalyzer and AdhocWorkspace can be substituted in tests |
| **Dependency Injection** | PARTIAL | No formal DI container, but provider pattern allows constructor injection in `CliMetadataProvider` |
| **Error Condition Triggers** | PASS | Can provide malformed .sln files, missing templates, invalid C# to test error paths |
| **Configuration Control** | PASS | CLI args and config files provide full configuration control |

**Controllability Recommendations:**
- Create test fixture solution with representative C# and .tst files
- Use NSubstitute to mock `IMetadataProvider` for unit tests
- Test configuration precedence (CLI args > config file > defaults)

### Observability: PASS

**Can we inspect system state and validate results?**

| Aspect | Assessment | Evidence |
|--------|------------|----------|
| **Output Inspection** | PASS | `ConsoleOutput` centralizes all user-facing messages; stdout/stderr separation enables capture |
| **Exit Codes** | PASS | Documented exit codes (0/1/2) provide clear success/failure signal |
| **File System Validation** | PASS | Generated .ts files can be compared against expected output |
| **Error Messages** | PASS | Compiler-style format (File:Line:Column: Message) enables parsing and validation |
| **Deterministic Output** | PASS | Same inputs produce byte-identical outputs (NFR-R1) |

**Observability Recommendations:**
- Implement string capture for `ConsoleOutput` in test mode
- Create golden file comparison tests for output parity validation
- Log execution time for performance baseline tracking

### Reliability: PASS

**Are tests isolated, reproducible, and deterministic?**

| Aspect | Assessment | Evidence |
|--------|------------|----------|
| **Test Isolation** | PASS | Each test can use isolated test fixture solution; no shared mutable state |
| **Parallel Execution** | PASS | Provider pattern creates fresh workspace per invocation; no singleton state |
| **Reproducibility** | PASS | Deterministic template engine; same C# + .tst = same .ts |
| **Cleanup Discipline** | PASS | Generated files can be cleaned up per test; no persistent side effects |
| **Race Conditions** | LOW RISK | Single-threaded CLI execution; no async operations in MVP |

**Reliability Recommendations:**
- Use temporary directories for generated output in tests
- Clean up generated files in test teardown
- Verify parallel test execution doesn't cause conflicts

---

## Architecturally Significant Requirements (ASRs)

These quality requirements drive architecture decisions and pose testability considerations.

### ASR-1: Output Parity (NFR-C2)

| Attribute | Value |
|-----------|-------|
| **Requirement** | CLI produces identical output to VS extension for same inputs |
| **Risk Category** | TECH |
| **Probability** | 2 (Possible) |
| **Impact** | 3 (Critical) |
| **Score** | 6 (HIGH) |
| **Testability Challenge** | Must compare byte-for-byte output between CLI and VS extension |

**Test Strategy:**
- Create reference output from VS extension
- Golden file comparison tests for CLI output
- Diff-based validation in CI pipeline

---

### ASR-2: Performance - Cold Start (NFR-P1)

| Attribute | Value |
|-----------|-------|
| **Requirement** | CLI startup time < 3 seconds cold start |
| **Risk Category** | PERF |
| **Probability** | 2 (Possible) |
| **Impact** | 2 (Degraded) |
| **Score** | 4 (MEDIUM) |
| **Testability Challenge** | Measure startup including Buildalyzer initialization |

**Test Strategy:**
- Benchmark tests measuring end-to-end execution time
- Profiling to identify slow initialization paths
- Baseline comparison in CI

---

### ASR-3: Performance - Per-File Generation (NFR-P2)

| Attribute | Value |
|-----------|-------|
| **Requirement** | Per-file generation time < 500ms per C# file |
| **Risk Category** | PERF |
| **Probability** | 1 (Unlikely) |
| **Impact** | 2 (Degraded) |
| **Score** | 2 (LOW) |
| **Testability Challenge** | Isolate generation time from workspace loading |

**Test Strategy:**
- Microbenchmarks for template compilation
- Separate timing for workspace load vs generation
- Performance regression tests

---

### ASR-4: Graceful Error Handling (NFR-R2)

| Attribute | Value |
|-----------|-------|
| **Requirement** | CLI handles malformed input gracefully (no unhandled exceptions) |
| **Risk Category** | OPS |
| **Probability** | 2 (Possible) |
| **Impact** | 2 (Degraded) |
| **Score** | 4 (MEDIUM) |
| **Testability Challenge** | Test diverse error conditions without crashing |

**Test Strategy:**
- Error injection tests (malformed .sln, invalid .tst, missing files)
- Exception handling validation at command handler level
- Verify error messages written to stderr with correct exit codes

---

### ASR-5: Code Reuse (NFR-M1)

| Attribute | Value |
|-----------|-------|
| **Requirement** | CLI shares >= 60% code with VS extension |
| **Risk Category** | TECH |
| **Probability** | 1 (Unlikely) |
| **Impact** | 2 (Degraded) |
| **Score** | 2 (LOW) |
| **Testability Challenge** | Existing tests cover shared code; new tests focus on CLI-specific |

**Test Strategy:**
- Ensure existing test suite continues passing (100%)
- New tests target only CLI-specific components (~510 lines)
- Coverage metrics for new code only

---

## Test Levels Strategy

Based on the architecture (CLI tool, .NET Framework 4.7.2, provider pattern), the recommended test distribution:

| Test Level | Percentage | Rationale |
|------------|------------|-----------|
| **Unit** | 60% | Pure business logic (TemplateFinder, PathResolver, CliSettings), error handling, configuration parsing |
| **Integration** | 35% | CliMetadataProvider + Buildalyzer + AdhocWorkspace, template engine integration, end-to-end command execution |
| **E2E** | 5% | Full CLI invocation via process execution, output parity validation |

### Unit Test Focus Areas

1. **TemplateFinder** - Pattern matching, exclusion rules, directory traversal
2. **PathResolver** - Relative path resolution, absolute path handling
3. **CliSettings** - JSON parsing, default values, validation
4. **ConsoleOutput** - Message formatting, ANSI color codes, stream routing
5. **Command Argument Parsing** - System.CommandLine argument handling

### Integration Test Focus Areas

1. **CliMetadataProvider + Workspace Loading** - Solution/project loading via Buildalyzer
2. **Template Engine Integration** - Full template processing with real C# code model
3. **Generate Command Handler** - End-to-end command execution with real files
4. **Configuration Precedence** - CLI args > config file > defaults

### E2E Test Focus Areas

1. **Output Parity** - Golden file comparison with VS extension output
2. **Exit Code Validation** - Process exit code verification
3. **Error Message Format** - Compiler-style error output validation

---

## NFR Testing Approach

### Security: N/A for MVP

**Assessment:** CLI is a local developer tool with no network communication, authentication, or sensitive data handling in MVP scope.

**Future Consideration:** If NuGet distribution (Vision phase) adds auto-update or telemetry, security testing would be required.

### Performance

**Approach:** Benchmark testing with MSTest/xUnit performance attributes

**Tools:**
- BenchmarkDotNet for microbenchmarks (optional for Growth phase)
- Manual timing via Stopwatch in tests for MVP
- CI baseline tracking

**Tests Required:**
- Cold start time measurement (< 3s threshold)
- Per-file generation time measurement (< 500ms threshold)
- Solution loading time (< 30s for 100-project solution)
- Memory usage validation (< 2GB)

**Sample Performance Test:**
```csharp
[Fact]
public void Generate_ShouldCompleteWithinPerformanceThreshold()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    var provider = new CliMetadataProvider(testSolutionPath);

    // Act
    var result = GenerateCommand.Execute(provider, testTemplatePath);
    stopwatch.Stop();

    // Assert
    stopwatch.ElapsedMilliseconds.ShouldBeLessThan(3000); // 3s cold start
}
```

### Reliability

**Approach:** Error injection and recovery testing

**Tests Required:**
- Malformed solution file handling
- Missing template file handling
- Invalid C# syntax in source files
- Template compilation errors
- Missing type references in templates
- Partial failure (some templates succeed, some fail)

**Sample Reliability Test:**
```csharp
[Fact]
public void Generate_WithMalformedSolution_ShouldReturnErrorExitCode()
{
    // Arrange
    var malformedSolutionPath = CreateMalformedSolution();

    // Act
    var exitCode = GenerateCommand.Execute(malformedSolutionPath);

    // Assert
    exitCode.ShouldBe(1); // Generation failure
    ConsoleOutput.Errors.ShouldContain(e => e.Contains("Unable to load solution"));
}
```

### Maintainability

**Approach:** CI-based code quality validation

**Metrics:**
- Test coverage >= 80% for new CLI code
- Existing VS extension tests continue passing (100%)
- Code duplication < 5% in new code

**Tests Required:**
- Coverage report generation in CI
- Regression tests for shared code (Parser, Compiler, CodeModel)

---

## Test Environment Requirements

### Local Development Environment

| Requirement | Specification |
|-------------|--------------|
| **OS** | Windows 10/11 |
| **Framework** | .NET Framework 4.7.2 |
| **Visual Studio** | 2022/2025 for building |
| **Test Runner** | xUnit via vstest.console.exe or dotnet test |
| **Test Data** | Test fixture solution with C# + .tst files |

### CI/CD Environment

| Requirement | Specification |
|-------------|--------------|
| **Build Agent** | Windows with .NET Framework 4.7.2 |
| **MSBuild** | VS 2022/2025 MSBuild |
| **Test Execution** | vstest.console.exe |
| **Artifacts** | Test results, coverage reports |

### Test Fixtures Needed

1. **Simple Solution Fixture** - Single project with 2-3 C# files and 1 .tst template
2. **Multi-Project Solution Fixture** - 3+ projects with inter-project references
3. **Error Fixture** - Malformed .sln, invalid .tst, missing references
4. **Golden Output Fixture** - Reference .ts files generated by VS extension

---

## Testability Concerns

### Concern 1: Buildalyzer Behavior Differences

| Attribute | Value |
|-----------|-------|
| **Description** | Buildalyzer may behave differently than VS workspace for edge cases |
| **Severity** | Medium |
| **Impact** | Output parity could fail for complex solutions |
| **Mitigation** | Comprehensive integration tests with diverse solution structures |
| **Owner** | Dev team |
| **Status** | Open |

### Concern 2: Test Data Management

| Attribute | Value |
|-----------|-------|
| **Description** | Test fixture solutions need maintenance as VS version evolves |
| **Severity** | Low |
| **Impact** | Tests could break on VS upgrade |
| **Mitigation** | Version-agnostic solution format; regenerate fixtures as needed |
| **Owner** | Dev team |
| **Status** | Open |

### Concern 3: Performance Test Reliability

| Attribute | Value |
|-----------|-------|
| **Description** | Performance tests may be flaky on different hardware |
| **Severity** | Low |
| **Impact** | False failures in CI |
| **Mitigation** | Use relative thresholds; run on dedicated CI agents |
| **Owner** | Dev team |
| **Status** | Open |

---

## Recommendations for Sprint 0

### Test Infrastructure Setup

1. **Create Test Fixture Solutions**
   - `TestFixtures/SimpleSolution/` - Basic happy path testing
   - `TestFixtures/MultiProjectSolution/` - Reference resolution testing
   - `TestFixtures/ErrorCases/` - Error handling testing
   - `TestFixtures/GoldenOutput/` - Output parity reference

2. **Add CLI Test Folder**
   - Create `src/Tests/CLI/` subfolder per architecture spec
   - Add `GenerateCommandTests.cs`, `CliMetadataProviderTests.cs`, etc.

3. **Configure Test Infrastructure**
   - Extend existing test project to include CLI tests
   - Add test helper for `ConsoleOutput` capture
   - Add test helper for temporary directory management

### Recommended Test Files for MVP

| File | Test Type | Priority |
|------|-----------|----------|
| `GenerateCommandTests.cs` | Integration | P0 |
| `CliMetadataProviderTests.cs` | Integration | P0 |
| `TemplateFinderTests.cs` | Unit | P1 |
| `ConsoleOutputTests.cs` | Unit | P1 |
| `PathResolverTests.cs` | Unit | P2 |
| `OutputParityTests.cs` | E2E | P0 |

---

## Quality Gate Criteria (Pre-Implementation)

Before proceeding to implementation, verify:

- [x] **Controllability confirmed** - Provider pattern enables mocking
- [x] **Observability confirmed** - Exit codes and console output enable validation
- [x] **Reliability confirmed** - Deterministic output, parallel-safe design
- [x] **Test levels defined** - 60% unit, 35% integration, 5% E2E
- [x] **NFR testing approach defined** - Performance benchmarks, error injection
- [x] **Test environment requirements documented** - Local + CI specifications
- [x] **Testability concerns identified** - 3 concerns with mitigations
- [ ] **Test fixtures created** - Pending implementation
- [ ] **Test infrastructure configured** - Pending implementation

---

## Appendix: Existing Test Infrastructure

### Current Test Stack

| Component | Technology | Notes |
|-----------|------------|-------|
| **Framework** | xUnit | Test discovery and execution |
| **Assertions** | Should | Fluent assertions (`result.ShouldBe(expected)`) |
| **Mocking** | NSubstitute | Interface mocking (`Substitute.For<IInterface>()`) |
| **DI Testing** | MefHostingFixture | VS extensibility DI testing |

### Existing Test Categories

| Category | Location | Count |
|----------|----------|-------|
| CodeModel Tests | `src/Tests/CodeModel/` | 14 files |
| Extension Tests | `src/Tests/Extensions/` | 2 files |
| Helper Tests | `src/Tests/Helpers/` | 1 file |
| Metadata Tests | `src/Tests/Metadata/` | 1 file |
| Render Tests | `src/Tests/Render/` | 1 file |

### Test Run Command

```bash
# Run all tests via vstest.console.exe
"/mnt/c/Program Files/Microsoft Visual Studio/18/Enterprise/Common7/IDE/Extensions/TestPlatform/vstest.console.exe" src/Tests/bin/Debug/net472/Typewriter.Tests.dll

# Or via dotnet test
dotnet test src/Tests/Typewriter.Tests.csproj
```

---

**Generated by:** BMad TEA Agent - Test Architect Module
**Workflow:** `_bmad/bmm/testarch/test-design` (System-Level Mode)
**Version:** 4.0 (BMad v6)
