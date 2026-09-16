## Why

FusionCanvas already has the creative context, catalog-defined printable areas, PNG design-file storage, supporting-image history, and slot placement needed to make artwork production part of the Design stage. Creators currently have to leave that workflow to generate artwork, manually derive prompts and dimensions, upscale results, and place files, which creates avoidable targeting and production-format errors.

This module establishes one safe, reviewable outcome: a creator can request artwork from the Design stage for an explicitly selected printable area and receive a normalized PNG at that area's required pixel dimensions, with the result retained as both the selected slot artwork and a supporting-image history entry.

## Origin

Primary intake issue: [#358](https://github.com/ThorvaldBoe/FusionCanvas/issues/358)

## What Changes

- Add a Design-stage Generate Artwork section below Supporting Images with a target Design Area selector, Generate action, and Transparent Background option.
- Default the target selector from a persisted store-level primary Design Area when that area is available for the active item's selected offering; preserve explicit user selection.
- Add an application-facing image-generation capability contract separate from the existing text-generation contract, including model capability discovery, supported output formats, transparency support, and usable resolution limits.
- Assemble a detailed, bounded image-generation request from the item's Idea, Concept/Design Triangle values, SLL when present, phrase, graphic description, and relevant creative context while excluding operational and secret data and treating workspace content as untrusted data.
- Negotiate the largest provider-supported generation resolution that preserves the selected Design Area aspect ratio, then normalize the generated result to a PNG at the Design Area's authoritative pixel dimensions.
- Apply a successful generated PNG to the selected Design Area slot and retain the generated result as a supporting image with provenance and outcome metadata sufficient for review.
- Provide recoverable states for missing targets, unavailable or incompatible models, unsupported transparency, invalid provider responses, cancellation, file-processing failure, persistence failure, read-only contexts, and stale late results.
- Preserve the existing manual PNG import, preview, and download workflows.

## Capabilities

### New Capabilities

- `design-artwork-generation`: Generate, normalize, persist, and apply artwork from Design-stage creative context with explicit target selection and safe failure behavior.

### Modified Capabilities

- `design-area-target-selection`: Extend target selection with a persisted store-level primary Design Area default and generation-target validation, while keeping explicit selection optional and editable.
- `asset-management`: Extend Item-linked generated artwork handling with provenance/history semantics for generated supporting images and normalized PNG outputs.

## Impact

- Domain: store preference for a primary Design Area and generated-artwork/provenance records or metadata, subject to the existing snapshot and relationship invariants.
- Application: image-generation port, capability and resolution negotiation, prompt/context assembly, PNG normalization/upscaling, generation orchestration, slot assignment, supporting-image persistence, and cancellation/stale-result guards.
- Integration: provider adapter extensions beyond the current text-only OpenRouter path, model-catalog parsing for image capabilities, managed-file raster processing, and deterministic image metadata/PNG handling.
- App: Design-stage controls and state presentation below Supporting Images, including loading, disabled, success, error, and read-only states with headless Avalonia coverage.
- Persistence and migration: snapshot/schema changes for the primary-area preference and generated-artwork provenance must be backward-compatible; existing manual assets and design slots must remain readable and editable.
- Dependencies and risks: actual provider image-generation API semantics, model capability metadata quality, transparency behavior, image dimensions/aspect-ratio constraints, alpha handling, and test fixtures for raster processing must be resolved in `design.md` before implementation.

