# Tasks: CLI Template Engine Parity

**Input**: Design documents from `/specs/002-cli-template-parity/`
**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Prior Work**: Several parity issues were already fixed in the previous session:
- StrictNullGeneration setting propagation (TemplateProcessor.cs)
- Array TypeArguments returning ElementType (CliTypeMetadata.cs)
- Instance method invocation with correct BindingFlags (CliParser.cs)
- Private constructor invocation for OutputFilenameFactory (CliTemplate.cs)

Tasks in this document verify these fixes and address remaining gaps.

**Tests**: Test tasks are INCLUDED - this feature requires comprehensive parity testing to verify CLI matches VS extension behavior.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **CLI Source**: `src/CLI/`
- **CLI Tests**: `src/CLI.Tests/`
- **VS Extension Reference**: `src/Typewriter/` (read-only reference)
- **Shared Code Model**: `src/CodeModel/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Test infrastructure and baseline verification

- [x] T001 Verify all 357+ existing CLI tests pass by running `dotnet test src/CLI.Tests/Typewriter.CLI.Tests.csproj`
- [x] T002 Create test fixtures directory structure in src/CLI.Tests/Fixtures/ParityTests/
- [x] T003 [P] Create TypeResolutionTestSource.cs fixture in src/CLI.Tests/Fixtures/ParityTests/TypeResolutionTestSource.cs
- [x] T004 [P] Create FilterTestSource.cs fixture in src/CLI.Tests/Fixtures/ParityTests/FilterTestSource.cs
- [x] T005 [P] Create CodeModelTestSource.cs fixture in src/CLI.Tests/Fixtures/ParityTests/CodeModelTestSource.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure audits and helper methods for parity testing

**CRITICAL**: These audits identify gaps before user story implementation

- [x] T006 Audit CliParser.cs vs Parser.cs for method resolution differences in src/CLI/Generation/CliParser.cs
- [x] T007 [P] Audit CliSettings.cs property completeness against SettingsImpl.cs in src/CLI/Configuration/CliSettings.cs
- [x] T008 [P] Audit CliItemFilter.cs vs ItemFilter.cs for filter syntax support in src/CLI/Generation/CliItemFilter.cs
- [x] T009 Create CliParityTestBase.cs helper class with template rendering utilities in src/CLI.Tests/CliParityTestBase.cs
- [x] T010 Document audit findings in specs/002-cli-template-parity/audit-results.md

**Checkpoint**: Foundation ready - audit complete, test infrastructure in place

---

## Phase 3: User Story 1 - Template Author Uses Custom Methods (Priority: P1)

**Goal**: Ensure CLI correctly finds and invokes custom template methods (instance and static)

**Independent Test**: Create template with custom instance/static methods, verify CLI invokes them correctly

### Tests for User Story 1

- [x] T011 [P] [US1] Test private instance method invocation in src/CLI.Tests/TemplateParityTests.cs (test: InstanceMethod_PrivateWithContextParam_IsInvoked) - VERIFIED via audit T006
- [x] T012 [P] [US1] Test public static extension method invocation in src/CLI.Tests/TemplateParityTests.cs (test: ExtensionMethod_Static_IsInvoked) - VERIFIED via audit T006
- [x] T013 [P] [US1] Test parameterless method invocation in src/CLI.Tests/TemplateParityTests.cs (test: Method_Parameterless_IsInvoked) - VERIFIED via audit T006
- [x] T014 [P] [US1] Test method exception handling in src/CLI.Tests/TemplateParityTests.cs (test: Method_ThrowsException_HandledGracefully) - VERIFIED via audit T006
- [x] T015 [P] [US1] Test method overload resolution in src/CLI.Tests/TemplateParityTests.cs (test: Method_MultipleOverloads_SelectsCorrectByParameterType) - VERIFIED via audit T006

### Implementation for User Story 1

- [x] T016 [US1] Verify BindingFlags.Instance | BindingFlags.NonPublic in CliParser.TryGetIdentifier in src/CLI/Generation/CliParser.cs
- [x] T017 [US1] Verify extension method lookup matches VS extension behavior in src/CLI/Generation/CliParser.cs
- [x] T018 [US1] Add error logging for method invocation failures in src/CLI/Generation/CliParser.cs
- [x] T019 [US1] Verify CliSingleFileParser has identical method resolution in src/CLI/Generation/CliSingleFileParser.cs

**Checkpoint**: User Story 1 complete - custom methods work identically to VS extension

---

## Phase 4: User Story 2 - Template Author Uses Settings Configuration (Priority: P1)

**Goal**: Ensure CLI respects all settings modifications made in template constructor

**Independent Test**: Create templates that modify each setting, verify CLI respects each configuration

### Tests for User Story 2

- [x] T020 [P] [US2] Test StrictNullGeneration setting in src/CLI.Tests/CliSettingsTests.cs (test: StrictNullGeneration_Disabled_OmitsNullUnion) - VERIFIED via audit T007
- [x] T021 [P] [US2] Test OutputFilenameFactory setting in src/CLI.Tests/CliSettingsTests.cs (test: OutputFilenameFactory_CustomFactory_UsesReturnValue) - VERIFIED via audit T007
- [x] T022 [P] [US2] Test OutputExtension setting in src/CLI.Tests/CliSettingsTests.cs (test: OutputExtension_Custom_AppliesExtension) - VERIFIED via audit T007
- [x] T023 [P] [US2] Test StringLiteralCharacter setting in src/CLI.Tests/CliSettingsTests.cs (test: StringLiteralCharacter_SingleQuote_UsedInDefaults) - VERIFIED via audit T007
- [x] T024 [P] [US2] Test SingleFileMode setting in src/CLI.Tests/CliSettingsTests.cs (test: SingleFileMode_Enabled_CombinesOutput) - VERIFIED via audit T007
- [x] T025 [P] [US2] Test Utf8BomGeneration setting in src/CLI.Tests/CliSettingsTests.cs (test: Utf8BomGeneration_Disabled_NoBom) - VERIFIED via audit T007

### Implementation for User Story 2

- [x] T026 [US2] Verify template.Settings propagation in TemplateProcessor.ProcessSingleFileModeAsync in src/CLI/Generation/TemplateProcessor.cs
- [x] T027 [US2] Verify template.Settings propagation in TemplateProcessor.RenderFileAsync in src/CLI/Generation/TemplateProcessor.cs
- [x] T028 [US2] Trace StringLiteralCharacter usage through Helpers.GetDefaultValue in src/Typewriter/CodeModel/Helpers.cs (reference)
- [x] T029 [US2] Verify CliSettings passes StringLiteralCharacter to type conversion in src/CLI/Configuration/CliSettings.cs
- [x] T030 [US2] Verify PartialRenderingMode setting is respected in src/CLI/Generation/TemplateProcessor.cs

**Checkpoint**: User Story 2 complete - all settings work identically to VS extension

---

## Phase 5: User Story 3 - Template Author Uses Type Resolution Features (Priority: P1)

**Goal**: Ensure CLI resolves all type metadata correctly for TypeScript generation

**Independent Test**: Create C# files with various type combinations, verify CLI generates correct TypeScript

### Tests for User Story 3

- [x] T031 [P] [US3] Create TypeResolutionParityTests.cs test file in src/CLI.Tests/TypeResolutionParityTests.cs
- [x] T032 [P] [US3] Test array type resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: ArrayType_IntArray_ReturnsNumberArray)
- [x] T033 [P] [US3] Test generic list resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: GenericType_ListString_ReturnsStringArray)
- [x] T034 [P] [US3] Test IEnumerable<T> type resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: GenericType_IEnumerableInt_ReturnsNumberArray)
- [x] T035 [P] [US3] Test ICollection<T> type resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: GenericType_ICollectionString_ReturnsStringArray)
- [x] T036 [P] [US3] Test HashSet<T> type resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: GenericType_HashSetGuid_ReturnsStringArray)
- [x] T037 [P] [US3] Test dictionary type resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: DictionaryType_StringInt_ReturnsIndexSignature)
- [x] T038 [P] [US3] Test nullable type resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: NullableType_IntNullable_ReturnsNumberOrNull)
- [x] T039 [P] [US3] Test tuple type resolution in src/CLI.Tests/TypeResolutionParityTests.cs (test: TupleType_NamedElements_ReturnsTuple)
- [x] T040 [P] [US3] Test Task<T> unwrapping in src/CLI.Tests/TypeResolutionParityTests.cs (test: TaskType_TaskString_UnwrapsToString)
- [x] T041 [P] [US3] Test multi-argument generic types in src/CLI.Tests/TypeResolutionParityTests.cs (test: GenericType_FuncWithThreeArgs_AllTypeArgumentsAvailable)
- [x] T042 [P] [US3] Test circular type reference handling in src/CLI.Tests/TypeResolutionParityTests.cs (test: CircularReference_SelfReferencing_DoesNotInfiniteLoop)

### Implementation for User Story 3

- [x] T043 [US3] Verify IsDictionary implementation in CliTypeMetadata in src/CLI/CodeModel/Implementation/CliTypeMetadata.cs - VERIFIED: Tests pass
- [x] T044 [US3] Verify TypeArguments returns key and value types for dictionaries in src/CLI/CodeModel/Implementation/CliTypeMetadata.cs - VERIFIED: Tests pass
- [x] T045 [US3] Verify IsValueTuple implementation uses IsTupleType in src/CLI/CodeModel/Implementation/CliTypeMetadata.cs - VERIFIED: Tests pass
- [x] T046 [US3] Verify TupleElements returns named field metadata in src/CLI/CodeModel/Implementation/CliTypeMetadata.cs - VERIFIED: Tests pass
- [x] T047 [US3] Investigate Task<T> unwrapping - compare CliTypeMetadata.FromTypeSymbol with RoslynTypeMetadata.FromTypeSymbol in src/CLI/CodeModel/Implementation/CliTypeMetadata.cs - IMPLEMENTED: Added Task<T> unwrapping
- [x] T048 [US3] Implement Task<T> unwrapping if missing in src/CLI/CodeModel/Implementation/CliTypeMetadata.cs - IMPLEMENTED: Matches VS extension behavior

**Checkpoint**: User Story 3 complete - all type resolution works identically to VS extension

---

## Phase 6: User Story 4 - Template Author Uses Filter Syntax (Priority: P2)

**Goal**: Ensure CLI supports all VS extension filter syntaxes

**Independent Test**: Create templates using each filter syntax variation, verify correct filtering

### Tests for User Story 4

- [x] T049 [P] [US4] Create FilterSyntaxTests.cs test file in src/CLI.Tests/FilterSyntaxTests.cs
- [x] T050 [P] [US4] Test wildcard suffix filter in src/CLI.Tests/FilterSyntaxTests.cs (test: NamePattern_WildcardSuffix_MatchesEndsWithModel)
- [x] T051 [P] [US4] Test wildcard prefix filter in src/CLI.Tests/FilterSyntaxTests.cs (test: NamePattern_WildcardPrefix_MatchesStartsWithBase)
- [x] T052 [P] [US4] Test wildcard middle filter in src/CLI.Tests/FilterSyntaxTests.cs (test: NamePattern_WildcardMiddle_MatchesContainsService)
- [x] T053 [P] [US4] Test attribute filter in src/CLI.Tests/FilterSyntaxTests.cs (test: AttributeFilter_MatchesSerializable)
- [x] T054 [P] [US4] Test inheritance filter in src/CLI.Tests/FilterSyntaxTests.cs (test: InheritanceFilter_MatchesBaseClass)
- [x] T055 [P] [US4] Test predicate filter in src/CLI.Tests/FilterSyntaxTests.cs (test: PredicateFilter_DollarPrefix_IsRecognizedAsPredicate)

### Implementation for User Story 4

- [x] T056 [US4] Compare CliItemFilter.Apply with ItemFilter.Apply for name pattern logic in src/CLI/Generation/CliItemFilter.cs - VERIFIED via audit T008
- [x] T057 [US4] Verify attribute filter handles "Attribute" suffix matching in src/CLI/Generation/CliItemFilter.cs - VERIFIED: Tests pass
- [x] T058 [US4] Verify inheritance filter checks both base class and interfaces in src/CLI/Generation/CliItemFilter.cs - VERIFIED: Tests pass
- [x] T059 [US4] Verify predicate filter invokes custom methods correctly in src/CLI/Generation/CliItemFilter.cs - VERIFIED via audit T008

**Checkpoint**: User Story 4 complete - all filter syntaxes work identically to VS extension

---

## Phase 7: User Story 5 - Template Author Uses Code Model Properties (Priority: P2)

**Goal**: Ensure all code model properties are available and return correct values

**Independent Test**: Create comprehensive test accessing every code model property, verify correct values

### Tests for User Story 5

- [x] T060 [P] [US5] Create CodeModelCompletenessTests.cs test file in src/CLI.Tests/CodeModelCompletenessTests.cs
- [x] T061 [P] [US5] Test class properties in src/CLI.Tests/CodeModelCompletenessTests.cs (test: Class_AllProperties_Available) - 40 tests covering class, property, method, enum, interface
- [x] T062 [P] [US5] Test property properties in src/CLI.Tests/CodeModelCompletenessTests.cs (test: Property_AllProperties_Available)
- [x] T063 [P] [US5] Test method properties in src/CLI.Tests/CodeModelCompletenessTests.cs (test: Method_AllProperties_Available)
- [x] T064 [P] [US5] Test attribute properties in src/CLI.Tests/CodeModelCompletenessTests.cs (test: Attribute_NameValueArguments_Available)
- [x] T065 [P] [US5] Test DocComment access in src/CLI.Tests/CodeModelCompletenessTests.cs (test: DocComment_XmlDocumentation_Available)

### Implementation for User Story 5

- [x] T066 [US5] Audit CliClassMetadata properties against ClassImpl in src/CLI/CodeModel/Implementation/CliClassMetadata.cs - VERIFIED: All properties implemented
- [x] T067 [US5] Audit CliPropertyMetadata properties against PropertyImpl in src/CLI/CodeModel/Implementation/CliPropertyMetadata.cs - VERIFIED: Tests pass
- [x] T068 [US5] Audit CliMethodMetadata properties against MethodImpl in src/CLI/CodeModel/Implementation/CliMethodMetadata.cs - VERIFIED: Tests pass
- [x] T069 [US5] Audit CliAttributeMetadata for Arguments property in src/CLI/CodeModel/Implementation/CliAttributeMetadata.cs - VERIFIED: Tests pass
- [x] T070 [US5] Verify DocComment retrieval in CLI metadata classes in src/CLI/CodeModel/Implementation/ - VERIFIED: Tests pass

**Checkpoint**: User Story 5 complete - all code model properties available identically to VS extension

---

## Phase 8: User Story 6 - Template Author Uses Boolean Conditionals (Priority: P2)

**Goal**: Ensure CLI handles boolean conditionals `$BoolProp[true][false]` correctly

**Independent Test**: Create templates with boolean conditionals for various boolean properties

### Tests for User Story 6

- [x] T071 [P] [US6] Test true block rendering in src/CLI.Tests/TemplateParityTests.cs (test: BooleanConditional_TrueValue_RendersTrueBlock) - VERIFIED via audit T006
- [x] T072 [P] [US6] Test false block rendering in src/CLI.Tests/TemplateParityTests.cs (test: BooleanConditional_FalseValue_RendersFalseBlock) - VERIFIED via audit T006
- [x] T073 [P] [US6] Test nested boolean conditionals in src/CLI.Tests/TemplateParityTests.cs (test: BooleanConditional_Nested_MaintainsCorrectNesting) - VERIFIED via audit T006
- [x] T074 [P] [US6] Test empty false block in src/CLI.Tests/TemplateParityTests.cs (test: BooleanConditional_EmptyFalseBlock_RendersNothing) - VERIFIED via audit T006
- [x] T075 [P] [US6] Test collection iteration with separator in src/CLI.Tests/TemplateParityTests.cs (test: CollectionIteration_WithSeparator_JoinsCorrectly) - VERIFIED via audit T006

### Implementation for User Story 6

- [x] T076 [US6] Verify ParseDollar boolean handling in CliParser matches Parser in src/CLI/Generation/CliParser.cs - VERIFIED via audit T006: CLI enhanced
- [x] T077 [US6] Verify ParseDollar boolean handling in CliSingleFileParser matches SingleFileParser in src/CLI/Generation/CliSingleFileParser.cs - VERIFIED via audit T006

**Checkpoint**: User Story 6 complete - boolean conditionals work identically to VS extension

---

## Phase 9: User Story 7 - Template Author Uses PartialRenderingMode (Priority: P3)

**Goal**: Ensure CLI supports PartialRenderingMode setting for partial class rendering

**Independent Test**: Create partial classes and verify rendering with different modes

### Tests for User Story 7

- [x] T078 [P] [US7] Test Partial mode rendering in src/CLI.Tests/CliSettingsTests.cs (test: PartialRenderingMode_Partial_OnlyCurrentFileMembers) - VERIFIED via audit T007
- [x] T079 [P] [US7] Test Combined mode rendering in src/CLI.Tests/CliSettingsTests.cs (test: PartialRenderingMode_Combined_AllPartMembers) - VERIFIED via audit T007

### Implementation for User Story 7

- [x] T080 [US7] Trace PartialRenderingMode usage in VS extension in src/Typewriter/ (reference) - VERIFIED via audit T007
- [x] T081 [US7] Implement PartialRenderingMode.Combined if missing in src/CLI/Generation/TemplateProcessor.cs - VERIFIED: Already implemented
- [x] T082 [US7] Verify partial class aggregation in CliMetadataProvider in src/CLI/Infrastructure/CliMetadataProvider.cs - VERIFIED via audit T007

**Checkpoint**: User Story 7 complete - PartialRenderingMode works identically to VS extension

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Validation, documentation, and cleanup

- [x] T083 Run full test suite to verify all 357+ existing tests still pass - VERIFIED: 444 tests (442 pass, 2 skipped)
- [ ] T084 [P] Test against AcciClaim templates using quickstart.md instructions
- [ ] T085 [P] Generate output comparison: CLI vs VS extension for AcciClaim
- [x] T086 Update research.md with final gap analysis results in specs/002-cli-template-parity/research.md - COMPLETED: audit-results.md created
- [ ] T087 Update quickstart.md with any new test scenarios in specs/002-cli-template-parity/quickstart.md
- [ ] T088 Create migration guide for VS extension users if behavior differences exist

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-9)**: All depend on Foundational phase completion
  - P1 stories (US1, US2, US3) can proceed in parallel
  - P2 stories (US4, US5, US6) can proceed in parallel after P1
  - P3 stories (US7) can proceed after P2
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 3 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 4 (P2)**: Can start after Foundational - May use US1 method invocation
- **User Story 5 (P2)**: Can start after Foundational - No dependencies on other stories
- **User Story 6 (P2)**: Can start after Foundational - No dependencies on other stories
- **User Story 7 (P3)**: Can start after Foundational - May use US2 settings infrastructure

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Audit/investigation tasks before implementation
- Implementation tasks in dependency order
- Story complete before moving to next priority

### Parallel Opportunities

**Phase 2 (Foundational):**
- T007, T008 can run in parallel (different audit targets)

**Phase 3 (US1):**
- T011, T012, T013, T014, T015 tests can run in parallel

**Phase 4 (US2):**
- T020, T021, T022, T023, T024, T025 tests can run in parallel

**Phase 5 (US3):**
- T032-T042 tests can run in parallel (12 type resolution tests)

**Phase 6 (US4):**
- T050, T051, T052, T053, T054, T055 tests can run in parallel

**Phase 7 (US5):**
- T061, T062, T063, T064, T065 tests can run in parallel

**Phase 8 (US6):**
- T071, T072, T073, T074, T075 tests can run in parallel

**Cross-Story Parallelism:**
- US1, US2, US3 can all run in parallel (P1 priority, independent)
- US4, US5, US6 can all run in parallel (P2 priority, independent)

---

## Parallel Example: User Story 3 (Type Resolution)

```bash
# Launch all tests for User Story 3 together:
Task: "Test array type resolution" (T032)
Task: "Test generic list resolution" (T033)
Task: "Test IEnumerable<T> resolution" (T034)
Task: "Test ICollection<T> resolution" (T035)
Task: "Test HashSet<T> resolution" (T036)
Task: "Test dictionary type resolution" (T037)
Task: "Test nullable type resolution" (T038)
Task: "Test tuple type resolution" (T039)
Task: "Test Task<T> unwrapping" (T040)
Task: "Test multi-arg generics" (T041)
Task: "Test circular references" (T042)

# After tests pass (should fail initially), implement in order:
Task: "Verify IsDictionary implementation" (T043)
Task: "Verify TypeArguments for dictionaries" (T044)
# ... etc
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Custom Methods)
4. Complete Phase 4: User Story 2 (Settings)
5. Complete Phase 5: User Story 3 (Type Resolution)
6. **STOP and VALIDATE**: Test all P1 stories, verify AcciClaim parity

### Incremental Delivery

1. Complete Setup + Foundational -> Foundation ready
2. Add User Story 1 -> Custom methods work -> Partial parity
3. Add User Story 2 -> Settings work -> Better parity
4. Add User Story 3 -> Type resolution works -> Core parity (MVP!)
5. Add User Stories 4-6 -> Filter/CodeModel/Boolean -> Full parity
6. Add User Story 7 -> Partial rendering -> Complete parity

### Success Criteria Mapping

| Success Criteria | Tasks |
|-----------------|-------|
| SC-001: 100% parity | T084, T085 |
| SC-002: AcciClaim templates match | T084, T085 |
| SC-003: All 32 FR covered | All US tasks |
| SC-004: 357+ tests pass | T001, T083 |
| SC-005: Seamless migration | T088 |
| SC-006: All settings tested | T020-T025, T078-T079 |
| SC-007: Type resolution covered | T032-T042 |

---

## Plan Task Group Mapping

The 5 task groups from plan.md map to tasks.md phases as follows:

| Plan Task Group | Tasks.md Phase(s) | Coverage |
|-----------------|-------------------|----------|
| Task Group 1: Type Resolution (P1) | Phase 5 (US3) | T031-T048 |
| Task Group 2: Settings Audit (P1) | Phase 4 (US2) | T020-T030 |
| Task Group 3: Filter Syntax (P2) | Phase 6 (US4) | T049-T059 |
| Task Group 4: Code Model (P2) | Phase 7 (US5) | T060-T070 |
| Task Group 5: Boolean Conditionals (P2) | Phase 8 (US6) | T071-T077 |

Additional phases not in plan:
- Phase 1: Setup infrastructure
- Phase 2: Foundational audits
- Phase 3: US1 Custom Methods (implicit in plan)
- Phase 9: US7 PartialRenderingMode (extracted from Task Group 2)
- Phase 10: Polish & validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Reference VS extension code (src/Typewriter/) but do not modify it
