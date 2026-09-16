## Context

The Design stage already models an Item's selected offering, active printable areas/placeholders, color rows, one slot assignment per row and area, managed PNG assets, supporting images, preview, download, and read-only workflow guards. The AI boundary currently supports text generation and model catalog discovery; it does not yet describe image outputs, supported sizes, transparency, or binary result handling.

The first module is deliberately limited to generation-to-slot. It gives the creator a trustworthy path from existing creative context to a production-sized PNG while preserving the existing manual import path. Transparent-artwork bounds editing is a separate module because it introduces a new raster-analysis and interactive placement editor with different acceptance and test risks.

## Goals / Non-Goals

**Goals:**

- Make Generate Artwork a compact, frequent Design-stage action directly below Supporting Images.
- Make the target Design Area explicit and safe, with a Store-level primary default that never overrides an explicit choice.
- Reuse the existing AI configuration pattern through a new Artwork purpose profile; do not add a second model selector to the Design surface in this module.
- Keep prompt assembly in Application code, with untrusted creative content separated from operational instructions and secrets.
- Negotiate provider-supported dimensions without distortion, normalize every successful result to the authoritative target pixel dimensions, and store PNG output locally.
- Commit generated asset, provenance metadata, supporting-image visibility, and slot assignment as one logical operation.
- Preserve cancellation, read-only, stale-result, failure, preview, download, and manual-import behavior.

**Non-Goals:**

- transparent-pixel bounds detection, interactive resize/reposition, cropping, or non-destructive placement metadata;
- AI super-resolution, background removal, vectorization, or SVG conversion;
- remote marketplace upload, publication, or provider-specific listing workflows;
- per-request model selection in the Design stage;
- silently padding, cropping, or distorting an image when no exact target aspect-ratio generation size is supported.

## Decisions

### Use a dedicated Artwork AI purpose profile

Artwork generation uses the existing AI settings and profile-selection pattern with a new `Artwork` purpose. The selected model is configured in AI Settings, with the same General inheritance behavior as other purposes. The Design stage displays readiness/error guidance but does not expose provider parameters or a second model picker.

Alternative considered: a model dropdown beside Generate. Rejected for this module because it duplicates AI settings, makes capability validation harder to explain, and increases the target workflow footprint. A later module can add an explicit override if real usage requires it.

### Extend capability metadata rather than infer image support from model names

The model descriptor/catalog contract gains image input/output capabilities, supported output formats, transparency support, and supported image sizes or aspect-ratio constraints. The provider adapter maps its native metadata into this contract. Model names such as “GPT Image” are never treated as capability evidence.

Alternative considered: a hard-coded allowlist. Rejected because provider catalogs change and an allowlist cannot safely express transparency, sizes, or provider-specific limits.

### Require exact supported aspect ratio before generation

The resolution negotiator receives target width/height and model capabilities. If the provider supports parameterized sizes, it computes the largest integer size within the model maximum that preserves the exact reduced ratio. If the provider exposes discrete sizes, it selects only an exact-ratio size. When none exists, generation stops with an actionable error instead of silently cropping or distorting.

Alternative considered: generate the nearest portrait size and crop/letterbox. Rejected because the requested outcome is artwork for a known printable region and the crop would be an undisclosed creative mutation. A future composition module may make such behavior explicit.

### Normalize with a deterministic local raster pipeline

The Integration layer owns decoding, alpha preservation, PNG encoding, and ordinary high-quality raster scaling. The Application layer owns the requested target dimensions and invokes an abstraction for normalization. The implementation must reject malformed, oversized, unsupported, or fully unreadable results and must not require an AI upscaler.

Alternative considered: provider-side upscale or AI super-resolution. Rejected for local-first determinism, cost, and the user's explicit acceptance of ordinary scaling for this first module.

### Store provenance in existing Asset metadata

Generated artwork remains an `ExportedImage` asset so existing Design-file, preview, download, and removal paths continue to work. A versioned metadata object in `Asset.MetadataJson` identifies generated origin, target area, model, generation size, final dimensions, transparency request, and generation time. No credential, prompt secret, provider token, or binary payload is persisted in metadata.

Alternative considered: a new GeneratedArtwork entity and history table. Deferred because the existing asset plus Item link and slot assignment already provide the required review/history behavior for this module. A separate entity can be introduced if provenance queries or multi-stage derivations become a real need.

### Use the catalog Placeholder as the authoritative target when available

The normalized catalog graph already supplies the active offering's printable area and pixel dimensions. Legacy `DesignArea` records remain readable through the existing compatibility path, but new generation requests resolve target identity through the same placeholder/area resolution used by the Design slot grid. The selected target must belong to the Item's selected offering and Store.

### Apply one generated asset to the default design row

Generation targets a selected Design Area and applies to the Item's default row. This matches the existing one-slot-per-row-and-area model and avoids inventing color-specific artwork semantics in the first module. The target selector remains area-only as requested; specific-row generation can be a later enhancement if the workflow demonstrates demand.

### Store primary Design Area as a nullable Store preference

The Store receives an optional `PrimaryDesignAreaId` (or equivalent normalized preference) validated against active Store-owned catalog data. Invalid or unavailable preferences are ignored for defaulting but not silently rewritten. Existing stores migrate to null, preserving current behavior.

## Risks / Trade-offs

- [Provider metadata is incomplete or inconsistent] → Treat capability metadata as untrusted, validate at request time, require explicit supported image output and size evidence, and surface a recoverable error.
- [OpenRouter's current text-only adapter cannot satisfy the contract] → Add an image-capable provider port and isolate provider-specific request/response parsing in Integration; do not weaken the existing text contract.
- [Exact aspect ratios are unavailable for common discrete image sizes] → Fail clearly in this module; record the limitation and leave nearest-size crop/fit behavior for a separately approved composition module.
- [Generated PNG is large or malformed] → Enforce bounded download/file sizes, decode validation, pixel-count limits, and cleanup on persistence failure.
- [A late result targets the wrong Item or area] → Capture an operation identity containing Item, target area, and document generation; validate it immediately before persistence.
- [Replacing a slot accidentally deletes a useful prior result] → Keep the generated asset as a supporting image and make removal explicit through existing confirmation policy.
- [Metadata migration breaks older workspaces] → Add nullable/default-safe persistence fields or versioned metadata parsing; run workspace round-trip tests with pre-feature snapshots.
- [Avalonia bindings drift from the view model] → Add focused headless view coverage for construction, target selection, disabled/busy/error state, and routed Generate action; keep prompt/resolution logic framework-free.

## Migration Plan

1. Add nullable Store preference persistence with a default of null; old snapshots load unchanged.
2. Extend AI configuration serialization with an inherited/empty Artwork purpose profile; old settings inherit General and remain text-only until a compatible model is configured.
3. Add versioned generated-artwork metadata parsing that treats absent metadata as manual asset metadata.
4. Introduce the provider/image-processing contracts and fake implementations for deterministic tests.
5. Enable the Design controls only when target, editability, credential, model capability, and provider configuration are valid.
6. If rollback is required, disable the generation action and retain generated assets and existing manual Design workflows; nullable preferences and metadata remain forward-compatible data.

## Open Questions

No product decision remains open for this first module. Provider-specific image endpoint details, exact catalog metadata fields, and the concrete raster library are implementation inputs to validate against the chosen provider contract; they must not change the behavioral decisions above without reopening this change.

## Implementation Plan

1. Domain and persistence: add the nullable Store primary-area preference; extend snapshot validation/SQLite read-write/transfer behavior; add versioned generated-artwork metadata value semantics and tests.
2. AI contracts: add Artwork purpose/profile support; extend model descriptors and catalog parsing with image capabilities; define image request/result/failure types and an image-generation port without leaking provider SDK types inward.
3. Application generation: implement prompt/context assembly, target resolution, exact-ratio size negotiation, image result validation, normalization boundary, atomic managed-file import plus asset/slot/supporting-image persistence, and operation cancellation guards.
4. Integration: implement the selected provider adapter's image request/response mapping, bounded binary download, raster decode/scale/PNG encode, and cleanup behavior. Use deterministic fakes in application tests.
5. UI: extend the Design-stage view model and MainWindow surface below Supporting Images with target selector, Generate action, transparency checkbox, readiness/error/busy states, and generated provenance display. Preserve existing preview/download/remove actions.
6. Verification: add domain tests for primary-target ownership and ratio negotiation; Application tests for prompt exclusion, capability gating, exact-size normalization orchestration, atomic failure behavior, and stale results; Integration tests for provider parsing and raster normalization; headless App tests for control state, selection, routed action, and error preservation; run strict OpenSpec validation and the solution baseline.

## Acceptance-to-Verification Map

- Generation controls and no-target/read-only states → deterministic headless App view tests.
- Prompt contents and untrusted-content boundary → framework-free Application request-builder tests inspecting serialized messages.
- Model capability and transparency gating → Application capability tests plus provider catalog parser tests.
- Exact-ratio negotiation and PNG normalization → Domain/Application pure tests and isolated Integration raster tests with known fixtures.
- Atomic apply/history and persistence cleanup → Application service tests with repository/file-store fakes; Integration workspace round-trip tests.
- Busy, cancellation, stale result, and recoverable failures → Application coordinator tests and headless UI state tests.
- Primary Design Area default and ownership → Domain/Application tests plus a headless selector-state test.

