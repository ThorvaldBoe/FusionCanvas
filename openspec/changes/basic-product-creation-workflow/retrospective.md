# Basic Product Creation Workflow Retrospective

## Outcome

The cross-layer `Listing` to `Item` rename, v4-to-v5 SQLite migration, stage-aware save contract, workflow policy, Design-file behavior, scrollable Avalonia Item document shell, built-in Stage Tools, real draft/status confirmations, and cross-context synchronization are implemented. Deterministic and real-desktop evidence is recorded in `verification.md`.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| App `SetItemStatusCommand` would surface a confirmation dialog for protected transitions | The interim scaffold bypassed the UI by passing `ConfirmProtectedTransition: true` | Replaced the bypass with policy-sourced confirmation state and explicit Confirm/Cancel commands; only the confirmed continuation passes `true`, and cancel retains the authoritative status | Implementation defect (interim resolved) | Change-specific | Phase 6 task 6.9 / 6.10 |
| Blank working title was rejected by the old inspector | The spec makes the working title optional for ID-only Items | `ItemMetadataCodec.ValidateName` now allows empty; the inspector test asserts blank succeeds and only multi-line is rejected | Missing requirement / spec-driven | Change-specific | None |
| Stage movement allowed arbitrary jumps; status allowed Draft-to-Paused | The spec requires adjacent-only movement and forbids Draft-to-Paused | `MoveItemStageAsync` and `SetItemStatusAsync` now delegate to `ItemWorkflowPolicy`; affected App/Application tests use allowed transitions | Spec-driven | Change-specific | None |
| A stage-aware save applied all creative metadata keys | Saving Idea could erase existing Concept/Phrase/Graphics metadata because null values from inactive fields were interpreted as removals | `ApplyStagePayload` now mutates only keys owned by the selected current stage; a focused preservation test locks the behavior | Implementation defect | Change-specific | No canonical guidance needed; the delta spec already requires preservation |
| Concept tool state omitted the Concept idea field | The accepted Concept payload contains Concept idea, Phrase, and Graphics description | Added `ConceptIdea` to inspector creative state, Concept tool load/save bindings, and focused tests | Implementation defect | Change-specific | None |

## Deferred or Change-Specific Notes

- `WorkflowStage.Listing`, the Listing stage tool/descriptor, marketplace-facing Listing language, and historical v4 `listings`/`listing_tags` SQL are intentional exceptions to the Item terminology migration.
- Lowercase `listing` locals that remain inside historical migration fixtures are test-fixture vocabulary, not public universal contracts.
- No missing product, UX, data, architecture, or acceptance decision was discovered during the XAML rewrite. The corrections above reconcile implementation to already-approved artifacts; no delta-spec correction was required.
- Later marketplace publishing, marketplace-specific Listing records, mockup generation, and exhaustive desktop transition combinations remain deliberately outside this module.
