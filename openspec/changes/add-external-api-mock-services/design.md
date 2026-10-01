## Context

The current outbound integrations are confined to the Integration layer:

- `OpenRouterClient` calls OpenRouter and implements `IAiCredentialValidator`, `IAiModelCatalogProvider`, `IAiImageModelCatalogProvider`, `IAiImageEndpointCatalogProvider`, `IAiTextProvider`, and `IAiImageGenerationProvider`.
- `PrintifyCatalogClient` calls Printify catalog and shop-product endpoints through `IPrintifyCatalogClient`.
- `PrintifyCredentialVerifier` calls the Printify shops endpoint through `IPrintifyCredentialVerifier`.

The application contracts already provide the correct dependency boundary. Existing live-adapter tests use local `HttpMessageHandler` fakes, while application tests commonly define local stubs. The module adds reusable mock twins without changing live construction or user-facing behavior.

## Goals / Non-Goals

**Goals:**

- Provide one explicit mock twin per current external API service boundary.
- Cover all six OpenRouter provider contracts and both Printify contracts.
- Keep mocks deterministic, in-memory, synthetic, cancellation-aware, and safe for unit-test use.
- Make relevant non-secret calls observable so tests can assert request shape and call count.
- Document the current inventory and the distinction between mock-consumer tests and live-adapter transport tests.

**Non-Goals:**

- Adding Shopify support.
- Introducing a production toggle or environment variable for mock routing.
- Replacing every existing local test stub in this module.
- Replacing transport fakes in tests that specifically verify live HTTP serialization and parsing.
- Persisting mock data or reproducing every provider quirk.

## Decisions

1. **Keep contracts in Application and mocks in Integration.** The existing interfaces are already application-facing ports and the Integration project owns external adapters. Placing mocks in Integration keeps the dependency direction intact and makes the live/mock pairing discoverable. A new shared testing project was rejected because it would add project topology for only three current services.

2. **Use explicit mock classes, not a generic dynamic mock framework.** `MockOpenRouterClient`, `MockPrintifyCatalogClient`, and `MockPrintifyCredentialVerifier` make parity review and safe behavior obvious. Moq/NSubstitute-style generated doubles were rejected because they do not provide provider-shaped synthetic defaults or a durable inventory of service twins.

3. **Use configurable properties plus typed observations.** Each mock has synthetic defaults and allows tests to replace contract-level results. It records only non-secret request data such as model, messages, shop ID, and selected product IDs. A scripted transport recorder was rejected for consumer tests because it would preserve HTTP details that the application contracts intentionally hide.

4. **Preserve live adapters and transport tests.** The live classes remain responsible for HTTP paths, headers, parsing, retries, limits, telemetry, and provider-specific error mapping. Their existing fake-handler tests remain valuable adapter tests; the new mocks cover consumers above that transport boundary.

5. **Keep mock routing opt-in.** No production composition root change selects mocks by default. Tests explicitly construct and inject them, preventing accidental real-user behavior changes. A future debug-only composition option can build on this boundary in a separate change.

## Risks / Trade-offs

- [Mock drift] A mock can become stale when a live contract changes → parity tests and the inventory name every interface implemented by each twin; future external services must add a twin in the same change.
- [Over-simplification] Synthetic responses cannot represent every provider nuance → retain focused live-adapter fake-HTTP tests for transport semantics and use configurable contract-level failures for consumer behavior.
- [Sensitive test data] Call observations could accidentally retain credentials → observations intentionally use typed records without API-key fields, and tests assert the supplied key is absent from serialized observations.
- [False confidence] Mock success can hide provider response-shape defects → the deterministic solution baseline keeps live adapters' local transport tests separate and required.

## Migration Plan

No database or user-data migration is required. Add the mock classes and focused tests, then use them in new application tests where external contracts are consumed. Existing live-adapter tests remain unchanged except for any small naming or inventory documentation updates. Rollback is deleting the mock types, tests, and this change; live runtime construction is unaffected.

## Open Questions

None for this module. A production/debug mock selector, recording/replay fixtures, and broader external-service scenario libraries are intentionally deferred until a concrete consumer requires them.

## Implementation Plan

1. Add `FusionCanvas.Integration.Testing` mock classes for OpenRouter, Printify catalog, and Printify credential verification. Keep one primary type per file and no HTTP or persistence dependencies.
2. Add typed non-secret observation records and synthetic defaults inside the Testing capability. Implement cancellation checks and configurable contract-level results/failures.
3. Add contract-parity and behavior tests under `tests/FusionCanvas.Integration.Tests/Testing`, including network-independent execution and secret non-retention assertions.
4. Add the external API inventory document under `docs/` and state that Shopify is not implemented. Keep it synchronized with the three live/mock pairs.
5. Run focused tests, strict OpenSpec validation, and `dotnet test .\FusionCanvas.sln -m:1`; record criterion-level evidence in `verification.md`.

## Acceptance-to-Verification Mapping

| Acceptance area | Verification |
| --- | --- |
| OpenRouter and Printify live/mock parity | Compile-time implementation plus reflection/interface assertions in `MockExternalApiServiceTests` |
| No external I/O | Mock tests run with no `HttpClient`; source review confirms Testing types have no endpoint-capable dependency; focused tests assert deterministic completion |
| Synthetic deterministic defaults and configurable outcomes | Focused mock behavior tests for defaults, configured result/failure, empty data, and cancellation |
| Secret-safe observations | Tests serialize observations and assert supplied keys are absent |
| Consumer-vs-adapter test boundary | Inventory document and test layout review; existing live adapter tests remain under AI/Stores and new consumer-facing mock tests are under Testing |
| Regression safety | `openspec validate` and solution-level deterministic test baseline |
