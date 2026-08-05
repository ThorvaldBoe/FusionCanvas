# Concept Refinement Instructions Verification

> Status: Planned. Evidence recorded when implementation completes.

## Acceptance Evidence

| Acceptance scenario | Method | Result | Evidence | Limitations |
| --- | --- | --- | --- | --- |
| Instruction steers a Fine tune | Application prompt-assembly + framework-free view-model test | Pending | Planned: assert the AI user message contains the instruction text for a Phrase Fine tune, and that the corner-matching instruction is the one passed to the service. | None. |
| Instruction steers a Change | Application prompt-assembly + framework-free view-model test | Pending | Planned: assert the AI user message contains the instruction text for a Graphic direction Change, and that the corner-matching instruction is the one passed to the service. | None. |
| Instruction for a different corner is not included | Application + framework-free view-model test | Pending | Planned: assert that a non-acting corner's instruction is absent from the assembled request and not passed by the view model. | None. |
| Empty instruction leaves behavior unchanged | Application prompt-assembly test | Pending | Planned: assert that when the instruction value is empty or whitespace-only, the assembled request contains no instruction content and action/corner are unchanged. | None. |
| Result applied but automatic-save commit fails | Framework-free view-model test | Pending | Planned: commit-failure stub asserts the instruction field is cleared and the applied value/history are retained per the applied-value commit requirement. | None. |
| The active Item changes or the session resets | Framework-free view-model test | Pending | Planned: session reset test asserts all three instruction fields are cleared. | None. |
| Failed or cancelled operation preserves the instruction | Framework-free view-model tests | Pending | Planned: failure and cancellation stubs assert the instruction field retains text and drafts/history/score are unchanged. | None. |
| Instruction field content has no persistence or history effect | Framework-free view-model test | Pending | Planned: typing into instruction fields before an action leaves inspector drafts, history, and score unchanged. No new persistence path is added; persistence is proven through the unchanged single inspector boundary. | No new persistence adapter exists to integration-test. |
| Instruction fields respect read-only state | Avalonia headless view test + markup review | Pending | Planned: find three instruction TextBoxes below the button pairs, assert placeholder, distinct accessible names, two-way edits, and read-only when the stage is read-only. | No live assistive-technology narration; deterministic visual-tree/control-state coverage is the project baseline. |
| Fine tune/Change context, apply-own-corner, Phrase normalization, empty corner | Existing coverage, extended | Pending | Planned: existing Application and view-model tests still pass with the instruction parameter added; request tests confirm the current value, other two corners, and any instruction are included. | None. |
| Request includes framework and creative context / non-empty instruction included and bounded | Application prompt-assembly tests | Pending | Planned: assert the framework and creative context remain present and the bounded-guidance line appears in the refinement system message. | None. |

## Validation Gates

- Focused tests: Pending — planned focused Concept refinement Application and App test run.
- Solution build: Pending — planned `dotnet build .\FusionCanvas.sln`.
- Solution test baseline: Pending — planned `dotnet test .\FusionCanvas.sln`.
- Strict change validation: Pending — planned `openspec validate concept-refinement-instructions --strict`.
- Changed-scope review: Pending — planned review that changes are limited to the Concept refinement service contract, session view model, Concept surface, focused tests, and this delivery package.

## Overall Result

Pending implementation and verification.
