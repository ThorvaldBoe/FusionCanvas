## Context

The Store Management → Niches editor already owns the niche name and editable context fields, and `NicheManagementService` already persists those fields through `NicheContext`. The application also has a shared `IAiTextGenerationService` boundary that resolves credentials, the model catalog, privacy settings, and the configured General profile.

Issue 397 adds a user-invoked suggestion step to this focused editor. The feature must remain draft-oriented: the user reviews and edits suggestions, then uses the existing Save action. It must not turn niche setup into autonomous research or add another AI settings surface.

## Goals / Non-Goals

**Goals:**

- Provide a `Populate` command beside the niche name field.
- Fill only blank Description, Audience, Humor style, Visual style guidance, Constraints, and Notes fields.
- Generate each field independently from the current niche name using the General AI profile.
- Keep Risks and Research notes entirely manual.
- Preserve draft, discard-confirmation, keyboard, accessibility, busy, and error behavior expected by the focused editor.
- Guide Visual style suggestions toward practical print-on-demand t-shirt graphics.

**Non-Goals:**

- No automatic persistence or automatic niche creation.
- No population for archived niches.
- No niche-specific AI profile or AI settings UI.
- No web research, trend analysis, legal validation, market claims, or autonomous niche recommendations.
- No changes to the existing manual niche save, archive, restore, or delete semantics.
- No production code or tests are part of this planning-only change.

## Decisions

### Use a dedicated Application-layer use case

Add a focused niche population service in `FusionCanvas.Application.Niches` rather than putting prompt construction, response parsing, or AI orchestration in `StoreManagementViewModel`. The service will depend on `IAiTextGenerationService`, expose General-purpose availability, and return a structured suggestion result without touching persistence.

This preserves the existing inward dependency direction and makes prompt and parsing behavior testable with deterministic fakes. Extending `NicheManagementService` was rejected because population is a draft-generation concern, not a persistence operation.

### Use the General AI profile

Population requests use `AiRequestPurpose.General`. This matches the requirement that the action depends on the main General model and avoids expanding settings with a new niche-purpose profile. The existing AI service remains responsible for credential, model, privacy, and parameter validation.

### Request only blank fields

At command time, the view model determines which eligible fields are blank using whitespace-aware checks. The application request contains the name and requested blank field identifiers, not existing non-blank values. The response is a JSON object whose supported properties map independently to field values. Unknown properties and excluded fields are ignored.

The UI applies a returned value only if the corresponding field is still blank when the request completes. This protects edits made while the request was in flight and prevents overwriting existing work.

### Keep output draft-only

Successful suggestions are assigned to the view model’s existing niche draft properties. This naturally participates in `HasUnsavedNicheChanges`, the existing Save action, and the existing discard-confirmation flow. The population service receives no repository dependency and cannot persist data.

### Make visual guidance explicitly t-shirt-oriented

The system prompt and field-specific instruction will describe Visual style guidance as practical direction for typical print-on-demand t-shirt graphics: wearable, legible, scalable, and suitable for common DTG or screen-print workflows. The prompt will explicitly exclude mockup photography, garment presentation, and product-styling direction. The generated text remains a user-editable creative suggestion, not a production guarantee.

### Keep research and risk fields manual

Risks and Research notes remain available as ordinary editable fields, but are not included in the population request or response contract. This avoids presenting unverified AI output as legal, safety, market, or research knowledge.

### UI placement and states

The action belongs in the focused Niches editor, directly beside the niche name field, because the niche name is the only required input and the action is occasional setup work. The button uses the compact command sizing already used by the editor. It has a stable automation id and accessible name, is disabled when unavailable, shows a visible busy treatment while running, and returns to an editable state after success, failure, cancellation, or unusable output.

## Risks / Trade-offs

- [AI output is malformed or unexpectedly verbose] → Require a structured JSON object, validate supported string values, ignore unknown properties, reject responses with no usable values, and add parser tests.
- [A slow request overwrites user edits] → Capture the blank-field set at request start and re-check each target field is still blank before applying its result.
- [Users mistake suggestions for verified niche research] → Exclude Risks and Research notes, use draft-oriented UI copy, and keep explicit user review and Save semantics.
- [General AI settings change while the editor is open] → Refresh population availability from the existing Settings AI change notifications without recreating the editor.
- [Adding another constructor dependency increases composition complexity] → Add one focused optional service seam to the existing Store Management composition and keep all AI behavior behind the Application contract.
- [AI prompt injection through a niche name] → Mark the niche name as untrusted creative data in the system instruction and never treat model output as executable instructions.

## Migration Plan

No database or settings migration is required. The feature is additive and reuses existing niche fields and General AI configuration. If the change is rolled back, existing manual niche editing and persisted data remain unaffected.

## Open Questions

None for the agreed first module. The implementation must not reopen field scope, overwrite semantics, AI profile choice, archived-niche eligibility, or visual-style direction.

## Implementation Plan

1. Add `NichePopulationRequest`, supported field identifiers, result types, and `INichePopulationService` under `src/FusionCanvas.Application/Niches`.
2. Implement `NichePopulationService` using `IAiTextGenerationService` with `AiRequestPurpose.General`, a system/user prompt, JSON response validation, independent field mapping, and user-safe failure results.
3. Compose the service through the workspace/application startup path and pass it to `StoreManagementViewModel` without exposing provider details to the UI.
4. Extend `StoreManagementViewModel` with availability, busy, command, and status state. Re-evaluate availability when `Settings.Ai.SettingsChanged` or `AvailabilityChanged` fires. Preserve fields changed during an in-flight request.
5. Add the compact `Populate` button beside the niche name input in `StoreEditorWindow.axaml`, with accessible naming, automation id, keyboard order, busy treatment, and guidance for unavailable/error states.
6. Add focused Application tests for request assembly, General purpose selection, field filtering, visual-style instruction, structured parsing, malformed output, and provider failures.
7. Add App view-model tests for eligibility, blank-only application, preservation of existing and concurrently edited fields, draft-only behavior, busy/failure states, settings refresh, and archived-niche exclusion.
8. Add Avalonia headless coverage for rendered placement, binding, accessibility id/name, enablement, and the successful editor journey. Do not add Appium coverage; no native desktop behavior or external-service integration is required for this action.
9. Run strict OpenSpec validation and `dotnet test .\FusionCanvas.sln` before implementation is considered complete.

## Acceptance-to-Verification Map

| Acceptance area | Planned verification |
| --- | --- |
| Eligible drafts, blank names, unavailable General AI, archived niches | View-model tests plus headless control enablement test |
| Blank-field filtering, independent values, General purpose, visual-style instruction | Application service tests inspecting the captured `AiTextRequest` |
| Draft-only application, name preservation, existing-field preservation, user edits | View-model tests plus headless editor journey |
| Busy, overlap prevention, provider failure, malformed/incomplete output | Application and view-model tests with controllable fake service |
| Settings refresh, keyboard order, accessible action, discard confirmation | View-model notification test plus Avalonia headless view tests |
| Persistence compatibility | Existing niche-management tests remain the regression baseline; no migration is expected |
