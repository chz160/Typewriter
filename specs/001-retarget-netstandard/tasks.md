# Tasks: Retarget CodeModel and Metadata to .NET Standard 2.0

**Input**: Design documents from `specs/001-retarget-netstandard/`
**Prerequisites**: plan.md (required), spec.md (required), research.md

**Tests**: No new tests required. Existing unit tests verify functionality; retargeting is verified by build success and existing test pass rates.

**Organization**: Tasks grouped by user story. US1 and US2 are both P1 priority and share implementation work - they are combined into a single phase since the same project file changes satisfy both stories.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source projects**: `src/CodeModel/`, `src/Metadata/`
- **Dependent projects**: `src/Roslyn/`, `src/Typewriter/`, `src/Tests/`
- **Solution file**: `Typewriter.sln`

---

## Phase 1: Setup (Pre-Migration Baseline)

**Purpose**: Establish baseline before making changes to enable rollback if needed

- [x] T001 Clean existing build artifacts in src/CodeModel/bin and src/CodeModel/obj directories
- [x] T002 [P] Clean existing build artifacts in src/Metadata/bin and src/Metadata/obj directories
- [x] T003 Build solution to verify current state passes: `msbuild Typewriter.sln /p:Configuration=Release`
- [x] T004 Run existing tests to capture baseline pass count: `dotnet test src/Tests/Typewriter.Tests.csproj`
- [x] T005 [P] Backup current project files (optional): copy src/CodeModel/Typewriter.CodeModel.csproj to .bak
- [x] T006 [P] Backup current project files (optional): copy src/Metadata/Typewriter.Metadata.csproj to .bak

**Checkpoint**: Baseline established - solution builds and tests pass before migration

---

## Phase 2: User Story 1+2 - Retarget Libraries (Priority: P1) ðŸŽ¯ MVP

**Goal**: Convert CodeModel and Metadata to SDK-style netstandard2.0 projects while maintaining backward compatibility with net472 consumers

**Independent Test**:
- US1: `dotnet build` succeeds on both projects; .NET 8 project can reference them
- US2: Full solution builds; all existing tests pass

### Implementation for User Story 1+2

- [x] T007 [US1] Replace src/CodeModel/Typewriter.CodeModel.csproj with SDK-style netstandard2.0 format per research.md
- [x] T008 [US1] Verify CodeModel builds independently: `dotnet build src/CodeModel/Typewriter.CodeModel.csproj`
- [x] T009 [US1] Replace src/Metadata/Typewriter.Metadata.csproj with SDK-style netstandard2.0 format per research.md
- [x] T010 [US1] Verify Metadata builds independently: `dotnet build src/Metadata/Typewriter.Metadata.csproj`
- [x] T011 [US2] Build full solution to verify net472 compatibility: `msbuild Typewriter.sln /p:Configuration=Release`
- [x] T012 [US2] Run all existing tests to verify no regressions: `dotnet test src/Tests/Typewriter.Tests.csproj`
- [x] T013 [US2] Verify Typewriter.Metadata.Roslyn project builds with retargeted dependency
- [x] T014 [US2] Verify Typewriter VS extension project builds with retargeted dependencies

**Checkpoint**: Both CodeModel and Metadata target netstandard2.0; solution builds; all tests pass

---

## Phase 3: User Story 3 - SDK-Style Format Verification (Priority: P2)

**Goal**: Verify SDK-style project format provides expected benefits (dotnet CLI support, simplified project files)

**Independent Test**: Both projects build successfully using `dotnet build` without Visual Studio

### Implementation for User Story 3

- [x] T015 [US3] Verify dotnet restore works: `dotnet restore src/CodeModel/Typewriter.CodeModel.csproj`
- [x] T016 [P] [US3] Verify dotnet restore works: `dotnet restore src/Metadata/Typewriter.Metadata.csproj`
- [x] T017 [US3] Verify dotnet build Release configuration: `dotnet build src/CodeModel/Typewriter.CodeModel.csproj -c Release`
- [x] T018 [P] [US3] Verify dotnet build Release configuration: `dotnet build src/Metadata/Typewriter.Metadata.csproj -c Release`
- [x] T019 [US3] Verify XML documentation files are generated in output directories

**Checkpoint**: SDK-style format fully functional with dotnet CLI tooling

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and cleanup

- [x] T020 Verify no new compiler warnings related to framework targeting in build output
- [x] T021 [P] Verify assembly metadata preserved (check AssemblyInfo attributes in compiled DLLs)
- [x] T022 Run quickstart.md verification steps to validate all success criteria
- [x] T023 Clean up backup files if created (T005, T006)
- [x] T024 Commit changes to version control with descriptive message

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - establishes baseline
- **User Story 1+2 (Phase 2)**: Depends on Setup - core implementation
- **User Story 3 (Phase 3)**: Depends on Phase 2 - verification of SDK-style benefits
- **Polish (Phase 4)**: Depends on all user stories - final validation

### Task Dependencies Within Phases

**Phase 1**:
- T001, T002 can run in parallel (different directories)
- T003 depends on T001, T002 completion
- T004 depends on T003 (solution must build first)
- T005, T006 can run in parallel, anytime

**Phase 2**:
- T007 must complete before T008
- T008 must complete before T009 (CodeModel is dependency of Metadata)
- T009 must complete before T010
- T010 must complete before T011
- T011 must complete before T012, T013, T014
- T013, T014 can run in parallel after T011

**Phase 3**:
- T015, T016 can run in parallel
- T017, T018 can run in parallel (after T015, T016)
- T019 depends on T017, T018

**Phase 4**:
- T020-T024 are sequential verification steps

### Parallel Opportunities

```text
Phase 1 parallel:
  T001 || T002  (clean both directories simultaneously)
  T005 || T006  (backup both files simultaneously)

Phase 2 parallel:
  T013 || T014  (verify dependent projects after solution builds)

Phase 3 parallel:
  T015 || T016  (restore both projects simultaneously)
  T017 || T018  (build both projects simultaneously)

Phase 4 parallel:
  T020 || T021  (verification checks)
```

---

## Implementation Strategy

### MVP First (User Stories 1+2)

1. Complete Phase 1: Setup (establish baseline)
2. Complete Phase 2: User Story 1+2 (core retargeting)
3. **STOP and VALIDATE**: Solution builds, tests pass, libraries consumable by net472 and net8.0
4. This is the MVP - cross-platform libraries work

### Full Delivery

1. Complete MVP (Phases 1-2)
2. Complete Phase 3: User Story 3 (SDK-style verification)
3. Complete Phase 4: Polish (final checks and commit)
4. All acceptance scenarios verified

### Rollback Procedure

If issues encountered during Phase 2:

```bash
# Restore original project files
git checkout -- src/CodeModel/Typewriter.CodeModel.csproj
git checkout -- src/Metadata/Typewriter.Metadata.csproj

# Or restore from backups if created
cp src/CodeModel/Typewriter.CodeModel.csproj.bak src/CodeModel/Typewriter.CodeModel.csproj
cp src/Metadata/Typewriter.Metadata.csproj.bak src/Metadata/Typewriter.Metadata.csproj

# Clean and rebuild
git clean -xfd src/CodeModel/bin src/CodeModel/obj
git clean -xfd src/Metadata/bin src/Metadata/obj
msbuild Typewriter.sln /p:Configuration=Release
```

---

## Notes

- No new tests needed - existing tests validate functionality
- US1 and US2 combined because same implementation satisfies both
- [P] tasks can run in parallel within their phase
- Each checkpoint validates independent story completion
- Total tasks: 24
- Estimated complexity: Low (project file changes only, no code modifications)
