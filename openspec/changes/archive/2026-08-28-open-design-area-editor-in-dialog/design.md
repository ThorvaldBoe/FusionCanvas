## Context

`StoreEditorWindow.axaml` currently renders `Catalog.DesignAreaEditor` beside `Catalog.DesignAreaList`. `CatalogSetupViewModel` already owns the complete Design Area draft, validation, compatibility choices, create/update path, error preservation, and reset behavior. The App layer also has established owner-modal patterns for Option Value and Variant workflows.

The review workflow is frequent and collection-focused; Add/Edit is occasional, multi-field configuration. The parent collection should consume the management surface, while a modal preserves Offering context and prevents concurrent interaction. Add, edit, validation failure, persistence failure, success, unchanged cancel, meaningful cancel, read-only, and stale-context states are all resolved here.

## Goals / Non-Goals

**Goals:**

- Move the existing complete form into one accessible, resizable, scrollable modal dialog.
- Keep the parent list full-width and free of hidden/reserved editor layout.
- Make dialog lifecycle deterministic for success, failure, cancellation, focus return, and context changes.
- Add meaningful-draft protection without changing service behavior.

**Non-Goals:**

- Redesigning form fields, validation rules, compatibility semantics, archive behavior, or data storage.
- Adding provider synchronization or changing archived-store permissions.
- Introducing a generic dialog framework.

## Decisions

1. **Reuse `CatalogSetupViewModel`; do not duplicate an editor view model.** Existing draft and service orchestration remain the single presentation source. Small dialog-specific properties/events belong there because they coordinate presentation state only.
2. **Open through an explicit `DesignAreaEditorRequested` event.** `BeginNewDesignArea` and `BeginEditDesignArea` prepare the draft and then request the owner modal. `StoreEditorWindow` enforces one dialog at a time and owns focus restoration.
3. **Close on `IsAddingPlaceholder == false`.** Successful save, confirmed cancel, and context reset already converge on draft reset. The dialog observes that state and closes once; failed save leaves it true and therefore remains open.
4. **Track a value snapshot for meaningful changes.** An App-only immutable snapshot covers all text fields, all-variants state, and sorted selected Variant identities. Add baseline is the initialized empty/default draft; Edit baseline is the loaded record. This avoids prompting merely because the dialog is open.
5. **Use an in-dialog discard prompt.** `RequestCancelDesignAreaCommand` shows a focused prompt only when the current snapshot differs. Confirm resets the draft; keep-editing dismisses the prompt and preserves values. Window close and Escape route through the same request, preventing bypass.
6. **Use a resizable window with a ScrollViewer.** Normal width presents the complete form comfortably; minimum dimensions remain usable because content scrolls rather than clipping. The Name field receives initial focus.
7. **Restore focus from the owner.** The request handler captures Add versus the edited stable identity. After close, it focuses the original Add button or the matching row Edit button if still present, falling back to Add.

## Risks / Trade-offs

- [Parent navigation may reset the draft while the modal is active] → observe draft state and close; do not persist or retarget the stale draft.
- [Closing interception can recurse] → use one internal allow-close flag set only after the view model has ended the draft.
- [Compatibility selection dirtiness can be missed] → include sorted selected Variant IDs in the draft snapshot and notify on choice changes.
- [Large forms may exceed narrow height] → place the body in a ScrollViewer and keep action/prompt controls reachable.
- [Existing headless tests assume inline controls] → update those assertions and add explicit owner-modal tests rather than weakening coverage.

## Migration Plan

No data migration is required. The change is App presentation-only. Reverting the new window/event wiring and restoring the inline XAML returns the prior layout without data impact.

## Open Questions

None. Exact dialog titles, focus, dismissal, and state behavior are resolved by the issue and project UX guidelines.

## Implementation Plan

1. Extend `CatalogSetupViewModel` with dialog title/mode, request event, meaningful-draft snapshot, discard-prompt state, and request/confirm/keep-editing commands. Raise appropriate property/command notifications from every Design Area field and compatibility choice.
2. Add `DesignAreaEditorWindow.axaml` and code-behind in the App Stores capability. Move the existing form markup intact, add automation IDs/names, scrolling/minimum sizing, initial Name focus, Escape/close interception, and close-on-ended-draft observation.
3. Update `StoreEditorWindow.axaml.cs` to subscribe to the request, show at most one owner-modal dialog, close it on context reset, and restore focus to Add or the matching Edit row.
4. Replace the Design Area management two-column grid in `StoreEditorWindow.axaml` with the existing list panel at full width; retain archive actions and current card behavior.
5. Reconcile `docs/Visuals/ui-descriptions/manage-design-areas.ui.yaml` so default state contains the collection and action while Add/Edit states describe the focused dialog.
6. Add/adjust framework-free `CatalogSetupViewModelTests` for mode, baseline dirtiness, failed-save preservation, cancel prompt, and context reset. Add Avalonia headless tests for list-only default, add/edit modal, populated binding, initial/focus return, meaningful close prompt, successful save closure, modal ownership, keyboard close, and narrow sizing.
7. Run focused tests, the full solution baseline, and strict OpenSpec validations. Record every scenario in `verification.md`.

## Acceptance-to-Verification Mapping

| Scenario | Verification |
| --- | --- |
| User reviews the Design Area collection | Headless test asserts full-width list automation region and absence of inline editor. |
| User adds a Design Area | Headless owner-modal test asserts Add title, defaults, ownership, parent disabled state, and Name focus. |
| User edits a Design Area | ViewModel plus headless test asserts stable-identity populated fields and Edit title. |
| Save fails validation or persistence | ViewModel test uses deterministic invalid/failing collaborator; headless test asserts dialog/draft remain. |
| Save succeeds | ViewModel/headless test invokes Save and asserts one record mutation, closure, selection, and focus return. |
| User dismisses an unchanged draft | Headless Cancel/Escape tests assert direct close, no mutation, and focus return. |
| User dismisses a meaningful draft | ViewModel/headless tests assert prompt, keep-editing preservation, and confirmed discard. |
| Editing context becomes stale | Headless test changes Offering/workspace state and asserts closure/no cross-context mutation. |
| Dialog is used with keyboard and supported sizes | Headless construction/layout/focus test at normal and minimum dimensions; no live desktop check required. |

