## Why

FusionCanvas already has the creative context, catalog-defined printable areas, PNG design-file storage, supporting-image history, and slot placement needed to make artwork production part of the Design stage. Creators currently leave that workflow to reconstruct prompts, choose technical image settings, resize outputs, and place files manually, creating avoidable targeting, formatting, and history-loss risks.

This module establishes one independently verifiable outcome: a creator with a complete Concept can generate one artwork image for an explicit Design Area in the selected Listing Configuration, receive a proportionally fitted PNG at the exact production dimensions, and retain the result and provenance while it is applied to the default design row.

## Origin

Primary intake issue: [#358](https://github.com/ThorvaldBoe/FusionCanvas/issues/358)

## What Changes

- Add a compact Generate Artwork section below Supporting Images with a Generate/Cancel action, Design Area selector, Transparent Background option, readiness guidance, progress, warnings, and errors.
- Add a dedicated Artwork image-model profile in AI Settings. It uses an image-only model catalog and never inherits the text-only General profile.
- Discover image capabilities and definitive provider-endpoint constraints, including ZDR eligibility, supported sizes/aspect ratios, raster formats, and transparency support; fail closed when no endpoint satisfies the active privacy policy and requested parameters.
- Let each Blueprint Offering identify one optional primary Design Area through its Design Area editor. The primary area becomes the initial generation target for Items using that Listing Configuration; missing primaries produce no silent fallback.
- Require a complete Concept and an existing default design row before generation. Use the Idea, current Design Triangle, a current non-stale SLL when present, target guidance, and relevant creative context to build a detailed prompt that treats workspace content as untrusted data and requires the Phrase verbatim.
- Select the supported provider size whose aspect ratio is closest to the target, then the largest such size up to the target. Request the exact target when supported; if every supported size is larger, use the smallest closest-ratio size. Normalize locally with ordinary high-quality resampling into an exact-size PNG, top-centered within a two-percent left/top/right safety inset, with transparent fitting margins and no crop or distortion.
- Prefer provider PNG output but accept supported raster output and convert locally. When transparency was requested but the valid result is opaque, still apply and retain it with a persistent warning; reject a fully transparent result with no visible artwork.
- Apply one successful result to the default row's selected Design Area slot, replacing existing slot artwork without confirmation while retaining the prior generated asset in Supporting Images.
- Distinguish unassignment from deletion: removing generated artwork from a slot keeps its history asset, while permanent deletion is unavailable until the asset is unassigned from every slot. A generated original also remains in history when an externally edited replacement is uploaded.
- Persist the final normalized PNG only, plus local provenance containing the resolved prompt, intended area, provider/model/request identity, requested and final dimensions, transparency outcome, warnings, generation time, and usage/cost when supplied. No credentials or operational secrets are stored.
- Mark an existing SLL stale after a committed Idea, Concept idea, Phrase, or Graphic direction change; offer confirmed Reset SLL and non-destructive Keep for reference actions. Artwork generation excludes stale SLL content.
- Preserve manual PNG import, preview, download, and existing non-generated Design-file behavior.

## Capabilities

### New Capabilities

- `design-artwork-generation`: Design-stage readiness, prompt assembly, target and row selection, provider request planning, proportional normalization, application, warnings, cancellation, and failure behavior.

### Modified Capabilities

- `ai-provider-configuration`: Add a dedicated image-model catalog, endpoint capability resolution, ZDR-aware Artwork profile, and image-generation provider boundary without weakening text profiles.
- `application-settings`: Add focused Artwork model selection and readiness inside AI Settings.
- `product-supplier-setup`: Let each Blueprint Offering configure one optional primary Design Area and define its archive/removal behavior.
- `asset-management`: Add generated-artwork provenance and history semantics, including separate slot unassignment and permanent deletion.
- `sll-generation`: Mark SLL content stale after committed upstream creative changes and provide Reset or Keep-for-reference handling.

## Impact

- **Domain:** Blueprint Offering primary-area preference and versioned generated-artwork provenance semantics; existing Item, slot, asset, and catalog invariants remain authoritative.
- **Application:** independent Artwork configuration resolution, image catalog and endpoint capabilities, prompt/context assembly, size selection, raster-normalization boundary, generation orchestration, logical atomic persistence, and cancellation/stale-result guards.
- **Integration:** OpenRouter's dedicated image catalog and generation API, endpoint capability parsing and ZDR filtering, bounded base64/raster handling, and deterministic PNG conversion/resampling.
- **App:** AI Settings Artwork selector; Blueprint Offering Design Area primary checkbox; Design-stage controls, progress, persistent warnings, provenance summaries, and stale-SLL actions.
- **Persistence and migration:** backward-compatible application-settings changes for Artwork selection, workspace schema/snapshot changes for the offering primary, and versioned Asset metadata. Existing stores, settings, manual assets, and slot assignments load unchanged.
- **Dependencies:** implementation follows the completed OpenRouter configuration foundation. The transparent-pixel bounds editor remains a separate follow-up delivery module under issue #358.
- **Verification:** framework-free policy and orchestration tests, isolated provider/raster/persistence tests, and focused Avalonia headless tests cover every acceptance scenario; no live provider or desktop session enters the deterministic baseline.

