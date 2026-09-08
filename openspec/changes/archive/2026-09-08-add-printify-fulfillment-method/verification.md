# Verification — Add Printify Fulfillment Method

## Acceptance scenarios

| Scenario | Method | Result | Evidence |
| --- | --- | --- | --- |
| Strategy list includes standalone Printify | Domain policy test and headless Store Editor test | Pass | `FulfillmentStrategyPolicyTests.SupportedStrategies_PrintifyStrategiesRequireKey`; `StoreEditorHeadlessTests.CatalogStrategyControlShowsManualAndExplainsFutureIntegrations` |
| Standalone Printify strategy persists | Application store management test | Pass | `StoreManagementServiceTests.UpdateStoreAsync_AllowsStandalonePrintify`; `StorePrintifyConfigurationServiceTests.Strategies_SaveAndReloadWithoutKeys` |
| Standalone Printify exposes configuration | Headless Store Editor test and existing Printify view-model tests | Pass | `StorePrintifyTests.StoreEditor_BindsThreeStrategiesAndCredentialControls`; existing `StorePrintifyTests` coverage |
| Shopify-only strategy does not expose configuration | Headless Store Editor workflow | Pass | `StorePrintifyTests.StoreEditor_BindsThreeStrategiesAndCredentialControls` switches to `ShopifyManual` and asserts hidden controls |
| Guidance distinguishes standalone Printify | Headless Store Editor text assertion | Pass | `StoreEditorHeadlessTests.CatalogStrategyControlShowsManualAndExplainsFutureIntegrations` |
| Leaving standalone Printify requires confirmation | Policy-driven transition path and headless Store Editor workflow | Pass | `StoreManagementViewModel` uses `RequiresPrintifyKey` for the existing confirmation gate; full App test suite passes |

## Required validation

- `dotnet test .\FusionCanvas.sln` — Pass: 1,525 tests passed, 0 failed, 0 skipped across the solution. Existing analyzer/compiler warnings remain outside this change.
- `openspec validate --strict` — Pass.
- No persistence migration required: existing enum values remain `0`, `1`, and `2`; standalone Printify is assigned value `3`.

## Limitations

- The future Printify listing tool and external publishing operations remain intentionally out of scope.
- No live desktop test was required; the affected editor behavior is covered by the deterministic Avalonia headless lane.
