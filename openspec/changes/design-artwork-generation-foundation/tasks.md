## 1. Persistence and domain foundations

- [ ] 1.1 Add the nullable Blueprint Offering primary-artwork Design Area invariant, including same-offering/active-area validation and atomic replacement/clearing behavior.
- [ ] 1.2 Persist and transfer the offering primary through SQLite and workspace snapshots with null-safe migration and ownership/archive/delete round-trip tests.
- [ ] 1.3 Add SLL source tracking and stale-state persistence for committed original Idea, Concept idea, Phrase, and Graphic direction changes.
- [ ] 1.4 Add a versioned generated-artwork provenance envelope with exact prompt, model/request identities, dimensions, alpha outcome, warnings, usage/cost, and safe unknown/malformed-version handling.

## 2. Artwork settings and provider capability contracts

- [ ] 2.1 Add an independent Artwork settings profile and image-model catalog; do not inherit or copy General text configuration.
- [ ] 2.2 Define provider-neutral image model, endpoint capability, request, result, usage, provenance, and normalized failure contracts at the Application boundary.
- [ ] 2.3 Implement endpoint-level compatibility policy for model, ZDR, image output, approved raster format, size, and requested transparency, including stale-cache and unavailable-selection states.
- [ ] 2.4 Add deterministic tests for settings migration/serialization, image-only catalog filtering, heterogeneous endpoints, ZDR changes, stale cache, and missing models.

## 3. Prompt, readiness, and request planning

- [ ] 3.1 Implement generation readiness from editability, Artwork configuration, compatible endpoint, selected Listing Configuration and target, complete Design Triangle, and existing default row; keep SLL optional.
- [ ] 3.2 Implement safe internal prompt assembly from original Idea, current Design Triangle, non-stale SLL, target guidance, and relevant creative context, with verbatim Phrase instructions and no implicit Supporting Image references.
- [ ] 3.3 Implement deterministic supported-size ranking: exact target first, then closest aspect ratio, then largest tied size at/below target or smallest tied size when all are larger.
- [ ] 3.4 Test incomplete prerequisites, instruction-like workspace data, absent/stale SLL, Phrase handling, target dimensions, endpoint filtering, size ties, and larger-only candidates.

## 4. Provider and raster integration

- [ ] 4.1 Implement OpenRouter image catalog/endpoint discovery and selected-model resolution without weakening existing text generation behavior.
- [ ] 4.2 Implement one-image generation dispatch with one POST, no automatic retry, bounded base64/raster handling, optional provider provenance, and actionable normalized failures.
- [ ] 4.3 Implement deterministic raster normalization to one exact-dimension PNG using proportional high-quality resampling, two-percent left/top/right inset, top-center placement, transparent fitting margins, alpha inspection, and no retained original.
- [ ] 4.4 Add isolated fixtures/tests for PNG and approved non-PNG input, upscale/downscale, aspect mismatch, inset placement, alpha preservation, opaque transparency shortfall, fully transparent rejection, malformed payloads, and byte/pixel limits.

## 5. Generation orchestration and asset lifecycle

- [ ] 5.1 Orchestrate target/endpoint resolution, prompt creation, dispatch, normalization, warning derivation, and one logical save of managed PNG, Asset, Item link, provenance, and default-row slot assignment.
- [ ] 5.2 Preserve generated results as newest-first Supporting Images history using the same Asset shown in a slot; replace occupied assignments without confirmation and retain displaced generated originals.
- [ ] 5.3 Separate generated `Remove from slot` from confirmed permanent deletion; disable deletion while assigned and preserve existing manual-asset removal semantics.
- [ ] 5.4 Add per-Item operation identity, progress stages, cancellation, late-result rejection on cancel/close/Item or Listing Configuration change, and best-effort staged-file cleanup.
- [ ] 5.5 Test empty/occupied placement, manual replacement, shared-asset history, ordering, unassignment/deletion guards, persistence failures, reload, cancellation, no retry, and cost-warning behavior.

## 6. User interfaces

- [ ] 6.1 Add AI Settings Artwork model search/selection/readiness independently of Advanced text profiles, with keyboard access and no provider quality controls.
- [ ] 6.2 Add `Primary for artwork generation` to the Design Area editor, including sole-primary behavior, read-only presentation, and confirmed archive/delete clearing.
- [ ] 6.3 Add Concept's non-modal stale-SLL warning with confirmed Reset SLL, Keep for reference, and regeneration-clears-stale behavior.
- [ ] 6.4 Add Generate Artwork below Supporting Images with Design Area selector, Generate/Cancel, Transparent Background, prerequisite guidance, progress, errors, and success target message.
- [ ] 6.5 Initialize target from the offering primary, keep explicit overrides session-scoped, reset on Listing Configuration change, and recalculate the transparency default per target/capability.
- [ ] 6.6 Show generated history/provenance and persistent transparency warnings while preserving manual import, preview, download, missing-state, and read-only behavior.
- [ ] 6.7 Add focused Avalonia headless tests for all meaningful bindings, selection/defaulting, keyboard flow, disabled/read-only/busy states, cancellation, stale-SLL actions, primary editing, generated lifecycle actions, and warning persistence.

## 7. Verification and delivery gates

- [ ] 7.1 Map every delta-spec scenario to a named automated test or recorded deterministic inspection in `verification.md`; leave no aggregate-only acceptance claim.
- [ ] 7.2 Run focused Domain, Application, Integration, and App suites and correct implementation or approved artifacts for every failed criterion.
- [ ] 7.3 Run `openspec validate design-artwork-generation-foundation --strict` and resolve every finding.
- [ ] 7.4 Run `dotnet test .\FusionCanvas.sln` and resolve every failure.
- [ ] 7.5 Complete scoped completion QA for architecture boundaries, migrations, secrets/prompt safety, ZDR enforcement, raster bounds, persistence atomicity, and headless UI coverage.
- [ ] 7.6 Record criterion-level evidence, residual limitations, and the deferred bounds-editor follow-up under issue #358 before approval for archive.
