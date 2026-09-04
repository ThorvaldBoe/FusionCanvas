## Why

Catalog Color and Size values currently have a sort-order field but users cannot control it, and newly created values do not receive a meaningful position. This makes provider/catalog choices appear in incidental database order and prevents creators from presenting variants in the order that best matches their product workflow.

## What Changes

- Add an explicit, persisted ordering operation for active option values within one Offering Option.
- Provide equivalent visible drag handles in the Color and Size value-management dialogs, with an accessible reorder action for keyboard and assistive-technology users.
- Normalize order after add, reorder, archive, and restore so active values have deterministic contiguous positions while archived values retain identity and references.
- Preserve existing value identities, variant memberships, mockup links, and other catalog relationships during reordering.
- Backfill order for existing databases deterministically from the current apparent value order, using stable identity as a tie-breaker, and ensure all relevant value consumers use the persisted order.
- Add focused application, persistence/migration, and UI behavior tests for both Color and Size values.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `variant-management`: value management supports explicit ordering for Color, Size, and other option values while preserving existing dialog, validation, accessibility, and persistence behavior.

## Impact

- Domain/application catalog value models and setup/reorder contracts.
- SQLite workspace schema initialization/migration, snapshot loading/saving, and ordered queries.
- Avalonia option-value management dialog and view model, including drag/drop and accessible keyboard fallback.
- Catalog consumers that present Color and Size choices.
- Domain/application/integration/App tests; no external service or new package is required.
