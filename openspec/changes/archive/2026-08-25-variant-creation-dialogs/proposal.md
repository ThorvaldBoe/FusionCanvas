# Open Add Variant and Bulk add in focused dialogs

## Origin

- GitHub issue: #196 — [https://github.com/ThorvaldBoe/FusionCanvas/issues/196](https://github.com/ThorvaldBoe/FusionCanvas/issues/196)

## Outcome

Move Variant creation out of the parent Variants screen and into two focused modal dialogs. **Add Variant** opens a dialog for creating one concrete sellable combination; **Bulk add** opens a dialog for selecting and generating multiple valid combinations. The parent Sellable Variants region no longer renders either creation form inline; successful creation closes the dialog and refreshes the Variant count and list while preserving the active Offering; cancel/close discards the in-progress draft and creates nothing; focus returns to the action that opened the dialog; and only one creation dialog may be open at a time. Blueprint Offering or workspace changes close any open creation dialog so it cannot edit stale context.

## Included features

- Remove the inline individual Variant creation editor from the Sellable Variants region.
- Remove the inline bulk Variant creation editor from the Sellable Variants region.
- **Add Variant** opens a modal dialog owned by the Store Editor window, titled "Add Variant", scoped to the active Offering by stable identity and using only its current Option Values.
- **Bulk add** opens a modal dialog owned by the Store Editor window, titled "Bulk add", scoped to the active Offering by stable identity.
- Both dialogs reuse the existing creation, validation, duplicate, cross-Offering, incomplete-combination, and persistence semantics without duplicating domain or application logic.
- Successful completion closes the dialog and refreshes the Variant count and list while preserving the active Offering.
- Cancel, close, and Escape discard the in-progress dialog draft and create no Variant.
- Keyboard focus returns to the action that opened the dialog after it closes.
- Only one creation dialog may be open at a time.
- A Blueprint Offering or workspace switch closes an open creation dialog so it cannot edit stale data.
- Bulk: a clear pre-confirmation summary of the combinations to create is shown before confirmation; existing safeguards against duplicates and unsupported combinations are kept; partial failure provides recoverable, specific guidance and leaves confirmed data consistent.

## Non-goals

- #194 (Option values dialog) is already delivered in this branch; the Available choices section and its **Manage values** dialog are not restructured.
- No new domain rules, application services, persistence changes, Option kinds, or bulk-combination algorithms. The existing `OfferingManagementService` bulk preview/confirm and `CatalogSetupService` single-Variant creation paths are reused unchanged.
- No pixel-perfect visual regression baseline.
- No live desktop UI pass is required; deterministic headless and framework-free tests are the completion gate.
- The low-fidelity `manage-variants.ui.yaml` wireframe is not restructured; it already shows only the action buttons and Variant table with no inline creation editor, so no content needs removal.

## Dependencies

- Builds on the accepted `variant-management` capability, including the choice-card, overflow-menu, and Option-value-dialog behavior from #192/#193/#194.
- Reuses the existing `CatalogSetupViewModel` Variant-creation state, commands, and the Store Editor dialog-host pattern introduced by #198 (Design Area archive confirmation) and refined by #194 (Option value management window).

## Risks

- **Modal focus and lifecycle correctness.** Each dialog must discard its in-progress draft on every close path (Save success, Cancel, Escape, system close, context switch) and return focus to the originating header action. Avalonia modal `ShowDialog`, `Opened`, and a session-flag observer are used to guarantee this deterministically, mirroring the proven #194 pattern.
- **Stale-context editing.** A programmatic Blueprint Offering or workspace switch while a creation dialog is open must not leave the dialog editing a stale Offering. The view model resets the creation session when the Offering context changes, and the host observes the session flag to close the dialog.
- **Test migration.** One existing headless test asserts the inline bulk editor and its named controls. It is rewritten to assert dialog open/close, title, focus, draft discard, and parent-screen disclosure at the same reliable layer.

## Verification approach

- Framework-free view-model tests for: the request events fire on the add/bulk actions; the creation session opens and closes; an Offering switch closes the session and discards the draft; mutual exclusivity of the individual and bulk sessions is preserved.
- Avalonia headless view tests for: the parent screen renders no inline creation editor; **Add Variant** opens a single owned modal dialog titled "Add Variant"; **Bulk add** opens a single owned modal dialog titled "Bulk add"; each dialog is scoped to the active Offering by stable identity; Cancel/Close/Escape discards the draft and returns focus to the opening action; successful creation closes the dialog and refreshes the list; only one creation dialog at a time; an Offering switch closes an open dialog; the bulk dialog shows a pre-confirmation summary before the confirm action.
- Strict OpenSpec validation of the change and, after sync, the main `variant-management` spec.
- Solution-level `dotnet test .\FusionCanvas.sln` baseline with zero failures.

## Scope rationale

This is one cohesive, independently verifiable outcome: Variant creation becomes focused dialogs instead of inline regions that compete with the Variant list. It touches one capability (`variant-management`), one view-model, one host window, two new dialog windows, and the tests that assert that surface. It does not introduce new domain behavior, so it is small and reviewable as a single module.
