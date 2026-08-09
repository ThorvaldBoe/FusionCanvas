# Verification: support-printify-store-catalog-mockup-setup

Implementation is complete for the scoped Store/catalog/mockup-setup module. This record separates verified implementation evidence from the repository-wide baseline gate, which remains blocked by unrelated App tests.

## Verified so far

| Area | Result | Evidence |
| --- | --- | --- |
| Domain strategy and model construction | Pass | `dotnet test .\tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj --no-restore`; 227 passed, including Placeholder dimensions/coverage, stable template targets, and revision-change invariants. |
| Provider-Network identity | Pass | Domain test verifies `Printify-Choice` normalizes to stable `printify-choice` and has no Print Provider. |
| Color-only mockup binding | Pass | Domain tests reject Size values and exercise one Color binding shared by multiple concrete sizes. |
| Active template-color uniqueness | Pass | Domain test rejects duplicate active `(template, color)` records. |
| Schema 11 store strategy persistence | Pass | Existing schema migration tests pass and Store round-trip includes `FulfillmentStrategy`; Store Editor now exposes Manual with future strategies explained as unavailable. |
| Normalized catalog/mockup SQLite round-trip | Pass | `ProductCatalogPersistenceTests` verifies Blueprints, Print Providers, Offerings, typed Options/Values, Variants, Placeholders, Templates, Colors, Revisions, and revision colors. |
| SQLite catalog invariants | Pass | Repository validation rejects cross-offering ownership and enforces template target/color relationships before save; schema-scope test confirms no rendering, override, credential, or Shopify tables. |
| Schema 10-to-11 migration bridge | Pass for implemented bridge | Populated schema-10 fixture preserves legacy Blueprint/Offering/Variant/Placeholder IDs, maps Choice to `printify-choice`, normalizes Color/Size values, preserves restricted compatibility, and malformed JSON rolls back without advancing the version. |
| Focused integration persistence suite | Pass | `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore --filter FullyQualifiedName~ProductCatalogPersistence`; 11 passed. |
| Changed-scope builds | Pass | `dotnet build .\FusionCanvas.sln --no-restore -v normal`; solution build passed with 0 warnings and 0 errors after restarting the build services. |
| Store strategy application policy | Pass | `StoreManagementServiceTests` strategy coverage passes; unavailable Shopify strategy changes are rejected and Manual remains persisted. |
| Catalog application contracts and lifecycle foundations | Pass | `CatalogSetupServiceTests` covers Store-scoped Blueprint/provider/offering creation, typed Option/Value/Variant creation, duplicate Variant rejection, Store isolation, dependency reporting, default Placeholder assignment, and restore; 4 passed. |
| Mockup template application foundation | Pass | `MockupTemplateSetupServiceTests` covers same-offering Placeholder targeting, color binding, revision creation, template archive/restore, and archived-Store read-only behavior; focused suite passes. |
| Application baseline | Pass | `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore`; 367 passed. |
| Integration baseline | Pass | `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore`; 181 passed. |
| Design-stage normalized target presentation | Pass | Design-stage application tests pass; normalized Placeholder metadata now supplies position, decoration method, dimensions, Blueprint context, and a Printify Choice Provider-Network warning to the tool view model. |
| Normalized catalog setup presentation | Pass | `CatalogSetupViewModelTests` passes 1/1, covering typed option selection, offering/Placeholder/template state, availability guards, and color-binding command state. |
| Store Editor focused headless coverage | Pass | `StoreEditorHeadlessTests` passes 7/7, including Catalog & mockups navigation, strategy helper text, progressive sections, future template state, and layout margins after filtering non-laid-out hidden controls. |
| Full solution no-build run | Partial / blocked by unrelated baseline | Domain 227/227, Application 367/367, and Integration 181/181 passed. App passed 481/487; six existing Workspace Tree/multi-selection/layout tests failed outside this change's Store/catalog scope. |

## Pending acceptance coverage

The design artifact contains an exact traceability index for all 87 acceptance-scenario titles. The scoped application CRUD/lifecycle orchestration, normalized Store Editor catalog navigation, and focused UI state coverage are implemented. Future-contract scenarios for listing application, rendering, asset upload, placement editing, and Shopify publication remain not applicable until those future modules exist. The only open delivery gate is the repository-wide deterministic baseline because six unrelated App tests fail.

## Limitations

- The new normalized catalog is additive to the existing compatibility model; legacy records remain available after migration while the new normalized graph is populated.
- The current module intentionally does not implement listing-stage selection, rendering, asset upload, placement editing, per-variant overrides, generated-mockup records, external credentials, network communication, or Shopify publication.
- No production rendering, source-image upload, placement editor, per-variant override, generated-mockup record, external credential, network, or Shopify behavior has been added.
- The solution build passed with zero warnings and errors after restarting MSBuild/build services; no host telemetry limitation remains for the build gate.
- A no-build solution test run reached all projects: Domain, Application, and Integration passed; the existing App test assembly reported 6 failures in unrelated workspace-tree/multi-selection/headless-layout baseline tests, so the deterministic repository-wide test gate remains open.
