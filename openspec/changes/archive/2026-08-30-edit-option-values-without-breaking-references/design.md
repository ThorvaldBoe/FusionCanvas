## Context

The focused Option Value management dialog already owns add/archive flows for all Option kinds. `OfferingOptionValue` is an immutable record with a stable `Id`; variants and template relationships store that ID, so a rename can be an ordinary snapshot update without relationship migration. The application update boundary currently accepts an Option Value name but does not enforce duplicate validation, while the view model has only add-value draft state.

## Goals / Non-Goals

**Goals:**

- Add focused, keyboard-accessible in-place renaming for active values.
- Apply trimmed, non-blank, same-Option active duplicate validation consistently to create and rename.
- Preserve IDs and all relationship collections, refresh the parent and dependent presentations, and keep cancellation non-mutating.
- Keep the existing modal surface and progressive disclosure pattern.

**Non-Goals:**

- Changing Option kind, offering ownership, archive/dependency rules, or relationship schemas.
- Renaming archived values or adding a separate editor window.

## Decisions

- **Reuse `UpdateCatalogRecordRequest` for renames.** The existing catalog boundary already represents an Option Value update and returns a refreshed state. A new service abstraction would duplicate the established mutation path.
- **Validate in `CatalogSetupService`.** The application owns store scope, active-record checks, normalization, and persistence orchestration. The domain record continues to enforce text validity at construction.
- **Use one edit draft in the existing dialog.** The row's Edit command sets the selected value and draft text; Save calls the existing update command path. Cancel clears only transient state. This keeps the dialog focused and supports Color/Size/custom kinds uniformly.
- **Use stable IDs for all updates.** The update maps only the matching Option Value's `Value` field, leaving its ID, OptionId, OfferingId, sort order, archive flag, and every relationship collection untouched.
- **Keep the current focused modal.** Edit is occasional setup work, so it remains progressively disclosed inside Option Value Management rather than consuming the parent workspace.

## Risks / Trade-offs

- [Risk] A concurrent reload could make an edit target stale → scope the command to the current active offering/option and refresh state after every successful mutation.
- [Risk] Duplicate checks could diverge between create and edit → centralize the normalized active-value comparison in the catalog service and use it for both operations.
- [Risk] A close/Escape path could leave draft state behind → reset edit state in the same dialog-close/reset path used for add drafts.

## Migration Plan

No database migration is required. Existing rows and relationship tables remain valid. Rollback is a code rollback; renamed display text is ordinary persisted data and is not automatically reverted.

## Open Questions

None. The issue defines the validation, identity, refresh, and cancellation behavior required for implementation.

## Implementation Plan

1. Update `ICatalogSetupService`/catalog contracts only as needed and modify `CatalogSetupService` so Option Value create and update trim required names and reject active normalized duplicates within the same Option, excluding the record being edited.
2. Extend `CatalogSetupViewModel` with edit draft state, edit/save/cancel commands, and reset/property notification behavior. Route successful edits through `UpdateAsync`; preserve existing mutation refresh behavior.
3. Add an accessible Edit action and compact edit form to `OptionValueManagementWindow.axaml`, with code-behind focus behavior matching the existing add editor and Escape/close reset semantics.
4. Add application tests for valid rename, duplicate/blank rejection, same-ID/reference preservation, and create/edit Color/Size parity; add app tests for command state, draft cancellation, and dialog presentation bindings where framework behavior is material.
5. Run focused tests, the full `dotnet test .\\FusionCanvas.sln` baseline, and `openspec validate`; record criterion-level evidence in `verification.md`.

## Acceptance Verification Map

- Edit a Color or Size value: application service test plus App view-model/dialog test.
- Invalid or duplicate rename: application service tests for blank and normalized duplicate input.
- Reference preservation: application/integration snapshot test asserting unchanged IDs and relationship collections.
- Cancel/close/Escape: view-model test and deterministic Avalonia headless dialog test if existing harness supports construction and key input.
