## Context

Mockup Templates are persisted as a root template plus active Color bindings, immutable revisions, current local source-image entries, applicability rows, and revision source-image snapshots. The focused `CatalogSetupViewModel` already knows how to edit a template and its local images, but the only creation path starts with an empty draft. Duplication should feed that existing workflow without introducing a second editor or copying mutable state by reference.

## Goals / Non-Goals

**Goals:**

- Make duplication available from the existing Mockup Template management surface.
- Produce a complete current-configuration copy with new mutable identities and shared immutable Asset identities.
- Open the result as an unsaved editable draft so the creator can replace images, rename it, or adjust metadata before saving.
- Preserve revision and readiness isolation between source and duplicate.
- Keep the operation atomic at the workspace snapshot boundary and compatible with existing SQLite persistence.

**Non-Goals:**

- Cross-Store or cross-Offering template copying.
- Duplicating archived templates, archived source-image entries, or historical revisions as editable history.
- Physical file copies for immutable source Assets.
- A new duplicate-specific dialog, image editor, or template schema.
- Changing source-image applicability, readiness, or revision rules.

## Decisions

1. **Duplicate in the application service.** Add a focused operation to `IMockupTemplateSetupService` and implement it beside existing create/update/archive operations. This keeps Store ownership, active-record validation, ID generation, and atomic repository persistence out of the UI. A UI-only composition of create plus many updates would expose partial-save failures and would not preserve the source snapshot reliably.

2. **Copy the current configuration only.** Read the source template's current revision and active source-image rows/conditions. Create one revision numbered 1 for the duplicate, containing the copied provider configuration and current active Colors plus copied source-image snapshot rows. Do not copy archived rows or historical revisions; the duplicate starts a new lifecycle while matching what the creator currently sees.

3. **Regenerate mutable identities, share Assets.** Create new IDs for the duplicate template, revision, Color bindings, source-image entries, applicability rows, and revision snapshot rows. Reuse each source entry's managed `SourceAssetId`; Assets are immutable and replacing a source image already creates a new Asset through the existing source-image service. This avoids unnecessary file duplication while preventing edits to one template's source-entry records from affecting the other.

4. **Use deterministic collision-safe naming.** Start with `Copy of {source.Name}` and compare active template names within the same offering. If occupied, try `(2)`, `(3)`, and so on. The service owns this because it must be deterministic across all callers and must use the same workspace snapshot as the insert.

5. **Open the duplicate through the existing focused editor.** Add a command next to each template card. The command calls duplication, applies the returned setup state, selects the returned template, and invokes the existing edit flow. The editor's existing draft baseline/unsaved-change guard protects the new draft. Duplication itself persists the copied draft so source-image replacement can use existing update operations; the user still explicitly saves subsequent draft edits.

## Risks / Trade-offs

- [Risk] Sharing an Asset could be mistaken for sharing editable image state → Mitigation: Assets and files are immutable; source-entry and revision identities are new, and tests prove replacement changes only the duplicate.
- [Risk] A duplicate of a ready template may be ready immediately, making it easy to confuse with the original → Mitigation: use a visible “Copy of …” name and open the duplicate selected in the editor; retain readiness evaluation from copied current configuration.
- [Risk] Source records may be inconsistent in old workspaces → Mitigation: copy only valid current active rows, validate all referenced records before mutation, and fail atomically with no partial snapshot.

## Migration Plan

No database migration is required. Existing tables and snapshot serialization already support all records. Older workspaces continue to load; the new operation is additive. Rollback is a code rollback and does not require data conversion because duplicates are ordinary templates.

## Open Questions

None. The module scope and identity/name rules are resolved for implementation.

## Implementation Plan

1. Application layer: add `DuplicateMockupTemplateRequest` and `DuplicateTemplateAsync` to the mockup setup contract. In `MockupTemplateSetupService`, validate Store/offering/template state, locate current revision and active Color/source rows, generate a collision-safe name, create new records, and save one updated `WorkspaceSnapshot`.
2. Application tests: extend `tests/FusionCanvas.Application.Tests/Mockups/MockupTemplateSetupServiceTests.cs` with configured deep-copy, name-collision, archived/missing rejection, and source-isolation cases using deterministic IDs and an in-memory repository.
3. App layer: add `DuplicateTemplateCommand` to `CatalogSetupViewModel`, expose it to the template-card action row, and on success apply state, select the returned template, and start the existing edit draft. Keep the command unavailable for read-only/busy states.
4. UI tests: extend the existing catalog/view-model tests and, if the card action's routed binding is exercised by the current headless harness, assert the duplicate automation name/command path. Static markup-only assertions are not required.
5. Verify with focused application/UI tests, the full `dotnet test .\FusionCanvas.sln` baseline, `openspec validate`, and criterion-level evidence in `verification.md`.

## Acceptance-to-Verification Plan

| Scenario | Planned verification |
| --- | --- |
| Configured template duplication | Deterministic application service test asserting copied values, new IDs, and unchanged source snapshot |
| Independent mutation | Application test using source-image update/archive against duplicate and comparing original records/revisions |
| Editable draft opening | View-model test asserting returned template selection, draft state, and editor-request path; focused headless test if binding behavior is exercised |
| Archived Store rejection | Application test plus command `CanExecute`/read-only state test |
| Missing/out-of-scope source rejection | Application service test asserting failure and unchanged snapshot |
| Collision-safe names | Application service test with existing copy names and exact generated name |
