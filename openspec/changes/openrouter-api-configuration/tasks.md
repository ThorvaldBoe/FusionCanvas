## 1. Dependency and application contracts

- [x] 1.1 Audit `ktsu.CredentialCache` 1.3.18 source, MIT license, package contents, transitive dependencies, supported native backends, and vulnerability status; record the evidence and stop for review if any mandatory security or platform gate fails.
- [x] 1.2 Add the locked credential-store package to `FusionCanvas.Integration` without exposing its types outside Integration.
- [x] 1.3 Add provider-independent AI request-purpose, message, profile, reasoning, catalog, credential-state, result, usage, and failure contracts under `FusionCanvas.Application/AI`.
- [x] 1.4 Add the focused Application ports for native credentials, credential validation, model discovery/cache, text providers, and text generation with cancellation on every I/O operation.
- [ ] 1.5 Implement and unit-test the recognized-parameter registry, typed gateway ranges, capability intersection, output-limit validation, and unknown-parameter tolerance.
- [ ] 1.6 Implement and exhaustively unit-test General/Ideation/Concept resolution, Advanced-mode behavior, whole-profile inheritance, first custom copy, retained custom restoration, privacy compatibility, and incomplete/unavailable profiles.

## 2. Application settings and model-cache persistence

- [x] 2.1 Extend `ApplicationSettings` with safe AI defaults and update in-memory stores and callers to carry complete immutable settings snapshots.
- [x] 2.2 Upgrade `JsonApplicationSettingsStore` to version 2 with atomic writes, version-1 Dark-mode migration, safe AI defaults, unknown-field tolerance, and recovery that preserves readable unrelated preferences and native credentials.
- [ ] 2.3 Add Integration fixtures proving version-1 migration, version-2 round trips for every profile mode and parameter, malformed-AI isolation, rapid complete-snapshot replacement, cancellation, and secret absence.
- [x] 2.4 Implement a versioned, policy-keyed `JsonAiModelCatalogCache` beside application settings with bounded reads, atomic replacement, stale timestamps, and tolerant missing/corrupt/unsupported cache handling.
- [ ] 2.5 Add cache tests for ZDR and broader catalogs, successful/stale lookup, policy isolation, corruption, unsupported version, write failure, bounded content, and cancellation.

## 3. Cross-platform native credential boundary

- [x] 3.1 Implement `NativeAiCredentialStore` over the package's low-level platform store factory using service name `FusionCanvas`, a stable OpenRouter identifier, no process singleton/cache, and distinct not-found/unavailable/locked/denied/malformed/write/delete results.
- [x] 3.2 Implement save, read, replace, and remove semantics so failed replacement preserves the prior key, failed removal does not report success, and no plaintext/session/file fallback is possible.
- [ ] 3.3 Add deterministic credential-contract tests with fake low-level backends for success, not found, unavailable/locked, denial, malformed data, replacement rollback, removal cancellation support, exception translation, and secret-safe diagnostics.
- [ ] 3.4 Add unique disposable native-vault smoke tests for Windows Credential Manager, macOS Keychain, and Linux Secret Service/libsecret, including overwrite, read, delete, missing, cleanup, and no OpenRouter dependency.
- [x] 3.5 Add a Windows/macOS/Linux CI matrix or equivalent repeatable scripts that provision an isolated Linux D-Bus/Secret Service and macOS keychain context, run the matching smoke tests without an interactive desktop, and clean up credentials on success or failure.

## 4. OpenRouter metadata and credential validation

- [ ] 4.1 Add internal OpenRouter JSON DTOs and tolerant bounded parsers for current-key data, user-filtered model metadata, supported/default parameters, pricing, limits, modalities, reasoning metadata, usage, and canonical errors.
- [x] 4.2 Implement a long-lived injected OpenRouter HTTP boundary with bearer authentication, 30-second metadata timeout, caller cancellation, and at most one eligible GET retry honoring bounded `Retry-After`.
- [x] 4.3 Implement current-key validation that distinguishes inference-capable, invalid/revoked, management-only, insufficient-permission, rate-limit, timeout/network, and transient-service states without making a generation request.
- [x] 4.4 Implement authenticated text-input/text-output model discovery with ZDR query filtering, explicit stable IDs, cache refresh/fallback, missing-model retention, no hardcoded selection, and no startup network call.
- [ ] 4.5 Add fake-HTTP tests for exact endpoints/query/header behavior, validation state mapping, text modality filtering, ZDR filtering, catalog/default/reasoning parsing, unknown fields, bounds, cancellation, one safe retry, retry-after, cache fallback, and secret-safe errors.

## 5. Provider-independent text generation

- [x] 5.1 Implement `AiTextGenerationService` so it validates application messages, resolves the effective purpose profile and readable credential at request time, and makes zero provider calls for incomplete or incompatible configuration.
- [x] 5.2 Implement OpenRouter non-streaming Chat Completions translation with the selected model/messages, `provider.require_parameters = true`, ZDR enforcement when enabled, and omission of every provider-default or unsupported value.
- [x] 5.3 Implement typed sampling/output/stop/seed serialization and mutually exclusive normalized reasoning effort, disabled, and token-budget serialization.
- [x] 5.4 Normalize successful text, requested/actual model, provider, finish reason, token usage, reported cost, and generation ID while tolerating absent optional metadata and rejecting textless/malformed success.
- [x] 5.5 Normalize authentication, insufficient-credit, no-eligible-provider, model-unavailable, rate-limit/retry-after, blocked/permission, timeout/network, incomplete-generation, invalid-response, and unexpected failures without exposing secrets or full content.
- [x] 5.6 Enforce a caller-bounded five-minute generation timeout, distinct cancellation, no automatic POST retry after dispatch, and an explicit ambiguous-failure warning about possible usage.
- [ ] 5.7 Add Application and fake-HTTP Integration tests for every profile-resolution branch, exact request JSON, omission rules, ZDR opt-out, strict routing, reasoning modes, complete/partial responses, error matrix, cancellation, zero invalid-state calls, zero POST retries, and no automatic settings/workspace/history persistence.
- [ ] 5.8 Add recording-logger and hostile-provider-data tests proving API keys, authorization headers, full prompts, full responses, and reasoning are not logged or executed and remote strings remain bounded display data.

## 6. AI Settings presentation and interaction

- [x] 6.1 Add `AI` to `SettingsSection` while preserving General as the section selected for every new Settings session.
- [x] 6.2 Add `AiSettingsViewModel` and focused profile/editor view models with lazy credential/catalog loading, readiness summaries, model search, capability-aware control projection, and application-setting change notifications.
- [x] 6.3 Implement explicit masked Add/Replace key draft, local validation, Save/Cancel, saved-but-unverified state, Validate, Remove confirmation, busy-state command gating, actionable errors, and prompt focus requests.
- [x] 6.4 Implement the unsaved-key discard guard for command and native-window dismissal, preserving the draft when discard is declined and clearing it promptly after save, cancel, or confirmed discard.
- [x] 6.5 Implement ZDR-on defaults and confirmed opt-out, preserving explicit incompatible model selections while updating readiness and catalog policy without silent replacement.
- [x] 6.6 Implement General plus Advanced Ideation/Concept editors, live Use-General summaries, retained custom profiles, model changes, primary reasoning/output controls, and an Additional-parameters expander.
- [x] 6.7 Update `SettingsViewModel` to queue the latest complete Dark-mode-plus-AI snapshot so rapid mixed edits retain the newest state and failed writes keep current-session values with inline warnings.
- [x] 6.8 Add `AiSettingsView.axaml` with compiled bindings, semantic theme resources, complete empty/loading/success/blocked/error states, inline confirmations, predictable keyboard order, and scrollable content at the approved Settings size.
- [x] 6.9 Integrate the AI view into `SettingsWindow`, enlarge its useful default/minimum bounds, and keep window code-behind limited to dismissal/focus coordination.
- [ ] 6.10 Add framework-free App tests for all draft, confirmation, command, loading, validation, stale/unavailable, search, readiness, profile retention, rapid-save, persistence-failure, workspace-independence, and focus-request behavior.
- [ ] 6.11 Extend Avalonia headless Settings tests for AI section construction and compiled bindings, General default, masking, progressive visibility, dynamic controls, confirmations, busy/blocked states, keyboard/focus behavior, theme coherence, and minimum-size scrolling without superficial static-markup assertions.

## 7. Composition and lifecycle

- [x] 7.1 Introduce an explicit `AppServices`/`AppServicesFactory` composition path for settings, native credentials, catalog cache, one OpenRouter `HttpClient`, provider adapters, configuration resolver, text service, and Settings view models without a service locator.
- [x] 7.2 Keep credential lookup, validation, catalog refresh, and all provider traffic lazy so application startup and workspace switching remain local and prompt-free.
- [x] 7.3 Expose the constructed `IAiTextGenerationService` to future application consumers without adding any Ideation or Concept assistance UI in this module.
- [x] 7.4 Flush the latest non-secret settings and dispose owned HTTP/resources during shutdown without blocking on provider calls or losing existing Settings behavior.
- [ ] 7.5 Add composition/lifecycle tests proving one shared HTTP boundary, no startup network or credential access, correct lazy initialization, future service availability, clean disposal, and unchanged workspace/theme startup.

## 8. Criterion-level verification and correction

- [x] 8.1 Create `verification.md` and assign a stable criterion ID to every scenario in all three delta specs, using the planned methods from `design.md`.
- [ ] 8.2 Run every Application, Integration, App view-model, and Avalonia headless criterion and record the exact test or inspection evidence for each scenario; justify any non-applicable method explicitly.
- [ ] 8.3 Run the disposable native credential smoke suite on Windows, macOS, and Linux and record platform/runtime/package versions plus cleanup evidence; treat any unavailable backend or prompt-dependent test as a failed criterion, not a skip.
- [x] 8.4 Inspect generated settings, catalog cache, workspace fixtures, logs, exception text, and package contents to verify no credential or full AI content is persisted or disclosed outside the approved native store.
- [x] 8.5 If any criterion fails or implementation reveals a missing product, UX, data, security, architecture, or acceptance decision, correct the implementation or approved artifact, rerun that criterion, and rerun affected regressions before proceeding.

## 9. Completion gates and scoped QA

- [x] 9.1 Run `openspec validate openrouter-api-configuration --strict` and correct every validation or spec-drift issue.
- [x] 9.2 Run package license, `dotnet list package --vulnerable --include-transitive`, and relevant outdated-package checks; record results and resolve security findings before completion.
- [x] 9.3 Run `dotnet build .\FusionCanvas.sln` and keep changed production code warning-clean.
- [x] 9.4 Run `dotnet test .\FusionCanvas.sln` and require the complete deterministic baseline, including meaningful Avalonia headless coverage, to pass without a live OpenRouter key or interactive display.
- [x] 9.5 Perform scoped completion QA for proposal/spec/design/task/code agreement, criterion evidence, layer dependencies, secret handling, external-input safety, settings migration, cross-platform credential behavior, UI state/focus/accessibility, and changed-scope spec drift.
- [x] 9.6 Confirm no Ideation/Concept assistant workflow, prompt history, streaming, structured output, tools, web search, image generation, multiple-provider UI, or plugin AI system entered the module.
- [ ] 9.7 Optionally perform one disposable low-limit live OpenRouter validation/text request as supplemental evidence only; never store or commit the key and do not make this optional check part of the completion verdict.
