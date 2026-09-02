## Why

Creators often need several mockup views for the same offering, differing mainly in their source images. Rebuilding each template repeats the offering, Design Area, applicability, placement, and provider metadata work and makes template setup unnecessarily slow.

## What Changes

- Add a duplicate action to the Mockup Template management list.
- Duplicate an active template within its current Store and Blueprint Offering as a new editable draft.
- Copy the source template's name, description, position, target Design Area, current provider-image configuration, Color bindings, and active local source-image configuration.
- Give the duplicate and its mutable template/revision/source-entry records new identities while retaining immutable managed Asset identities until the creator replaces an image.
- Open the duplicate in the existing focused editor, with a collision-safe “Copy of …” name that can be edited before save.
- Keep the original template, revisions, source entries, and assets unchanged; archived templates and archived Stores cannot be duplicated.

## Capabilities

### New Capabilities

- `duplicate-mockup-template`: Duplicate an active Mockup Template into an independently editable draft within the same offering.

### Modified Capabilities

- None. The existing source-image requirements remain unchanged; duplication composes those behaviors without changing their standalone contract.

## Impact

- Application: extend the mockup-template setup contract/service with a duplication use case that copies the current configuration and source-image applicability/revision snapshot.
- App: expose a duplicate command beside each active template and initialize the existing focused editor with the returned draft.
- Domain: no new business entity is required; existing template, revision, source-image, option-value, and asset identities must be preserved or regenerated according to the design.
- Tests: add application tests for deep-copy semantics, validation, archived/read-only behavior, and deterministic name generation; add view-model/headless coverage for command placement and editor initialization where framework behavior is material.
- Persistence: no schema migration; the existing snapshot tables already represent all duplicated records.
