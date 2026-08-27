# Manage Option values in a focused dialog

## Origin

- GitHub issue: #194 — [https://github.com/ThorvaldBoe/FusionCanvas/issues/194](https://github.com/ThorvaldBoe/FusionCanvas/issues/194)

## Outcome

Move Option value management out of the parent Variants screen and into a focused modal dialog. Selecting **Manage values** on any Option card opens a single dialog scoped to that Option's stable identity, exposing the existing add-value and archive-value capabilities. The parent Variants screen keeps its Available choices and Sellable Variants hierarchy uninterrupted, and the dialog returns focus to the originating **Manage values** control when it closes.

## Included features

- Remove the inline Option value editor from the Manage Variants page.
- **Manage values** opens a modal dialog owned by the Store Editor window.
- The dialog title identifies the Option: "Manage &lt;Option name&gt; values".
- The dialog shows only the selected Option's values and reuses the existing add-value, archive-value, validation, dependency, error, and persistence semantics without duplicating domain or application logic.
- Only one value-management dialog may be open at a time.
- Explicit finish (Done) closes the dialog; Cancel/Close/Escape discards any unfinished add-value draft without persisting.
- Keyboard focus returns to the originating **Manage values** control after the dialog closes.
- Switching Blueprint Offerings or workspaces closes the dialog so it cannot edit stale data.
- Successful add or archive operations refresh the Option card value summary and affected Variant state through the existing refresh paths.
- Custom Option kinds (not only Color and Size) are supported data-driven, with no hard-coded screens.

## Non-goals

- #195 refines the Archive action styling inside this dialog. The existing archive-value command and label styling remain unchanged here; #195 will restyle it later.
- #196 restructures the Sellable Variants / Add Variant / Bulk add region. That region is left intact and not restructured by this change.
- No new domain rules, application services, persistence changes, or new Option kinds.
- No pixel-perfect visual regression baseline.
- No live desktop UI pass is required; deterministic headless and framework-free tests are the completion gate.

## Dependencies

- Builds on the accepted `variant-management` capability, including the choice-card and overflow-menu behavior from #192/#193.
- Reuses the existing `CatalogSetupViewModel` value-management state, commands, and the Store Editor dialog-host pattern introduced by #198 (Design Area archive confirmation window).

## Risks

- **Modal focus and lifecycle correctness.** The dialog must discard unfinished add-value drafts on any close path (Done, Cancel, Escape, system close) and return focus to the originating control. Avalonia modal `ShowDialog` and `Opened`/`Closing` events are used to guarantee this deterministically.
- **Stale-context editing.** A programmatic Blueprint Offering or workspace switch while the dialog is open must not leave the dialog editing a stale Option. The view model closes the management session when the Offering context changes, and the host observes the session flag to close the dialog.
- **Test migration.** One existing headless test asserts the inline editor. It is rewritten to assert dialog open/close, title, focus, and draft discard at the same reliable layer.

## Verification approach

- Framework-free view-model tests for: dialog title derives from the selected Option; the management session opens/closes; an Offering switch closes the session; close discards an unfinished add-value draft.
- Avalonia headless view tests for: the inline editor is gone; **Manage values** opens a single owned modal dialog; the title identifies the Option; Done focuses and closes; Escape closes and discards the draft; focus returns to the originating **Manage values** control; only one dialog at a time; custom Option kinds open the same dialog.
- Strict OpenSpec validation of the change and, after sync, the main `variant-management` spec.
- Solution-level `dotnet test .\FusionCanvas.sln` baseline with zero failures.

## Scope rationale

This is one cohesive, independently verifiable outcome: value management becomes a focused dialog instead of an inline region. It touches one capability (`variant-management`), one view-model, one host window, one new dialog window, and the tests that assert that surface. It does not introduce new domain behavior, so it is small and reviewable as a single module.
