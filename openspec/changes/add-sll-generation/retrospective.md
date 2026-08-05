# Add SLL Generation Retrospective

## Outcome

The Concept stage now ships an SLL generation section below "Refine with AI": it becomes available when the Design Triangle is complete (deterministic score 100) and SLL-purpose AI is available, generates a full minimal SLL (assumptions, intent, triangle, ASCII sketch, notes, validation), persists it with the item through the Concept-stage automatic-save path, supports generate + regenerate (replace), and has a fresh `AiRequestPurpose.Sll` with its own profile/availability/settings editor.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Adding an `Sll` field and calling `CommitEditsAsync` would persist the SLL | Spec review (SR-001): `HasCurrentStageDraftChanges` only tracked the three triangle corners, so an SLL-only change would silently no-op | Extend the Concept dirty-check to include `Sll != _originalSll`; add `_originalSll` baseline tracking and payload threading | Implementation defect / missing requirement | Reusable scope | Design D2 + tasks 4.3 |
| SLL document model could live in Application | Spec review (SR-002): validation is a domain invariant; layer must not be deferred | Place `SllDocument` + `Validate` in `Domain/Concepts/` | Architecture | Change-specific | Design plan step 1 |
| Persisted model mirrors the framework's full §6 fields | Spec review (SR-003): D1 listed 10 fields but D5 parses 6 blocks | Trim persisted model to the 6 parsed blocks; defer richer fields | Missing requirement | Change-specific | Design D1 |
| Unlabelled phrase mutation handled as soft assertion | Spec review (SR-004): SHALL-preserve-phrase is a hard gate | Treat unlabelled mutation as an invalid-response failure; add `REVISED PHRASE:` marker path | Missing requirement | Reusable scope | Design D5; spec scenario 2c |
| Adding an AI purpose needs no config migration | Spec review (SR-005): pre-`Sll` settings files would deserialize `Sll=null` and NRE in the resolver | Null-coalesce `Sll` to `InheritGeneral` in `JsonApplicationSettingsStore.Normalize` | Missing requirement | Reusable scope | Design D3; task 2.2; scenario C1 |
| Stale SLL after a triangle edit unspecified | Spec review (SR-006): user-facing state coherence decision | Keep SLL displayed with a stale marker; Regenerate gated on completeness | Missing requirement | Change-specific | Design D8; spec scenario 3c |

## Deferred or Change-Specific Notes

- In-place editing of the sketch/notes and multiple persisted SLL variants are explicit non-goals (framework §7); a `SllGenerationService` block parser exists to be reused/extended by a future SLL editor.
- Task 7.4 (optional live desktop visual judgment of the rendered ASCII sketch) remains available as supplemental evidence only; it is not a module-completion gate.
- `SllGenerationService` reuses `ConceptRefinementTriangle` (Application.ConceptRefinement) and `IDesignTriangleGuidanceSource`; cross-capability namespaces accepted for this module (SR-007/SR-008).
