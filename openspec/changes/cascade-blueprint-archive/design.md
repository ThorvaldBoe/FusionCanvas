## Context

The normalized catalog model stores archive state on the Blueprint and on each dependent catalog record. Existing `ArchiveAsync` intentionally blocks a Blueprint when active Offerings exist. This module adds a separate explicit cascade path so ordinary record-level dependency safeguards remain unchanged.

## Functional Design

The cascade is scoped by stable ownership: Blueprint → Offerings → Options/Option Values/Variants/Placeholders/Mockup Templates. Child mapping and revision records that carry no archive column are preserved automatically; child records with archive state are archived when they belong to an affected template. No external listing/design relationship is deleted or detached. Restore behavior remains the existing record-level behavior and is not expanded into a new cascade workflow in this module.

The Blueprint detail action is infrequent and destructive, so it belongs in the existing Basic section rather than the Offering list. The confirmation is a prominent warning panel/window with a concise impact summary, separate Cancel and “Archive Blueprint and dependents” actions, and no mutation until confirmation.

## Implementation Plan

1. Add a catalog service contract/request for cascade archiving and implement it in `CatalogSetupService` as one snapshot transformation. Validate store ownership, Blueprint existence, active state, and writable Store. Select dependent IDs once, update all lifecycle-bearing descendants, and save through the existing atomic mutation helper.
2. Keep `ArchiveAsync` unchanged for ordinary archive requests so option, variant, offering, and normal Blueprint archives retain their current dependency checks.
3. Extend `StoreManagementViewModel` with pending Blueprint archive state, command/event plumbing, warning text, and mutation result handling. Refresh the catalog state and keep the selected Blueprint context coherent after success or failure.
4. Add a focused Avalonia confirmation surface or inline warning in `StoreEditorWindow` using shared warning resources, explicit accessible names, and compact content-sized actions. Ensure cancel/dismiss returns to the Blueprint detail.
5. Add application tests covering complete descendant cascade, preservation of external relationships, cancellation at the view-model level, and repository failure atomicity where the existing test collaborators support it. Add a view test only for meaningful confirmation state/binding behavior.

## Edge Cases and Decisions

- Already archived descendants remain archived; the operation is idempotent for the selected Blueprint.
- Archived descendants do not block the operation; only the Blueprint and Store active/writable checks matter.
- A dependent record with an unexpected ownership mismatch is not silently reassigned; the service must fail validation before saving.
- Child join/revision tables without `is_archived` are not deleted or rewritten because preserving their parent records preserves the relationship graph.
- The existing permanent-delete naming/API remains untouched; “archive” is reversible and does not remove files or records.

## Verification Mapping

- Cascade result and all lifecycle-bearing descendants: deterministic `CatalogSetupService` tests.
- External relationship preservation and atomic failure: repository/application tests with an isolated snapshot repository.
- Warning copy, explicit confirmation, cancellation, and accessible commands: focused `StoreManagementViewModel`/Avalonia headless test where existing test infrastructure supports the surface.
- Full regression: `dotnet test .\FusionCanvas.sln` and strict `openspec validate`.
