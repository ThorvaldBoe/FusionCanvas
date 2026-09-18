## Why

Blueprints are currently blocked from archiving when they have active offerings, even though archiving is the reversible lifecycle operation intended for inactive catalog work. This leaves users unable to retire a configured blueprint without manually archiving every dependent record first, and makes the effect of the operation unclear.

## What Changes

- Add an explicit blueprint archive-with-dependents operation.
- Cascade the archive state through the blueprint's offerings, options, option values, variants, placeholders, mockup templates, and their catalog-owned child records while preserving all records and relationships for later review.
- Keep the existing dependency safeguards for ordinary record-level archive commands.
- Add a prominent, focused confirmation in Blueprint detail that explains the broad impact, names the dependent data classes affected, and requires an explicit confirmation before the cascade runs.
- Keep the operation atomic and recoverable; on failure, no partial archive state is exposed.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `blueprint-offering-list`: Blueprint detail gains a confirmed archive-with-dependents lifecycle action and explains its impact.

## Impact

- Application catalog service contract and snapshot mutation logic.
- Store Management / Blueprint detail view model and Avalonia confirmation surface.
- Catalog application tests and focused UI/view-model coverage as appropriate.
- No database migration is expected because all affected catalog records already have archive state; persistence continues through the existing atomic workspace snapshot save.
