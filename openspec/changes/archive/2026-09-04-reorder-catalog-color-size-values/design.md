## Context

Issue 262 asks creators to control the display order of catalog Color and Size values. `OfferingOptionValue` already carries `SortOrder`, the view model orders its dialog values by that property, and SQLite stores the column. However, creation currently defaults values to zero, there is no reorder application operation, and consumers need a single deterministic ordering contract. The change must preserve stable IDs and all variant/mockup relationships.

## Goals / Non-Goals

**Goals:**

- Make active Option Value order explicit, contiguous, persisted, and deterministic.
- Support equivalent Color and Size drag-handle interactions in the focused management dialog.
- Provide accessible keyboard-equivalent move actions and target-specific labels.
- Preserve identities, references, and existing dialog behavior.
- Backfill older workspaces without changing their apparent order.

**Non-Goals:**

- Ordering Options themselves, Variants, or unrelated workspace tree records.
- Alphabetical sorting or provider re-synchronization.
- Recreating values, changing relationship schemas, or adding external dependencies.
- Reordering archived values independently of active list order.

## Decisions

1. **Use the existing `SortOrder` field and identity.** This avoids a schema redesign and keeps serialization compatible. The application owns normalization and validates that all IDs belong to one active Option.
2. **Normalize active values to zero-based positions.** After mutations, order is stable, easy to compare, and avoids gaps from archived records. New values append at the active count.
3. **Use a focused reorder application contract.** The view model sends an ordered list (or source ID plus target position) to the catalog setup service; the service validates context and atomically updates only the affected values. UI code does not manipulate snapshots directly.
4. **Expose both drag and command paths.** A visible grip initiates pointer drag/drop, while Move up/Move down commands provide keyboard and assistive-technology parity. The handle and commands use the value name in accessible labels.
5. **Read order at every consumer boundary.** Snapshot loaders and presentation projections order active values by `SortOrder`, then `Id` as a defensive tie-breaker. Migration/backfill orders existing rows by their current stable apparent query order and ID, without touching IDs or links.

## Risks / Trade-offs

- [Risk] Existing rows may share the default sort order → [Mitigation] deterministic backfill and stable ID tie-breaker, covered by isolated SQLite migration tests.
- [Risk] Pointer drag behavior is framework-sensitive → [Mitigation] keep reorder decisions in framework-free application tests and add Avalonia headless coverage for handle discovery, drop behavior, and accessible commands.
- [Risk] A stale dialog could overwrite newer order → [Mitigation] validate Offering/Option ownership and apply one transaction; reject invalid or stale IDs without partial updates.
- [Risk] Archived rows can create confusing gaps → [Mitigation] normalize active rows after archive/restore and never use archived rows for active choice projections.

## Migration Plan

1. Ensure schema initialization/migration recognizes the existing sort-order column and backfills active Option Values deterministically where legacy data is tied or unset.
2. Deploy the application service and UI against the same snapshot format; old workspaces remain readable.
3. Save normalized order atomically with the workspace snapshot/repository transaction.
4. Rollback is code rollback only; the existing integer column and stable identities remain readable, and no destructive data migration is required.

## Open Questions

None. The issue and existing focused-dialog UX establish the required surface; custom Option kinds retain the same behavior through the generic value-management path.

## Implementation Plan

1. Update catalog application contracts/service and view model mutation plumbing to append and reorder values with validation, normalization, and notifications.
2. Update SQLite save/load/migration and all catalog choice projections to preserve and consume `SortOrder` with deterministic tie-breaking.
3. Add visible grip controls and drag/drop handlers to `OptionValueManagementWindow`, plus Move up/Move down accessible commands for each row.
4. Add domain/application tests for normalization and invalid requests, integration tests for round-trip and legacy backfill, and App/headless tests for equivalent Color/Size dialog behavior and accessibility labels.
5. Run focused tests, strict OpenSpec validation, and the full `dotnet test .\FusionCanvas.sln` baseline; record criterion-level evidence in `verification.md`.
