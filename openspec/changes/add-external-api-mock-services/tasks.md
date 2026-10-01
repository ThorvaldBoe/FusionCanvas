## 1. Inventory and mock boundary

- [x] 1.1 Add the external API inventory and testing-boundary document, covering the three live service types, all eight application-facing interfaces, the absence of Shopify implementation, and the live-adapter versus mock-consumer test distinction.
- [x] 1.2 Reconcile the implementation against the inventory and OpenSpec capability so every current outbound service has exactly one named mock twin and no unimplemented Shopify mock is added.

## 2. OpenRouter mock twin

- [x] 2.1 Implement `MockOpenRouterClient` in the Integration Testing capability with all six OpenRouter application contracts, synthetic deterministic defaults, configurable contract-level results/failures, and no endpoint-capable dependencies.
- [x] 2.2 Add non-secret OpenRouter request observations for text and image generation plus cancellation checks; verify supplied API keys are not retained or exposed.
- [x] 2.3 Add focused tests for OpenRouter mock interface parity, default catalogs/endpoints/generation, configurable validation and generation failures, observations, cancellation, and deterministic repeated calls.

## 3. Printify mock twins

- [x] 3.1 Implement `MockPrintifyCatalogClient` with the complete `IPrintifyCatalogClient` surface, synthetic blueprint/provider/product data, deterministic selection behavior, configurable empty/failure results, non-secret observations, and cancellation checks.
- [x] 3.2 Implement `MockPrintifyCredentialVerifier` with synthetic shop data, configurable verification outcomes, cancellation checks, and no retained credential.
- [x] 3.3 Add focused tests for Printify mock interface parity, catalog/shop selection, empty and invalid-request outcomes, verification outcomes, observations, cancellation, and deterministic repeated calls.

## 4. Consumer test adoption and verification

- [x] 4.1 Add at least one application-facing consumer test that injects a reusable mock twin instead of constructing a live adapter or HTTP client, while leaving transport-specific adapter tests in their existing focused suites.
- [x] 4.2 Run focused mock and consumer tests; map every external-api-mocking acceptance scenario to a concrete test or documented source-review evidence in `verification.md`.
- [x] 4.3 Run `openspec validate` and correct any artifact/spec issues before final verification.
- [ ] 4.4 Run the canonical deterministic baseline `dotnet test .\FusionCanvas.sln -m:1` and record the result and any environmental limitation in `verification.md`.
