## 1. Application contracts and service

- [x] 1.1 Add niche-population request, supported-field identifiers, suggestion, and result contracts under `src/FusionCanvas.Application/Niches`.
- [x] 1.2 Implement `NichePopulationService` over `IAiTextGenerationService`, using `AiRequestPurpose.General` and a prompt that treats the niche name as untrusted creative data.
- [x] 1.3 Implement structured-response parsing that accepts only supported eligible fields, excludes Risks and Research notes, rejects responses with no usable values, and preserves user-safe failure kinds/messages.
- [x] 1.4 Add application tests covering General-purpose request assembly, blank-field filtering, independent values, print-on-demand t-shirt visual guidance, malformed/empty output, provider failure, and excluded fields.

## 2. Application composition and availability

- [x] 2.1 Compose the niche-population service through the existing workspace/application startup path without adding a new AI settings profile or persistence dependency.
- [x] 2.2 Add Store Management availability refresh wiring for General AI settings and availability changes.
- [x] 2.3 Add view-model tests for missing name, unavailable AI, archived niche exclusion, readiness refresh, and no-request guarantees.

## 3. Store Management draft behavior

- [x] 3.1 Add the `Populate` command, eligibility state, busy state, and recoverable status/error state to `StoreManagementViewModel`.
- [x] 3.2 Capture the blank eligible fields at request start and apply each result only if its field is still blank when the request completes.
- [x] 3.3 Preserve the niche name, Risks, Research notes, and all pre-existing non-blank eligible values.
- [x] 3.4 Ensure population updates only in-memory draft properties, marks ordinary unsaved niche changes, prevents overlapping requests, and leaves the existing Save and discard-confirmation flows authoritative.
- [x] 3.5 Add view-model tests for successful draft application, blank-only behavior, concurrent edits, name preservation, draft-only persistence, busy overlap prevention, failure recovery, and incomplete responses.

## 4. Focused editor UI

- [x] 4.1 Add the compact `Populate` button beside the niche name input in `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`.
- [x] 4.2 Provide a stable automation id, meaningful accessible name, predictable keyboard order, disabled guidance, and a distinguishable in-progress treatment.
- [x] 4.3 Add Avalonia headless tests for button placement, binding, accessibility metadata, eligibility, busy state, successful population, and preservation of the existing draft/discard interaction.

## 5. Criterion-level verification and delivery gates

- [x] 5.1 Map every scenario in `openspec/changes/niche-ai-population/specs/niche-ai-population/spec.md` to passing focused application, view-model, or headless UI evidence; correct and rerun any failed criterion.
- [x] 5.2 Run `openspec validate` and resolve all strict validation findings for the change package.
- [x] 5.3 Run `dotnet test .\FusionCanvas.sln` and resolve all regressions before implementation completion.
- [x] 5.4 Record final verification evidence and limitations in the implementation change before archiving the OpenSpec package.
