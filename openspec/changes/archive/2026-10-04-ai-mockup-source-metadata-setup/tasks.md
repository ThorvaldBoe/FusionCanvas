## 1. Provider-independent multimodal AI boundary

- [x] 1.1 Add optional image parts and bounded image-input request contracts while preserving existing text-only AI callers.
- [x] 1.2 Extend General AI configuration resolution to expose whether the selected model supports image input, including ZDR compatibility behavior.
- [x] 1.3 Update the OpenRouter adapter and mock twin to serialize image-bearing messages, propagate ZDR, enforce request limits, and avoid recording image bytes in diagnostics.
- [x] 1.4 Add focused AI contract and integration-adapter tests for text-only compatibility, multimodal serialization, unsupported image input, cancellation, limits, and telemetry redaction.

## 2. Mockup metadata-assistance application service

- [x] 2.1 Add request, result, per-image status, confidence, and context contracts under `FusionCanvas.Application.Mockups`.
- [x] 2.2 Add a bounded local image-content reader port and integration implementation that can read both managed and newly uploaded-but-unsaved selected image paths without persistence side effects.
- [x] 2.3 Implement metadata-first request assembly using filenames, dimensions, current metadata, active Offering options/values, existing mappings, and Design Area context.
- [x] 2.4 Implement conditional image inclusion for unresolved visual fields, General AI invocation, stable option-token mapping, response parsing, and validation of unknown/malformed/duplicate/missing results.
- [x] 2.5 Implement all-size fallback, confidence-aware replacement rules, deterministic compatible placement-reference selection, conflict preservation, per-image partial failures, and cancellation/stale-operation protection.
- [x] 2.6 Add application tests covering metadata-only requests, conditional image requests, ZDR propagation, option allowlisting, size fallback, placement reuse/conflict handling, replacements, malformed responses, partial failures, unavailable AI, unreadable images, and cancellation.

## 3. Mockup editor integration

- [x] 3.1 Wire the assistance service and image reader through `AppWorkspaceFactory`, application-service composition, and Store Management construction.
- [x] 3.2 Add Catalog setup command state, availability guidance, busy/cancel state, operation versioning, and draft-only application of per-image results.
- [x] 3.3 Extend local source draft presentation with transient assistance status and review-needed indicators without persisting AI state.
- [x] 3.4 Add the metadata-assistance action beside the existing source-image table actions with accessible name/help text, selection gating, and progress/error presentation.
- [x] 3.5 Preserve and verify existing selection gestures, active-row editing, unsaved-change prompts, manual editing, and normal Template Save behavior.

## 4. Application and UI verification

- [x] 4.1 Add framework-free Catalog setup tests for command gating, draft application, high-confidence replacement, uncertain-result preservation, per-row status, partial results, and no metadata loss.
- [x] 4.2 Add Avalonia headless tests for no-selection and unavailable states, action invocation, busy/cancel presentation, selected-row status, accessible metadata, keyboard focus, and draft-only results.
- [x] 4.3 Run focused application, integration, and App tests and fix all regressions in the affected scope.

## 5. Completion verification

- [x] 5.1 Reconcile any approved artifact corrections and record criterion-level results and evidence in `verification.md`.
- [x] 5.2 Run `git diff --check` and `openspec validate --all --strict`.
- [x] 5.3 Run the required solution baseline: `dotnet test .\\FusionCanvas.sln`.
