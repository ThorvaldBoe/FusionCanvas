## Why

Creators refining a concept have no way to steer the AI in a specific direction (for example "make the phrase shorter"), so they must re-run Fine tune or Change until a result happens to match their intent, or fall back to writing their own starting point. This change lets the creator supply a short instruction that the AI considers when Fine tuning or Changing one design-triangle corner, without disrupting the existing refinement flow.

## What Changes

- Add three small, unlabeled instruction text fields to the Refine with AI panel, one for each corner (Concept idea, Phrase, Graphic direction), each placed just below its corresponding Fine tune/Change button pair.
- Use a placeholder text of `Instructions` for each field; no label is shown.
- When the instruction field for a corner contains non-whitespace text, that text is included in the AI request for that corner's Fine tune or Change action, supplementing the existing action semantics (Fine tune preserves direction, Change proposes a different direction).
- When the instruction field is empty or whitespace-only, the request and behavior are identical to today.
- Clear the instruction field for the corner after a successful Fine tune or Change application of that corner. A failed or cancelled operation keeps the instruction text. No persistence of instruction text is added.
- Preserve read-only, AI availability, busy, Initialize, history, rollback, scoring, and result-application behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `concept-refinement`: The per-corner Fine tune and Change actions accept an optional per-corner user instruction that is added to the AI request, and the refinement panel exposes three small unlabeled `Instructions` fields below each button pair that are cleared after a successful apply.

## Delivery Module Boundaries

This module has one outcome: creators can give the AI a short direction for a single-corner refinement. It includes the three instruction fields, the request-contract change, the clear-after-success lifecycle, and focused verification because they share one refinement surface, one session view model, and one application service boundary.

Dependencies are limited to the Concept refinement service contract, its calling session view model, the Avalonia Concept surface, and the existing refinement apply/history/score machinery. Non-goals are persisting instruction text across operations, applying instructions to Initialize, changing Fine tune/Change semantics, altering scoring, history, or rollback, or affecting other stage tools.

The primary workflow is brief, optional, in-context creative steering inside the main Concept workspace, so each field sits inline beneath its own button pair and stays out of the way when empty. Empty, unavailable, busy, read-only, failure, cancellation, and success states continue to use existing behavior, with the single addition that a successful apply clears that corner's instruction text.

## Impact

- `FusionCanvas.Application`: `IConceptRefinementService.RefineAsync` gains an optional instruction parameter; `ConceptRefinementService` includes it in the user message when non-empty.
- `FusionCanvas.App`: `ConceptRefinementSessionViewModel` gains three instruction properties, passes the corner-appropriate instruction to the service, and clears it on successful apply; `MainWindow.axaml` adds three instruction text fields below the button pairs.
- Tests: `tests/FusionCanvas.Application.Tests/ConceptRefinement/ConceptRefinementServiceTests.cs`, `tests/FusionCanvas.App.Tests/ConceptRefinementSessionViewModelTests.cs`, and `tests/FusionCanvas.App.Tests/ConceptRefinementViewTests.cs`.
- No Domain, Integration, database, file format, dependency, or migration changes.
- Main risks are threading the wrong corner's instruction, leaking instructions into non-refinement prompts, or failing to clear/preserve instruction text on success/failure; each receives focused regression coverage.
