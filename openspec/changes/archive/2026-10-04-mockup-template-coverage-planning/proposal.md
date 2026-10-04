## Why

Mockup Templates can currently remain Draft even when the creator has uploaded and configured several images, but the editor does not make the missing Variant coverage concrete enough to recover quickly. Creators need both an image-first workflow and a metadata-first workflow that can generate a useful coverage plan without creating fake empty image assets.

This module makes readiness explainable and gives creators a guided way to create or complete the required source-image applicability, while preserving the existing exact-one authoritative readiness gate.

## What Changes

- Add an Offering-scoped Mockup Template coverage view that reports resolved, missing, and ambiguous Variant coverage with creator-facing names and applicability summaries.
- Add a metadata-first coverage-plan action that derives missing requirements from the selected Template's target Design Area and compatible Variants.
- Support smart coverage defaults, including Color-specific rows with Size and other options left unrestricted when that safely covers the Variant set.
- Allow each missing coverage requirement to start an image assignment or upload flow with its applicability preselected, while retaining the existing image-first upload-and-configure workflow.
- Allow an optional existing source image to be assigned or used as an applicability/mapping pattern without silently copying unsafe pixel mappings across incompatible image dimensions.
- Reuse the same coverage model for future bulk-upload matching so image-first and metadata-first setup converge on the same source-image records and readiness result.
- Improve Listing-stage diagnostics with concise coverage counts and actionable missing or ambiguous Variant guidance.
- Keep empty requirements derived from current catalog and Template state; do not persist fake source-image records without managed raster Assets.

## Capabilities

### New Capabilities

- `mockup-template-coverage-planning`: Explainable per-Variant coverage planning and metadata-first image assignment for a Mockup Template.

### Modified Capabilities

- `listing-mockup-template-diagnostics`: Extend Draft-template diagnostics with coverage counts and affected Variant guidance while preserving the ready-only selector and authoritative eligibility gate.

## Impact

- Affects the Domain/Application mockup readiness and source-resolution presentation contracts, while retaining `MockupTemplateSourcePolicy` as the single matching authority.
- Affects Store Editor Mockup Template management and the focused Mockup Template editor, including progressive disclosure, keyboard flow, upload/assignment actions, and recoverable incomplete states.
- Affects Listing-stage diagnostics and presentation models; it does not change mockup rendering eligibility or permit Draft templates to render.
- Requires focused domain/application tests for coverage grouping and exact-one resolution, view-model tests for draft/assignment state, and Avalonia headless coverage for the meaningful coverage-panel and assignment interactions.
- No provider synchronization, AI image classification, multi-Design-Area rendering, or external publishing is included.


