# Verification: Transparent model and provider routing

Verification was run against the implementation in this change. Existing unrelated working-tree changes were left untouched.

## Acceptance matrix

| Requirement / scenario | Method | Result | Evidence and limitations |
| --- | --- | --- | --- |
| Profiles — existing profile is migrated | Integration test + code inspection | PASS | `JsonApplicationSettingsStoreTests.LoadAsync_WithoutSelectedStorePreferenceRemainsBackwardCompatible` and `JsonApplicationSettingsStore.Normalize` preserve model/parameters and default routing to Automatic. |
| Profiles — specific provider | Application contract test + headless settings test | PASS | `AiRoutingTests` and `AiSettingsViewTests.ProfileEditor_RendersTransparentRoutingChoices` cover non-secret provider identity and the provider explanation. |
| Profiles — exact endpoint | Application contract test + persistence test + headless settings test | PASS | `AiRoutingTests`, `JsonApplicationSettingsStoreTests.Version2_RoundTripsCompleteAiSettingsWithoutASecret`, and the routing view test preserve endpoint identity and expose the no-silent-fallback explanation. |
| Endpoint metadata — loads | Integration fake-HTTP test | PASS | `OpenRouterClientTests.GetEndpointsAsync_ParsesProviderVariantCapabilitiesAndPrivacy` covers identity, variant, limits, privacy, pricing and performance parsing. |
| Endpoint metadata — stale | Cache test + headless settings test | PASS | `JsonAiModelEndpointCatalogCacheTests` marks stale entries; `AiSettingsViewTests` verifies the stale status message. The refresh button invokes the existing lazy-load path. |
| Endpoint metadata — unavailable | View-model implementation review + existing settings coverage | PASS | The editor retains the saved route and reports that endpoint information is unavailable; loading failures fall back to stale cache when present. No live provider call is made by the deterministic test lane. |
| Routing — Automatic | Integration request serialization test | PASS | `OpenRouterClientTests.GenerateAsync_TranslatesRoutingModeWithoutWeakeningZdr` verifies Automatic omits explicit provider routing while the receipt identifies Automatic routing. |
| Routing — specific provider | Integration request serialization test | PASS | The same theory verifies `provider.only` and `allow_fallbacks:false`. |
| Routing — exact endpoint | Integration request and failure tests | PASS | The same theory verifies `provider.order` and disabled fallback; `GenerateAsync_StrictRouteFailureIsActionableAndDoesNotRetry` verifies the route-specific failure. |
| Routing — strict parameters and ZDR | Existing and new integration tests | PASS | `GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage` plus the routing theory verify `require_parameters:true`, ZDR, and existing parameter serialization remain active. |
| Selection — endpoint disappears | Application resolver test | PASS | `AiRoutingTests.ResolverPreservesExplicitEndpointWhenCatalogNoLongerContainsIt` retains the policy and reports `EndpointUnavailable`; no replacement is selected. |
| Selection — endpoint becomes incompatible | Application resolver tests + code inspection | PASS | `AiRoutingTests` covers missing/incompatible provider routing; resolver preserves the explicit policy and returns a blocking availability. |
| Receipt — successful generation | Integration result parsing test + contract review | PASS | `GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage` verifies model, usage and generation response mapping; `AiGenerationReceipt` carries route, provider, endpoint, finish and cost fields when supplied. |
| Receipt — strict route failure | Integration failure test | PASS | `GenerateAsync_StrictRouteFailureIsActionableAndDoesNotRetry` verifies `RouteNotFulfilled`, requested endpoint retention, no fabricated actual endpoint, and one POST. |
| Receipt — displayed/copied summary | Application contract review | PASS with limitation | `AiGenerationReceipt.Summary` is concise and excludes credentials/content by construction. The current workspace has no completed generation-diagnostics surface to exercise a copy command yet. |
| Failures — no eligible endpoint | Integration failure mapping test + code inspection | PASS | Strict provider/provider-response failures map to `RouteNotFulfilled` where applicable, while normal provider failures retain their stable failure kinds and sanitized messages. |
| Failures — after dispatch | Integration request-count test | PASS | `OpenRouterClientTests.GenerateAsync_MapsFailuresWithoutRetry` and the strict-route test verify one generation POST and no automatic resubmission. |
| Settings — Automatic transparency | Headless view test + code inspection | PASS | The provider-routing expander keeps the model visible and explains that OpenRouter may vary providers and use fallback endpoints. |
| Settings — specific provider transparency | Headless view test + code inspection | PASS | The provider selector is separate from the model selector and the explanation states endpoint variants may still differ. |
| Settings — exact endpoint transparency | Headless view test | PASS | The exact endpoint selector, endpoint summary and no-silent-fallback explanation are present. |
| Settings — progressive disclosure | Avalonia headless test | PASS | `AiSettingsViewTests.ProfileEditor_RendersTransparentRoutingChoices` verifies the expander, selectors, endpoint items, and summary. |
| Settings — endpoint data unavailable | View-model/UI review | PASS | The selector remains empty, the summary explains refresh is required, and model selection remains available. |
| Settings — incompatible route blocks readiness | Application resolver + readiness mapping review | PASS | `RoutingUnavailable` is surfaced by `AiSettingsViewModel` as an unavailable provider/endpoint rather than silently changing the route. |
| Settings — route changes and discard | Existing settings persistence/discard coverage + additive model behavior review | PASS | Routing is part of the existing profile snapshot/save/discard path; no separate persistence path or focus reset was introduced. |

## Deterministic verification commands

- `dotnet build .\FusionCanvas.sln -c Release --no-restore` — PASS.
- Focused Application routing tests — PASS, 4 tests.
- Focused Integration routing/cache/settings tests — PASS, 74 tests in the final focused run.
- Focused Avalonia routing/settings tests — PASS, 6 tests in the final focused run.
- `dotnet test .\FusionCanvas.sln -c Release --no-restore` — PASS, 2,234 tests across Domain, UI-description, Application, Integration, and App suites.
- `openspec validate transparent-model-provider-routing --strict` — PASS.
- `openspec validate --all --strict` — PASS, 82 artifacts.

The default Debug build was attempted earlier and was blocked only by a running FusionCanvas process holding Debug output assemblies. The Release build is the deterministic build evidence; the user process was not terminated.

## Desktop coverage decision

No new Appium scenario is warranted for this module. The change is an AI Settings configuration surface with no completed generation journey in the current workspace. Avalonia headless coverage exercises the meaningful selector, disclosure, stale-state, and capability-summary behavior without depending on a user desktop or real provider credentials.

## Changed-scope review

- Application contracts remain provider-neutral; OpenRouter request vocabulary is confined to Integration.
- Route identifiers and endpoint metadata are non-secret; credentials, prompts, responses, and reasoning content are not stored in settings, cache envelopes, or receipts.
- Endpoint cache writes are bounded, versioned, atomic, and tolerant of malformed entries.
- Explicit selections are preserved when metadata changes; no fallback model/provider/endpoint is selected by FusionCanvas.
- Generation POSTs are not retried automatically.
