# Concept Refinement Instructions Verification

## Acceptance Evidence

| Acceptance scenario | Method | Result | Evidence | Limitations |
| --- | --- | --- | --- | --- |
| Instruction steers a Fine tune | Application prompt-assembly + framework-free view-model test | Pass | `RefineAsync_NonEmptyInstruction_IncludedInUserMessage` asserts `Creator instruction: make the phrase shorter` in the Phrase Fine tune user message while `Improve the Phrase` is retained; `FineTune_ThreadsCornerMatchingInstruction_AndClearsOnSuccess` asserts the corner-matching instruction is passed and cleared on success. | None. |
| Instruction steers a Change | Application prompt-assembly + framework-free view-model test | Pass | `RefineAsync_InstructionOnChange_IncludedInUserMessage` asserts the instruction in a Graphic direction Change message with `Propose a materially different direction` retained; `Change_ThreadsCornerMatchingInstruction_AndClearsOnSuccess` asserts the corner-matching instruction is passed and cleared. | None. |
| Instruction for a different corner is not included | Application + framework-free view-model test | Pass | `FineTune_ThreadsCornerMatchingInstruction_AndClearsOnSuccess` asserts `LastInstruction` is the Phrase instruction and not the Concept idea instruction; `Change_ThreadsCornerMatchingInstruction_AndClearsOnSuccess` asserts only the Graphic instruction is passed while a Concept instruction is set. | None. |
| Empty instruction leaves behavior unchanged | Application prompt-assembly test | Pass | `RefineAsync_EmptyInstruction_NotIncludedInUserMessage` asserts `Creator instruction` is absent when the value is whitespace-only; action/corner unchanged by existing captured-request coverage. | None. |
| Result applied but automatic-save commit fails | Framework-free view-model test | Pass | `CommitFailureAfterApply_ClearsInstructionAndRetainsAppliedValue` (failSaves stub) asserts the instruction clears, the applied value remains in the inspector draft, and one history entry is retained. | None. |
| The active Item changes or the session resets | Framework-free view-model test | Pass | `SessionReset_ClearsAllInstructions` asserts all three instruction fields are cleared after `ResetSession`. | None. |
| Failed or cancelled operation preserves the instruction | Framework-free view-model tests | Pass | `RefinementFailure_PreservesInstruction` and `RefinementCancellation_PreservesInstruction` assert the instruction text is retained with an inline error (failure) or no error (cancellation). | None. |
| Instruction field content has no persistence or history effect | Framework-free view-model + headless binding tests | Pass | `InstructionText_NoPersistenceOrHistorySideEffect` asserts inspector drafts, score, and history are unchanged with instructions typed before an action; view test asserts instruction text does not touch the inspector draft. No new persistence path is added; persistence is proven through the unchanged single inspector boundary. | No new persistence adapter exists to integration-test. |
| Instruction fields respect read-only state | Avalonia headless view test | Pass | `InstructionFields_AreReadOnlyDuringConceptReview` asserts all three instruction fields are read-only when the Concept stage is read-only. | No live assistive-technology narration; deterministic visual-tree/control-state coverage is the project baseline. |
| Fine tune/Change context, apply-own-corner, Phrase normalization, empty corner | Existing Application and view-model coverage | Pass | Existing `RefineAsync_*` and `ConceptRefinementSessionViewModelTests` tests pass with the new instruction parameter; request tests confirm the current value and other two corners remain included. | None. |
| Request includes framework and creative context / non-empty instruction included and bounded | Application prompt-assembly tests | Pass | `RefineAsync_Instruction_SystemMessageBoundsIt` asserts the system message contains `supplemental guidance` and `cannot override`; existing captured-request test asserts the framework and creative context remain present. | None. |

## Validation Gates

- Focused tests: Pass — Application Concept refinement 21 passed / 0 failed; App `ConceptRefinementSessionViewModelTests` 40 passed / 0 failed; App `ConceptRefinementViewTests` 17 passed / 0 failed.
- Solution build: Pass — 0 errors.
- Solution test baseline: Pass — Application 274, Domain 177, Integration 130, App 375; 0 failed, 0 skipped (total 956).
- Strict change validation: Pass — `openspec validate concept-refinement-instructions --strict`.
- Changed-scope review: Pass — changes are limited to the Concept refinement service contract (one optional `string? instruction` parameter), the refinement prompt assembly (instruction segment + bounded-guidance system message), the session view model (three instruction properties, threading, clear-on-success/reset), the Concept surface (three instruction fields), and focused tests. No Domain or Integration edits, no persistence, history, rollback, or score semantics changed; Initialize is unaffected.

## Overall Result

All acceptance scenarios pass with deterministic evidence. The solution build and test baseline are green (0 failures). The delivery package is complete and ready for archive or review.
