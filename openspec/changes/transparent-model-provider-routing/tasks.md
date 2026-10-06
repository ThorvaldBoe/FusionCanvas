## 1. Application routing contracts

- [x] 1.1 Add typed `AiRoutingMode`, `AiRoutingPolicy`, endpoint identity/descriptor records, endpoint metadata state, and route-readiness/failure contracts under `src/FusionCanvas.Application/AI`.
- [x] 1.2 Extend `AiProfileSettings` and the normalized AI result contract with routing policy and `AiGenerationReceipt` data while preserving existing model/provider/usage fields.
- [x] 1.3 Add endpoint-catalog and routing-resolution ports with provider-neutral types; keep OpenRouter JSON vocabulary out of Application callers.
- [x] 1.4 Extend effective-profile resolution to validate model ownership, privacy compatibility, supported parameters, stale/unavailable metadata, and strict-route readiness without replacing saved selections.
- [x] 1.5 Add focused Application tests for Automatic, Specific provider, and Exact endpoint policy matrices, inheritance, incompatible selections, route failures, receipt truthfulness, and missing optional metadata.

## 2. Settings persistence and endpoint cache

- [x] 2.1 Extend versioned application-settings serialization so profiles missing routing data migrate to `Automatic` and existing appearance/workspace-independent settings remain intact.
- [x] 2.2 Add a bounded, versioned endpoint-cache envelope keyed by model and privacy policy with freshness metadata, atomic writes, tolerant corruption handling, and no secret/prompt/response fields.
- [x] 2.3 Add persistence tests for migration, round trips, malformed AI-only content, stale caches, unavailable endpoint data, and explicit selection preservation.

## 3. OpenRouter endpoint discovery and request enforcement

- [x] 3.1 Implement bounded OpenRouter endpoint-list retrieval and parsing, including provider identity, endpoint variant, capability limits, privacy compatibility, pricing, and optional performance metadata.
- [x] 3.2 Integrate endpoint retrieval with the endpoint cache and existing credential, timeout, ZDR, safe-read retry, and secret-redaction boundaries.
- [x] 3.3 Extend text request serialization so Automatic omits explicit provider routing, Specific provider restricts the selected supplier without different-provider fallback, and Exact endpoint sends the selected endpoint with `allow_fallbacks:false`.
- [x] 3.4 Preserve `require_parameters:true`, active ZDR enforcement, current parameter validation, and the no-generation-POST-retry rule for all routing modes.
- [x] 3.5 Map provider responses and generation metadata into truthful receipts; map no-eligible-provider and strict-route rejection to stable actionable failures without fabricating endpoint facts.
- [x] 3.6 Add Integration fake-HTTP tests for endpoint parsing/cache inputs, exact Automatic/provider/endpoint JSON, ZDR and parameter preservation, missing endpoint metadata, strict failures, and request call counts.

## 4. AI Settings and composition

- [x] 4.1 Wire endpoint catalog, cache, routing resolver, and receipt services through the existing explicit App composition root without a service locator or credential leakage.
- [x] 4.2 Extend AI Settings/profile view models with routing mode selection, provider/endpoint choices, lazy loading, stale/loading/unavailable/incompatible states, and explicit strict-route failure guidance.
- [x] 4.3 Extend the compiled Avalonia AI Settings view with progressive disclosure, capability summaries, fallback/no-fallback explanations, and route receipt contract support without secrets or full content.
- [x] 4.4 Preserve route selections, draft/save/discard behavior, and keyboard focus through refreshes, stale data, rejected discard, route changes, and unavailable endpoint metadata.
- [x] 4.5 Add Application/App view-model tests for transparency messages, route readiness, selection preservation, save behavior, receipt formatting, and explicit retry/route-change actions.
- [x] 4.6 Add Avalonia headless view tests for selector bindings, progressive disclosure, loading/error/blocked visibility, disabled actions, and predictable focus behavior. Record why no new Appium journey is warranted.

## 5. Criterion-level verification and review

- [x] 5.1 Create or update `verification.md` and map every `ai-provider-routing` and `application-settings` scenario to focused test evidence or an explicitly recorded blocked/unknown result.
- [x] 5.2 Run `dotnet build .\FusionCanvas.sln` and fix changed-scope compiler or analyzer issues.
- [x] 5.3 Run `dotnet test .\FusionCanvas.sln`, including deterministic Application, Integration, and Avalonia headless coverage.
- [x] 5.4 Run `openspec validate transparent-model-provider-routing --strict` and `openspec validate --all --strict`; correct any artifact or cross-change drift.
- [x] 5.5 Complete changed-scope architecture, persistence/migration, privacy and secret-redaction, UI-state/focus, and no-silent-substitution review; record optional live checks separately from the deterministic verdict.
