## Context

Issue #726 follows the completed Mockup source-image multi-selection work. The current editor stores uploaded and managed source images as transient `LocalMockupSourceDraftViewModel` rows, keeps one active row for detailed editing, and persists all reviewed changes through the existing Mockup Template Save workflow. The current AI text boundary is provider-independent but text-only, while source-image metadata assistance needs structured results and optional image input.

The primary workflow is an occasional but high-friction setup action in the focused Mockup Template dialog:

```text
Select source rows
        │
        ▼
Set up metadata with AI
        │
        ├── filename/context resolves fields ──► metadata-only request
        │
        └── unresolved visual field ────────────► request with selected image input
        │
        ▼
Apply per-row draft results
        │
        ▼
Review/edit, then Save Template
```

The action must improve batch throughput without introducing hidden persistence, provider-specific UI logic, or an AI dependency for ordinary manual setup.

## Goals / Non-Goals

**Goals:**

- Assist all selected new and existing local source-image drafts in one user operation.
- Use filenames, dimensions, existing metadata, Offering option values, existing mappings, and Design Area context as first-class inputs.
- Add image content only when unresolved metadata needs visual evidence, while honoring the configured ZDR setting.
- Return structured, validated per-image results with success, uncertainty, and failure states.
- Apply results to editable drafts and retain the existing Save, cancellation, and revision behavior.
- Reuse established placement mappings instead of asking AI to invent pixel coordinates.
- Cover application logic, provider serialization, and meaningful Avalonia interaction risk with deterministic tests.

**Non-Goals:**

- No automatic persistence, AI-result history, or new database schema.
- No general-purpose image understanding, garment segmentation, artwork generation, or visual placement editor.
- No independent Mockup-specific AI settings profile; use General AI configuration.
- No replacement of manual metadata controls or source-image multi-selection behavior.
- No image upload when filename and existing context are sufficient for the requested fields.

## Decisions

### 1. Add a Mockup metadata-assistance application service

Create an application-facing service such as `IMockupSourceMetadataAssistanceService` under `FusionCanvas.Application.Mockups`. Its request contains immutable source-image inputs and a bounded Template context; its result contains one validated outcome per source draft. The App project owns selection, draft mutation, busy state, and presentation only.

This keeps orchestration and response validation out of `CatalogSetupViewModel`, while allowing the service to be tested with deterministic AI and file-content collaborators.

### 2. Extend the AI boundary for optional image-bearing messages

Preserve the existing General profile resolution and ZDR flag, but extend the provider-independent AI request model with optional image parts containing bytes and MIME type. Existing text-only calls continue to serialize as string content. Image-bearing calls serialize through the provider adapter as multimodal message content.

The application must not expose OpenRouter types to the UI. The OpenRouter adapter and mock twin own serialization, image-input capability checks, and provider response details. Image-bearing telemetry must redact or omit request image bytes even when diagnostic capture is enabled.

### 3. Use metadata-first conditional image inclusion

Before any provider call, the assistance service identifies fields that can be resolved deterministically:

- an unambiguous active Color token in the filename resolves Color without image input;
- existing validated metadata is available as context and may be retained or replaced only by a supported, high-confidence result;
- Size falls back to all active Size values when no reliable Size signal exists;
- placement is selected from existing draft mappings, not inferred from raw pixels.

If a selected row still needs visual evidence for an unresolved field, its image content is included in the multimodal request when the configured General model supports image input. Rows that do not need image evidence remain metadata-only. If visual evidence is required but unsupported, that row receives an explicit review/failure result while metadata-only rows continue.

### 4. Treat provider output as untrusted structured data

The prompt requests one JSON result per stable draft token. The service accepts only known option-value tokens, bounded confidence values, and valid result states. Unknown values, invalid JSON, duplicate row results, missing row results, out-of-range confidence, and invalid mappings become per-row review/failure outcomes rather than exceptions that discard the batch.

Stable opaque tokens map back to Offering Option Value identities in application code; the model never creates or chooses arbitrary database identifiers.

### 5. Apply results directly to drafts, not persistence

The App view model applies accepted fields to each selected `LocalMockupSourceDraftViewModel`, records transient assistance status, and refreshes row summaries. Existing manual values remain visible and editable. High-confidence, contextually supported suggestions may replace selected-row values because the user explicitly invoked setup and can review the result before Save; uncertain values do not replace existing data.

This uses the current draft dirty-state and cancellation workflow and avoids creating revisions for an abandoned AI attempt.

### 6. Reuse placement references deterministically

For each selected row, candidate mappings come from other current draft rows with valid mappings. Ranking prefers matching inferred Color, overlapping Size values, and then the most common mapping among compatible candidates. A single materially consistent winner is copied after dimension normalization/validation. No candidate or conflicting candidates leave placement unchanged and add a review status.

### 7. Keep the feature in the focused editor

The action belongs beside Upload and Archive in the existing source-image table header. It is visible when relevant, disabled with an actionable reason when unavailable, shows a compact busy state, and exposes per-row review status without expanding the primary workspace. Existing keyboard selection, focus, and unsaved-change behavior remain intact.

## Risks / Trade-offs

- [Risk] Image-bearing AI requests can expose local creative assets to an external provider. → Include image bytes only when needed, follow the configured ZDR setting, show concise action guidance, and never persist or log image payloads in diagnostics.
- [Risk] Filenames may contain ambiguous or misleading color words. → Require unambiguous matching against active Offering values; otherwise use visual input or mark review needed.
- [Risk] AI may return plausible but invalid option values or malformed JSON. → Use stable allowlisted tokens, strict parsing, deterministic validation, and per-row failure results.
- [Risk] Existing mappings may encode legitimate differences rather than one convention. → Copy only an unambiguous compatible reference; preserve conflicting mappings and surface review.
- [Risk] A large selection may exceed provider context or local memory limits. → Bound selected-row count, metadata size, image byte size, and response size; report excluded rows individually and keep successful results.
- [Risk] The current AI configuration may select a text-only General model. → Permit metadata-only processing and report only the rows requiring unsupported visual input; do not make the whole editor unusable.
- [Risk] A late asynchronous result could mutate a different Template draft. → Use an operation version tied to the active Template/draft identity and discard stale results.

## Migration Plan

No database migration is required. Existing source-image rows, mappings, revisions, and manual editing remain compatible. The feature is additive and can be rolled back by removing the action and service wiring; no persisted AI state needs cleanup.

## Open Questions

No blocking product decisions remain. The implementation must preserve the decisions above: conditional image inclusion, General AI configuration, ZDR propagation, draft-only application, confidence-aware replacement, and deterministic placement reuse.

## Implementation Plan

1. Add provider-independent multimodal AI request types and extend the General AI generation path so optional image parts, model input capability, ZDR propagation, cancellation, and image-safe telemetry are handled below the UI. Update the OpenRouter adapter and mock twin tests.
2. Add Mockup metadata-assistance request/result contracts, metadata-first request assembly, structured response parsing, confidence handling, known-option mapping, all-size fallback, and deterministic placement-reference selection in `FusionCanvas.Application.Mockups`.
3. Add a bounded local image-content reader/application port for selected draft paths, including managed and newly uploaded-but-unsaved files, with size/type/cancellation safeguards.
4. Wire the service through `AppWorkspaceFactory`, `MainWindowApplicationServices`, `StoreManagementViewModel`, and `CatalogSetupViewModel`. Add the command, operation versioning, draft application, per-row status, and availability guidance.
5. Update `MockupTemplateEditorWindow.axaml` and its code-behind for action state, busy/cancel presentation, row review status, and accessible names/help text while preserving current selection gestures and focus behavior.
6. Add framework-free application and view-model tests for metadata-first behavior, conditional image input, replacements, size fallback, placement reuse/conflicts, malformed/partial responses, unavailable AI, cancellation, and no metadata loss.
7. Add Avalonia headless tests for no-selection gating, action invocation, status presentation, draft-only application, partial results, and keyboard/focus behavior. No new Appium journey is warranted: the risk is deterministic binding, command, and routed-input behavior already covered by the headless harness.
8. Run focused tests, the full `dotnet test .\\FusionCanvas.sln` baseline, `git diff --check`, `openspec validate --all --strict`, and record criterion-level evidence in `verification.md`.

## Acceptance-to-verification mapping

| Acceptance area | Planned verification |
| --- | --- |
| No-selection and unavailable gating | `CatalogSetupViewModelTests` and `StoreEditorHeadlessTests` |
| Batch result per selected row and unselected-row preservation | Application service tests and headless action test |
| Metadata-first request assembly and conditional image parts | Application AI-assistance tests with recording doubles |
| ZDR propagation and image-safe provider serialization | OpenRouter adapter tests and AI contract tests |
| Known option mapping and all-size fallback | Application service tests |
| Placement reuse, conflict preservation, and review status | Application service tests and view-model tests |
| Draft-only mutation and normal Save persistence path | View-model tests plus existing Mockup persistence regression tests |
| Partial failure, malformed response, unavailable provider, and cancellation | Application and view-model tests |
| Accessible action, busy state, row status, and focus | Avalonia headless tests |
