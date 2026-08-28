## Context

`StoreEditorWindow.axaml` currently renders `Catalog.MockupTemplateEditor` beside `Catalog.MockupTemplateList`. `CatalogSetupViewModel` already owns the full template draft, provider candidates, placement mapping, validation, Color/Design Area selection, create/update and revision service path, error preservation, and resets. The App layer already uses Store Editor-owned modal windows for focused catalog tasks.

The frequent workflow is reviewing Offering-scoped template summaries. Add/Edit is an occasional, spatially demanding task whose provider preview and placement mapping benefit from a dedicated resizable window. Empty, Add, Edit, unavailable image, invalid/failing save, success, unchanged cancel, meaningful cancel, archived/read-only, and stale-context states are resolved.

## Goals / Non-Goals

**Goals:**

- Move the complete existing preview-first form into one accessible, resizable, scrollable modal.
- Keep the parent collection full-width and free of hidden/reserved editor layout.
- Make dialog lifecycle deterministic for success, failure, cancellation, stale context, and focus return.
- Guard meaningful drafts without changing template service behavior or placement semantics.

**Non-Goals:**

- Adding provider sync, upload, drag/drop image import, rendering, or new placement algorithms.
- Changing template identity, Color applicability, Design Area compatibility, revisions, archive behavior, or persistence.
- Introducing a generic dialog framework or changing archived-store permissions.

## Decisions

1. **Reuse `CatalogSetupViewModel`.** The existing draft and orchestration remain the single presentation source. Dialog-specific mode/title, request, dirtiness, and confirmation state remain App presentation concerns.
2. **Request the owner modal after draft preparation.** `BeginNewTemplate` and `BeginEditTemplate` prepare a stable draft and raise `MockupTemplateEditorRequested`. `StoreEditorWindow` enforces one active dialog.
3. **Close when `IsAddingTemplate` becomes false.** Successful save, confirmed cancel, and context reset converge on the existing draft reset. Invalid or failed save keeps the flag true, so the dialog and draft remain.
4. **Snapshot every meaningful field.** The immutable baseline includes name, selected provider candidate/reference, Design Area identity, selected Color IDs, mapping coordinates/dimensions/image dimensions, and edited template identity. Sorted identities prevent order-only prompts.
5. **Use an in-dialog discard prompt.** Cancel, Escape, and window close route through one request. Keep editing preserves the draft; confirmed discard uses the normal reset path. This is deterministic and avoids a second nested native modal.
6. **Preserve preview-first composition inside a resizable, scrollable dialog.** Normal width retains preview and configuration peer regions. A narrow layout remains reachable by scrolling; the first template-name control receives initial focus.
7. **Restore owner focus by stable identity.** The owner captures Add versus Edit and returns focus to the original Add control or matching template Edit control, falling back to Add if the record changed.
8. **Honor read-only state at both entry and form controls.** Existing command `CanExecute` prevents opening Add/Edit for archived Stores, and form/placement controls retain `CanEdit` bindings.

## Risks / Trade-offs

- [Provider candidates load asynchronously after the draft opens] → retain current load path and snapshot stable selected references; loading alone does not count as a user edit.
- [Context reset can occur while the modal is active] → observe `IsAddingTemplate`, close once, and never retarget the draft.
- [Close interception can recurse] → use an internal allow-close flag after the view model ends the draft.
- [Large preview/configuration can exceed narrow dimensions] → use a ScrollViewer and minimum dimensions while preserving all required controls.
- [Existing tests and deterministic UI descriptions assume master-detail] → update their semantic/layout expectations and renderings with the implementation.

## Migration Plan

No data migration is required. This is App presentation state only. Reverting the window/event wiring and restoring the inline region returns the previous UI without affecting saved templates or revisions.

## Open Questions

None. Dialog titles, focus, cancellation, read-only behavior, and retained preview-first composition are resolved by the issue and repository UX guidance.

## Implementation Plan

1. Extend `CatalogSetupViewModel.cs` with template dialog mode/title, request event, immutable meaningful-draft baseline, discard-prompt state, and request/confirm/keep commands. Notify dirtiness for every template field, provider/Design Area/Color selection, and placement change without treating asynchronous candidate hydration as a user edit.
2. Add `MockupTemplateEditorWindow.axaml` and code-behind under App Stores. Move the complete editor, retain `MockupPlacementEditor`, accessibility identities and `CanEdit` bindings, add scrolling/minimum sizing, focus, Escape/close interception, and close-on-ended-draft observation.
3. Update `StoreEditorWindow.axaml.cs` to subscribe to the request, show at most one owner-modal template window, clean an unfinished draft after external closure, and restore Add or stable-row Edit focus.
4. Replace the two-column template management Grid in `StoreEditorWindow.axaml` with the full-width existing collection and one named Add action. Retain cards, archive actions, prerequisites, and empty state.
5. Reconcile `manage-mockup-templates.ui.yaml`, semantic tests, and any generated fixtures with collection-only default plus focused Add/Edit dialog states.
6. Extend `CatalogSetupViewModelTests.cs` and `StoreEditorHeadlessTests.cs` for modes, baselines, prompt outcomes, invalid-save preservation, context reset, read-only entry, modal ownership, bindings, preview mapping, focus, keyboard close, and supported sizes.
7. Run focused tests, full baseline, strict OpenSpec validation, and criterion-level verification. Do not change Domain/Application/Integration behavior or reopen provider-sync/image-source decisions.

## Acceptance-to-Verification Mapping

| Scenario | Verification |
| --- | --- |
| User reviews the Mockup Template collection | Headless and UI-description tests assert list-only full-width layout and one gated Add action. |
| User adds a Mockup Template | Headless owner-modal test asserts Add title, fresh draft, ownership, parent disabled state, and initial focus. |
| User edits a Mockup Template | ViewModel/headless tests assert stable-identity populated fields, placement, selections, and Edit title. |
| Preview-first mapping remains available | Headless test exercises the placement editor and synchronized numeric fields at normal/narrow dimensions. |
| Save fails validation or persistence | Deterministic ViewModel test asserts the active dialog draft/revisions remain unchanged. |
| Save succeeds | ViewModel/headless test asserts one service mutation/revision, close, selected refresh, and focus restoration. |
| User dismisses an unchanged draft | Headless Cancel/Escape test asserts immediate close with no mutation. |
| User dismisses a meaningful draft | ViewModel/headless test asserts prompt, keep preservation, and confirmed discard. |
| Editing context becomes stale | ViewModel/headless test resets Offering/workspace and asserts close/no retargeting. |
| Archived store is reviewed | ViewModel/headless test asserts entry and editing commands/controls are disabled. |
| Dialog is used with keyboard and supported sizes | Headless layout/focus/close test; live desktop review is optional supplemental evidence. |
