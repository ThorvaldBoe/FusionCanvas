## Context

FusionCanvas already has an application-wide Settings window, a version-1 JSON settings document containing appearance, an application-facing settings-store port, and an App composition root. It has no credential boundary, remote model catalog, HTTP provider adapter, or AI request service. The accepted architecture requires external APIs and secret storage to remain behind Application contracts implemented by Integration, and the product must remain useful without AI.

The user configures AI occasionally, so setup belongs in the focused Settings window rather than the primary creative workspace. Future Ideation and Concept features need different configurations, but their prompt and generated-content workflows are deliberately outside this module. The foundation must work on Windows, macOS, and Linux, default to OpenRouter Zero Data Retention, support any current text-input/text-output model, and avoid making provider-specific metadata part of application callers.

OpenRouter exposes a current-key endpoint, user-filtered model catalog, OpenAI-compatible non-streaming Chat Completions endpoint, per-model supported-parameter metadata, richer normalized reasoning metadata, usage data, stable error categories, strict parameter routing, and per-request ZDR enforcement. The catalog does not provide a complete arbitrary UI schema for every current or future parameter, so FusionCanvas must combine remote capability names with a local typed descriptor registry.

## Goals / Non-Goals

**Goals:**

- Let a user securely save, replace, remove, and validate one OpenRouter inference key on every supported desktop operating system.
- Keep credential material out of ordinary settings, workspace data, exports, logs, and source control.
- Make ZDR the default and make broader data-handling consent explicit.
- Discover text-capable models dynamically, cache the catalog safely, preserve explicit selections, and show capability-aware controls.
- Resolve General, Ideation, and Concept configurations predictably with progressive disclosure.
- Expose a provider-neutral non-streaming text service with normalized success, usage, cancellation, and failure behavior.
- Keep all routine tests deterministic and add targeted operating-system credential smoke coverage without a live OpenRouter account.
- Preserve version-1 appearance settings and local-first behavior.

**Non-Goals:**

- Ideation or Concept assistant controls, prompts, context composition, candidate review, or generated-content persistence.
- Automatic prompt, response, reasoning, or conversation history.
- Streaming, structured-output workflows, tool calling, server tools, web search, images, audio, or embeddings.
- Multiple simultaneously configured providers, provider selection UI, external AI plugins, or provider-account management.
- OpenRouter API-key creation, rotation, deletion, or account-privacy management.
- Arbitrary JSON parameters or controls generated from unknown capability names.
- Automatic model selection, automatic fallback between model IDs, or a hardcoded recommended model.

## UX Preflight

- **User and outcome:** A creator occasionally opens Settings to securely connect OpenRouter and choose cost/quality behavior that later tools can use.
- **Frequency and placement:** Credential and profile setup is occasional administration, so it lives only in a focused `AI` Settings section. It adds no persistent main-window footprint and Settings still opens on General.
- **Footprint:** Increase the resizable Settings default size to approximately 880 by 640 with useful minimum bounds, keep the section rail fixed, and scroll only the active content pane. Existing General and Workspace panes retain their compact layouts.
- **Progressive disclosure:** Connection, privacy, General model, and primary reasoning/output controls remain visible. Advanced mode reveals purpose profiles. Each purpose shows a compact inheritance/readiness summary; only the selected custom editor expands. Less-common sampling and stop controls live in an `Additional parameters` expander.
- **Initial and empty states:** With no credential, show the privacy default and an explanation plus `Add API key`; model discovery explains that a saved key is needed. With a credential but no model, show searchable selection and an incomplete readiness state.
- **Loading and success:** Save, validate, refresh, and remove actions have local busy states and cannot start duplicates. Successful save becomes `Saved, not verified`; successful validation becomes `Connected`. General, Ideation, and Concept readiness are shown separately.
- **Blocked and errors:** Secure-store unavailable/locked, invalid key, management key, network failure, stale catalog, incompatible model, and preference-write failure remain distinct and inline. A failed operation retains all unaffected state.
- **Drafts and cancellation:** API-key text is the only pane-owned draft. Entry begins with focus in a masked field and explicit Save/Cancel. Closing with a non-empty draft raises an inline discard confirmation and keeps focus/draft when declined. Successful save or cancel clears the draft promptly.
- **Destructive actions:** Key removal and ZDR opt-out use inline confirmations consistent with existing FusionCanvas patterns. Cancel preserves state; completion moves focus to Add/Replace or the privacy control.
- **Keyboard:** Rail, controls, expanders, confirmation actions, retry actions, and dismissal remain reachable in predictable order. Busy/status updates do not steal focus.

## Decisions

### 1. Keep provider-neutral contracts in Application

Create a cohesive `FusionCanvas.Application.AI` capability containing:

- `AiRequestPurpose` with `General`, `Ideation`, and `Concept`.
- `AiMessageRole`, `AiTextMessage`, `AiTextRequest`, `AiTextResult`, usage metadata, and stable failure categories.
- Typed settings records for `AiConfigurationSettings`, `AiProfileSettings`, purpose inheritance, reasoning mode, and recognized optional values.
- `IAiCredentialStore`, `IAiModelCatalogProvider`, `IAiTextProvider`, and `IAiTextGenerationService`.
- `AiConfigurationResolver` and `AiTextGenerationService`.

Callers provide only purpose, instructions/messages, and cancellation. The service resolves credential, model, privacy, and parameters. It does not allow callers to inject provider fields or override the saved privacy policy.

Provider-neutral ports are justified by the already-decided future provider variation point. A generic untyped parameter dictionary is rejected because it would leak provider vocabulary inward and defeat validation.

### 2. Implement OpenRouter directly with HttpClient

Create `FusionCanvas.Integration.AI.OpenRouter` with internal JSON DTOs and one long-lived injected `HttpClient`:

- `OpenRouterModelCatalogProvider` calls the authenticated user-filtered models endpoint, requests text input/output, and applies the ZDR query filter when required.
- `OpenRouterCredentialValidator` calls `GET /api/v1/key`.
- `OpenRouterTextProvider` calls `POST /api/v1/chat/completions` with `stream: false`.
- Shared internal parsing translates provider DTOs and errors at the Integration boundary.

Use `System.Net.Http` and `System.Text.Json`; do not add an OpenRouter or OpenAI SDK for this bounded protocol surface. This avoids SDK types crossing layers, reduces dependency and upgrade surface, and permits exact strict-routing/error tests with a fake `HttpMessageHandler`.

Use a 30-second linked timeout for key/catalog reads and a five-minute linked timeout for generation, always bounded by caller cancellation. Safe GETs receive at most one automatic retry for eligible transient failures, honoring a reasonable `Retry-After`. Generation POSTs receive no automatic retry after dispatch because resubmission can duplicate usage and cost.

### 3. Use native credential stores through one audited package and a FusionCanvas adapter

Add the stable .NET 10 version of `ktsu.CredentialCache` selected and locked during implementation, currently `1.3.18`, after confirming its MIT license, package contents, transitive graph, published source, and vulnerability status. Use its platform-native store factory and low-level store interface only:

- Windows Credential Manager through `CredReadW`, `CredWriteW`, and `CredDeleteW`.
- macOS Keychain Services.
- Linux libsecret/freedesktop Secret Service.

Do not use its process-wide singleton or credential cache; load the key only for validation or request dispatch and release references promptly. Wrap all library types in `NativeAiCredentialStore : IAiCredentialStore`, with service name `FusionCanvas` and a stable OpenRouter credential identifier. Map not-found separately from unavailable, locked, denied, malformed, and write/delete failure.

The Linux adapter requires `libsecret` and a running Secret Service in the user's graphical login session. If unavailable, persistent AI configuration fails closed with an actionable message. No plaintext, encrypted-file, environment-variable, or session-memory persistence fallback is offered by the UI.

Alternatives rejected:

- **Plaintext or reversible settings storage:** violates the public-repository and local security requirements.
- **Windows DPAPI file plus unrelated mechanisms elsewhere:** creates three storage formats and weaker platform integration.
- **Invoking `security`, `secret-tool`, or Git Credential Manager subprocesses:** adds installation, escaping, process-output, and secret-exposure risks.
- **Hand-written P/Invoke for all three platforms:** substantially expands security-sensitive native interop and test surface without product differentiation.

If the locked package fails its mandatory source/security or OS smoke gates, implementation stops and returns the dependency decision for review rather than substituting a weaker store.

### 4. Separate credential state, preferences, and catalog cache

Persist three distinct data classes:

1. **Credential:** native store only; the settings document never records plaintext, encoded key material, key prefix/suffix, or a recoverable copy.
2. **Non-secret preferences:** version-2 `settings.json`, extending `ApplicationSettings` with AI settings.
3. **Catalog cache:** versioned `openrouter-models.json` beside settings, written atomically and bounded in size.

`ApplicationSettings` gains safe defaults:

- `RequireZeroDataRetention = true`
- `AdvancedMode = false`
- empty General model
- Ideation and Concept using General
- empty retained custom profiles

Version-1 settings migrate in memory to version 2 while preserving `DarkMode`; the next successful save writes version 2. Deserialize AI content independently enough that malformed AI values recover safely without discarding readable appearance. Catalog corruption or unsupported cache version deletes nothing automatically; it is ignored with a refreshable stale/unavailable state. Maintain separate cache envelopes for ZDR-required and broader catalogs so an offline privacy change cannot misuse a catalog fetched under another policy.

Workspace SQLite, workspace snapshots, and workspace transfer remain untouched.

### 5. Use typed profile snapshots and whole-profile inheritance

`AiProfileSettings` contains a model ID plus nullable typed overrides for:

- `MaxCompletionTokens`
- `Temperature`
- `TopP`
- `TopK`
- `MinP`
- `TopA`
- `FrequencyPenalty`
- `PresencePenalty`
- `RepetitionPenalty`
- `Seed`
- bounded stop sequences
- a discriminated reasoning setting: provider default, disabled, effort, or token budget

Null means `Provider default` and therefore omission. No arbitrary extension dictionary is persisted or submitted.

General is always a concrete profile. Ideation and Concept each contain `UseGeneral` plus a retained custom snapshot. While Advanced is off, all purposes resolve General without mutating saved purpose state. Turning Custom on copies General only when that purpose has never had retained custom state; subsequently it restores the retained snapshot. This satisfies both initial-copy and later-retention scenarios without hidden mutation.

### 6. Combine a local parameter registry with remote capabilities

Add an Application-owned immutable registry of recognized parameter descriptors: key, label, kind, valid gateway range/choices, and OpenRouter field mapping owned by Integration. Effective controls are the intersection of:

- installed recognized descriptors;
- the selected catalog model's `supported_parameters`;
- richer reasoning metadata;
- output/context bounds supplied by the catalog.

Changing models recomputes validity. Unsupported retained values remain in the saved custom snapshot for possible future reselection but are excluded from the effective configuration and request. Unknown advertised names are retained only in raw cache DTO data if necessary for forward-compatible parsing; they never become controls or outbound fields.

`max_completion_tokens` is capped by the model's reported maximum when known. Reasoning effort choices come only from `supported_efforts`; mandatory reasoning removes Off; token budget and effort are mutually exclusive. Dynamic router models remain selectable when returned as text models, but absent reasoning metadata means no model-specific reasoning override.

### 7. Preserve selections; never guess

No model is selected by default. Catalog refresh never replaces a saved model ID. If the model disappears, conflicts with ZDR, or becomes incompatible with explicit settings, the profile is unavailable and the UI explains the reason. The user must select a replacement or change the relevant policy.

With a policy-compatible cached catalog, a saved selection can remain usable while refresh is offline. Strict request-time routing provides final enforcement when metadata has drifted.

### 8. Enforce privacy and parameters on every generation

`OpenRouterTextProvider` always sends:

```json
"provider": {
  "require_parameters": true,
  "zdr": true
}
```

when ZDR is enabled. When the user has confirmed opt-out, omit `zdr` rather than claiming enforcement; keep `require_parameters: true`.

Only explicit validated overrides are serialized. Reasoning uses the normalized `reasoning` object and never sends effort and token budget together. Do not send OpenRouter debug echo, user identifiers, prompt logging options, provider order, provider `only`/`ignore`, tools, plugins, routes, or fallback model arrays.

### 9. Normalize result and failure behavior

`AiTextResult` includes generated text plus optional requested/actual model, provider, finish reason, usage, reported cost, and generation ID. Missing optional metadata does not invalidate usable text.

Define stable failures for invalid application request, not configured, credential unavailable, invalid configuration, authentication, wrong key type, insufficient credit, rate limit with optional retry-after, blocked/permission, no eligible provider, model unavailable, timeout/network, incomplete generation with explicitly diagnostic partial text, invalid provider response, and unexpected provider failure.

Cancellation remains cancellation rather than a failure category. Never log authorization headers, API keys, full prompts, full responses, or reasoning. Log only safe operation category, model ID, duration, status/error type, and generation ID. Treat all remote strings as bounded display data.

### 10. Keep catalog and validation lazy

Application startup loads only local non-secret preferences. It does not retrieve the credential, prompt the keychain, validate the key, or fetch models.

Selecting the AI section loads credential status and refreshes the appropriate catalog on demand. A manual Refresh is available. Text generation reads the credential and resolves the current stored preferences at request time, so future tool callers do not depend on the Settings window being open.

### 11. Compose without a service locator

Replace the narrow synchronous `AppSettingsFactory.LoadInitialState` construction path with a focused composition object in `FusionCanvas.App`, likely `AppServices`/`AppServicesFactory`, that owns:

- the application settings store and loaded settings;
- native AI credential adapter;
- catalog cache and OpenRouter HttpClient adapters;
- configuration resolver and text-generation service;
- Settings and AI Settings view models.

Constructors continue to receive explicit dependencies. Views and view models do not resolve services from a container. The App composition root owns disposal of `HttpClient` and flushes pending non-secret settings on shutdown.

### 12. Keep AI Settings as a focused child view model and view

Add `AiSettingsViewModel`, profile row/editor view models, and an `AiSettingsView.axaml` user control. `SettingsViewModel` continues to own section selection and the latest complete `ApplicationSettings` save queue. The AI child raises typed non-secret preference changes; the parent saves the whole latest snapshot so rapid Dark-mode and AI edits cannot overwrite one another.

Credential operations bypass ordinary settings persistence and use explicit asynchronous commands. Inline confirmation flags follow established FusionCanvas patterns. `SettingsWindow` closing asks the view model whether dismissal is allowed; an unsaved key draft activates the inline discard prompt and cancels window close until confirmed.

The view shows only recognized controls valid for the selected model and uses compiled bindings. Model search/filter, readiness, profile resolution, validation state, and command enablement live in view models and Application policies, not code-behind.

## Implementation Plan

### Application layer

1. Add `src/FusionCanvas.Application/AI/` records and enums for request purpose, messages, profile settings, reasoning settings, catalog models/capabilities, credential status, normalized results, and failures.
2. Add `IAiCredentialStore`, `IAiCredentialValidator` or validator operation on a focused provider boundary, `IAiModelCatalogProvider`, `IAiModelCatalogCache`, `IAiTextProvider`, and `IAiTextGenerationService`.
3. Implement the immutable recognized-parameter registry, profile/configuration validator, `AiConfigurationResolver`, and `AiTextGenerationService`.
4. Keep `FusionCanvas.Domain` unchanged; no generated content or provider concept becomes a domain entity.

### Integration layer

5. Add and lock the audited `ktsu.CredentialCache` dependency to `FusionCanvas.Integration`; use its store interface without its process cache. Implement `NativeAiCredentialStore` with stable identifiers and safe exception/result mapping.
6. Add internal OpenRouter request/response/error/catalog DTOs and an injected `OpenRouterHttpClient` boundary using `HttpClient` and `System.Text.Json`.
7. Implement current-key validation, user-filtered text model discovery, ZDR filtering, bounded GET retry, non-streaming Chat Completions, strict parameter serialization, usage/result parsing, and stable error translation.
8. Implement `JsonAiModelCatalogCache` under the application-local FusionCanvas directory with atomic writes, version/policy envelopes, bounded reads, and tolerant corruption handling.
9. Upgrade `JsonApplicationSettingsStore` to version 2 with v1 migration, complete-snapshot atomic writes, safe AI defaults, and isolation between malformed AI content and readable unrelated preferences.

### App layer

10. Extend `ApplicationSettings`, in-memory test stores, `SettingsSection`, `SettingsViewModel`, and the settings save queue to carry one complete latest snapshot across General and AI edits.
11. Add `AiSettingsViewModel` and focused profile/editor view models with lazy loading, explicit credential draft/save/cancel/replace/remove/validate operations, ZDR confirmation, model refresh/search, readiness, progressive disclosure, and focus-request signals.
12. Add `AiSettingsView.axaml`, integrate it into `SettingsWindow.axaml`, increase the focused window's useful default/minimum size, and add inline confirmations/status/error/loading states with compiled bindings and semantic theme resources.
13. Update `SettingsWindow` closing coordination for the credential discard guard and meaningful focus return without placing provider logic in code-behind.
14. Introduce `AppServices`/`AppServicesFactory` composition, create one long-lived OpenRouter `HttpClient`, expose `IAiTextGenerationService` for future consumers, avoid startup network/keychain work, flush settings, and dispose owned resources at shutdown.

### Tests and platform verification

15. Add `FusionCanvas.Application.Tests/AI/` tests for every profile-resolution branch, model/parameter/reasoning validation rule, unavailable configuration, normalized service result/failure, cancellation, and no-call-before-validation behavior.
16. Add `FusionCanvas.Integration.Tests/AI/` tests using fake native-store backends and fake `HttpMessageHandler` responses for credential mapping, v1/v2 settings compatibility, catalog cache/policy/corruption, request JSON, strict routing/ZDR, response/error parsing, safe GET retry, and no generation retry.
17. Add operating-system smoke tests using unique disposable credential identifiers. Run them in a Windows/macOS/Linux CI matrix; provision an isolated D-Bus/Secret Service session on Linux and appropriate disposable keychain context on macOS. Always remove test credentials in cleanup. These tests require no OpenRouter key or interactive desktop.
18. Add framework-free `AiSettingsViewModelTests` for draft guards, confirmations, rapid preference changes, loading/error/readiness, filtering, custom-profile retention, and command state.
19. Extend Avalonia headless `SettingsWindowTests` for AI section construction, compiled bindings, progressive visibility, control enablement, inline confirmation, keyboard reachability/focus requests, scrollability at minimum size, and theme coherence. Do not test static markup merely for existence.
20. Create `verification.md` with criterion-level results, run strict OpenSpec validation, dependency license/vulnerability checks, build, all deterministic tests, the three-OS credential matrix, and changed-scope architecture/security/spec-drift review. A personal live OpenRouter request is optional supplemental evidence only.

### Decisions not to reopen during implementation

- OpenRouter is the only provider and Chat Completions is the only generation protocol in this module.
- Persistent keys require the native store and fail closed; there is no plaintext or session-only UI fallback.
- ZDR defaults on and opt-out is confirmed.
- The model is user-selected; there is no hardcoded default or silent replacement.
- Unknown parameters do not receive arbitrary controls.
- General/Ideation/Concept use whole-profile inheritance and retained custom snapshots.
- Generation is non-streaming, has no automatic POST retry, and is not persisted automatically.
- Stage-tool assistance UI and prompt design remain out of scope.

## Migration Plan

1. Add the v2 settings reader before any v2-only write path.
2. Read v1 `{ version, darkMode }` as v2 in memory with safe AI defaults; preserve the original file until a normal preference save succeeds.
3. Write v2 through the existing atomic sibling-file replacement pattern. A write failure keeps the current-session configuration and the original readable file.
4. Create catalog cache only after a successful catalog parse and atomic write. Cache failure does not block credential or preference use.
5. Create no native credential until the user explicitly saves a key. Existing users therefore receive no keychain prompt or external network call on upgrade/startup.
6. Rollback to the prior application version can still read no new secret from disk and leaves the native credential orphaned but secure. A later upgraded version can recover it through the stable identifier. Document manual OS-vault removal if downgrade cleanup is required.

## Risks / Trade-offs

- **Security-sensitive third-party native wrapper** → Lock one audited version, keep it behind `IAiCredentialStore`, avoid its memory cache, run vulnerability/license/source review and all three OS smoke gates, and stop for review if a gate fails.
- **Linux desktop lacks Secret Service or libsecret** → Fail closed with installation/unlock guidance; never claim the key was saved.
- **macOS/Linux native vault may prompt or block** → Invoke only from explicit Settings actions or request dispatch, keep UI asynchronous/cancellable, and cover prompt-free isolated CI contexts.
- **Remote catalog schema or model availability changes** → Tolerant DTO parsing, bounded versioned cache, unknown-field tolerance, explicit unavailable selections, and no silent model substitution.
- **Model-level capabilities differ from endpoint-level behavior** → Submit only known advertised values and set `provider.require_parameters = true`.
- **ZDR reduces available models/endpoints** → Explain the filter, show compatibility, permit confirmed opt-out, and never weaken it automatically.
- **Reasoning normalization is approximate across providers** → Present relative advertised effort choices, not provider-independent token guarantees.
- **Large Settings surface** → Dedicated child view, scrollable pane, profile summaries, expanders, and headless minimum-size verification.
- **Credential string exists briefly in managed memory** → Use masked entry, avoid copies/logging, clear drafts immediately, avoid process caching, and make no false guarantee that managed strings can be zeroed.
- **Ambiguous POST failure may already have incurred cost** → Never auto-retry generation; make the error explicit.
- **No first user workflow consumes the service** → Validate the foundation through connection/model Settings behavior and provider-neutral service contract tests; defer workflow UX instead of inventing it here.

## Planned Acceptance Verification

Every named scenario below receives a criterion ID and evidence row in `verification.md`.

| Capability / requirement | Scenarios | Planned verification |
|---|---|---|
| ai-provider-configuration / OpenRouter credentials use native secure storage | User saves an API key; Native credential storage is unavailable; Existing credential cannot be read; Credential replacement fails | Application/Integration credential contract tests, secret-absence inspection of settings/workspace/log fixtures, plus Windows/macOS/Linux disposable-vault smoke tests |
| ai-provider-configuration / Credential editing is explicit and secret-preserving | User views a configured credential; User cancels credential entry; User closes Settings with an unsaved credential draft; User removes a saved credential; User cancels credential removal | App view-model tests plus Avalonia headless masking, discard guard, confirmation, and focus tests |
| ai-provider-configuration / OpenRouter credentials can be validated independently | User saves a key while offline; Inference key validates; Key is invalid or revoked; Management-only key is supplied; Validation is interrupted | Fake-HTTP Integration tests and App status/command tests proving only the current-key GET is sent |
| ai-provider-configuration / Zero Data Retention is the default privacy policy | User opens AI settings for the first time; User opts out of Zero Data Retention; User cancels the privacy opt-out; Privacy change makes the selected model incompatible | Application default/resolution tests, App confirmation tests, and headless visibility/state tests |
| ai-provider-configuration / Model selection uses a dynamic text-capable catalog | Catalog loads successfully; No model has been selected; User searches the model catalog; Catalog refresh fails with a cached catalog; Catalog refresh fails without a cache; Saved model is absent from the current catalog | Fake catalog/cache Integration tests, Application filtering/availability tests, and App search/loading/stale-state tests |
| ai-provider-configuration / Parameter controls follow advertised model capabilities | Model supports a recognized parameter; User leaves a parameter at Provider default; Model does not support a recognized parameter; Model advertises an unknown parameter; Selected model changes | Application parameter-registry/validation tests, request-JSON omission tests, and App dynamic-control tests |
| ai-provider-configuration / Reasoning controls use normalized model metadata | Model does not expose reasoning selection; Model supports optional effort selection; Model requires reasoning; Model supports a reasoning token budget | Application reasoning matrix tests, request-JSON tests, and App control-choice/headless tests |
| ai-provider-configuration / Advanced profiles resolve predictably | Advanced mode is off; Advanced mode is enabled initially; User creates a custom purpose profile; Purpose profile returns to General; Advanced mode is disabled and re-enabled | Exhaustive Application resolver tests, settings round-trip tests, and App profile editor tests |
| ai-provider-configuration / Non-secret AI configuration remains application-wide | User switches workspace; Existing settings are upgraded; AI settings content is invalid | Application/App workspace-independence test and Integration v1/v2/tolerant-recovery fixtures |
| ai-text-generation / Application callers use a provider-independent text service | Caller submits a text request; Caller supplies invalid application input | Application API/boundary inspection and service tests with recording providers |
| ai-text-generation / Text requests resolve the effective profile | Advanced mode is off; Purpose inherits General; Purpose has a custom profile; Effective profile is not ready | Application resolver/service matrix proving selected profile and zero provider calls on invalid state |
| ai-text-generation / OpenRouter translation is strict and privacy-preserving | Request uses safe defaults; Request contains explicit supported overrides; Zero Data Retention is disabled; Reasoning effort is configured; Reasoning token budget is configured | Exact fake-HTTP JSON assertions for omission, strict routing, ZDR, and mutually exclusive reasoning |
| ai-text-generation / Successful responses are normalized without automatic persistence | OpenRouter returns a complete response; Provider omits optional metadata; Request completes | Integration response fixtures, Application result tests, and settings/workspace recording fakes proving no persistence |
| ai-text-generation / Provider failures are actionable and secret-safe | Authentication fails; Account has insufficient credit; No endpoint satisfies the request; Request is rate limited; Request is blocked; Provider returns partial text with an error; Response cannot be interpreted | Error-code fixture matrix, retry-after assertions, partial-output classification, and secret/prompt redaction checks |
| ai-text-generation / Cancellation and retries avoid duplicate generation cost | Caller cancels a pending generation; Generation transport fails ambiguously; Safe metadata read fails transiently | Cancellation-aware fake handlers and call-count tests proving zero POST retries and bounded GET retry |
| ai-text-generation / AI content is treated as untrusted external input | Provider content contains markup or instructions; Diagnostic logging is enabled | Boundary/display-data tests and recording logger assertions for redaction and no execution/service access |
| application-settings / Settings provides a focused AI section | User selects AI settings; User opens Settings frequently for other preferences; User operates AI settings with a keyboard | Settings view-model tests and Avalonia headless section selection, default, tab-order/focus tests |
| application-settings / AI settings uses progressive disclosure | Advanced mode is off; Advanced mode is on; Selected model has additional parameters | App view-model tests and headless visibility/expander tests |
| application-settings / AI settings presents complete interaction states | AI has not been configured; Validation or catalog loading is in progress; Configuration is ready; Operation fails recoverably | App state/command tests and headless inline-state tests |
| application-settings / AI credential drafts and destructive actions preserve user intent | User starts entering a credential; User saves a credential draft; Credential draft is invalid locally; Confirmed destructive action completes | App view-model tests and headless focus/validation/confirmation tests |
| application-settings / Non-secret AI preference edits persist without a pane-wide draft | User changes a valid non-secret AI preference; User changes preferences repeatedly; Non-secret preference cannot be saved | App save-generation tests and Integration complete-settings snapshot round trips |

Routine completion gates are `openspec validate openrouter-api-configuration --strict`, package vulnerability/license checks, `dotnet build .\FusionCanvas.sln`, and `dotnet test .\FusionCanvas.sln`. The native credential smoke matrix supplements the deterministic baseline without contacting OpenRouter. A live desktop/OpenRouter request is optional and cannot change the verdict.

## Open Questions

None. If the selected native credential package fails the mandatory audit or three-platform smoke gates, that is an escalation condition rather than permission to substitute a weaker persistence model.
