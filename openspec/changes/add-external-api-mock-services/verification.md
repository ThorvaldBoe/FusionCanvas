# External API Mock Services Verification

## Acceptance scenarios

| Requirement / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Current inventory identifies OpenRouter and both Printify services | Source inventory and implementation review | PASS | `docs/external-api-mocking.md` lists three live services, eight application-facing interfaces, three mock twins, and no Shopify implementation. |
| OpenRouter mock implements all six live contracts | Reflection/interface parity test | PASS | `MockExternalApiServiceTests.MocksImplementTheCompleteCurrentExternalServiceContracts`; `MockOpenRouterClient` implements all six contracts. |
| Printify mocks implement both live contracts | Reflection/interface parity test | PASS | Same parity test; `MockPrintifyCatalogClient` and `MockPrintifyCredentialVerifier` implement their respective interfaces. |
| Mock calls produce in-memory results without external I/O | Focused test run and source review | PASS | Six mock tests passed; Testing types contain no `HttpClient`, transport, persistence, or credential-store dependency. |
| Tests remain deterministic without network | Repeated-call mock test and focused test run | PASS | Synthetic OpenRouter response and catalog data are stable; focused run passed 6/6 without endpoint access. |
| Defaults are synthetic and stable | Focused behavior tests | PASS | Synthetic model, provider, image, blueprint, product, and shop values are defined in the mock classes and exercised by tests. |
| Configurable failures are returned deterministically | Focused behavior tests | PASS | OpenRouter text/image failures and Printify catalog/credential failures are configured and asserted. |
| Cancellation is observed before work | Focused behavior tests | PASS | Cancelled OpenRouter catalog and Printify verification calls throw before recording/performing the operation. |
| API keys are not retained or exposed | Serialized observation assertions and source review | PASS | Text and Printify observations serialize without the supplied synthetic keys; mock APIs do not store key parameters. |
| Non-secret request shape is observable | Focused behavior tests | PASS | OpenRouter text observations and Printify catalog observations assert model, messages, shop, and product data. |
| Application consumer can inject a mock twin | Application-facing consumer test | PASS | `AiTextApplicationServiceCanUseTheReusableOpenRouterMock` constructs `AiTextGenerationService` with `MockOpenRouterClient`; no live adapter or HTTP client is used. |
| Live adapter tests remain transport-focused | Test-layout and existing-suite review | PASS | Existing OpenRouter/Printify fake-handler suites remain under `AI`/`Stores`; new mock-consumer tests are under `Testing`. No live endpoint call was added. |

## Validation runs

- `openspec validate add-external-api-mock-services`: PASS.
- `git diff --check`: PASS.
- Focused mock suite: PASS, 6 passed, 0 failed.
- `dotnet test .\FusionCanvas.sln -m:1`: PARTIAL/FAILED because of unrelated existing App-test failures. Domain (265/265), Application (541/541), Integration (301/301), and UI-description (29/29) passed. App tests reported 851 passed and 8 failed: one existing Snowclones production-source-layout violation, five dispatcher thread-affinity failures in `WorkspaceManagementViewModelTests`, and one existing `MainWindowViewModelTests` timeout (the remaining failure count includes the parameterized cases). The new Integration mock tests passed within the 301 Integration tests.
- Restore/build emitted `NU1900` warnings because NuGet vulnerability metadata at `https://api.nuget.org/v3/index.json` was unreachable in the sandbox. This did not prevent compilation or the focused tests.

The full solution gate remains unresolved outside this module's changed scope, so the delivery module is not marked ready to archive.
