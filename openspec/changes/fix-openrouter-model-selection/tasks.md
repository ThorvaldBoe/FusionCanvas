# Tasks: fix-openrouter-model-selection

## 1. Application failure contract

- [ ] 1.1 Add `AiModelCatalogFailureKind` (`Authentication`, `RateLimited`, `NetworkOrService`, `InvalidResponse`, `ZdrDataUnavailable`) and `AiModelCatalogFetchException` (kind plus optional retry-after) under `src/FusionCanvas.Application/AI`; keep every provider port signature unchanged.

## 2. Honest OpenRouter catalog fetch

- [ ] 2.1 In `OpenRouterClient.GetModelsAsync`, remove the non-existent `zdr` query parameter from the `api/v1/models/user` request and replace `EnsureSuccessStatusCode` with explicit status mapping to the new failure kinds (401 → `Authentication`, 429 → `RateLimited` with retry-after, other non-success/timeout/IO → `NetworkOrService`, JSON/shape violations → `InvalidResponse`), preserving the single transient retry.
- [ ] 2.2 Fetch `api/v1/endpoints/zdr` on every catalog load without an authorization header through the same bounded-read pipeline; build a case-insensitive model-ID set; fail with `ZdrDataUnavailable` when the list cannot be read while ZDR is required, and degrade to an empty set when it is not.
- [ ] 2.3 Stamp `ZeroDataRetentionCompatible` in `ParseModel` from the set under both policies so the flag is a model fact.
- [ ] 2.4 Extend `tests/FusionCanvas.Integration.Tests/AI/OpenRouterClientTests.cs`: no `zdr` query parameter and bearer key on the models request, no authorization header on the ZDR-list request, true/false flags from listed/unlisted models under both policies, ZDR-list failure closed/degraded per policy, status-to-kind mapping with retry-after, and unchanged single retry.

## 3. View-model orchestration and robustness

- [ ] 3.1 Add `EnsureCatalogAsync(bool force)` to `AiSettingsViewModel` containing the refresh body in the new order: fetch, apply to selector, then cache save inside its own degradation `try/catch` (warning message, models retained); map failure kinds to secret-safe actionable messages.
- [ ] 3.2 Wire automatic triggers: after a `Valid` validation result (`force: true`); in `EnsureLoadedAsync` when a readable credential exists and no cache exists for the active policy; after both ZDR policy transitions when no cache exists for the new policy; automatic triggers skip while a load is running and never fire without a readable credential; remove the fire-and-forget cache loads; `RefreshModelsCommand` calls `EnsureCatalogAsync(force: true)`.
- [ ] 3.3 Extend `ApplyModelFilter` to narrow to `ZeroDataRetentionCompatible` models while ZDR is required, re-evaluate on policy change, and add empty-selector guidance messages (no key / not loaded / no compatible models) on the existing inline message surface.
- [ ] 3.4 Extend `tests/FusionCanvas.App.Tests/Settings/AiSettingsViewModelTests.cs` with fakes for every trigger and guard in 3.2, cache-save degradation, failure-kind message mapping with secret absence, ZDR selector narrowing and restoration, and guidance text per empty cause.

## 4. Headless view coverage

- [ ] 4.1 Extend `tests/FusionCanvas.App.Tests/Settings/AiSettingsViewTests.cs`: empty state renders guidance with `Refresh models` available; after a fake successful validation the General model `ComboBox` lists exactly the fetched ZDR-compatible models.

## 5. Verification and completion gates

- [ ] 5.1 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` with zero warnings and zero failures.
- [ ] 5.2 Run `openspec validate fix-openrouter-model-selection --strict` and `openspec validate --all --strict`.
- [ ] 5.3 Write `verification.md` mapping every delta-spec scenario to its test evidence; record the changed-scope security check (no credential sent to the public ZDR endpoint, bounded parsing, secret-safe messages); confirm the `openrouter-api-configuration` archive-ordering dependency.
- [ ] 5.4 Optional supplemental live check with a user-supplied key in a disposable environment: validation auto-populates the selector and the ZDR-required list is a strict subset of the full list; record as non-gating evidence only.
