# Tasks: Fast CLI Project Loading

**Input**: Design documents from `/specs/001-fast-cli-loading/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are REQUIRED per Constitution Principle II (Test Coverage Requirements).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **CLI Infrastructure**: `src/CLI/Infrastructure/`
- **CLI Tests**: `src/Tests/CLI/`
- **Test Fixtures**: `src/Tests/TestProjects/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, data models, and test fixtures

- [x] T001 Create data model classes (ProjectInfo, SolutionInfo, SolutionProject, SourceFileResult, LoadingResult, DiagnosticMessage) in src/CLI/Infrastructure/Models/
- [x] T002 [P] Create IProjectFileParser interface in src/CLI/Infrastructure/IProjectFileParser.cs
- [x] T003 [P] Create ISolutionFileParser interface in src/CLI/Infrastructure/ISolutionFileParser.cs
- [x] T004 [P] Create ISourceFileDiscovery interface in src/CLI/Infrastructure/ISourceFileDiscovery.cs
- [x] T005 [P] Create IReferenceResolver interface in src/CLI/Infrastructure/IReferenceResolver.cs
- [x] T006 Create test fixture: SDK-style project in src/Tests/TestProjects/SdkStyleProject/
- [x] T007 [P] Create test fixture: Legacy project in src/Tests/TestProjects/LegacyProject/
- [x] T008 [P] Create test fixture: Mixed solution with external file reference in src/Tests/TestProjects/MixedSolution/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T009 Implement SourceFileDiscovery class using Microsoft.Extensions.FileSystemGlobbing in src/CLI/Infrastructure/SourceFileDiscovery.cs
- [x] T010 Implement ReferenceResolver using AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") in src/CLI/Infrastructure/ReferenceResolver.cs
- [x] T011 [P] Write unit tests for SourceFileDiscovery (include patterns, exclude patterns, obj/bin default exclusion) in src/CLI.Tests/SourceFileDiscoveryTests.cs
- [x] T012 [P] Write unit tests for ReferenceResolver in src/CLI.Tests/ReferenceResolverTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Single Project Fast Loading (Priority: P1)

**Goal**: Load a single project with 700 source files in under 5 seconds using direct Roslyn parsing

**Independent Test**: Run CLI against SDK-style and legacy projects, measure load time under 5 seconds

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T013 [P] [US1] Write unit tests for ProjectFileParser SDK-style detection in src/CLI.Tests/ProjectFileParserTests.cs
- [x] T014 [P] [US1] Write unit tests for ProjectFileParser Compile include/exclude extraction in src/CLI.Tests/ProjectFileParserTests.cs
- [x] T015 [P] [US1] Write integration test for SDK-style project loading in src/CLI.Tests/DirectRoslynWorkspaceTests.cs
- [x] T016 [P] [US1] Write integration test for legacy project loading in src/CLI.Tests/DirectRoslynWorkspaceTests.cs
- [x] T016a [P] [US1] Write integration test for project with source files outside project directory in src/CLI.Tests/DirectRoslynWorkspaceTests.cs

### Implementation for User Story 1

- [x] T017 [US1] Implement ProjectFileParser.IsSdkStyleProject() checking for Sdk attribute in src/CLI/Infrastructure/ProjectFileParser.cs
- [x] T018 [US1] Implement ProjectFileParser.Parse() for SDK-style projects (default globs, explicit includes/excludes) in src/CLI/Infrastructure/ProjectFileParser.cs
- [x] T019 [US1] Implement ProjectFileParser.Parse() for legacy projects (explicit Compile includes) in src/CLI/Infrastructure/ProjectFileParser.cs
- [x] T020 [US1] Implement DirectRoslynWorkspace.LoadProjectAsync() creating CSharpCompilation directly in src/CLI/Infrastructure/DirectRoslynWorkspace.cs
- [x] T021 [US1] Add XML documentation to ProjectFileParser per Constitution Principle I in src/CLI/Infrastructure/ProjectFileParser.cs
- [x] T022 [US1] Run unit tests and verify all pass for ProjectFileParser

**Checkpoint**: Single project loading works independently - can be tested and demoed

---

## Phase 4: User Story 2 - Solution Fast Loading (Priority: P2)

**Goal**: Load a solution with multiple projects in under 30 seconds with cross-project type resolution

**Independent Test**: Run CLI against MixedSolution test fixture, verify all projects load and types resolve

### Tests for User Story 2

- [x] T023 [P] [US2] Write unit tests for SolutionFileParser project extraction in src/Tests/CLI/SolutionFileParserTests.cs
- [x] T024 [P] [US2] Write unit tests for SolutionFileParser solution folder filtering in src/Tests/CLI/SolutionFileParserTests.cs
- [x] T025 [P] [US2] Write integration test for solution loading with project references in src/Tests/CLI/DirectRoslynWorkspaceTests.cs

### Implementation for User Story 2

- [x] T026 [US2] Implement SolutionFileParser.Parse() using regex extraction in src/CLI/Infrastructure/SolutionFileParser.cs
- [x] T027 [US2] Implement SolutionFileParser.GetCSharpProjectPaths() filtering by project type GUID in src/CLI/Infrastructure/SolutionFileParser.cs
- [x] T028 [US2] Implement DirectRoslynWorkspace.LoadSolutionAsync() with topological sort for project references in src/CLI/Infrastructure/DirectRoslynWorkspace.cs
- [x] T029 [US2] Add project reference resolution to DirectRoslynWorkspace using CompilationReference in src/CLI/Infrastructure/DirectRoslynWorkspace.cs
- [x] T030 [US2] Add XML documentation to SolutionFileParser per Constitution Principle I in src/CLI/Infrastructure/SolutionFileParser.cs
- [x] T031 [US2] Run unit tests and verify all pass for SolutionFileParser

**Checkpoint**: Solution loading works independently - CI/CD pipelines can use this

---

## Phase 5: User Story 3 - Graceful Fallback (Priority: P3)

**Goal**: Fall back to Buildalyzer when fast loading fails, with clear warning messages

**Independent Test**: Introduce malformed project file, verify fallback triggers and warning displays

### Tests for User Story 3

- [x] T032 [P] [US3] Write integration test for fallback on malformed project file in src/Tests/CLI/FallbackBehaviorTests.cs
- [x] T033 [P] [US3] Write integration test for fallback on unsupported project features in src/Tests/CLI/FallbackBehaviorTests.cs
- [x] T034 [P] [US3] Write integration test for --verbose flag showing fallback reason in src/Tests/CLI/FallbackBehaviorTests.cs

### Implementation for User Story 3

- [x] T035 [US3] Refactor CliRoslynWorkspace.LoadProjectAsync() to try fast loading first in src/CLI/Infrastructure/CliRoslynWorkspace.cs
- [x] T036 [US3] Add fallback to Buildalyzer on fast loading failure in src/CLI/Infrastructure/CliRoslynWorkspace.cs
- [x] T037 [US3] Add warning message output when fallback occurs in src/CLI/Infrastructure/CliRoslynWorkspace.cs
- [x] T038 [US3] Refactor CliRoslynWorkspace.LoadSolutionAsync() with same fallback pattern in src/CLI/Infrastructure/CliRoslynWorkspace.cs
- [x] T039 [US3] Add detailed fallback reason to LoadingResult.FallbackReason in src/CLI/Infrastructure/CliRoslynWorkspace.cs
- [x] T040 [US3] Run fallback tests and verify all pass

**Checkpoint**: Fallback works reliably - users never blocked by fast loading failures

---

## Phase 6: User Story 4 - Template Compatibility (Priority: P1)

**Goal**: Ensure generated TypeScript output is byte-identical between fast and slow loading

**Independent Test**: Run same templates with both loading methods, diff outputs

### Tests for User Story 4

- [x] T041 [P] [US4] Write compatibility test comparing class metadata output in src/Tests/CLI/TemplateCompatibilityTests.cs
- [x] T042 [P] [US4] Write compatibility test comparing attribute metadata output in src/Tests/CLI/TemplateCompatibilityTests.cs
- [x] T043 [P] [US4] Write compatibility test comparing generic type metadata output in src/Tests/CLI/TemplateCompatibilityTests.cs
- [x] T044 [P] [US4] Write compatibility test comparing nullability metadata output in src/Tests/CLI/TemplateCompatibilityTests.cs
- [x] T045 [P] [US4] Write compatibility test comparing inheritance metadata output in src/Tests/CLI/TemplateCompatibilityTests.cs

### Implementation for User Story 4

- [x] T046 [US4] Verify DirectRoslynWorkspace produces same Document structure as Buildalyzer in src/CLI/Infrastructure/DirectRoslynWorkspace.cs
- [x] T047 [US4] Fix any metadata discrepancies found during compatibility testing
- [x] T048 [US4] Run all compatibility tests and verify byte-identical output

**Checkpoint**: Template compatibility verified - existing users unaffected

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T049 Add timing output to GenerateCommand with --verbose flag in src/CLI/Commands/GenerateCommand.cs
- [x] T050 [P] Add performance benchmark test (700 files < 5 seconds) in src/Tests/CLI/PerformanceBenchmarkTests.cs
- [x] T051 [P] Add memory usage verification test (< 1GB) in src/Tests/CLI/PerformanceBenchmarkTests.cs
- [x] T052 Code cleanup: remove any dead code paths from CliRoslynWorkspace
- [x] T053 Update quickstart.md with actual test results and performance numbers
- [x] T054 Run full test suite and verify all tests pass
- [x] T055 Build solution and verify no warnings per Constitution Principle I

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - US1 and US4 can proceed in parallel (both P1)
  - US2 depends on US1 (needs ProjectFileParser)
  - US3 depends on US1 and US2 (needs both parsers before fallback integration)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Depends on US1 completion (reuses ProjectFileParser)
- **User Story 3 (P3)**: Depends on US1 and US2 (integrates all fast loading components)
- **User Story 4 (P1)**: Can start after US1 (needs working fast loading to compare against)

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Parsers before workspace integration
- Core implementation before validation
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup interface tasks (T002-T005) can run in parallel
- All Setup test fixture tasks (T006-T008) can run in parallel
- Foundational implementation tasks (T009-T010) are sequential
- Foundational test tasks (T011-T012) can run in parallel after T009-T010
- All test tasks within each user story marked [P] can run in parallel
- US1 and US4 can be worked on in parallel after Foundational
- Polish tasks marked [P] can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Write unit tests for ProjectFileParser SDK-style detection"
Task: "Write unit tests for ProjectFileParser Compile include/exclude extraction"
Task: "Write integration test for SDK-style project loading"
Task: "Write integration test for legacy project loading"

# After tests fail, implement sequentially:
Task: "Implement ProjectFileParser.IsSdkStyleProject()"
Task: "Implement ProjectFileParser.Parse() for SDK-style projects"
Task: "Implement ProjectFileParser.Parse() for legacy projects"
Task: "Implement DirectRoslynWorkspace.LoadProjectAsync()"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Single Project Loading)
4. **STOP and VALIDATE**: Test against real project with 700 files
5. Measure performance - should be under 5 seconds

### Incremental Delivery

1. Complete Setup + Foundational -> Foundation ready
2. Add User Story 1 -> Test independently -> 20x faster single project loading!
3. Add User Story 2 -> Test independently -> Solution support for CI/CD
4. Add User Story 3 -> Test independently -> Reliability guarantee
5. Add User Story 4 -> Test independently -> Compatibility verified
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Single Project)
   - Developer B: User Story 4 (Compatibility Tests - can start writing tests immediately)
3. After US1 complete:
   - Developer A: User Story 2 (Solution)
   - Developer B: Continues US4 implementation fixes
4. After US2 complete:
   - Developer A: User Story 3 (Fallback)
5. Polish phase together

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing (TDD per Constitution Principle II)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Constitution Principle I requires XML documentation on all public members
- Constitution Principle II requires test coverage for all new features
