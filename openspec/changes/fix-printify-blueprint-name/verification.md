# Verification

## Acceptance Scenarios

| Scenario | Method | Result | Evidence |
| --- | --- | --- | --- |
| Blueprint includes options, variants, and design areas | Existing application import regression | Pass | `PrintifyCatalogImportServiceTests` retains option, variant, placeholder, and relationship assertions. |
| Selected shop product has a catalog identity | Integration client test | Pass | `SelectedShopProductUsesAuthoritativeCatalogBlueprintIdentity` resolves brand `Gildan` and model `64000` from `/catalog/blueprints/68.json`. |
| Catalog identity fields are incomplete | Application import test | Pass | `ShopProductImportPreservesLegacyOfferingIdentityAndMockupTemplates` uses missing brand/model and verifies title fallback `Updated Tee`. |
| Catalog Blueprint lookup fails | Client error boundary | Pass | Required catalog lookup returns the existing classified provider result before `PrintifyCatalogImportService` can save. |
| Malformed payload is received | Existing import validation tests | Pass | Existing malformed/duplicate relationship coverage remains green. |

## Validation

- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --filter FullyQualifiedName~SelectedShopProductUsesAuthoritativeCatalogBlueprintIdentity`: passed.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --filter FullyQualifiedName~PrintifyCatalogImportServiceTests`: 8 passed.
- `dotnet test .\FusionCanvas.sln --no-restore -v minimal`: passed — 432 Application, 221 Integration, and 644 App tests.
- `openspec validate --changes`: passed — 13 changes validated.
- `git diff --check`: passed; only line-ending normalization warnings were reported.
