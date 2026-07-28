# Proposal: fix-openrouter-model-selection

## Why

A creator who saves and successfully validates an OpenRouter API key still cannot select a model: the model selector stays empty. Investigation of the report found three defects in the current implementation:

1. **The catalog is never loaded automatically.** The model catalog is fetched only when the user manually presses `Refresh models`. Saving or validating a credential never triggers a fetch, toggling the privacy policy only reloads an empty cache, and the empty selector gives no guidance. A first-time user with a valid key sees an empty, unexplained selector.
2. **Zero Data Retention catalog filtering is fabricated.** The client calls `GET /api/v1/models/user?zdr=true`, but the OpenRouter API has no such query parameter (verified against the live OpenAPI specification and the live service). The parameter is silently ignored, and every returned model is stamped `ZeroDataRetentionCompatible` with the *requested* policy flag rather than any real endpoint data. With ZDR required — the default — every model is falsely labeled ZDR-compatible, a privacy mislabeling defect. The base change's verification record confirms the live contract was never exercised (`Live OpenRouter request: Not run`). The selector also never narrows to privacy-compatible choices even though the accepted requirement says it must.
3. **Refresh is fragile.** If the cache write fails after a successful fetch, the fetched catalog is discarded and the user is told the refresh failed; all failure causes (rejected key, rate limit, network, parse) collapse into one generic message.

## What Changes

- **Auto-load the model catalog** in three situations, using the existing safe metadata GET: after a credential validates successfully; when AI settings loads with a readable credential but no cached catalog for the active policy; and after the privacy policy changes when no cache exists for the newly active policy. The manual `Refresh models` action remains as the explicit retry.
- **Make ZDR catalog filtering honest.** Remove the non-existent `?zdr=true` request parameter. When loading the catalog, also read OpenRouter's public ZDR endpoint list (`GET /api/v1/endpoints/zdr`) and stamp each model's `ZeroDataRetentionCompatible` from real data: the model has at least one ZDR-policy endpoint. While ZDR is required, the model selector shows only compatible models, as the accepted requirement already states. If the ZDR list cannot be loaded while ZDR is required, the refresh fails closed with an actionable message instead of mislabeling models. Request-time ZDR enforcement (`provider.zdr: true`) is already correct and is unchanged.
- **Make refresh robust.** A successfully fetched catalog is applied to the selector even when the cache write fails; the cache failure surfaces as a non-blocking warning. Refresh failures are mapped to secret-safe, actionable categories (key rejected, rate limited with retry-after, network/timeout, unexpected response).
- **Explain the empty selector.** When no catalog is available, the AI pane says why and points to the retry action instead of showing a silently empty dropdown.

Non-goals: changing generation-time ZDR enforcement, multiple providers, per-endpoint provider display, model recommendations or automatic model choice, cache-file format or contract redesign (the version-1 dual-envelope cache stays; its per-model flags become truthful after the next refresh), and any Settings layout redesign.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-provider-configuration`: the catalog requirement changes to honest, evidence-based ZDR filtering with a privacy-compatible selector; new requirements cover automatic catalog loading, refresh failure categorization, cache-write degradation, and empty-catalog guidance. This capability is defined by the **active** base change `openrouter-api-configuration`; this fix must not be archived before that base change is synchronized or archived (same ordering rule already used by `integrate-ideation-openrouter-snowclones`).

## Impact

- **Code:** `FusionCanvas.Integration/AI/OpenRouterClient.cs` (catalog fetch, ZDR endpoint list, typed catalog failure), `FusionCanvas.App/Settings/AiSettingsViewModel.cs` (auto-load triggers, refresh ordering, failure/empty-state messaging), `AiProfileEditorViewModel.cs` (privacy-compatible selector narrowing), `FusionCanvas.Application/AI` (catalog failure/result contract types only).
- **APIs:** OpenRouter `GET /api/v1/models/user` (unchanged usage, fabricated query parameter removed) and `GET /api/v1/endpoints/zdr` (new read; public, no credential, no submitted content).
- **Dependencies:** none added. No schema, settings-document, or cache-format migration.
- **Specs/OpenSpec:** delta spec for `ai-provider-configuration`; archive ordering constrained by the active base change.
- **Tests:** deterministic fake-HTTP and view-model tests; Avalonia headless view tests for selector narrowing and empty-state guidance. A live OpenRouter smoke check is optional supplemental evidence only.

## UX Preflight

User-facing, in the existing focused Settings AI pane (occasional administration — no main-workspace footprint). The primary workflow is one-time connection setup: save key → validate → choose model. After this fix, that flow populates the selector without a manual refresh press. Auto-loads reuse the existing busy indication and cannot start duplicate fetches; failure states keep the manual retry and show a secret-safe reason; the empty selector always explains itself. No new controls are added.
