## 1. Application Contract and Prompt Assembly

- [x] 1.1 Add an optional `string? instruction` parameter to `IConceptRefinementService.RefineAsync`.
- [x] 1.2 In `ConceptRefinementService.RefineAsync`, trim the instruction and append a labeled `Creator instruction:` line to the user message only when it has non-whitespace content.
- [x] 1.3 Extend the refinement system message to instruct the model to treat the creator instruction as bounded, non-overriding supplemental guidance that cannot override output rules or action semantics.

## 2. Session View Model State and Flow

- [x] 2.1 Add `ConceptIdeaInstructions`, `PhraseInstructions`, and `GraphicDirectionInstructions` properties to `ConceptRefinementSessionViewModel`, cleared on session reset, with no effect on enablement, score, or history.
- [x] 2.2 Thread the corner-matching instruction into `RefineAsync` from `ExecuteFineTuneAsync` and `ExecuteChangeAsync`, using only the acting corner's instruction.
- [x] 2.3 Clear the matching instruction property after that corner's AI result succeeds in `GuardApplyAsync` (regardless of the subsequent commit outcome); preserve it on failure and cancellation.

## 3. Avalonia Surface

- [x] 3.1 Expand the per-corner action grid and add one unlabeled single-line `TextBox` per corner below its input/action row, with `PlaceholderText="Instructions"`, two-way binding to the corner instruction property, a distinct accessible name, and `IsReadOnly` bound to `!ItemInspector.CanEditStage`.

## 4. Automated Verification

- [x] 4.1 Add application prompt-assembly tests asserting the instruction is present when non-empty and absent when empty/whitespace, action/corner unchanged, a different corner's instruction excluded, and the bounded-guidance line present in the refinement system message.
- [x] 4.2 Add view-model lifecycle tests for corner-matching instruction threading, exclusion of a different corner's instruction, clear-after-success (including commit-failure-after-apply), preserve-on-failure/cancellation, no persistence/history/score side effect, and session-reset clearing.
- [x] 4.3 Add Avalonia headless view tests for the three instruction fields: presence below the button pairs, placeholder, two-way editing, accessible names, and read-only state.

## 5. Completion Gates

- [x] 5.1 Record criterion-level evidence for every acceptance scenario in verification.md.
- [x] 5.2 Run focused Application and App Concept refinement tests, the solution build and test baseline, strict OpenSpec validation, and a changed-scope regression review; resolve any in-scope failures.
