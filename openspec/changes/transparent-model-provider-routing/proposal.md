## Why

OpenRouter separates model selection from provider-endpoint routing, but the current FusionCanvas AI foundation intentionally leaves that routing implicit. Users who choose a model cannot yet tell whether OpenRouter may use different suppliers, what endpoint capabilities are available, or whether a request will fail or silently fall back when a selected route is unavailable.

FusionCanvas users are technically capable and use their own provider accounts. This module gives them transparent, per-profile control over model routing and a truthful record of what actually served each generation, without pretending that FusionCanvas guarantees provider quality.

## What Changes

- Add transparent per-profile routing choices for General, Ideation, and Concept AI configurations:
  - Automatic OpenRouter routing with provider fallback disclosed.
  - Specific-provider routing restricted to a selected supplier.
  - Exact-endpoint routing restricted to one provider endpoint variant with fallbacks disabled.
- Extend model discovery with endpoint-level information needed for an informed choice, including provider identity, endpoint variant, context and output limits, supported parameters, privacy compatibility, pricing, and available performance metadata when supplied.
- Show routing and endpoint choices in the focused AI Settings surface using progressive disclosure, with clear stale, unavailable, and no-fallback warnings.
- Preserve explicit model and endpoint selections when catalogs change; mark unavailable or incompatible selections rather than silently substituting another model or endpoint.
- Enforce the selected routing policy on every text-generation request and fail visibly when the requested endpoint or policy cannot be honored.
- Return and present a generation receipt containing the requested model, actual provider information when reported, routing mode, usage, cost, finish state, and generation identifier when available.
- Keep endpoint and routing preferences application-wide and per AI profile; do not store credentials, prompts, responses, or reasoning content in the new routing data.
- Add deterministic Application, Integration, and Avalonia headless coverage for endpoint selection, strict routing JSON, unavailable-endpoint failures, receipt presentation, selection preservation, and keyboard-accessible settings behavior.
- **BREAKING**: The AI configuration contract changes from model-only selection to model-plus-routing policy. Existing profiles migrate to explicit `Automatic` routing so their prior behavior remains available and visible.

This module does not promise model quality, uptime, latency, or immutable provider infrastructure. It promises that FusionCanvas will accurately apply and report the user's chosen routing policy.

## Capabilities

### New Capabilities

- `ai-provider-routing`: Endpoint-aware OpenRouter catalog data, per-profile routing policy, strict provider/endpoint enforcement, selection preservation, generation receipts, and failure behavior when a routing constraint cannot be honored.

### Modified Capabilities

- `application-settings`: Extend the focused AI Settings surface with transparent model/provider-endpoint selection, routing-mode explanations, endpoint capability details, stale/unavailable states, and receipt access without exposing secrets or consuming primary-workspace space.

## Impact

- **Application:** Add provider-neutral endpoint descriptors, routing-policy records, effective-profile validation, routing resolution, generation-receipt contracts, and stable routing-related failure categories.
- **Integration:** Extend the OpenRouter catalog adapter to load per-model endpoint data and extend request serialization with `provider.order`, `provider.only`, `allow_fallbacks`, and strict parameter/privacy controls. Preserve the existing no-retry rule for generation POSTs.
- **App:** Extend AI Settings profile editors and the focused settings view with routing-mode and endpoint selectors, capability summaries, stale-data warnings, explicit failure guidance, and receipt display/copy behavior.
- **Persistence:** Extend versioned application settings and catalog caches with non-secret routing policy and endpoint identity. Existing profiles migrate to Automatic routing; no workspace records or credential-store formats change.
- **Dependencies:** Depends on the active `openrouter-api-configuration` foundation and the `fix-openrouter-model-selection` catalog corrections. The new module must not duplicate or reopen their credential, ZDR, cache, or model-selection decisions except where endpoint routing requires an explicit extension.
- **Security and privacy:** API keys remain in native credential storage. Endpoint metadata, model IDs, provider names, usage, and generation IDs are bounded and secret-safe. Prompts, responses, reasoning, authorization headers, and credentials remain excluded from receipts and logs by default.
- **Verification:** Run focused deterministic tests, Avalonia headless Settings tests for meaningful selector/binding/focus behavior, the full `dotnet test .\FusionCanvas.sln` baseline, strict validation for this change and the combined OpenSpec set, plus changed-scope architecture, privacy, persistence, and spec-drift review.
