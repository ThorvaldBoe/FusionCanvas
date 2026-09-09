# Verification

## Current evidence

| Acceptance area | Result | Evidence | Limitations |
| --- | --- | --- | --- |
| Store-scoped catalog retrieval guards | Pass | `PrintifyCatalogImportServiceTests`: Manual Store refuses credential/client access; valid Shopify + Printify Store reaches client with the native secret kept inside the service boundary. | Persistence import is only entered after a successful confirmed selection. |
| Catalog HTTP contract | Pass | `PrintifyCatalogClientTests`: blueprint list, selected provider/variant/placeholder retrieval, request method/origin/auth header, status mapping, malformed JSON, oversized response, cancellation, and response-body redaction. | No live provider account was used. |
| Composition | Pass | Integration and App projects build; `AppServicesFactory`, `AppServices`, `MainWindow`, and `StoreManagementViewModel` wire the catalog client/import service and workspace repository. | Composition is covered by baseline and integration tests. |
| SQLite schema compatibility | Pass | `SqliteDatabaseSchema` migrations 16–17 add Store-scoped Printify indexes and option/value metadata defaults; `ProductCatalogPersistenceTests` and the real import integration test pass. | No destructive migration is used. |
| Store Editor preview surface | Pass | App build succeeded; the Catalog overview exposes a Printify-gated selection panel with loading, empty, error, selection, load-selected, cancel, focus, and Escape states. | Dedicated headless assertions now cover the new panel. |
| Focused regression tests | Pass | Application import tests: 5 passed; real SQLite import integration: 1 passed; App import-session tests: 4 passed; headless import surface: 1 passed; full solution baseline: 1,542 passed. | Existing analyzer warnings are unrelated legacy test debt. |

## Final acceptance evidence

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Retrieval / saved Shopify + Printify Store | Application test | Pass | `PrintifyCatalogImportServiceTests.LoadsCatalogOnlyAfterAllStoreGuardsPass`. |
| Retrieval / unsupported Store | Application test | Pass | `RefusesManualStoreWithoutReadingCredential` verifies no credential read or provider request. |
| Selection / confirmation and cancellation | View-model + headless view tests | Pass | Confirmation refreshes and closes; `CancellingSelectionDoesNotSubmitAnImport` and the headless Escape test verify no mutation/request on cancellation. |
| Idempotency / changed data / local-only preservation | Application + SQLite integration tests | Pass | `RepeatedImportUpdatesProviderRecordsAndPreservesLocalOnlyBlueprint` verifies repeated import, changed titles/dimensions, reload, and preservation. |
| Identity / Store isolation | Application test | Pass | `ImportsProviderIdentityWithinTargetStoreOnly` verifies external keys are Store-scoped. |
| Identity / options, variants, placeholders | Application + SQLite round-trip tests | Pass | Import assertions verify option-kind mapping, provider metadata, and exact placeholder variant compatibility; normalized persistence round-trip passes. |
| Validation / malformed and duplicate payload | Application + client tests | Pass | `RejectsDuplicateProviderPayloadWithoutSaving`; HTTP fixtures cover malformed response and invalid status/size cases. |
| Persistence / atomic commit and rollback | Integration persistence tests | Pass | Existing cross-offering rejection verifies failed writes leave the prior catalog unchanged; real SQLite import test verifies committed reload. |
| Migration / uniqueness | Integration persistence tests | Pass | `ProductCatalogPersistenceTests` covers migration, schema version 17, normalized reload, and Store-scoped uniqueness indexes. |
| Failure / provider errors and late results | Client + VM tests | Pass | Safe result mapping, `FailedConfirmationLeavesSelectionOpenAndRecoverable`, and `LateBlueprintResultForChangedStoreIsIgnored`. |
| Store Editor / bindings, focus, keyboard, visual tree | Avalonia headless tests | Pass | `PrintifyImportPanelSupportsKeyboardCancelWithoutProviderImageControls` verifies automation ID, panel/list construction, focus, Escape routing, and no provider image controls. |
| Composition and security boundary | Inspection + baseline | Pass | Printify ports stay in Application, HTTP/SQLite stay in Integration, and credentials/raw provider responses do not enter UI or catalog persistence. |

## Scoped completion QA

- `dotnet test .\\FusionCanvas.sln` passed: 1,542 passed, 0 failed, 0 skipped.
- `openspec validate import-printify-catalog --strict` passed.
- `git diff --check` passed.
- Architecture review passed: Domain remains free of Avalonia, SQLite, and HTTP dependencies; Application owns orchestration and ports; Integration owns external I/O and persistence; App owns commands, focus, and routed input.
- Security review passed: no secrets or raw response/header exposure; SQLite writes remain parameterized; no new network dependency was introduced into tests.
- Persistence review passed: migration 17 supplies backward-compatible metadata defaults; confirmed imports use the existing SQLite transaction; validation occurs before persistence.
- UI review passed: import remains Store Editor-only, strategy-gated, confirmation-gated, keyboard reachable, and covered by deterministic headless tests.
- Scope-drift review passed: no mockup image/template creation, publishing, generalized marketplace framework, or unrelated feature work was added.

## Limitations

- No live Printify account or interactive desktop check was used; mandatory evidence is deterministic and offline.
- Existing repository analyzer warnings remain outside this change's scope and do not fail the baseline.
