## Context

`DesignStageService.BuildState` currently preserves a persisted listing configuration whose Offering is no longer selectable, appends the stale projection for review, and makes the whole Design state read-only. `MainWindow.axaml` binds both configuration selectors to `!DesignTool.IsReadOnly`, while `DesignStageToolViewModel.SelectedOffering` immediately calls `SelectConfigurationAsync`. The application therefore has no way to distinguish a prohibited ordinary edit from the one recovery mutation needed to leave stale state.

The existing configuration-switch use case already establishes the intended reset boundary: it replaces `ItemListingConfiguration`, removes Item-owned selected colors and Design rows plus their row-color and slot relationships, and clears saved artwork-target/transparency preferences. It does not delete managed Assets, Supporting Images, generic Item relationships, workflow/lifecycle data, or downstream Listing records. Recovery should reuse that boundary but must add a stale precondition, explicit confirmation, and atomic revalidation because the Design surface starts read-only and the candidate Offering may change concurrently.

This is an occasional exception workflow performed by a creator returning an Item from Listing to Design after its catalog Offering was archived or otherwise removed. It belongs beside Listing Configuration in the primary Design workspace because the stale relationship and its effect are local to that Item. The recovery controls consume no persistent space during normal Design work; the candidate selector, guidance, and inline confirmation are progressively disclosed only in stale state.

UX states are explicit:

- Normal/no configuration: retain the existing Design configuration experience.
- Stale with candidates: show the stale identity, read-only explanation, and active same-Store replacement choices; keep ordinary mutations disabled.
- Stale without candidates: show review-only content and guidance to create or restore an Offering in Store Editor.
- Pending confirmation: name old and new Offerings, summarize reset/preservation behavior, and expose keyboard-reachable Replace and Cancel actions.
- Busy: disable duplicate recovery submissions and candidate changes.
- Success: reload authoritative state, select the replacement, restore normal Design editability, and focus the configuration control.
- Cancellation: persist nothing and return focus to the recovery selector.
- Validation/persistence error: retain stale state, show an actionable message, refresh candidates when necessary, and return focus to the recovery selector or retry action.

There is no editable Design draft to reconcile while stale because all ordinary Design mutations remain unavailable. Recovery itself is destructive only to Offering-specific relationships, so it requires confirmation but does not delete managed files or downstream outputs.

## Goals / Non-Goals

**Goals:**

- Let an otherwise editable Design-stage Item replace a missing, archived, or inactive listing configuration with an active Blueprint Offering from the same Store.
- Keep the stale identity reviewable and all unrelated Design mutations blocked until recovery succeeds.
- Make the reset/preservation boundary explicit before confirmation and enforce it atomically in the Application layer.
- Rebuild Design state exclusively from the replacement Offering without inferred Placeholder, Variant, color, row, slot, or Asset mappings.
- Preserve creative history, managed files, Supporting Images, generic relationships, workflow/lifecycle state, and downstream Listing data.
- Cover application decisions at a framework-free layer and material binding/control/focus behavior with Avalonia headless tests.

**Non-Goals:**

- Restoring, creating, importing, or editing catalog records from the Design tool.
- Smart or manual mapping between old and replacement Placeholders, Variants, colors, rows, slots, or Assets.
- Reattaching preserved managed Design files automatically or adding a new Asset browser.
- Deleting or rewriting downstream Listing data, generated mockups, or other historical outputs.
- Declaring preserved downstream output compatible with the replacement Offering.
- Changing normal configuration selection, catalog archive/delete rules, provider synchronization, or persistence schema.

## Decisions

### 1. Model stale recovery separately from ordinary read-only state

Extend `DesignStageState` with explicit stale/recovery facts rather than making `IsReadOnly` ambiguous. The state should expose the persisted stale Offering ID and best available display identity, whether stale recovery is permitted, active same-Store recovery candidates, and no-candidate guidance. A missing Offering remains stale even when neither normalized nor compatibility projection can resolve its name; the UI uses a stable unavailable-label fallback rather than treating the Item as unconfigured.

Recovery is permitted only when the persisted Item configuration is stale and `ItemWorkflowPolicy` otherwise allows Design-stage mutation for an active Store at the Item's current Design stage. An archived Store, Published/Rejected/archived Item, earlier-stage review, or any independent edit guard keeps recovery disabled.

**Alternative considered:** clear the configuration automatically when its Offering disappears. Rejected because it destroys review context and bypasses the creator's decision about when and how to recover.

### 2. Use a dedicated confirmed application operation

Add `RecoverStaleConfigurationAsync(itemId, replacementOfferingId)` to `IDesignStageService`. The operation reloads authoritative state and validates, in order:

1. the Item and active Store exist;
2. normal Design-stage workflow policy permits mutation;
3. the Item still has a persisted configuration and it is still stale under the same selection policy used by `BuildState`;
4. the replacement is an active normalized Blueprint Offering in the same Store whose Blueprint is also active; and
5. the replacement still has the compatibility projection needed by the current Design surface.

Only after all validation passes does it construct one replacement snapshot and call the repository once. `SelectConfigurationAsync` may share a private snapshot-transformation helper, but recovery remains a separate public use case so ordinary selection cannot bypass the stale precondition or confirmation contract.

**Alternative considered:** enable the existing ComboBox and call `SelectConfigurationAsync` directly. Rejected because the binding persists on selection, cannot provide informed confirmation, and conflates recovery authority with normal editability.

### 3. Reset the established Offering-specific relationship set and preserve everything else

The atomic transformation replaces the Item's `ItemListingConfiguration`; removes all `DesignSelectedColors` for the Item; removes its `DesignVariantRows`, the corresponding `DesignVariantRowColors`, and all `DesignSlotAssignments` owned by those rows; and removes artwork target, target-preference, and transparency metadata keys from the Item while advancing its update timestamp.

All other snapshot collections remain byte-for-byte or value-equivalent except for the updated Item/configuration. In particular, the operation does not call `IWorkspaceFileStore` deletion APIs and does not remove Asset records/files, Supporting Image links, Idea/Concept/SLL metadata, Tags, prompts, generic relationships, stage/status/archive state, Listing fields, Listing assets, or generated mockup records. Preserved downstream records remain historical data; this module adds no compatibility or readiness claim for them.

**Alternative considered:** retain rows or slots when names or dimensions happen to match. Rejected because display labels and dimensions are not stable relationship keys, compatibility can differ by Variant, and silent mappings would hide production risk.

### 4. Keep confirmation inline and next to Listing Configuration

`DesignStageToolViewModel` owns separate recovery selection and pending-confirmation state; it must not reuse the auto-persisting `SelectedOffering` setter. In stale state, `MainWindow.axaml` shows the unavailable current configuration plus a `Replacement listing configuration` ComboBox backed only by recovery candidates. Choosing a candidate reveals a compact inline confirmation immediately below it with:

- the stale and replacement identities;
- the color/row/slot/artwork-preference reset list;
- the preserved creative, file, Supporting Image, and Listing-data summary;
- a warning that prior slot mappings and downstream outputs are not validated for the replacement; and
- `Replace configuration` and `Cancel` actions.

The confirmation is part of the existing vertically scrollable Design surface, not a new window. Code-behind remains limited to routed action and focus adaptation. On opening confirmation, focus moves to Replace; Cancel/Escape returns focus to the recovery ComboBox; success returns focus to the normal configuration selector; recoverable failure returns focus to the candidate/retry control. Accessible names identify the recovery selector and both actions.

**Alternative considered:** a modal dialog. Rejected because this is a single local transition with concise consequences, and an inline disclosure keeps the stale context visible without creating another window lifecycle.

### 5. Refresh authoritative state after every terminal recovery outcome

Success reloads the Design state and all open Item representations through the existing authoritative refresh path. A failure caused by concurrent catalog mutation reloads candidate availability without changing persisted Design state. Cancellation changes only presentation state. Busy state suppresses duplicate confirmation and candidate changes.

**Alternative considered:** optimistically rewrite the view model collections. Rejected because stale catalog state and snapshot-wide relationships require one authoritative source and existing project guidance prohibits stale contexts from overwriting confirmed state.

## Risks / Trade-offs

- **[Risk]** Resetting rows and slot assignments can surprise a creator who expects similarly named Placeholders to retain artwork. → The confirmation names every reset category, no mutation occurs on selection alone, and no matching is inferred.
- **[Risk]** Preserved managed slot assets may no longer have a slot link and the current Design tool has no asset-browser reattachment flow. → Keep the files and records rather than deleting creative work, state clearly that assignments are cleared, and leave reusable Asset browsing/remapping to a separate module.
- **[Risk]** Preserved Listing/mockup outputs may describe the old Offering. → Preserve them as required history, warn that compatibility is not validated, and do not mark them ready or regenerate them automatically.
- **[Risk]** An Offering can be archived or deleted between selection and confirmation. → Reload and revalidate inside `RecoverStaleConfigurationAsync`; on failure save nothing and refresh candidates.
- **[Risk]** Adding recovery exceptions can weaken ordinary read-only guards. → Expose a dedicated `CanRecoverStaleConfiguration` condition and method; leave every existing mutation guarded by `IsReadOnly`.
- **[Risk]** Missing legacy projections can make a stale Offering unnamed. → Preserve and display the persisted Offering ID with an unavailable fallback and allow replacement based on active normalized candidates.

## Migration Plan

No schema or file migration is required. Existing stale configurations become recoverable when the new application version loads them. Deployment consists of the state/service/UI changes and tests. Rollback restores the former review-only behavior; Items not yet recovered remain unchanged. A completed recovery intentionally cannot reconstruct cleared Offering-specific relationships automatically, while preserved files and downstream records remain available.

## Open Questions

None. The product and data decisions are fixed for this module: same-Store active replacement, explicit confirmation, established configuration-switch reset boundary, preservation of creative and downstream history, no automatic mapping, and no catalog administration inside Design.

## Implementation Plan

### Application state and recovery policy

1. Extend `src/FusionCanvas.Application/DesignFiles/DesignStageState.cs` with explicit stale and recovery presentation facts. Keep persisted configuration identity separate from the resolved active Offering so a deleted/missing target is not mistaken for no configuration.
2. Refactor the selection-validity calculation in `src/FusionCanvas.Application/DesignFiles/DesignStageService.cs` into focused private helpers used by both `BuildState` and recovery validation. Active recovery candidates must be normalized Blueprint Offerings owned by the Item's Store, non-archived, under a non-archived same-Store Blueprint, and resolvable to the current compatibility presentation.
3. Add `RecoverStaleConfigurationAsync` to `src/FusionCanvas.Application/DesignFiles/IDesignStageService.cs` and implement it in `DesignStageService`. Revalidate all policy/catalog facts from a fresh snapshot, create the reset snapshot without file deletion, save once, and return rebuilt authoritative state.
4. Extract a private configuration replacement transformation only if it keeps `SelectConfigurationAsync` and recovery behavior identical without broadening the public API. Do not change normal configuration-switch behavior as incidental work.

### Presentation and interaction

1. Extend `src/FusionCanvas.App/StageTools/DesignStageToolViewModel.cs` with `HasStaleConfiguration`, `CanRecoverStaleConfiguration`, recovery candidate collection/selection, pending replacement identity, confirmation text/visibility, and confirm/cancel operations. Recovery selection must not invoke the normal `SelectedOffering` persistence path.
2. On Item/context reload, cancel pending recovery presentation state and apply only the latest load generation. During confirmation, use `IsBusy` to prevent duplicate submission. After success reload authoritative state; after cancellation or error preserve/reload stale state as appropriate.
3. Update the Listing Configuration area in `src/FusionCanvas.App/Views/MainWindow.axaml` to distinguish normal, stale-with-candidates, and stale-without-candidates states. Place the inline confirmation directly beneath the recovery selector, use concise canonical guidance from this design, and leave all other mutation controls bound to ordinary read-only state.
4. Add minimal routed handlers/focus coordination in `src/FusionCanvas.App/Views/MainWindow.axaml.cs` for confirmation, cancellation/Escape, and focus return. Do not place validation or persistence decisions in code-behind.
5. Update every test fake implementing `IDesignStageService` for the new method without changing unrelated behavior.

### Persistence and compatibility

1. Reuse the existing `IWorkspaceRepository.SaveAsync` snapshot transaction; add no table, column, migration, or new managed-file operation.
2. Verify through reload that the replacement configuration and cleared rows/relationships persist while representative Asset, Supporting Image, Item metadata, workflow/lifecycle, and downstream Listing/mockup records remain unchanged.
3. Keep missing or archived stale records readable through their persisted identity/fallback without recreating catalog data or compatibility rows.

### Test sequence

1. Add failing application tests in `tests/FusionCanvas.Application.Tests/DesignFiles/DesignStageServiceTests.cs` for archived and missing Offering detection before implementation.
2. Add application recovery tests for the exact reset/preservation boundary, different Placeholder/Variant shapes, protected/cross-Store/inactive/concurrently changed candidates, atomic save failure, and authoritative reload.
3. Add focused presentation tests in `tests/FusionCanvas.App.Tests/DesignStageToolViewModelTests.cs` for selection-without-persistence, confirmation, cancellation, duplicate suppression, error retention, and successful state refresh.
4. Add Avalonia headless coverage in `tests/FusionCanvas.App.Tests/DesignStageToolHeadlessTests.cs` for stale review state, the sole enabled recovery mutation, no-candidate guidance, accessible confirmation controls, routed confirm/cancel, focus return, and post-success normal editability.
5. Add or extend `tests/FusionCanvas.Integration.Tests/Persistence/ProductCatalogPersistenceTests.cs` only where the application tests cannot prove save/reload preservation through SQLite.
6. Run focused test projects, strict OpenSpec validation, and `dotnet test .\FusionCanvas.sln -m:1`.

### Decisions not to reopen during implementation

- Recovery is allowed only for an otherwise editable current Design-stage Item and an active same-Store normalized Offering.
- The reset and preservation lists are exact; implementation must not add inferred mapping or delete preserved files/downstream data.
- Selection alone never persists; confirmation is required.
- The recovery UI is inline beside Listing Configuration and absent from normal state.
- No schema migration, catalog editor shortcut, provider synchronization, manual mapping UI, or downstream readiness redesign belongs in this change.

## Planned Acceptance Verification

| Acceptance scenario | Planned verification |
| --- | --- |
| Otherwise editable Item has a stale configuration | `DesignStageServiceTests` proves stale/recovery state; `DesignStageToolHeadlessTests` proves only recovery mutation is enabled and stale identity/guidance are rendered. |
| Item is protected for another reason | Application policy tests plus headless control-state assertions for archived Store, protected status, and reviewed/non-current Design. |
| No active replacement is available | Application state test and headless assertion for Store Editor guidance with no invalid candidate. |
| Creator chooses a replacement | `DesignStageToolViewModelTests` proves selection creates pending confirmation with zero repository/service recovery calls; headless test checks named old/new identities and summary. |
| Creator cancels recovery | View-model and headless routed-input/Escape tests prove no mutation and focus returns to the recovery selector. |
| Creator confirms a valid replacement | `DesignStageServiceTests` compares every reset collection and representative preserved creative, Asset, Supporting Image, lifecycle, and Listing record; integration reload test covers SQLite transaction persistence if needed. |
| Replacement has different Placeholders or Variants | Application test seeds deliberately similar and different catalog values and proves no old relationship is retained or inferred. |
| Recovery validation or persistence fails | Application tests cover missing/archived/cross-Store/concurrent candidate and throwing repository paths with unchanged snapshot; view-model test proves actionable error and retry state. |
| Recovery succeeds | Application plus view-model/headless tests prove replacement selection, cleared state, restored normal editability, authoritative refresh, and focus. |
| Recovered Item is reopened | SQLite persistence or repository reload test proves replacement durability, absence of cleared relationships, and presence of preserved records. |
| Stale Design configuration is intentionally recovered | Covered by the valid-replacement preservation comparison and the reload test, mapped to the modified `basic-product-workflow` requirement. |

No mandatory live desktop scenario is allocated: the material control state, routed input, focus, persistence, and reload risks are deterministic under the existing Avalonia headless and SQLite test harnesses. An optional disposable live check may supplement verification if focus behavior differs on Windows, but it is not a completion gate.
