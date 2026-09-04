## Context

The Listing stage is currently a status-only Avalonia view model. The repository already contains Offering-owned Mockup Templates, revisioned image-space mappings, local source-image Assets, Design PNGs, Item color working sets, and managed workspace file storage. Issue #137 therefore needs a composition/use-case boundary and a Listing presentation surface, not a second template authoring system.

## Goals / Non-Goals

**Goals:**

- Resolve an Item's active Offering, selected Colors, Design PNGs, and ready template revisions.
- Render local raster composites and persist successful outputs as attributable Item assets.
- Make partial missing-color failures understandable and recoverable.
- Keep generation local-first and deterministic in automated tests.

**Non-Goals:**

- Store-global template ownership, because the accepted catalog model intentionally owns templates by Offering.
- Drag-and-drop or click-to-place template editing; the existing placement editor remains authoritative.
- Marketplace publishing, cloud rendering, batch scheduling, image editing, or manual per-output overrides.

## Decisions

- Use a new application-facing `IMockupGenerationService` to own eligibility, color/design matching, orchestration, atomic snapshot mutation, and output summaries. The App project only binds state and commands.
- Use an application-facing raster compositor contract, implemented in Integration with the repository's supported raster library. The compositor receives streams and a validated mapping and returns encoded PNG bytes/stream; it does not know workspace or database types.
- Store each result using the existing `Asset` with `AssetKind.MockupImage` and `AssetLink(EntityKind.Item, itemId)`. JSON metadata contains stable IDs and revision/color values; no new persistence table is needed.
- Treat one apply action as a set of independent output jobs. A missing or invalid color is reported in the result while other successful jobs are committed individually, so a late failure cannot erase earlier valid outputs. Each individual file import and snapshot save is still atomic.
- Fit the design inside the mapped rectangle using a contain calculation, center the remaining axis, and preserve the template's dimensions. Alpha compositing is performed over the source template.
- Reuse the existing `GetEligibleTemplatesAsync` readiness gate and existing Design-stage summaries rather than accepting draft or incompatible templates.

Alternatives considered: putting raster work in the App layer would violate the clean boundary and make deterministic tests harder; adding a dedicated generated-mockup table would duplicate existing Asset storage without a current query or lifecycle need; overwriting prior results would lose the revision trace required by the issue.

## Risks / Trade-offs

- [Raster package availability] → isolate the dependency in Integration and keep a deterministic fake compositor for Application tests.
- [Many colors/design files] → process sequentially with cancellation and a busy state; partial results are explicit.
- [Ambiguous design-to-color matching] → use the existing Design row color values and default row/slot conventions; do not invent new selection rules in the UI.
- [Template source asset can disappear] → validate managed-file existence/readability before composing and report a per-output failure.

## Migration Plan

No database migration is expected. Existing workspaces gain the capability when opened by the new application version; existing Asset/AssetLink records remain unchanged. Rollback is code-version rollback, with generated assets remaining ordinary managed assets.

## Open Questions

None for this bounded module. A future module may revisit true store-global template ownership or replacement/cleanup policy for generated outputs.

## Implementation Plan

1. Add domain/application records for a generation request, output metadata, per-job result, and Listing generation state; add `IMockupGenerationService` and `IMockupRasterCompositor` contracts.
2. Implement application orchestration against `IWorkspaceRepository`, `IWorkspaceFileStore`, and the existing template readiness/design models. Resolve source assets by active color applicability, choose matching Design slots, validate mappings, compose, import generated PNGs as `MockupImage`, and persist Asset/AssetLink metadata.
3. Implement the Integration compositor and DI wiring using the existing local workspace services. Keep source and result streams disposed and prevent path traversal by routing all output through the managed file store.
4. Replace the Listing stage placeholder view model with load/apply commands, generated output summaries, busy/error/blocked states, and read-only handling. Extend MainWindow construction and Listing AXAML while preserving stage visibility and existing lifecycle behavior.
5. Add domain/application/integration tests and Avalonia headless tests for populated, blocked, missing-color, protected, busy, replacement, and persistence-failure scenarios.

## Acceptance-to-Verification Plan

| Acceptance area | Planned verification |
| --- | --- |
| Applicable template/design presentation and blocked/protected states | Listing view-model tests plus Avalonia headless construction/binding tests |
| Correct contain scaling, mapping, dimensions, and missing-color diagnostics | Framework-free compositor tests and application service tests with deterministic fixtures |
| Managed Item assets, metadata, replacement, and save failure cleanup | Application tests with fake repository/file store plus isolated Integration persistence/file tests |
| Keyboard/busy/focus behavior | Avalonia headless control-state and command tests; live desktop check is supplemental only if focus behavior remains framework-sensitive |
