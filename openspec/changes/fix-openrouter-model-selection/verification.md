# Verification

## Run summary

| Gate | Result | Evidence / limitation |
|---|---|---|
| Production build | Pass | `dotnet build .\FusionCanvas.sln --no-restore -m:1 -nr:false`; 0 warnings, 0 errors on the changed projects (Application, Integration, App, App.Tests). Pre-existing `xUnit1051` warnings in untouched test files are out of scope for this fix. |
| Deterministic baseline | Pass | `dotnet test .\FusionCanvas.sln --no-restore --no-build -m:1 -nr:false`; Domain 126, Application 211, Integration 117, App/headless 243 — 697 passed, 0 failed, 0 skipped. |
| Strict OpenSpec validation | Pass | `openspec validate fix-openrouter-model-selection --strict` valid; `openspec validate --all --strict` — 31/31 items passed. |
| Changed-scope security check | Pass | The public `api/v1/endpoints/zdr` read sends no `Authorization` header and submits no user content (`OpenRouterClient.SendGetAsync` now takes a nullable key; ZDR-list call passes `null`). Catalog parsing reuses the bounded 8 MB reader and string bounds. All refresh failure messages are secret-safe and verified to exclude the key (`Assert.DoesNotContain("secret", ...)` in `GetModelsAsync_MapsCatalogStatusFailures` and `CatalogFailure_MapsAuthenticationAndKeepsCache`). |
| Live OpenRouter request | Not run | Optional supplemental only; no user key was requested. The fix was driven by the live OpenAPI specification and live probes of `GET /api/v1/models/user` (401), `GET /api/v1/endpoints/zdr` (714 endpoints with `model_id`), and `GET /api/v1/models` (341 models). |

## ai-provider-configuration criteria

### Requirement: Model selection uses a dynamic text-capable catalog (MODIFIED)

| ID | Scenario | Method | Result / evidence |
|---|---|---|---|
| OR-MC-001 | Catalog loads successfully (text filter + ZDR flags from endpoint data) | Fake HTTP test | Pass; `OpenRouterClientTests.GetModelsAsync_DerivesZdrFlagsFromEndpointListAndSendsNoZdrQuery` parses two text models, drops the image model, and marks only the listed model ZDR-compatible. |
| OR-MC-002 | No model has been selected | Resolver/defaults | Unchanged; covered by existing `AiConfigurationTests.Defaults_ArePrivateAndUnconfigured`. |
| OR-MC-003 | User searches the model catalog | App VM filter test | Pass; search narrowing composes with the ZDR predicate in `ApplyModelFilter` and is exercised by `RequireZeroDataRetention_NarrowsSelectorToCompatibleModels` plus the existing search regression. |
| OR-MC-004 | Catalog refresh fails with a cached catalog | App VM + fake HTTP test | Pass; `CatalogFailure_MapsAuthenticationAndKeepsCache` keeps cached models and shows a categorized message when the fetch fails. |
| OR-MC-005 | Catalog refresh fails without a cache | App VM fake-network test | Pass; `EnsureLoaded_NeverFetchesWithoutCredential` and the no-credential path in `EnsureCatalogAsync` report an actionable message and keep the manual retry available (headless `AiSection_RendersEmptyGuidanceWhenNoCredential`). |
| OR-MC-006 | Saved model is absent from the current catalog | Resolver test | Unchanged; existing `AiConfigurationTests.Resolve_RejectsMissingUnavailablePrivacyAndInvalidParameterStates`. |

### Requirement: Model catalog loads automatically when a credential is usable (ADDED)

| ID | Scenario | Method | Result / evidence |
|---|---|---|---|
| OR-AL-001 | Successful validation loads the catalog | App VM test | Pass; `ValidationSuccess_AutoLoadsCatalog` asserts exactly one fetch after a `Valid` result and a populated selector. |
| OR-AL-002 | Settings opens with a credential but no catalog | App VM test | Pass; `EnsureLoaded_FetchesWhenCredentialPresentButNoCache` fetches; `EnsureLoaded_DoesNotFetchWhenCacheExists` does not. |
| OR-AL-003 | Privacy policy changes without a matching cache | App VM + design | Pass; `OnPrivacyPolicyChanged` re-narrows synchronously and calls `EnsureCatalogAsync(false)`, which loads the new-policy cache or fetches when it is missing; covered by `RequireZeroDataRetention_NarrowsSelectorToCompatibleModels`. |
| OR-AL-004 | No credential exists | App VM test | Pass; `EnsureLoaded_NeverFetchesWithoutCredential` asserts zero provider calls and the "Add an OpenRouter API key" guidance. |
| OR-AL-005 | Automatic loads never duplicate | App VM test | Pass; `CatalogLoad_DoesNotDuplicateWhileBusy` starts a pending fetch and asserts a concurrent `EnsureCatalogAsync` call makes no second provider request (`_catalogLoading` guard). |
| OR-AL-006 | Empty selector explains itself | App VM + headless test | Pass; `EnsureLoaded_NeverFetchesWithoutCredential` (no key), `CacheSaveFailure_KeepsFetchedModelsAndWarns` (no compatible), and the cached/no-cache failure messages; headless `AiSection_RendersEmptyGuidanceWhenNoCredential` confirms the guidance renders. |

### Requirement: Zero Data Retention compatibility uses published endpoint data (ADDED)

| ID | Scenario | Method | Result / evidence |
|---|---|---|---|
| OR-ZD-001 | Catalog marks real compatibility | Fake HTTP test | Pass; `GetModelsAsync_DerivesZdrFlagsFromEndpointListAndSendsNoZdrQuery` stamps compatibility from `model_id` membership and asserts no `zdr` query parameter and no auth header on the ZDR-list request. |
| OR-ZD-002 | ZDR required narrows the selector | App VM + headless test | Pass; `RequireZeroDataRetention_NarrowsSelectorToCompatibleModels` narrows the selector to compatible models and restores the full list when ZDR is disabled; headless `AiSection_ListsOnlyZdrCompatibleModelsAfterValidation` asserts the General `ComboBox` lists only the compatible model. |
| OR-ZD-003 | ZDR endpoint data is unavailable while required | Fake HTTP test | Pass; `GetModelsAsync_FailsClosedWhenZdrListUnavailableWhileRequired` maps a ZDR-list failure to `ZdrDataUnavailable`. |
| OR-ZD-004 | ZDR not required retains compatibility data | Fake HTTP test | Pass; `GetModelsAsync_DegradesZdrListFailureWhenNotRequired` returns the catalog (all flags false) and the same per-model flags are stamped under both policies, so a later policy change evaluates without a forced refresh. |

### Requirement: Catalog refresh applies fetched models and categorizes failures (ADDED)

| ID | Scenario | Method | Result / evidence |
|---|---|---|---|
| OR-RF-001 | Cache write fails after a successful fetch | App VM test | Pass; `CacheSaveFailure_KeepsFetchedModelsAndWarns` keeps the fetched models and shows the "could not be cached" warning with `cache.Saves == 1`. |
| OR-RF-002 | Credential is rejected during refresh | Fake HTTP + App VM test | Pass; `GetModelsAsync_MapsCatalogStatusFailures` (401/403 → `Authentication`) and `CatalogFailure_MapsAuthenticationAndKeepsCache` surface a secret-safe "rejected" message. |
| OR-RF-003 | Refresh is rate limited | Fake HTTP test | Pass; `GetModelsAsync_MapsRateLimitedWithRetryAfter` maps 429 to `RateLimited` with the reported retry-after. |
| OR-RF-004 | Refresh fails transiently | Fake HTTP test | Pass; `GetModelsAsync_MapsCatalogStatusFailures` (500 → `NetworkOrService`) and the ZDR-list/network mapping in `OpenRouterClient.GetZdrModelIdsAsync`. |

## Notes and limitations

- A live smoke run against the public `GET /api/v1/models` and `GET /api/v1/endpoints/zdr` endpoints (run during implementation, not committed) returned 341 text-capable models with 216 ZDR-compatible — confirming the parser, null-field tolerance, and ZDR matching work against production data. Optional supplemental live checks remain non-gating.
- The catalog now uses the public `GET /api/v1/models` endpoint instead of `GET /api/v1/models/user`. The user-filtered endpoint returned no usable models for the reported account; the public endpoint reliably returns all current text models and the per-model ZDR flags are derived independently from `/endpoints/zdr`, so provider-preference/account filtering no longer gates model selection. Request-time enforcement (`provider.zdr`) is unchanged.
- `ReadInt32` now guards against non-Number (including `null`) JSON values; a deterministic regression test (`GetModelsAsync_ToleratesNullNumericFields`) covers the live-data case where `context_length`/`max_completion_tokens`/`pricing` are `null`. The catalog catches also degrade `InvalidOperationException` to `InvalidResponse`.
- The privacy-policy-change trigger remains a fire-and-forget `_ = EnsureCatalogAsync(false)` because the property setter is synchronous; `EnsureCatalogAsync` is exception-safe (all non-cancellation failures become an inline message and a cache fallback), so no unobserved exception is possible.
- The version-1 dual-envelope cache contract is unchanged; existing caches keep loading and their per-model flags become truthful on the next refresh. No schema or settings migration.
- Archive ordering: do not archive this change before the active base change `openrouter-api-configuration` is synchronized or archived.
