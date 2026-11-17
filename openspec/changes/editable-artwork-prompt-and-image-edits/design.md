## Context

The Design stage currently assembles an image prompt inside `ArtworkGenerationService` immediately before dispatch. It combines fixed instructions, Item Concept fields, the linked Niche metadata, a stale-checked SLL blob, design-area data, and Item notes. The user cannot inspect or edit that assembled text before generation. `AiImageGenerationRequest` is text-only, while OpenRouter's Image API accepts reference images through `input_references`; endpoint catalog parameters already expose model-specific request capabilities.

The selected Design slot stores normalized PNG artwork as a managed `ExportedImage` Asset. Item metadata is extensible JSON and already stores per-Item Design preferences, so no SQLite migration is needed. The current slot assignment and related Asset links are persisted in the workspace snapshot.

## Goals / Non-Goals

**Goals:**
- Make a deterministic, inspectable default prompt that separates design content, composition, style/contrast, and print constraints.
- Let creators edit and persist the exact prompt used for generation or image editing, then reset it from current saved context.
- Make foreground contrast explicit and independent from transparent/opaque background behavior.
- Edit the existing assigned PNG through the same image endpoint using one reference image, while preserving the original and atomically replacing the selected slot on success.
- Gate image editing on the selected endpoint's advertised `input_references` capability.

**Non-Goals:**
- Curated style catalogs or preset management; this module only creates a GitHub issue for that follow-up.
- Inpainting masks, multi-image references, chat-based image editing, batch editing, or support for non-PNG slot assets.
- Guaranteeing exact visual compliance from generative models.
- Changing how transparent/opaque output backgrounds are selected.
- A second AI call to rewrite or optimize the prompt.

## Decisions

### Compile the default prompt deterministically in Application

Add an Application prompt compiler that receives typed Item concept values, linked Niche creative context, current valid composition guidance, target area/print constraints, and the selected `ArtworkContrast`. It emits readable sections with the phrase preserved verbatim. The compiler remains deterministic: identical inputs produce identical text. Fixed safety and production instructions remain in the compiler; editable creative values remain clearly delimited as data.

Only extract layout-relevant material from a structurally valid, current SLL, such as its ASCII composition sketch and relationship. Do not append the full SLL JSON or its assumptions, repeated Triangle, or old product/style descriptions. A stale or malformed SLL contributes no composition guidance. Niche audience, humor, visual style, and constraints are read from the Item's currently linked Niche at compilation time. There is no separate LLM prompt-writing step.

### Persist prompt override and contrast per Item

Add optional metadata keys for the selected contrast, edited generation prompt, and separate adjustment instructions. The absence of an edited generation prompt means the editor displays the freshly compiled default. Save prompt edits when focus leaves the editor, following existing Item text-edit persistence behavior. Reset clears the generation-prompt override and immediately recompiles from the current workspace snapshot. If a creator edits the generation prompt, that exact text is authoritative for the next generation request; changing the contrast selector while the generated default is active updates that default, while a deliberate prompt override remains intact until edited or reset. Adjustment instructions are independent, saved with the Item, and used only for image editing. The UI keeps the selected contrast visible so the user can reconcile an override.

### Reuse the prompt editor for generation and edit instructions

Place a compact, expandable Artwork prompt section in the Design stage, near the artwork generation actions. It contains a Light on dark / Dark on light selector, multiline generation prompt editor, separate multiline adjustment-instructions editor, and Reset to default action. Text-to-image generation sends the current generation editor text. When an assigned final PNG is selected, Adjust existing artwork sends the separate instructions and selected slot PNG as one reference. Unsupported endpoint capability, missing image, blank instructions, read-only Item, busy state, and provider/persistence failure are represented explicitly.

On successful image editing, preserve an immediate copy/link of the input artwork as an Item-linked Related Image, write the normalized PNG as a new managed Asset, and update the selected Design slot in one repository save. A failed save cleans up newly written files and leaves the prior slot and related assets unchanged. Recheck Item, slot, assigned Asset, relevant concept/context, and target area after provider completion before persisting, following the existing stale-context pattern.

### Advertise and send one image reference only where supported

Extend endpoint parameter capabilities with the maximum `input_references` count parsed from the OpenRouter endpoint's `supported_parameters`. Edit endpoint selection requires at least one advertised reference. Extend the application image request with optional image-reference data URL(s); the OpenRouter adapter serializes them using the documented `input_references: [{ type: "image_url", image_url: { url } }]` envelope and keeps the existing generation request shape unchanged when absent. Only send managed PNG bytes for this feature. Never log the base64 body or reference data URL.

### Use headless UI coverage, not a new Appium journey

Prompt text, contrast selection, reset/save states, endpoint availability, and rendered Design controls are deterministic and covered by Application and Avalonia headless tests. A new real-desktop Appium scenario is not warranted: this flow adds no native window, OS integration, or pointer-specific behavior that the headless harness cannot represent. Existing app-level scenario packs remain unchanged.

## Risks / Trade-offs

- **[Risk]** Endpoint catalogs may omit or misreport `input_references` support. → Disable editing unless a positive maximum is advertised; keep ordinary generation independent of that capability.
- **[Risk]** Image editing can alter details outside the requested change. → Keep the prior image as a Related Image and retain the existing explicit slot replacement result; communicate the operation as an AI edit, not a pixel-locked edit.
- **[Risk]** A saved custom prompt can diverge from changed Concept or Niche settings. → Preserve it as an explicit user override and make Reset to default rebuild from current saved context.
- **[Risk]** Reference data URLs enlarge request memory and payload size. → Use one current PNG only and enforce existing image/file limits before encoding; avoid additional reference copies.

## Migration Plan

No database migration is required. Existing Items with no new metadata keys show the compiled default and the default contrast choice. Existing generation remains text-to-image until the creator explicitly chooses Adjust existing artwork.

## Open Questions

None. The selected prompt, contrast, reference asset, and failure semantics are bounded by the proposal, specs, and decisions above.

## Implementation Plan

1. **Application prompt and preference model**
   - Add an `ArtworkContrast` enum, Item metadata keys/codec helpers for contrast, generation prompt override, and adjustment instructions, plus a typed default prompt context/compiler in `FusionCanvas.Application.AI` or `DesignFiles`.
   - Use Item metadata for Concept values and linked Niche context; parse current SLL through the injected `ISllDocumentCodec` and contribute only the current SLL's composition sketch/relationship. Omit stale/invalid SLL.
   - Extend `DesignStageState` with compiled default/current prompt, adjustment instructions, contrast, edit capability, and current assigned artwork identity/availability needed by the UI.
   - Extend `DesignStageService` to persist prompt/adjustment/contrast edits as an atomic Item metadata update and to rebuild defaults from the current repository snapshot.

2. **Prompt editor and contrast control**
   - Update `DesignStageToolViewModel` and `MainWindow.axaml` to expose the generation editor, separate adjustment-instructions editor, contrast selector, reset, and current selected slot's Adjust existing artwork action.
   - Keep editor focus, persistence, reset, read-only, busy, and error states coherent; save explicit prompt overrides with the Item.
   - Add focused App headless tests for bindings, contrast selection, reset, selected artwork edit availability, and read-only/unavailable states.

3. **Reference-image request and endpoint capability**
   - Extend `AiImageEndpointParameterCapabilities`, OpenRouter endpoint parsing, `AiImageGenerationRequest`, and `OpenRouterClient.BuildImageRequestJson` for one optional reference image.
   - Add Application endpoint policy for requiring advertised input-reference support only for edit operations. Update `AiImageGenerationService`/configuration projections as needed so edit availability reflects the selected model/provider endpoint.
   - Add integration tests for reference serialization and capability parsing, including a generation request with no reference remaining byte-for-byte/schema compatible in field set.

4. **Application edit flow and persistence**
   - Extend `ArtworkGenerationService` with an explicit generate/edit operation. For edits, read the selected assigned PNG from the managed file store, encode a PNG data URL, submit current prompt plus reference, normalize output, verify captured context is still current, and atomically preserve the prior image as a related Item Asset while assigning the new generated Asset.
   - Preserve cleanup behavior on save failure/cancellation and reject unavailable/missing source images or unsupported endpoint edits without dispatch.
   - Add Application tests for exact prompt/reference delivery, successful replacement/backup, stale-source rejection, unsupported capability, and failure cleanup.

5. **Verification and follow-up issue**
   - Create one GitHub issue for curated styles and keep it out of this code change.
   - Map each scenario below to focused tests and run `dotnet test .\FusionCanvas.sln` plus strict OpenSpec validation. Record scenario-level evidence in `verification.md`.

### Acceptance-to-verification mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Current default prompt reflects Concept and Store settings | Application compiler test asserts current fields, deterministic output, selective SLL composition, and omission of repeated SLL Triangle/assumptions. |
| Creator edits and uses image prompt | App headless editor test plus Application provider fake asserts exact prompt dispatch and prompt override reload. |
| Creator resets prompt after source changes | Application persistence/compiler test changes source snapshot, resets, and verifies current values replace saved override. |
| Prompt default excludes stale SLL | Application compiler test with a mismatching fingerprint. |
| Light on dark / Dark on light | Compiler tests verify explicit foreground instructions and no background fill instruction; App headless test verifies persistence. |
| Creator edits contrast-bearing prompt | App headless test verifies changing contrast with default active recompiles and reopen restores the selected value. |
| Creator adjusts assigned artwork | Application service test verifies one reference image and exact adjustment instructions, unchanged generation prompt, normalized output assignment, and preservation of prior image as Related Image. |
| Editing unavailable for unsupported endpoint | Endpoint policy test plus App headless test for disabled action and explanation. |
| Edit fails/cancels/stale context | Application tests assert existing assignment/assets remain unchanged and staged output cleanup runs. |
