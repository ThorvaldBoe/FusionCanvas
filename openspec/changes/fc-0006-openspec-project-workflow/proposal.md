## Why

FusionCanvas already depends on OpenSpec to turn roadmap intent into accepted behavior, but the workflow itself is only described across planning documents. This change makes the project workflow explicit so contributors can consistently propose, review, implement, validate, and preserve feature decisions without turning every change into heavy process.

## What Changes

- Define OpenSpec as the standard workflow for significant FusionCanvas feature work.
- Clarify the relationship between roadmap/PRD planning documents, active OpenSpec changes, accepted specs, and archived changes.
- Establish the expected lifecycle for a change: propose, review, apply, test, and archive.
- Require feature specifications to capture user-facing behavior, requirements, scope boundaries, acceptance criteria, open questions, and related notes where relevant.
- Preserve completed or superseded specifications as durable project context instead of casually deleting them.
- Keep the workflow lightweight enough for Phase 1 and later feature work.

## Capabilities

### New Capabilities

- `openspec-project-workflow`: Defines how FusionCanvas contributors convert planning intent into OpenSpec proposals, accepted specs, implementation tasks, validation evidence, and archived project history.

### Modified Capabilities

- None.

## Impact

- Affects project documentation and planning conventions under `openspec/`.
- Establishes contributor expectations for future roadmap items from `docs/lifeos/prd`.
- Does not require application runtime code, user-facing UI, storage schema changes, or third-party dependencies.
