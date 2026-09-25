## Context

The Design stage already has the selected Listing Configuration, active Design Areas, a default color row, one artwork slot per row and area, managed PNG assets, Supporting Images, preview, download, and read-only guards. Concept already owns the original Idea, Design Triangle, and optional SLL. AI Settings and OpenRouter integration currently focus on text generation; their model catalog and purpose inheritance cannot safely establish image-output capability.

This module creates a complete generation-to-slot path. It does not implement the transparent-pixel bounds editor requested for later work because raster edge analysis and interactive placement form a separate, independently testable outcome under issue #358.

## Goals / Non-Goals

**Goals:**

- Generate one artwork image from current creative context for an explicit Design Area.
- Default that target from an optional primary Design Area on the selected Blueprint Offering.
- Configure Artwork through a dedicated image-model selector that is independent of General text settings.
- Resolve one provider endpoint that satisfies image output, privacy, size, format, and optional transparency together.
- Select the closest supported aspect ratio, generate at the best useful size, and normalize deterministically to the exact target PNG dimensions.
- Apply the result to the default design row while retaining every valid generated result as reviewable history.
- Preserve exact prompt and non-secret provider provenance, persistent warnings, cancellation safety, and existing manual workflows.

**Non-Goals:**

- transparent-pixel bounds detection or an interactive resize/reposition editor;
- AI super-resolution, sharpening, background removal, vectorization, or SVG output;
- using Supporting Images as automatic model references;
- prompt preview/editing or Design-stage model overrides;
- OCR or an automated guarantee that generated lettering matches the Phrase;
- automatic retries, batch/variant generation, remote publication, or marketplace upload.

## Decisions

### Keep Artwork configuration independent from text profiles

AI Settings gains a dedicated Artwork profile and image-model catalog. It never inherits General and is visible independently of Advanced text-purpose profiles. This module exposes model selection and readiness only; provider-specific quality and sampling parameters retain provider defaults.

The saved model identity is preserved if it disappears or becomes incompatible. FusionCanvas reports it unavailable instead of silently substituting another model.

### Evaluate definitive capabilities per provider endpoint

Model-level union metadata is insufficient because different endpoints for one model can have different privacy, format, size, and transparency properties. Readiness and request planning evaluate endpoint records independently and select only an endpoint that simultaneously satisfies:

- the selected and resolved image model;
- global Zero Data Retention policy;
- image output and an approved raster format;
- a usable supported size or resolution tier;
- transparent output when it is requested.

Image catalog caching follows the existing local-first pattern, but request-time endpoint enforcement is mandatory. A stale compatible cache may sustain readiness with guidance; absence of compatible catalog evidence fails closed.

### Store the primary target on each Blueprint Offering

The primary setting belongs to the offering because the selected Listing Configuration already establishes the Store and offering. A nullable `PrimaryArtworkDesignAreaId` (or equivalent normalized identity) references one active Design Area owned by that offering. The Design Area editor exposes `Primary for artwork generation`; saving it atomically clears any previous primary in the same offering.

There is no first-area fallback. Archiving or permanently deleting the primary requires confirmation and clears the designation in the same operation without selecting a replacement. Existing offerings migrate with no primary.

The Design selector contains all active areas from the selected offering. The selected target and transparency choice are persisted as Item-local Design preferences and restored when the same Listing Configuration is reopened. Invalid saved targets are ignored, and changing Listing Configuration clears the saved preferences before resetting the selector to the new offering's primary or no target.

### Require a real destination before spending provider resources

Generation requires a complete Design Triangle, editable Design state, ready Artwork endpoint, explicit target, and an existing default design row serving at least one selected color. SLL is optional. If no default row exists, the user is directed to select a product color before Generate is enabled.

This avoids generating a paid result with no unambiguous slot destination.

### Assemble and persist one resolved prompt internally

Application code builds the prompt and sends it immediately; the first module has no prompt editor. The prompt includes the original Idea, current Concept idea, Phrase, Graphic direction, selected area's dimensions and artwork guidance, relevant user-authored creative context, and a current non-stale SLL when available. It requires the Phrase verbatim and tells the user to inspect lettering after generation because no OCR check is claimed.

Workspace values are delimited and treated as untrusted creative data subordinate to system and technical instructions. Credentials, internal paths, and unrelated operational metadata are excluded. Supporting Images are not uploaded or referenced automatically.

The exact resolved prompt is persisted locally with successful artwork for reproducibility. If an upstream Idea or Design Triangle field changes after SLL generation, the SLL becomes stale. `Keep for reference` retains it with a warning but excludes it from artwork prompts; confirmed `Reset SLL` removes it; successful SLL regeneration replaces it and clears staleness.

### Rank supported sizes by aspect ratio, then useful pixel area

The selected Design Area supplies the exact final width `Tw` and height `Th`. If an eligible endpoint directly supports `(Tw, Th)`, request it. Otherwise, for each endpoint-supported candidate `(W, H)`, calculate the absolute aspect-ratio distance from the target using a deterministic representation (for example `abs(W * Th - H * Tw)` normalized for comparison).

Choose the candidate with the smallest ratio distance. Among candidates tied at that distance:

1. prefer candidates whose pixel area is at or below `Tw * Th`;
2. from those, choose the largest pixel area;
3. if every tied candidate is larger, choose the smallest pixel area.

Stable provider-declared ordering is the final deterministic tie-breaker. A differing aspect ratio never causes failure by itself; local fitting absorbs the mismatch without crop or distortion.

### Normalize one final PNG with deterministic fitting

Integration owns bounded decode, raster validation, proportional resampling, alpha inspection, compositing, and PNG encoding behind an Application port. Native PNG is preferred, but approved raster formats such as JPEG or WebP may be accepted and converted. The provider-original payload is transient and never becomes a second Asset.

The normalizer creates an exact `Tw × Th` transparent canvas. A two-percent safety inset is applied to the left, top, and right (`ceil(0.02 * Tw)` horizontally and `ceil(0.02 * Th)` at top, subject to at least one drawable pixel). The source is proportionally scaled to fit the remaining width and available height, top-centered inside the safe region. Any unavoidable vertical remainder stays below the artwork. A deterministic high-quality non-AI filter such as Lanczos or a well-defined bicubic equivalent is used. No crop, stretch, sharpening, or AI upscaling occurs.

Unused fitting canvas is transparent even when Transparent Background is unchecked; that checkbox controls the provider request, not whether local letterboxing is painted opaque. A fully transparent normalized result is rejected. If transparency was requested but the valid result is fully opaque, it is still applied with a persistent warning.

### Use one Asset for the slot and generated history

Every successful operation creates one managed final PNG Asset and Item link. That same Asset appears in Supporting Images and may be assigned to the default row's selected-area slot; the UI does not copy it to create history.

Generating into an occupied slot replaces the assignment without confirmation. A displaced generated Asset remains Item-linked and visible in Supporting Images. The same applies when a manually edited PNG replaces generated artwork. `Remove from slot` only unassigns generated artwork. Permanent deletion is disabled while a generated Asset is assigned anywhere, and confirmed deletion of an unassigned generated Asset removes its Item link, record, and managed PNG.

Generated history is newest-first. Compact entries show Generated artwork, intended area, resolved model, final dimensions, and persistent warnings. Secondary details expose the full prompt and remaining provenance.

### Persist a versioned provenance envelope

Generated artwork remains compatible with the existing Asset model. A versioned metadata envelope records:

- generated-artwork origin and generation timestamp;
- exact resolved prompt;
- intended Design Area identity and display name;
- provider, selected model ID, and resolved model ID;
- provider request/generation ID when supplied;
- requested provider dimensions and exact final dimensions;
- transparency request, observed alpha outcome, and warnings;
- provider-reported usage and cost when available.

No API key, credential-store reference, transient payload, provider authorization header, or unrelated file-system path is stored. Missing optional values remain absent rather than fabricated. Older assets without this envelope remain manual assets.

### Treat the save as one logical operation

After generation and normalization, Application stages the managed PNG, Asset, Item link, provenance, warning state, and slot assignment. Workspace persistence commits the records together. If it fails, no record or assignment is accepted and the staged file is removed best-effort. The previous confirmed state remains intact.

One artwork operation may run per Item document. Generate becomes Cancel and reports generation, normalization, or saving progress. Cancel, Item close/change, or Listing Configuration change cancels local work where possible and invalidates the operation identity so late results cannot apply. Once a provider POST has been dispatched, it is never automatically retried and cancellation warns that provider cost may still occur.

## Architecture and Responsibility Placement

- **Domain:** offering-primary ownership and active-area invariants; deterministic supported-size ranking; generated provenance value semantics where they are persistence-independent.
- **Application:** readiness policy, prompt construction, endpoint request planning, operation identity/cancellation, orchestration, and logical persistence transaction.
- **Integration:** image catalog/endpoint mapping, OpenRouter request and result translation, bounded payload handling, raster normalization, SQLite/snapshot migration, and managed-file cleanup.
- **App:** AI Settings selector, Store Design Area checkbox, Design controls and status, stale-SLL warning/actions, generated-history presentation, and accessibility.

Provider SDK or wire types do not cross into Application or Domain. UI view models do not implement size ranking, prompt policy, raster operations, or persistence invariants.

## Data and Migration Plan

1. Add nullable offering-primary persistence to SQLite and workspace snapshots. Existing data loads as null.
2. Extend settings serialization with an independent Artwork selection. Existing settings load with no selection; no General value is copied.
3. Add SLL source fingerprint or equivalent persisted comparison data plus stale state in a backward-compatible form. Existing SLL without sufficient source evidence is handled conservatively according to the migration implementation and covered by tests.
4. Add versioned generated-artwork metadata parsing. Missing or unknown metadata versions never reclassify a manual asset as generated.
5. Keep generated files and metadata readable if the feature is later disabled; manual preview, download, import, and slot behavior remain available.

## Error and Edge-Case Policy

- No target, row, complete Concept, credential, selected image model, or compatible endpoint: do not dispatch; explain the exact prerequisite.
- Catalog refresh failure: use only a compatible cached image catalog, mark it stale, and still enforce endpoint constraints at dispatch.
- Provider timeout, authentication, rate limit, moderation, or malformed response: no retry and no Design mutation.
- Excessive encoded bytes, decoded pixels, unsupported raster, decode failure, or fully transparent result: reject and retain no asset.
- Requested transparency but opaque result: apply and persist a warning.
- Persistence failure after staging: preserve prior state and clean up the staged file best-effort.
- Read-only Item or catalog: show state but permit no mutation.
- Primary area becomes inactive: explicit archive/removal flow clears it; generation never falls back by order.

## Risks / Trade-offs

- **Provider metadata can be incomplete.** Fail closed, preserve the saved selection, and distinguish cached readiness from request-time enforcement.
- **Nearest-ratio fitting can leave transparent margins.** This is explicit, deterministic, and safer than undisclosed crop or distortion; the later bounds editor can refine placement.
- **Ordinary enlargement can soften artwork.** This is accepted for the first module and avoids AI-upscale cost and non-determinism.
- **Persisting prompts may include user-authored sensitive content.** Store locally only, expose it in secondary details, and never add secrets or transmit Supporting Images implicitly.
- **Generation can incur cost despite cancellation or uncertain transport failure.** Never retry automatically and make the cost ambiguity visible.
- **One Asset shown in two places may look duplicated.** Present slot assignment and Supporting Images as two views of one shared managed asset with consistent identifiers/actions.

## Implementation Plan

1. Add offering-primary, SLL staleness/source tracking, Artwork settings, and generated metadata persistence with backward-compatible migrations and round-trip tests.
2. Define image catalog, endpoint capability, request/result, provenance, raster normalization, and operation contracts at the Application boundary.
3. Implement pure policies for readiness, prompt assembly, endpoint filtering, size ranking, warning derivation, and operation invalidation.
4. Implement OpenRouter image catalog/detail and generation adapters with ZDR filtering, one-request semantics, bounded payloads, and normalized errors.
5. Implement deterministic raster normalization and fixtures for alpha, aspect-ratio mismatch, up/downscale, format conversion, safety inset, and rejection paths.
6. Implement logical save/orchestration, generated history, replacement, unassignment, deletion guards, and cleanup.
7. Add AI Settings Artwork configuration, Store primary checkbox, Concept stale-SLL handling, and Design generation/history UI with keyboard and headless coverage.
8. Verify every named scenario, strict OpenSpec validation, full deterministic tests, and scoped completion QA before implementation is considered complete.

### User-reported corrections

- Normalize text and image catalog descriptors by model ID in `AiSettingsViewModel`, unioning declared modalities and supported parameters so a cached or live text-only duplicate cannot shadow image capabilities during Artwork readiness resolution. Keep ZDR compatibility conservative across duplicates.
- In `DesignStageToolViewModel.GenerateArtworkAsync`, pass the caller token to the post-save `LoadAsync`; the loader cancels its previous artwork-operation token as part of starting a new load.
- In `ArtworkGenerationService`, resolve the Item's Niche from the loaded snapshot and add its name, description, audience, humor style, visual guidance, constraints, risks, research notes, and notes to `ArtworkPromptContext`. `ArtworkPromptBuilder` labels those values as untrusted creative data and directs image generation to return flat artwork without a product, wearer, or mockup.
- Focused regression tests for model duplicate merging, prompt fields and exclusions, and a successful post-save view reload remain outstanding.

## Acceptance-to-Verification Map

| Delta scenarios | Verification |
|---|---|
| `User configures Artwork`; `General text model changes`; `Artwork model is absent`; `User opens AI Settings with image models available`; `Artwork is not configured`; `Privacy policy changes`; `User operates Artwork settings by keyboard` | Settings serialization/view-model tests plus focused Avalonia headless settings tests. |
| `ZDR is required`; `One model has heterogeneous endpoints`; `Image catalog refresh fails`; `No image catalog is available`; `Compatible request is dispatched`; `Transient failure follows dispatch`; `Provider returns excessive data` | Application endpoint-policy tests and isolated Integration catalog/request adapter tests with deterministic fixtures. |
| `User marks a Design Area primary`; `User leaves an offering without a primary`; `User reviews an archived Store or read-only offering`; `Cross-offering primary is requested`; `User archives the primary Design Area`; `User permanently removes the primary Design Area`; `Archive or removal is cancelled or fails` | Domain ownership tests, repository/snapshot round trips, application transaction tests, and Store editor headless tests. |
| `Primary Design Area supplies the initial target`; `Offering has no primary Design Area`; `No valid Listing Configuration target exists`; `Listing Configuration changes`; `Explicit target and transparency preference are persisted per Item` | `DesignStageServiceTests.SaveArtworkPreferencesAsync_LegacyDesignArea_RoundTripsAndIsRestored`; `DesignStageServiceTests.SelectConfigurationAsync_ClearsPersistedArtworkPreferences`; `DesignStageToolHeadlessTests.ArtworkGenerationSection_ExposesTargetAndTransparencyControls`. |
| `Complete workflow is ready`; `Concept is incomplete`; `Default row does not exist`; `Design is read-only`; `Artwork AI is unavailable` | Framework-free readiness tests plus headless disabled/read-only guidance tests. |
| `Target recommends transparency`; `Target changes`; `Model does not support transparency` | Readiness/default policy tests and headless checkbox-state tests. |
| `Current SLL is available`; `SLL is absent or stale`; `Phrase is supplied`; `Workspace content resembles instructions`; `Supporting Images exist` | Prompt-builder tests that inspect exact resolved requests and prove reference images/secrets are absent. |
| `Concept content changes after SLL generation`; `Original Idea changes after SLL generation`; `User keeps stale SLL for reference`; `User requests SLL reset`; `User confirms SLL reset`; `User regenerates SLL` | SLL application/persistence tests plus focused Concept view-model and headless warning/action tests. |
| `Exact target size is supported`; `Multiple smaller sizes are available`; `Every supported size is larger`; `No compatible endpoint exists` | Pure size-ranking and endpoint-selection tests, including deterministic tie cases and the 3692×4800 example class. |
| `Smaller closest-ratio result is returned`; `Larger closest-ratio result is returned`; `Provider returns a non-PNG raster`; `Provider returns invalid or unsafe output`; `Provider returns no visible artwork`; `Transparency request succeeds`; `Provider returns opaque artwork` | Isolated raster fixture tests asserting exact pixels/dimensions, safety inset, alpha/warnings, bounds, and absence of retained originals. |
| `Empty target slot receives artwork`; `Occupied target slot receives new generated artwork`; `Existing generated artwork is replaced by manual upload`; `Persistence fails after file creation` | Application orchestration tests with repository/file-store fakes plus persistence reload and cleanup tests. |
| `Generation is running`; `User cancels`; `Request fails after dispatch` | Coordinator cancellation/late-result/no-retry tests and headless progress/Cancel tests. |
| `Provider returns complete metadata`; `Optional provider metadata is absent`; `User reviews generated history`; `Generated result remains assigned`; `Generated result is replaced` | Metadata round-trip, history projection, ordering, shared-asset identity, and headless presentation tests. |
| `User previews a Design file`; `User exports a Design file`; `Managed file is missing`; `User removes a manual Design file`; `User removes generated artwork from a slot`; `User tries to delete assigned generated artwork`; `User deletes unassigned generated artwork`; `Removal persistence fails` | Existing asset regressions plus focused application transaction, file-store, reload, and headless action-state tests. |

## Open Questions

No product, UX, data, architecture, or acceptance decision remains open for this delivery module. The concrete raster library and exact OpenRouter wire fields are implementation choices constrained by these requirements and must not alter them without reopening the change.
