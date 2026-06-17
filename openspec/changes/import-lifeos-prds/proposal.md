## Why

FusionCanvas has a detailed LifeOS PRD corpus that defines intended product behavior across the application foundation, creative workspace, product workflow, batch operations, AI assistance, plugin platform, publishing integrations, analytics, and automation.

Those PRDs should be reviewed through the OpenSpec process before becoming accepted baseline specifications. Keeping them in a change preserves the distinction between proposed future behavior and already accepted or implemented behavior.

## What Changes

- Import the LifeOS PRD corpus as proposed OpenSpec capability specs.
- Preserve the original FC identifiers in capability names wherever available.
- Preserve phase, audit, and roadmap documents as proposed planning specifications.
- Keep existing accepted baseline specs in `openspec/specs`.
- Defer all product implementation work to future changes.

## Capabilities

### New Capabilities

- `fc-0001` through `fc-0807`: Proposed FusionCanvas product capabilities imported from LifeOS PRDs.
- `phase-0-foundation`, `phase-1-mvp-creative-workspace`, and `phase-2-product-creation-workflow`: Proposed phase-level planning specifications.
- `prd-audit-2026-06-17`: Proposed audit notes preserved for review.
- `roadmap`: Proposed roadmap content preserved for review.

### Modified Capabilities

None.

## Impact

- Adds proposed OpenSpec requirements only.
- Does not implement application behavior.
- Does not change runtime code, project structure, storage, UI, integrations, or tests.
- Provides a review gate before imported PRDs become accepted baseline specs.
