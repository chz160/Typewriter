# Test Design: Epic 1 - Generate Command Foundation (MVP)

**Date:** 2026-01-10
**Author:** Noah (via TEA Agent - Murat)
**Status:** Draft

---

## Executive Summary

**Scope:** Full test design for Epic 1 - Generate Command Foundation

**Risk Summary:**

- Total risks identified: 8
- High-priority risks (>=6): 3
- Critical categories: TECH (output parity)

**Coverage Summary:**

- P0 scenarios: 15 (30 hours)
- P1 scenarios: 7 (7 hours)
- P2/P3 scenarios: 1 (0.5 hours)
- **Total effort**: 37.5 hours (~5 days)

---

## Risk Assessment

### High-Priority Risks (Score >= 6)

| Risk ID | Category | Description | Probability | Impact | Score | Mitigation | Owner | Timeline |
|---------|----------|-------------|-------------|--------|-------|------------|-------|----------|
| R-001 | TECH | Buildalyzer workspace behavior differs from VS workspace | 2 | 3 | 6 | Golden file comparison tests; identical test fixtures for CLI and VS | Dev | Sprint 1 |
| R-002 | TECH | Custom C# code blocks (${...}) execute differently due to metadata differences | 2 | 3 | 6 | Integration tests with complex templates; output diff validation | Dev | Sprint 1 |
| R-004 | DATA | Generated TypeScript not byte-identical to VS extension output | 2 | 3 | 6 | E2E parity tests with VS-generated golden files; CI diff validation | QA | Sprint 1 |

### Medium-Priority Risks (Score 3-5)

| Risk ID | Category | Description | Probability | Impact | Score | Mitigation | Owner |
|---------|----------|-------------|-------------|--------|-------|------------|-------|
| R-003 | TECH | #reference directive resolution fails without DTE | 2 | 2 | 4 | Integration tests for relative path resolution | Dev |
| R-006 | OPS | Missing template files cause unhandled exception | 2 | 2 | 4 | Error injection tests; graceful error handling | Dev |
| R-008 | TECH | Project reference resolution incomplete | 2 | 2 | 4 | Multi-project test fixture; cross-project type tests | Dev |

### Low-Priority Risks (Score 1-2)

| Risk ID | Category | Description | Probability | Impact | Score | Action |
|---------|----------|-------------|-------------|--------|-------|--------|
| R-005 | OPS | Invalid solution path crashes CLI | 1 | 2 | 2 | Monitor |
| R-007 | TECH | System.CommandLine argument parsing edge cases | 1 | 1 | 1 | Monitor |

### Risk Category Legend

- **TECH**: Technical/Architecture (flaws, integration, scalability)
- **SEC**: Security (access controls, auth, data exposure)
- **PERF**: Performance (SLA violations, degradation, resource limits)
- **DATA**: Data Integrity (loss, corruption, inconsistency)
- **BUS**: Business Impact (UX harm, logic errors, revenue)
- **OPS**: Operations (deployment, config, monitoring)

---

## Test Coverage Plan

### P0 (Critical) - Run on every commit

**Criteria**: Blocks core journey + High risk (>=6) + No workaround

| Test ID | Requirement | Test Level | Risk Link | Owner | Notes |
|---------|-------------|------------|-----------|-------|-------|
| 1.1-INT-001 | CLI project builds as part of Typewriter.sln | Integration | - | Dev | Build validation |
| 1.1-INT-002 | CLI references CodeModel, Metadata, Roslyn assemblies | Integration | - | Dev | Architecture |
| 1.2-INT-001 | Valid .sln file loads via Buildalyzer | Integration | R-001 | Dev | Core workspace |
| 1.2-INT-002 | AdhocWorkspace created with solution projects | Integration | R-001 | Dev | Provider foundation |
| 1.2-INT-003 | `--solution ./path.sln` locates and validates file | Integration | R-001 | Dev | FR8, FR20 |
| 1.2-UNIT-001 | CliMetadataProvider has no VS dependencies | Unit | - | Dev | Architecture constraint |
| 1.3-INT-001 | Solution with .tst files discovered and processed | Integration | - | QA | FR5 |
| 1.3-INT-002 | Standard Typewriter syntax works ($Classes, $Properties) | Integration | R-002 | QA | FR1 |
| 1.3-INT-003 | Custom C# code blocks (${...}) execute correctly | Integration | R-002 | QA | FR6 - High risk |
| 1.3-INT-004 | #reference directives resolve relative to template | Integration | R-003 | QA | FR7 |
| 1.3-INT-005 | Types from multiple projects available in code model | Integration | R-008 | QA | Cross-project |
| 1.3-E2E-001 | Full template processing produces TypeScript output | E2E | R-004 | QA | End-to-end |
| 1.4-E2E-001 | Generated .ts files byte-identical to VS extension | E2E | R-004 | QA | **Critical** NFR-C2 |
| 1.4-E2E-003 | Exit code 0 on successful generation | E2E | - | QA | FR35 |
| 1.4-INT-002 | Deterministic output (same inputs = same outputs) | Integration | R-004 | QA | NFR-R1 |

**Total P0**: 15 tests, 30 hours

### P1 (High) - Run on PR to main

**Criteria**: Important features + Medium risk (3-4) + Common workflows

| Test ID | Requirement | Test Level | Risk Link | Owner | Notes |
|---------|-------------|------------|-----------|-------|-------|
| 1.1-UNIT-001 | `--help` displays generate command and options | Unit | - | Dev | FR23 |
| 1.1-UNIT-003 | `generate --help` shows `--solution` option | Unit | - | Dev | FR23 |
| 1.2-INT-004 | Invalid .sln path returns error, non-zero exit | Integration | R-005 | Dev | Error handling |
| 1.2-INT-005 | Malformed .sln displays actionable error | Integration | R-006 | Dev | NFR-R2 |
| 1.4-E2E-002 | Success message shows file count | E2E | - | QA | FR13 |
| 1.4-INT-001 | No interactive prompts during execution | Integration | - | QA | FR38 |
| 1.4-E2E-004 | Multiple runs produce identical output | E2E | R-004 | QA | Reproducibility |

**Total P1**: 7 tests, 7 hours

### P2 (Medium) - Run nightly/weekly

**Criteria**: Secondary features + Low risk (1-2) + Edge cases

| Test ID | Requirement | Test Level | Risk Link | Owner | Notes |
|---------|-------------|------------|-----------|-------|-------|
| 1.1-UNIT-002 | `--version` displays version number | Unit | - | Dev | FR24 |

**Total P2**: 1 test, 0.5 hours

---

## Execution Order

### Smoke Tests (<2 min)

**Purpose**: Fast feedback, catch build-breaking issues

- [ ] 1.1-INT-001: CLI project builds as part of Typewriter.sln (30s)
- [ ] 1.2-INT-001: Valid .sln file loads via Buildalyzer (45s)
- [ ] 1.3-E2E-001: Full template processing produces TypeScript output (45s)

**Total**: 3 scenarios

### P0 Tests (<15 min)

**Purpose**: Critical path validation - output parity

- [ ] 1.1-INT-002: CLI references all required assemblies
- [ ] 1.2-INT-002: AdhocWorkspace created with solution projects
- [ ] 1.2-INT-003: `--solution` argument works
- [ ] 1.2-UNIT-001: No VS dependencies in CliMetadataProvider
- [ ] 1.3-INT-001: Template discovery works
- [ ] 1.3-INT-002: Standard Typewriter syntax works
- [ ] 1.3-INT-003: Custom C# code blocks execute
- [ ] 1.3-INT-004: #reference directives resolve
- [ ] 1.3-INT-005: Cross-project types available
- [ ] 1.4-E2E-001: **Output parity with VS extension**
- [ ] 1.4-E2E-003: Exit code 0 on success
- [ ] 1.4-INT-002: Deterministic output

**Total**: 15 scenarios (including smoke)

### P1 Tests (<25 min)

**Purpose**: Important feature coverage

- [ ] 1.1-UNIT-001: `--help` works
- [ ] 1.1-UNIT-003: `generate --help` works
- [ ] 1.2-INT-004: Invalid path error handling
- [ ] 1.2-INT-005: Malformed solution error
- [ ] 1.4-E2E-002: Success message with file count
- [ ] 1.4-INT-001: Non-interactive operation
- [ ] 1.4-E2E-004: Reproducible output

**Total**: 7 scenarios

---

## Resource Estimates

### Test Development Effort

| Priority | Count | Hours/Test | Total Hours | Notes |
|----------|-------|------------|-------------|-------|
| P0 | 15 | 2.0 | 30 | Complex setup, output parity |
| P1 | 7 | 1.0 | 7 | Standard coverage |
| P2 | 1 | 0.5 | 0.5 | Simple scenario |
| **Total** | **23** | **-** | **37.5** | **~5 days** |

### Prerequisites

**Test Data:**

- SimpleSolution fixture (single project, 2-3 C# files, 1 .tst template)
- MultiProjectSolution fixture (3+ projects with inter-project references)
- GoldenOutput fixture (VS extension-generated .ts files for parity comparison)
- ErrorCases fixture (malformed .sln, invalid .tst, missing references)

**Tooling:**

- xUnit for test framework
- Should for fluent assertions
- NSubstitute for mocking IMetadataProvider
- File diff utility for golden file comparison

**Environment:**

- Windows 10/11 with .NET Framework 4.7.2
- Visual Studio 2022/2025 for building
- vstest.console.exe for test execution

---

## Quality Gate Criteria

### Pass/Fail Thresholds

- **P0 pass rate**: 100% (no exceptions)
- **P1 pass rate**: >= 95% (waivers required for failures)
- **P2/P3 pass rate**: >= 90% (informational)
- **High-risk mitigations**: 100% complete or approved waivers

### Coverage Targets

- **Critical paths**: >= 80%
- **Output parity tests**: 100%
- **Error handling**: >= 70%
- **Edge cases**: >= 50%

### Non-Negotiable Requirements

- [ ] All P0 tests pass
- [ ] No high-risk (>=6) items unmitigated
- [ ] Output parity test (1.4-E2E-001) passes - **BLOCKER**
- [ ] Exit code validation passes (FR35)

---

## Mitigation Plans

### R-001: Buildalyzer workspace behavior differs from VS (Score: 6)

**Mitigation Strategy:** Create comprehensive integration tests comparing CliMetadataProvider output to known-good VS extension metadata. Use identical test fixture solutions. Add edge case coverage for complex project structures.

**Owner:** Dev team
**Timeline:** Sprint 1
**Status:** Planned
**Verification:** 1.2-INT-001, 1.2-INT-002, 1.3-INT-005 all pass; no metadata discrepancies

### R-002: Custom C# code blocks execute differently (Score: 6)

**Mitigation Strategy:** Create test templates with complex ${...} blocks including string manipulation, LINQ queries, and conditional logic. Compare output against VS extension golden files.

**Owner:** Dev team
**Timeline:** Sprint 1
**Status:** Planned
**Verification:** 1.3-INT-003 passes; golden file diff shows zero differences

### R-004: Generated TypeScript not byte-identical (Score: 6)

**Mitigation Strategy:** Generate reference output using VS extension, commit as golden files. E2E tests diff CLI output against golden files. Fail build on any byte difference.

**Owner:** QA team
**Timeline:** Sprint 1
**Status:** Planned
**Verification:** 1.4-E2E-001 passes; `diff` shows identical output

---

## Assumptions and Dependencies

### Assumptions

1. VS extension produces correct output (golden files are trusted baseline)
2. Buildalyzer submodule is stable and functional
3. Existing template engine code requires zero modification
4. Test fixture solutions will cover representative scenarios

### Dependencies

1. **GoldenOutput fixture** - VS extension-generated reference files required before E2E tests
2. **Buildalyzer build** - Submodule must compile successfully
3. **System.CommandLine package** - Must be added to CLI project

### Risks to Plan

- **Risk**: Golden files may drift if VS extension is updated
  - **Impact**: False test failures
  - **Contingency**: Regenerate golden files when VS extension changes

- **Risk**: Buildalyzer version incompatibility
  - **Impact**: Solution loading fails
  - **Contingency**: Pin Buildalyzer version; test with multiple VS solution formats

---

## Test File Structure

### Recommended Test Organization

```
src/Tests/CLI/
├── GenerateCommandTests.cs          # 1.1 tests - command structure
├── CliMetadataProviderTests.cs      # 1.2 tests - workspace provider
├── TemplateIntegrationTests.cs      # 1.3 tests - template engine
├── OutputParityTests.cs             # 1.4 tests - golden file comparison
└── Fixtures/
    ├── SimpleSolution/              # Basic happy path
    ├── MultiProjectSolution/        # Cross-project references
    ├── ErrorCases/                  # Malformed inputs
    └── GoldenOutput/                # VS extension reference output
```

### Sample Test Implementation

```csharp
// OutputParityTests.cs
[Fact]
public void Generate_ShouldProduceIdenticalOutputToVsExtension()
{
    // Arrange
    var solutionPath = GetFixturePath("SimpleSolution/SimpleSolution.sln");
    var goldenOutput = GetFixturePath("GoldenOutput/Models.ts");
    var actualOutput = Path.Combine(TempDirectory, "Models.ts");

    // Act
    var exitCode = RunCli($"generate --solution {solutionPath}");

    // Assert
    exitCode.ShouldBe(0);
    File.ReadAllText(actualOutput).ShouldBe(File.ReadAllText(goldenOutput));
}
```

---

## Follow-on Workflows (Manual)

- Run `*atdd` to generate failing P0 tests before implementation
- Run `*automate` for broader coverage once Epic 1 implementation exists
- Run `*trace` after implementation to validate coverage mapping

---

## Approval

**Test Design Approved By:**

- [ ] Product Manager: _____________ Date: _______
- [ ] Tech Lead: _____________ Date: _______
- [ ] QA Lead: _____________ Date: _______

**Comments:**

---

## Appendix

### Knowledge Base References

- `risk-governance.md` - Risk classification framework
- `probability-impact.md` - Risk scoring methodology
- `test-levels-framework.md` - Test level selection
- `test-priorities-matrix.md` - P0-P3 prioritization

### Related Documents

- PRD: `_bmad-output/planning-artifacts/prd.md`
- Epic: `_bmad-output/planning-artifacts/epics.md` (Epic 1)
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- System Test Design: `_bmad-output/test-design-system.md`

### Functional Requirements Covered

| FR | Description | Test Coverage |
|----|-------------|---------------|
| FR1 | Generate TypeScript from C# using .tst templates | 1.3-INT-002, 1.3-E2E-001 |
| FR4 | Identical output to VS extension | 1.4-E2E-001 |
| FR5 | Process all .tst template files | 1.3-INT-001 |
| FR6 | Execute custom C# code blocks (${...}) | 1.3-INT-003 |
| FR7 | Resolve #reference directives | 1.3-INT-004 |
| FR8 | Specify solution file path | 1.2-INT-003 |
| FR20 | `--solution` CLI argument | 1.2-INT-003 |
| FR23 | `--help` argument | 1.1-UNIT-001, 1.1-UNIT-003 |
| FR24 | `--version` argument | 1.1-UNIT-002 |
| FR35 | Exit code 0 on success | 1.4-E2E-003 |
| FR38 | Non-interactive operation | 1.4-INT-001 |

---

**Generated by:** BMad TEA Agent - Test Architect Module
**Workflow:** `_bmad/bmm/testarch/test-design` (Epic-Level Mode)
**Version:** 4.0 (BMad v6)
