# Design: fix-openrouter-model-selection

## Context

A creator reported: saved an OpenRouter API key, validation said the key was valid, but no model could be selected; disabling ZDR did not help. Investigation (this worktree, 2026-07-28) established the facts below with live evidence:

- `AiSettingsViewModel` fetches the catalog only inside `RefreshModelsAsync`, bound to the manual `Refresh models` button. `SaveCredentialAsync` and `ValidateCredentialAsync` never fetch, and the ZDR toggle only reloads the cache. A first-time user therefore always faces an empty selector with no explanation.
- `OpenRouterClient.GetModelsAsync` requests `api/v1/models/user?zdr=true`. The live OpenAPI specification lists only `offset`/`limit` for `/models/user` (and no `zdr` parameter anywhere on `/models`). The parameter is silently ignored; `ParseModel` then stamps every model with `ZeroDataRetentionCompatible = <requested policy>`, so a ZDR-required catalog labels all ~341 models ZDR-compatible regardless of real endpoint policies. The base change's `verification.md` records `Live OpenRouter request: Not run`.
- OpenRouter's real catalog-side ZDR mechanism is the public, unauthenticated `GET /api/v1/endpoints/zdr` (verified live: 714 entries, each with a `model_id` such as `google/gemma-4-26b-a4b-it`, ~0.53 MB). Request-time enforcement via `provider.zdr: true` already exists in `BuildRequestJson` and is correct.
- `RefreshModelsAsync` writes the cache before applying models; a cache-write exception discards the fetched catalog and reports failure. All fetch failures surface as one generic message.
- Cache (`JsonAiModelCatalogCache`, version 1, dual envelopes `models-zdr.json`/`models-all.json`) and `IAiModelCatalogProvider`/`IAiModelCatalogCache` contracts are otherwise sound.

## Goals / Non-Goals

**Goals:**

- A valid saved credential leads to a populated model selector without a manual refresh press: after successful validation, on settings load when no cache exists, and after a privacy-policy change when no cache exists for the new policy.
- Per-model ZDR compatibility is derived from OpenRouter's published ZDR endpoint list; the selector narrows to compatible models while ZDR is required; nothing is marked compatible by assumption.
- A fetched catalog survives a cache-write failure; refresh failures are secret-safe and actionable.
- The empty selector always states why and what to do next.

**Non-Goals:**

- Generation-time ZDR enforcement (`provider.zdr: true` — already correct), multi-provider support, per-endpoint provider display, model recommendations/automatic selection, cache contract or format redesign, Settings layout redesign, and any new NuGet dependency.

## Decisions

### 1. One catalog orchestrator in the view model drives manual and automatic loads

Add a private `EnsureCatalogAsync(bool force)` to `AiSettingsViewModel` that contains today's refresh body (credential read, provider fetch, apply, cache save, messaging) with the reordering from Decision 4. Call sites:

- `ValidateCredentialAsync`: after a `Valid` result, `await EnsureCatalogAsync(force: true)` before clearing `IsBusy`, so the selector populates as part of the same busy period. Non-`Valid` results do not fetch.
- `EnsureLoadedAsync`: after the cache load, `EnsureCatalogAsync(force: false)` only when `HasCredential` and the cache returned no catalog for the active policy.
- ZDR policy change (`RequireZeroDataRetention` setter when enabling, and `ApplyZdrOptOut` when disabling): replace the fire-and-forget `_ = LoadCatalogFromCacheAsync()` with an awaited cache load followed by `EnsureCatalogAsync(force: false)` when `HasCredential` and no cache exists for the new policy. This removes the current unobserved-exception fire-and-forget.
- `RefreshModelsCommand`: `EnsureCatalogAsync(force: true)` (unchanged manual retry semantics).

Duplicate suppression uses the existing `IsBusy` serialization: command `CanExecute` already blocks manual re-entry, and automatic call sites skip when `IsBusy` is set rather than queueing a second request. No new concurrency primitives.

Alternatives rejected:

- *Auto-fetch on save instead of validate:* save is offline-safe by design; fetching on an unverified key surfaces avoidable rejected-key noise. Validation remains the trigger; the settings-load trigger covers returning users.
- *A scheduled/background refresh:* occasional admin surface; on-enter and on-change triggers are sufficient and keep network use explicit and observable.

### 2. ZDR compatibility comes from `/api/v1/endpoints/zdr`, fetched on every catalog load

`OpenRouterClient.GetModelsAsync` requests `api/v1/models/user` with the credential (dropping `?zdr=true`) and `api/v1/endpoints/zdr` **without** the credential (public data; the key is sent only where required). The ZDR response's `data[*].model_id` values form a case-insensitive set; `ParseModel` sets `ZeroDataRetentionCompatible = set.Contains(id)` for every catalog entry, under both policies, so the flag becomes a model fact rather than an echo of the request.

- ZDR required and the ZDR list fails (non-success, timeout, unparseable): fail the whole refresh with a typed `ZdrDataUnavailable` failure. Conservative failure matches OpenRouter's own stance and prevents mislabeling.
- ZDR not required and the ZDR list fails: proceed with an empty set (all flags `false`). The selector is unfiltered under this policy, so selection still works; a later switch to required simply evaluates conservatively until the next refresh. Documented degradation, not silent mislabeling.
- The existing dual-envelope cache contract is unchanged: both envelopes now store truthful flags, and the envelope policy stamp continues to record which policy the catalog was fetched under.

Alternatives rejected:

- *Per-model `/models/{id}/endpoints` lookups:* hundreds of extra authenticated calls per refresh; the aggregate public list carries the same policy fact in one unauthenticated read.
- *Drop catalog-side ZDR data and rely on request-time enforcement only:* the accepted requirement ("selector narrows the currently privacy-compatible model choices") and the readiness resolver need per-model compatibility before any request.
- *Fetch the ZDR list only when required:* flags would stop being model facts and toggling the policy back on would misjudge a ZDR-off catalog; one small public GET avoids that inversion.

### 3. Selector narrowing happens in the existing filter

`AiSettingsViewModel.ApplyModelFilter` gains the privacy predicate: when `RequireZeroDataRetention` is set, only `model.ZeroDataRetentionCompatible` entries reach the profile editors; search narrowing composes on top. `AiProfileEditorViewModel`, the resolver's `PrivacyIncompatible` branch, and the views are unchanged — the flags they already consume simply become truthful.

### 4. Refresh ordering and typed, secret-safe failure categories

Add `AiModelCatalogFailureKind` (`Authentication`, `RateLimited`, `NetworkOrService`, `InvalidResponse`, `ZdrDataUnavailable`) and `AiModelCatalogFetchException` (Kind + optional `RetryAfter`) under `FusionCanvas.Application/AI`. `GetModelsAsync` maps HTTP 401 → `Authentication`, 429 → `RateLimited` (with retry-after), other non-success/timeout/IO → `NetworkOrService`, JSON/shape violations → `InvalidResponse`, and Decision 2's closed failure → `ZdrDataUnavailable`. The provider interface signature is unchanged; failure information travels on the exception the view model already catches.

In `EnsureCatalogAsync` the order becomes: fetch → apply to selector → attempt cache save inside its own `try/catch` that degrades to a non-blocking "catalog may not survive restart" warning. Failure kinds map to existing-style inline messages (rejected key → re-validate/replace guidance; rate limited → retry-after when supplied; others → retryable unavailable). No content, key, or header is ever included.

Alternatives rejected:

- *Result record instead of exception:* contract churn across the Application boundary for a path the single caller already handles by catch; the typed exception carries the same data with a smaller diff.

### 5. Empty-selector guidance reuses the inline message surface

When the selector is empty and no load is running, `Message` states the cause and next action: no credential (add a key), credential but no catalog loaded (refresh in progress or failed — use `Refresh models`), or loaded-but-empty (no compatible models returned). This reuses the established inline-message pattern (stale/refresh warnings already use it) rather than adding new controls.

## Implementation Plan

### Application (`src/FusionCanvas.Application/AI`)

- Add `AiModelCatalogFetchException.cs` containing `AiModelCatalogFailureKind` and the exception type (Kind, optional `RetryAfter`). No interface changes.
- Tests: none at this layer (no behavior); resolver tests stay green unchanged.

### Integration (`src/FusionCanvas.Integration/AI/OpenRouterClient.cs`)

- `GetModelsAsync`: drop the `zdr` query parameter; after the models GET, issue the ZDR-list GET (no authorization header) through the same bounded-read/retry pipeline; build the ID set; apply Decision 2's failure policy; stamp flags in `ParseModel`.
- Failure mapping per Decision 4 (`EnsureSuccessStatusCode` replaced by explicit status mapping).
- Tests (`tests/FusionCanvas.Integration.Tests/AI/OpenRouterClientTests.cs`):
  - catalog request path contains no `zdr` query parameter and still sends the bearer key; ZDR-list request sends no authorization header;
  - models with/without a listed ZDR endpoint get true/false flags under both policies;
  - ZDR-list failure while required → `ZdrDataUnavailable`; while not required → catalog returned with all flags false;
  - 401/429/other-status mapping to kinds, retry-after preserved, single transient retry unchanged.

### App (`src/FusionCanvas.App/Settings/AiSettingsViewModel.cs`)

- Add `EnsureCatalogAsync(bool force)` per Decisions 1 and 4; rewire validate/load/policy-change/manual call sites; remove the fire-and-forget cache loads; add `IsBusy` skip for automatic triggers.
- Extend `ApplyModelFilter` with the privacy predicate (Decision 3) and refresh the selector when the policy changes.
- Add empty-selector guidance and failure-kind messages (Decision 5); cache-save degradation warning.
- Tests (`tests/FusionCanvas.App.Tests/Settings/AiSettingsViewModelTests.cs`) with fake credential store/validator/provider/cache:
  - validation success triggers exactly one fetch and populates editors; invalid/management key does not fetch;
  - load with credential + no cache fetches; with cache does not; without credential never calls the provider;
  - policy change with no matching cache fetches; with cache does not;
  - automatic trigger during a running load starts no second request;
  - cache-save failure keeps fetched models and sets the warning;
  - failure-kind → message mapping, secret absence;
  - ZDR required narrows `General.Models` to compatible entries; disabled ZDR restores the full list.
- Headless view tests (`tests/FusionCanvas.App.Tests/Settings/AiSettingsViewTests.cs`):
  - empty state renders guidance and keeps `Refresh models` available;
  - after a fake successful validation auto-load, the General `ComboBox` items contain the fetched compatible models only.

### Sequencing and gates

1. Application failure types → Integration client + tests → App view-model orchestration + tests → headless view tests.
2. Gates: `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, `openspec validate fix-openrouter-model-selection --strict`, `openspec validate --all --strict`, changed-scope security review (no credential to the public endpoint; bounded parsing; secret-safe messages).
3. `verification.md` maps every delta-scenario to evidence at apply time. Optional supplemental live check with a user-supplied key (validate → selector populates; ZDR-required list is a strict subset of the full list) uses a disposable environment only and never gates completion.

### Compatibility and rollback

No settings-document, schema, or cache-format change. Existing version-1 caches keep loading; their per-model flags become truthful on the next refresh, and conservative evaluation covers the interim. Rollback is a code revert. Do not archive this change before the base change `openrouter-api-configuration` is synchronized or archived.

Decisions not to reopen during implementation: OpenRouter remains the sole provider; cache contract and dual envelopes unchanged; no automatic model selection; request-time `provider.zdr: true` unchanged; automatic loads only at the three decided triggers.

## Risks / Trade-offs

- **[OpenRouter changes or rate-limits the public ZDR list]** → Bounded read, single transient retry, fail closed only when ZDR is required, cached catalogs unaffected; failure is an actionable message, not a crash.
- **[`model_id` values drift from catalog IDs]** → Case-insensitive matching; absent models evaluate as incompatible (conservative) rather than compatible.
- **[Extra GET adds refresh latency]** → ~0.5 MB public read on an occasional admin action; both requests reuse the 30-second metadata timeout.
- **[Automatic loads surprise the user with network activity]** → Only at the three decided triggers with a usable credential, under the existing busy indication; the manual refresh remains the explicit control.
- **[Base change still active]** → Delta declared as MODIFIED with a recorded archive-ordering dependency; strict validation over the combined change set is a required gate.

## Migration Plan

1. Ship against the current settings/cache formats; no migration.
2. On first run after the fix, existing users' next validation, settings open without cache, or manual refresh populates truthful flags automatically; stale cached flags from the fabricated-filter era are conservative-only (all-false in a ZDR-off envelope) until then.
3. Roll back by reverting code; no data rewrite.

## Open Questions

None. Trigger points, ZDR data source, failure policy, selector narrowing, cache compatibility, and archive ordering are resolved above.
