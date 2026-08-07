## Context

The Concept stage currently renders an upper Concept card with editable Concept idea, Phrase, and Graphic direction fields, followed by a separately framed `Refine with AI` section that renders another set of working fields for the same three values. The refinement view model already owns the Design Triangle inputs, AI actions, instructions, completeness score, session history, and rollback behavior. The original Idea-stage value is stored separately as `ItemInspector.Idea`, but is not visible while the Concept surface is active.

The desired Concept surface is one continuous editor. Base idea is context and remains read-only; the three Design Triangle fields are the editable Concept values. AI actions enhance those fields but are not required for manual work.

## Goals / Non-Goals

**Goals:**

- Present one Concept surface with Base idea, Initialize, the three editable triangle fields, per-corner actions/instructions, completeness, and history.
- Preserve the existing Concept data model and AI contracts.
- Make manual edits in the visible triangle fields persist through the existing automatic-save path.
- Preserve read-only review behavior, AI-unavailable guidance, session history, rollback-and-save, and context-transition safety.
- Keep the SLL section below the Concept refinement content.

**Non-Goals:**

- No changes to the domain model, SQLite schema, AI providers, prompt contracts, or Design Triangle scoring.
- No removal of Graphic direction from the Design Triangle or persisted Concept data.
- No new Concept version persistence; refinement history remains session-scoped except for the selected rollback state being saved as current Item data.
- No changes to the Idea-stage editor or the workflow-stage navigator.

## Decisions

### One continuous Concept editor

Remove the upper Concept idea, Phrase, and Graphic direction editors and the `Refine with AI` heading/separator. Keep the refinement rows as the sole editable Concept fields. This removes duplicate controls while preserving all current functionality.

The Concept card will show `Base idea` bound to `ItemInspector.Idea`. It will be read-only regardless of current stage editability, because the Concept surface must not provide a second owner for Idea-stage content. Read-only is preferred over disabled so users can still select and copy the original idea.

### Refinement fields become the manual editing path

The three visible refinement inputs remain session working values for AI operations, but their committed values must be copied to the inspector and saved when the user leaves a field or when a context transition requires pending edits to be committed. The existing inspector automatic-save path remains the persistence boundary, validation boundary, and source of saved-state notifications.

This preserves the existing history model: a successful manual commit is observed by the refinement session and creates one manual history entry; AI operations continue to apply through the inspector and create one labeled AI history entry; selecting history continues to restore the selected triangle and call `CommitEditsAsync`.

### Keep AI enhancement visible but optional

Initialize, Fine tune, Change, and Instructions remain visible in the Concept surface. Their current availability and disabled-reason behavior is retained. When AI is unavailable, the three fields remain editable and the AI commands remain visible but disabled with guidance.

### Commit and transition behavior

A refinement-field edit is pending when its local input differs from the corresponding inspector draft. Field exit commits the local value into the inspector draft and invokes the existing commit drain. Before changing Item, tab, active stage, lifecycle state, or closing the document, the view model must flush pending local triangle values before allowing the transition. Failed validation or persistence keeps the draft and current context available with the existing inline error behavior.

Phrase normalization remains enforced by the existing stage-aware save path. Base idea is never included in Concept commits.

## Risks / Trade-offs

- **[Risk] Local working values and inspector drafts can diverge during editing.** → Centralize copy/commit logic and test field exit plus context transitions for each triangle corner.
- **[Risk] A transition could discard a local refinement edit.** → Flush all pending refinement inputs before transition and add headless tests for item, tab, stage, and close paths where the existing transition hooks permit deterministic testing.
- **[Risk] Removing the upper fields could accidentally remove manual editing for users without AI.** → Keep refinement inputs editable when `CanEditStage` is true and add a test proving edits persist with AI unavailable.
- **[Risk] History selection could create duplicate history entries.** → Keep rollback guarded by the existing rollback flag and verify that selecting a history entry saves the selected state without appending a manual-edit entry.
- **[Risk] Existing tests may identify the first Concept field by accessible name.** → Update selectors and add explicit assertions that the Base idea is read-only and the refinement inputs are the only editable Concept triangle controls.

## Migration Plan

No data migration is required. Existing Concept idea, Phrase, Graphic direction, and session history data remain compatible. The implementation is a UI/application behavior replacement and can be rolled back by restoring the prior Concept surface and refinement-field wiring.

## Open Questions

None for this module. Base idea is treated as read-only context, and “remove Graphic” is interpreted as removing only the duplicate upper field, not the Graphic direction triangle value.

## Implementation Plan

1. Update `src/FusionCanvas.App/Views/MainWindow.axaml` to replace the upper Concept editors with a read-only Base idea field, remove the refinement heading/separator, and retain the existing refinement rows, score, history, and SLL ordering.
2. Update `ConceptRefinementSessionViewModel` and the relevant `MainWindow` transition/focus hooks so local refinement inputs copy into `ItemInspectorViewModel` and commit through `CommitEditsAsync` on field exit and context transitions without affecting instruction fields.
3. Preserve the existing AI operation and rollback paths; audit guards so manual commits do not create duplicate history entries and rollback remains a save operation.
4. Update or add `ConceptRefinementViewTests` and session view-model tests for layout, read-only Base idea, manual editing without AI, field-exit persistence, transition flushing, read-only review, and rollback-and-save.
5. Run strict OpenSpec validation and the full `dotnet test .\FusionCanvas.sln` baseline.

## Acceptance Verification Plan

| Acceptance area | Planned evidence |
| --- | --- |
| Single Concept surface and Base idea context | Avalonia headless view test inspecting visible controls, bindings, and read-only state |
| Manual triangle editing without AI | Application/view-model test with disabled AI availability and isolated persistence stub |
| AI actions and instruction fields remain available | Existing focused concept refinement tests plus headless accessibility/control-state assertions |
| History rollback persists selected triangle | Session view-model test asserting inspector values and persistence call after selection |
| Read-only review and transition failure behavior | Existing regression tests plus focused transition/commit tests |
| No data/model regression | Full solution test baseline and OpenSpec validation |
