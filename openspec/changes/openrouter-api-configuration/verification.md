# Verification

## Run summary

## Verification update — 2026-08-06

The following focused evidence was added during this apply session:

| Area | Result | Evidence |
|---|---|---|
| Parameter ranges and profile resolution | Pass | `AiConfigurationTests.Validate_AcceptsRecognizedGatewayRangesAndKnownReasoningModes`, `Validate_RejectsOutOfRangeValuesAndUnsupportedReasoning`, and the existing inheritance/privacy/resolution matrix; 9 focused tests passed. |
| Settings migration and AI isolation | Pass | `JsonApplicationSettingsStoreTests.LoadAsync_MalformedAiSectionPreservesReadableAppearancePreference` plus the version-1/version-2 round-trip, cancellation, and secret-absence fixtures; 22 focused Integration tests passed with cache coverage. |
| Catalog cache safety | Pass | `JsonAiModelCatalogCacheTests` now covers policy isolation, stale timestamps, corruption, unsupported versions, size bounds, write failure, and cancellation. |
| Native credential contract | Pass | `NativeAiCredentialStoreTests` now covers malformed values, locked/unavailable translation, replacement rollback, removal failure, cancellation, and secret-safe diagnostics; 5 focused tests passed. |
| OpenRouter transport | Pass | `OpenRouterClientTests` now covers key-status mapping, cancellation behavior, retry handling, hostile provider text, strict request routing, ZDR headers/query behavior, and normalized responses; 20 focused tests passed. |
| Avalonia Settings behavior | Pass | `AiSettingsViewTests` now covers progressive Advanced-profile disclosure in addition to masking, empty guidance, ZDR model filtering, and compiled bindings; all 4 focused view tests passed. |
| Windows native credential smoke | Pass | `NativeCredentialSmokeTests` round-trip/overwrite/delete/cleanup passed on the current Windows runner. macOS and Linux evidence remains pending external CI. |

The full deterministic solution baseline also passed serially without build: Domain 188, Application 329, Integration 173, and App/headless 429; 1,119 passed, 0 failed, 0 skipped. Strict validation passed for the change and all 44 repository/spec items.

The criterion-by-criterion reconciliation is complete. The only remaining completion gate is the required macOS/Linux native-vault smoke evidence. The optional live OpenRouter request was not run and is non-gating.

| Gate | Result | Evidence / limitation |
|---|---|---|
| Package source and license audit | Pass | `ktsu.CredentialCache` 1.3.18 NuGet package and repository commit `0c15644636973e289d461c983a25e5a43a1e1025`; MIT license; net9/net10 assemblies; low-level factory selects Windows Credential Manager, macOS Keychain, or Linux Secret Service and throws on unsupported platforms; no automatic plaintext or in-memory fallback; no logging/file persistence in the native implementations. |
| Package dependency audit | Pass | Direct dependencies: Polyfill 10.11.2, ktsu.RoundTripStringJsonConverter 1.0.17, and ktsu.Semantics.Strings 2.5.3. `dotnet list .\FusionCanvas.sln package --vulnerable --include-transitive` reported no vulnerable packages on 2026-07-27. Version 1.3.18 remains intentionally locked although 1.3.19 is available. |
| Windows native credential smoke | Pass | `NativeCredentialSmokeTests.NativeStore_RoundTripsOverwritesAndCleansUp`; Windows 11, .NET SDK 10.0.302 / runtime 10.0.10, package 1.3.18; unique persona; missing/save/read/overwrite/delete/missing and `finally` cleanup passed. |
| macOS native credential smoke | Pending external CI | `.github/workflows/native-credential-smoke.yml` provisions a temporary default keychain and cleans it in an `always()` step. It has not run in this local Windows worktree. |
| Linux native credential smoke | Pending external CI | `.github/workflows/native-credential-smoke.yml` provisions libsecret, D-Bus, and gnome-keyring and runs inside `dbus-run-session`. It has not run in this local Windows worktree. |
| Live OpenRouter request | Not run | Optional and not part of the completion verdict; no user key was requested or read. |
| Strict OpenSpec validation | Pass | `openspec validate openrouter-api-configuration --strict` reported the change valid. |
| Production build | Pass | `dotnet build .\FusionCanvas.sln --no-restore -m:1 -nr:false`; 0 warnings, 0 errors. Serial execution avoids an Avalonia BuildServices shared-log race observed with parallel MSBuild. |
| Deterministic baseline | Pass | `dotnet test .\FusionCanvas.sln --no-restore --no-build -m:1 -nr:false`; 510 passed, 0 failed, 0 skipped (Domain 96, Application 158, Integration 65, App/headless 191). |
| Scoped completion QA | Conditional pass | Project references still point inward; `ktsu.CredentialCache` types occur only in Integration; no AI key, authorization header, prompt, response, or reasoning logging/persistence path was found; `git diff --check` found no whitespace errors. Completion remains blocked only on externally run macOS/Linux native credential smoke. |
| QA correction rerun | Pass | Scoped QA found that a past HTTP-date `Retry-After` could normalize to a negative duration. The parser now clamps it to zero; the warning-free solution build, all 510 deterministic tests, and strict OpenSpec validation passed again after the correction. |

## AI provider configuration criteria

| ID | Scenario | Method | Result / evidence |
|---|---|---|---|
| OR-PC-001 | User saves an API key | App VM test plus low-level credential contract test | Implemented; `NativeAiCredentialStoreTests.SaveReadRemove_RoundTripsThroughLowLevelStore`. |
| OR-PC-002 | Native credential storage is unavailable | Fake low-level backend exception test | Implemented; `NativeAiCredentialStoreTests.BackendFailure_IsTranslatedWithoutSecretDisclosure`. |
| OR-PC-003 | Existing credential cannot be read | Fake low-level backend state/exception test | Pass; `NativeAiCredentialStoreTests.ReadAndRemove_TranslateMalformedAndUnavailableStates` covers malformed, unavailable, and not-found translation. |
| OR-PC-004 | Credential replacement fails | Fake low-level backend replacement test | Pass; `NativeAiCredentialStoreTests.FailedReplacement_PreservesPreviousCredential` verifies rollback. |
| OR-PC-005 | User views a configured credential | App VM state test and headless masking inspection | Pass; `AiSettingsViewModelTests.EnsureLoaded_IsLazyAndRunsOnlyOnce` and `AiSettingsViewTests.AiSection_ConstructsCompiledBindingsAndMasksCredentialDraft`. |
| OR-PC-006 | User cancels credential entry | App VM command test | Pass by inspection of `AiSettingsViewModel` draft cancellation path; `AiSettingsViewModelTests.SettingsClose_WithKeyDraftRequiresExplicitDiscard` confirms draft ownership and clearing behavior. |
| OR-PC-007 | User closes Settings with an unsaved credential draft | App VM plus headless window test | Pass; `AiSettingsViewModelTests.SettingsClose_WithKeyDraftRequiresExplicitDiscard`. |
| OR-PC-008 | User removes a saved credential | App VM and credential contract test | Implemented; credential removal contract is covered. |
| OR-PC-009 | User cancels credential removal | App VM command test | Pass by inspection of the confirmation command path; cancellation leaves credential state unchanged. |
| OR-PC-010 | User saves a key while offline | App VM fake-network test | Pass by inspection of the save/validation separation; save exposes saved-but-unverified state without provider access. |
| OR-PC-011 | Inference key validates | Fake HTTP test | Pass; current-key success is exercised by `OpenRouterClientTests.SafeGet_RetriesAtMostOnce`. |
| OR-PC-012 | Key is invalid or revoked | Fake HTTP status matrix | Pass; `OpenRouterClientTests.ValidateAsync_MapsKeyEndpointFailures` includes unauthorized/revoked mapping. |
| OR-PC-013 | Management-only key is supplied | Fake HTTP test | Pass; `OpenRouterClientTests.ValidateAsync_UsesCurrentKeyEndpointAndRejectsManagementKey`. |
| OR-PC-014 | Validation is interrupted | Cancellation-aware fake HTTP test | Pass by transport inspection; cancellation is passed to `ValidateAsync` and remains distinct from provider failures. |
| OR-PC-015 | User opens AI settings for the first time | Defaults plus App/headless test | Pass; `AiConfigurationTests.Defaults_ArePrivateAndUnconfigured`. |
| OR-PC-016 | User opts out of Zero Data Retention | App VM confirmation test | Pass; `AiSettingsViewModelTests.ZdrOptOutRequiresConfirmationAndDoesNotReplaceModel`. |
| OR-PC-017 | User cancels the privacy opt-out | App VM command test | Pass; `AiSettingsViewModelTests.ZdrOptOutRequiresConfirmationAndDoesNotReplaceModel` verifies confirmation gating and cancellation. |
| OR-PC-018 | Privacy change makes the selected model incompatible | Resolver and App readiness test | Pass; `AiConfigurationTests.Resolve_RejectsMissingUnavailablePrivacyAndInvalidParameterStates` plus `AiSettingsViewModelTests.RequireZeroDataRetention_NarrowsSelectorToCompatibleModels`. |
| OR-PC-019 | Catalog loads successfully | Fake HTTP catalog test | Pass; `OpenRouterClientTests.GetModelsAsync_FiltersNonTextModelsAndUsesZdrPolicy`. |
| OR-PC-020 | No model has been selected | Defaults/resolver test | Pass; `AiConfigurationTests.Defaults_ArePrivateAndUnconfigured`. |
| OR-PC-021 | User searches the model catalog | App VM filter test | Pass by inspection of the view-model search projection; catalog filtering is exercised by `AiSettingsViewTests.AiSection_ListsOnlyZdrCompatibleModelsAfterValidation`. |
| OR-PC-022 | Catalog refresh fails with a cached catalog | App VM and cache fake test | Pass; `AiSettingsViewModelTests.CatalogFailure_MapsAuthenticationAndKeepsCache` verifies cached fallback. |
| OR-PC-023 | Catalog refresh fails without a cache | App VM fake-network test | Pass by inspection of the empty-catalog error path; `AiSettingsViewModelTests.ValidationFailure_DoesNotLoadCatalog` confirms no false-ready state. |
| OR-PC-024 | Saved model is absent from the current catalog | Resolver test | Pass; unavailable branch in `AiConfigurationTests.Resolve_RejectsMissingUnavailablePrivacyAndInvalidParameterStates`. |
| OR-PC-025 | Model supports a recognized parameter | Registry test and headless dynamic-control test | Pass; `AiConfigurationTests.Validate_AcceptsRecognizedGatewayRangesAndKnownReasoningModes` and compiled dynamic-control bindings in `AiSettingsViewTests`. |
| OR-PC-026 | User leaves a parameter at Provider default | Exact request JSON test | Pass; omitted nullable values in `OpenRouterClientTests.GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage`. |
| OR-PC-027 | Model does not support a recognized parameter | Registry effective-profile test | Pass; `AiConfigurationTests.Effective_OmitsUnsupportedValuesAndUnknownCapabilities`. |
| OR-PC-028 | Model advertises an unknown parameter | Parser/registry test | Pass; catalog parser and registry tests retain but do not execute `future`. |
| OR-PC-029 | Selected model changes | App profile projection test | Pass by inspection of model-change projection and readiness recomputation; `AiSettingsViewModelTests.RequireZeroDataRetention_NarrowsSelectorToCompatibleModels` exercises selector updates. |
| OR-PC-030 | Model does not expose reasoning selection | Registry and App projection test | Pass; `AiConfigurationTests.Effective_OmitsUnsupportedValuesAndUnknownCapabilities` removes unsupported reasoning and the headless view binds only projected controls. |
| OR-PC-031 | Model supports optional effort selection | Registry/request JSON test | Pass; `AiConfigurationTests.Validate_AcceptsRecognizedGatewayRangesAndKnownReasoningModes` and `OpenRouterClientTests.GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage`. |
| OR-PC-032 | Model requires reasoning | Registry test | Pass; `AiConfigurationTests.Validate_RejectsOutOfRangeValuesAndUnsupportedReasoning` covers incompatible reasoning modes and mandatory capability metadata. |
| OR-PC-033 | Model supports a reasoning token budget | Registry/request JSON test | Pass; `OpenRouterClientTests.GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage` verifies normalized reasoning serialization. |
| OR-PC-034 | Advanced mode is off | Resolver test | Pass; `AiConfigurationTests.ProfileFor_UsesGeneralUntilAdvancedCustomProfileIsEnabled`. |
| OR-PC-035 | Advanced mode is enabled initially | Persistence/App VM test | Pass; version-2 settings round-trip plus `AiSettingsViewTests.AiSection_ProgressivelyDisclosesAdvancedPurposeProfiles`. |
| OR-PC-036 | User creates a custom purpose profile | App VM test | Pass; `AiSettingsViewModelTests.AdvancedProfiles_CopyGeneralOnceAndRestoreRetainedCustomProfile`. |
| OR-PC-037 | Purpose profile returns to General | Resolver/App VM test | Pass; retained custom path covered by the same test. |
| OR-PC-038 | Advanced mode is disabled and re-enabled | Resolver/App VM test | Pass; `AiConfigurationTests.EnableCustom_CopiesGeneralOnceAndRetainsExistingCustomProfile` and `AiSettingsViewModelTests.AdvancedProfiles_CopyGeneralOnceAndRestoreRetainedCustomProfile`. |
| OR-PC-039 | User switches workspace | Composition inspection | Pass by inspection: AI settings and credentials are application-scoped and workspace events do not mutate them. |
| OR-PC-040 | Existing settings are upgraded | Integration settings test | Pass; existing version-1 migration tests plus version-2 tests. |
| OR-PC-041 | AI settings content is invalid | Integration settings isolation test | Pass; `JsonApplicationSettingsStoreTests.LoadAsync_MalformedAiSectionPreservesReadableAppearancePreference` and cache corruption/unsupported-version fixtures. |

## AI text generation criteria

| ID | Scenario | Method | Result / evidence |
|---|---|---|---|
| OR-TG-001 | Caller submits a text request | Application service plus fake HTTP test | Pass; successful service and provider tests. |
| OR-TG-002 | Caller supplies invalid application input | Application service test | Pass; `AiTextGenerationServiceTests.GenerateAsync_InvalidRequestMakesNoExternalCalls`. |
| OR-TG-003 | Advanced mode is off | Resolver test | Pass; general resolution test. |
| OR-TG-004 | Purpose inherits General | Resolver test | Pass; general inheritance test. |
| OR-TG-005 | Purpose has a custom profile | Resolver test | Pass; custom resolution test. |
| OR-TG-006 | Effective profile is not ready | Application service test | Pass; `GenerateAsync_IncompleteConfigurationMakesNoCredentialOrProviderCall`. |
| OR-TG-007 | Request uses safe defaults | Exact request JSON test | Pass; strict provider routing and omitted defaults asserted. |
| OR-TG-008 | Request contains explicit supported overrides | Exact request JSON test | Pass; `OpenRouterClientTests.GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage` covers typed overrides and strict routing. |
| OR-TG-009 | Zero Data Retention is disabled | Exact request JSON test | Pass; the exact-request fixture verifies the provider privacy object omits `zdr` when opt-out is active. |
| OR-TG-010 | Reasoning effort is configured | Exact request JSON test | Pass; effort present and budget absent. |
| OR-TG-011 | Reasoning token budget is configured | Exact request JSON test | Pass; `OpenRouterClientTests.GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage` verifies token-budget/effort mutual exclusion. |
| OR-TG-012 | OpenRouter returns a complete response | Fake HTTP response test | Pass; text/model/finish/usage/cost/id normalization. |
| OR-TG-013 | Provider omits optional metadata | Fake HTTP response test | Pass; response normalization tolerates absent optional fields in `OpenRouterClientTests.GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage`. |
| OR-TG-014 | Request completes | Persistence inspection | Pass by inspection: generation returns a result and has no settings/workspace/history dependency. |
| OR-TG-015 | Authentication fails | Fake HTTP matrix | Pass; `GenerateAsync_MapsFailuresWithoutRetry`. |
| OR-TG-016 | Account has insufficient credit | Fake HTTP matrix | Pass. |
| OR-TG-017 | No endpoint satisfies the request | Fake HTTP matrix | Pass. |
| OR-TG-018 | Request is rate limited | Fake HTTP matrix | Pass; `OpenRouterClientTests.GenerateAsync_MapsFailuresWithoutRetry` verifies category and safe retry metadata handling. |
| OR-TG-019 | Request is blocked | Fake HTTP matrix | Pass. |
| OR-TG-020 | Provider returns partial text with an error | Fake HTTP response test | Pass; `OpenRouterClientTests.GenerateAsync_MapsFailuresWithoutRetry` maps terminal provider failures without treating partial content as authoritative success. |
| OR-TG-021 | Response cannot be interpreted | Fake HTTP malformed response test | Pass by parser inspection and failure matrix in `OpenRouterClientTests.GenerateAsync_MapsFailuresWithoutRetry`. |
| OR-TG-022 | Caller cancels a pending generation | Cancellation-aware fake handler | Pass; `AiTextGenerationServiceTests.GenerateAsync_CancellationIsPropagatedWithoutProviderDispatch`. |
| OR-TG-023 | Generation transport fails ambiguously | Fake handler call-count test | Pass; `OpenRouterClientTests.GenerateAsync_MapsFailuresWithoutRetry` and `SafeGet_RetriesAtMostOnce` distinguish POST no-retry from safe GET retry. |
| OR-TG-024 | Safe metadata read fails transiently | Fake HTTP retry test | Pass; `OpenRouterClientTests.SafeGet_RetriesAtMostOnce`. |
| OR-TG-025 | Provider content contains markup or instructions | Hostile response test plus UI inspection | Pass; `OpenRouterClientTests.GenerateAsync_TreatsHostileProviderTextAsBoundedData`. |
| OR-TG-026 | Diagnostic logging is enabled | Logger inspection | Pass by inspection: the module performs no AI content, header, or key logging. |

## Application Settings criteria

| ID | Scenario | Method | Result / evidence |
|---|---|---|---|
| OR-AS-001 | User selects AI settings | Avalonia headless test | Pass; `AiSettingsViewTests.AiSection_ConstructsCompiledBindingsAndMasksCredentialDraft`. |
| OR-AS-002 | User opens Settings frequently for other preferences | Existing headless regression tests | Pass when the full App baseline passes; AI loading remains selection-triggered. |
| OR-AS-003 | User operates AI settings with a keyboard | Headless focus/order test | Pass by headless visual-tree inspection; controls use the declared Avalonia order and status changes do not request focus. |
| OR-AS-004 | Advanced mode is off | App VM/headless test | Pass; `AiSettingsViewTests.AiSection_ProgressivelyDisclosesAdvancedPurposeProfiles` verifies custom editors are hidden. |
| OR-AS-005 | Advanced mode is on | App VM/headless test | Pass; `AiSettingsViewTests.AiSection_ProgressivelyDisclosesAdvancedPurposeProfiles` verifies summaries and custom controls appear progressively. |
| OR-AS-006 | Selected model has additional parameters | Headless capability-projection test | Pass; compiled dynamic bindings plus `AiConfigurationTests.Effective_OmitsUnsupportedValuesAndUnknownCapabilities` establish capability-aware projection. |
| OR-AS-007 | AI has not been configured | App VM/headless test | Pass through defaults and initial headless construction. |
| OR-AS-008 | Validation or catalog loading is in progress | App VM/headless test | Pass; `AiSettingsViewModelTests.CatalogLoad_DoesNotDuplicateWhileBusy` verifies busy command gating. |
| OR-AS-009 | Configuration is ready | App readiness test | Pass; `AiSettingsViewModelTests.SllReadiness_ShowsUsingGeneral_WhenSllUsesGeneral` and application availability tests verify readiness projections. |
| OR-AS-010 | Operation fails recoverably | App VM fake failure test | Pass; `AiSettingsViewModelTests.CatalogFailure_MapsAuthenticationAndKeepsCache` and `CacheSaveFailure_KeepsFetchedModelsAndWarns`. |
| OR-AS-011 | User starts entering a credential | Avalonia headless test | Pass; masked draft control is visible and non-secret state is separate. |
| OR-AS-012 | User saves a credential draft | App VM command test | Pass by inspection of the async save command and draft clearing; `AiSettingsViewModelTests.ValidationSuccess_AutoLoadsCatalog` confirms post-save loading. |
| OR-AS-013 | Credential draft is invalid locally | App VM command test | Pass by command CanExecute inspection: empty/whitespace drafts do not invoke the native store and retain field focus. |
| OR-AS-014 | Confirmed destructive action completes | App VM/headless confirmation test | Pass; `AiSettingsViewModelTests.SettingsClose_WithKeyDraftRequiresExplicitDiscard` and `ZdrOptOutRequiresConfirmationAndDoesNotReplaceModel` cover confirmation and state preservation. |
| OR-AS-015 | User changes a valid non-secret AI preference | Settings persistence test | Version-2 complete snapshot round-trip passes. |
| OR-AS-016 | User changes preferences repeatedly | Existing Settings save-queue regression plus mixed snapshot inspection | Pass by shared complete-snapshot queue inspection; `SettingsViewModelTests.RapidToggle_RetainsMostRecentValueForNextStart` and `RapidToggle_StaleCompletionDoesNotOverwriteLatestValue` establish latest-write semantics. |
| OR-AS-017 | Non-secret preference cannot be saved | Existing Settings failure regression | Existing inline warning behavior remains in the shared complete-snapshot save path. |
> Post-merge integration note (2026-07-27): Ideation is now a production caller
> of the provider-neutral text-generation service. Availability is resolved from
> the effective profile, cached catalog, privacy policy, and native credential
> state by `integrate-ideation-openrouter-snowclones`.
