## Context

FusionCanvas's active AI foundation deliberately models a selected OpenRouter model while leaving provider routing implicit. That is appropriate for a basic integration, but it is not transparent enough for technically capable users who bring their own provider account. OpenRouter can select among provider endpoints for one model, and the endpoint API exposes materially different capabilities, privacy compatibility, pricing, and performance metadata. A model-only selector therefore does not tell a user what will actually serve a request.

The current text request path already enforces strict parameter support and optional Zero Data Retention, and the normalized result already carries several actual-response fields. This change extends those foundations rather than replacing them. The existing active changes for OpenRouter configuration and model selection remain dependencies; their credential, privacy, cache, and model-selection decisions are not reopened here.

The user-facing setup belongs in the focused AI Settings surface. The primary workspace has no completed generation workflow yet, so this module defines the provider-neutral receipt and the Settings presentation contract without inventing a new assistant workflow or persisting prompts and responses.

## Goals / Non-Goals

**Goals:**

- Let each General, Ideation, and Concept profile choose Automatic routing, a specific provider, or an exact provider endpoint.
- Load endpoint-level metadata lazily and show enough capability, privacy, pricing, and available performance information for an informed choice.
- Preserve an explicit selection when metadata changes and report why it is stale, unavailable, or incompatible.
- Translate each routing policy into OpenRouter request fields so strict choices fail rather than silently using another provider.
- Return a truthful, secret-safe routing receipt for every usable generation result and actionable information for strict-route failures.
- Keep the feature application-wide, per profile, local-first, and compatible with existing settings persistence and credential boundaries.
- Verify Application contracts, Integration JSON, persistence/migration, and meaningful Avalonia interaction behavior deterministically.

**Non-Goals:**

- Benchmarking providers, ranking quality, or promising a quality, latency, uptime, or price guarantee.
- Automatically choosing a provider, endpoint, model replacement, or fallback route on the user's behalf.
- Managing provider accounts or collecting direct supplier credentials; OpenRouter remains the integration boundary.
- Adding a new generation workflow, streaming, tools, structured output, images, or arbitrary provider-specific parameters.
- Persisting prompts, responses, reasoning, authorization headers, or credentials in routing data or receipts.
- Expanding beyond OpenRouter in this module or adding hidden FusionCanvas quality tiers.

## UX Preflight

- **User and outcome:** A technically capable creator opens AI Settings to understand and deliberately choose how each profile is routed through OpenRouter.
- **Frequency and placement:** Routing is occasional configuration, so it stays in the focused AI Settings section and adds no primary-workspace footprint.
- **Progressive disclosure:** The model selector and compact routing mode are visible. Provider or endpoint selectors, capability details, performance metadata, and receipt fields appear in an expandable routing-details area after a model is selected.
- **States:** Loading, cached/stale, unavailable, empty, incompatible, and strict-route-blocked states are explicit and preserve the user's saved selection. Automatic mode remains available when endpoint discovery is unavailable.
- **Keyboard and drafts:** Selectors, expanders, retry actions, and route warnings have predictable focus order. Route changes use the existing non-secret settings save/discard behavior; closing a pane never discards an unsaved change without the established confirmation path.
- **Desktop coverage:** Appium is not warranted for this module because the behavior is bounded to Settings selectors, validation, and receipts. Avalonia headless tests are warranted for visibility, bindings, selection state, focus retention, and disabled generation/action states. A live desktop check may supplement but cannot gate completion.

## Decisions

### 1. Use typed, provider-neutral routing contracts

Add an `AiRoutingMode` discriminated by `Automatic`, `Provider`, and `Endpoint`, plus an `AiRoutingPolicy` containing the selected provider or endpoint identity where applicable. Add an `AiModelEndpointDescriptor` with model ID, provider name, endpoint slug/variant, context and maximum output limits, supported parameter names, Zero Data Retention compatibility, pricing, and optional latency/throughput metadata. Add an `AiGenerationReceipt` for requested route, actual reported model/provider/endpoint, usage, cost, finish state, generation ID, and availability flags.

Application callers consume these records and do not construct OpenRouter JSON. The endpoint identity is an opaque provider-neutral value with an OpenRouter adapter representation at the Integration boundary. Avoid a string dictionary because it would leak provider vocabulary into Application and make invalid combinations persistable.

### 2. Keep routing policy inside each profile and migrate old profiles to Automatic

Extend `AiProfileSettings` with a routing policy. A missing field in existing settings deserializes as `Automatic`; the settings version is incremented using the version established by the active AI foundation. General, Ideation, and Concept each retain an independent policy, following the existing whole-profile inheritance rules.

The policy is application-wide and not workspace data. Endpoint caches are separate bounded non-secret data. Credentials remain in native credential storage, and no route choice changes the existing privacy default or credential lifecycle.

### 3. Discover endpoint data lazily and tolerate staleness

Use the authenticated OpenRouter endpoint-list API for the selected model when routing details are opened or a non-Automatic route needs validation. Do not fetch endpoint metadata for every model during ordinary catalog refresh. Cache endpoint data by model and privacy policy in a bounded, versioned envelope with a freshness timestamp.

Fresh metadata enables a new endpoint selection. Stale metadata remains visible with a warning and can support an already-saved choice subject to request-time validation. If no endpoint metadata is available, preserve a saved route for inspection but block a new endpoint selection and keep the model selector and refresh action usable.

### 4. Translate the three routing modes explicitly

- **Automatic:** omit explicit provider ordering/allow-list fields. OpenRouter may choose among eligible suppliers and may fall back; the UI and receipt identify that this variability is expected. Existing `require_parameters` and ZDR enforcement remain active.
- **Specific provider:** send an allow-list or equivalent ordered provider constraint for the selected supplier and disable fallback to a different supplier. Provider endpoint variants may still be selected by OpenRouter; the UI says that the supplier is fixed but the exact endpoint variant is not.
- **Exact endpoint:** send the selected endpoint variant in `provider.order` and `allow_fallbacks: false`. Do not rely on `allow_fallbacks: false` without the exact ordered endpoint. A request that cannot use that endpoint fails as a routing failure.

All strict routes still send `require_parameters: true` and the current ZDR flag when required. No model fallback array, retry with a different route, or hidden route substitution is added. Throughput/latency values are preferences or observations, not guarantees.

### 5. Validate effective routing before dispatch and preserve intent

The Application resolver validates that a selected provider or endpoint belongs to the selected model, satisfies the active privacy policy, and can support the effective request parameters. A missing or incompatible choice makes the profile not ready and provides a reason. Catalog refresh does not replace it with another provider, endpoint, or model.

At dispatch time Integration remains authoritative for the final request shape because remote capabilities can drift. A strict route that the provider rejects is classified distinctly from a generic model failure and does not trigger an automatic second generation request.

### 6. Make receipts truthful when OpenRouter omits endpoint detail

The normalized result records the requested model and routing policy, then overlays actual model/provider/endpoint/usage/cost/generation data reported by OpenRouter. If the response or generation metadata does not identify the exact endpoint, the receipt shows `Unavailable` rather than inferring it from the request. It may show the requested exact endpoint separately as `Requested`, never as an actual fact.

Receipts are available to existing callers through the result contract and can be displayed or copied in a concise Settings/result surface. They exclude credentials, headers, full prompts, full responses, and reasoning. Missing optional fields do not invalidate usable generated text.

### 7. Use focused, transparent Settings interaction

Add routing mode and endpoint controls to the existing AI Settings profile editor. Automatic mode shows provider variability and fallback behavior. Specific provider shows the supplier and warns that endpoint variants may vary. Exact endpoint shows endpoint capabilities and the fail-not-fallback rule. Stale metadata, no eligible endpoint, and incompatible selections are inline and actionable.

Route edits participate in the existing complete-snapshot save queue. No route choice is committed by merely opening an expander, and busy/status updates do not steal focus.

### 8. Keep failure and cost behavior conservative

Use a stable no-eligible-endpoint/route-not-fulfilled failure category with safe diagnostic context. Do not automatically resubmit a generation after dispatch, because an ambiguous transport result may already have incurred usage. Messages explain that the user can choose Automatic or another route and that any retry is an explicit user action. Cost and zero-output handling remain grounded in the provider response and existing generation semantics; the receipt must not claim billing certainty that OpenRouter did not report.

## Risks / Trade-offs

- **Endpoint metadata can drift or be incomplete.** Preserve selections, mark stale data, validate again at dispatch, and fail strict routes rather than silently substituting.
- **Lazy endpoint fetch adds interaction latency.** Fetch only when routing details are needed, show progress, retain bounded cache data, and keep Automatic mode usable from the model catalog.
- **Provider identity and endpoint variant naming may be ambiguous.** Display the provider and exact endpoint slug separately and treat the opaque endpoint identifier as the stable selection key.
- **OpenRouter may not report the exact endpoint in a generation response.** Separate requested and actual fields and render unavailable metadata honestly.
- **Strict routing can increase visible failures.** Explain the trade-off at the point of choice and offer explicit user-controlled alternatives; never hide the failure behind fallback.
- **The current primary workspace has no receipt host.** Deliver the provider-neutral result and focused Settings/result presentation contract now; integrate with future generation surfaces without inventing one in this module.
- **Settings migration can be malformed independently of appearance/workspace data.** Apply the existing tolerant AI-settings migration pattern, default only the missing route policy, and preserve readable unrelated preferences.

## Migration Plan

1. Implement against the active OpenRouter configuration and model-selection changes; those changes must be synchronized or archived in the required order before this module is archived.
2. Increment the application-settings version according to the active foundation. Existing profiles without routing fields deserialize to `Automatic`, preserving prior OpenRouter behavior while making the possibility of provider fallback visible.
3. Keep existing model catalog caches readable. Endpoint caches are additive and can be absent, stale, or discarded without losing model selections or credentials.
4. On save, write the complete non-secret settings snapshot atomically using the existing settings-store safeguards. No workspace migration, credential migration, or destructive data rewrite is required.
5. Rollback is a code/artifact revert; do not rewrite user settings to remove route fields. Older builds should ignore the additive fields according to the established tolerant settings contract.

## Implementation Plan

### Application contracts and policy

1. Extend `src/FusionCanvas.Application/AI/AiProfileSettings.cs` with the routing policy and add focused immutable records/enums for routing mode, endpoint descriptors, endpoint metadata state, route readiness, and generation receipts.
2. Add endpoint catalog and routing-resolution ports beside the existing AI catalog/text-provider ports. Keep endpoint descriptors provider-neutral and keep OpenRouter-specific field names out of Application.
3. Extend the effective-profile resolver to validate provider/endpoint ownership, privacy compatibility, supported parameters, stale/unavailable state, and strict-route readiness without mutating saved selections.
4. Extend stable AI failure/result contracts with route-not-fulfilled and truthful receipt data while preserving existing actual model/provider/usage/generation fields.

### Persistence and metadata

5. Update versioned application-settings serialization and migration so missing policies become `Automatic`; add bounded endpoint-cache envelopes keyed by model and privacy policy with freshness and schema version.
6. Add round-trip, migration, malformed-AI-content, stale-cache, and selection-preservation tests. Confirm no secrets, prompts, responses, or reasoning are serialized.

### OpenRouter Integration

7. Add endpoint-list request/response DTOs and bounded parsing to `src/FusionCanvas.Integration/AI/OpenRouterClient.cs` or its focused adapter, retaining the existing credential, timeout, ZDR, and retry boundaries.
8. Extend text request JSON construction with the exact routing translation: no explicit provider fields for Automatic; selected-provider restriction with no different-provider fallback for Specific provider; exact `provider.order` plus `allow_fallbacks:false` for Exact endpoint. Preserve `require_parameters:true` and ZDR behavior.
9. Map response and generation metadata into receipts without inventing endpoint facts. Map no-eligible-provider and strict-route rejection to stable actionable failures. Add fake-HTTP tests for exact serialized JSON, missing metadata, endpoint fetch failures, and no POST retry.

### Settings UX and composition

10. Extend the AI Settings/profile editor view models and compiled Avalonia view with routing mode, provider/endpoint selectors, capability summaries, stale/loading/error/blocked states, explicit warnings, and receipt display/copy behavior.
11. Preserve selection and focus through refresh, route changes, rejected discard, and unavailable endpoint data. Add Avalonia headless tests for bindings, visibility, selector state, keyboard focus, and disabled actions; do not add ceremonial Appium coverage.
12. Wire endpoint catalog, cache, resolver, and result services through the existing explicit App composition root without a service locator or provider credential leakage.

### Verification gates

13. Verify each `ai-provider-routing` scenario with focused Application and Integration tests, each `application-settings` scenario with view-model and headless view tests, and record criterion-level evidence in `verification.md` during implementation.
14. Run `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, `openspec validate transparent-model-provider-routing --strict`, and `openspec validate --all --strict`.
15. Complete changed-scope architecture, persistence, privacy/secret-redaction, UI-state, and spec-drift review. Optional live OpenRouter/desktop checks use disposable state and cannot replace the deterministic gates.

### Acceptance-to-verification mapping

| Acceptance area | Primary evidence |
| --- | --- |
| Profile modes, endpoint metadata, selection preservation | Application policy/serialization tests and endpoint-cache tests |
| Automatic/provider/exact request semantics | Integration fake-HTTP JSON assertions and route-failure tests |
| Strict parameters and ZDR remain enforced | Existing AI request tests extended with routing combinations |
| Truthful receipt and missing metadata | Integration response fixtures plus Application result tests |
| Settings transparency and progressive disclosure | View-model tests and Avalonia headless visibility/focus/interaction tests |
| Migration, stale/unavailable states, and no secret persistence | Settings-store/cache round trips, malformed-input fixtures, and redaction assertions |
| No duplicate generation cost from fallback/retry | Call-count and cancellation/ambiguous-transport tests |
| Repository readiness | Full build/test baseline, strict OpenSpec validation, and changed-scope review |

## Open Questions

None. Exact control labels, visual spacing, and the final placement of receipt access within the existing result surface are implementation details constrained by the decisions above, not product-blocking choices.
