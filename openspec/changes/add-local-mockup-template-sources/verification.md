# Verification

## Focused evidence

| Acceptance area | Evidence | Result |
|---|---|---|
| Local source image is managed, store-owned, and independent of a provider API | `MockupTemplateSourceImageService.AddAsync`; `MockupTemplateSourceImage` domain model; application build | Pass |
| One image can apply to one or more arbitrary option values | `MockupTemplateSourcePolicyTests.Resolve_UsesAllOptionValuesAndReportsMissingAndAmbiguousVariants`; conjunction matching in policy | Pass |
| Missing and ambiguous variant coverage is explicit and blocks readiness | `MockupTemplateSourcePolicy`; focused domain tests | Pass |
| Source mappings are positive and in bounds | `MockupImageSpaceMapping`; `SourceEntities_RejectEmptyIdentityAndInvalidMapping` | Pass |
| Source and revision graphs persist through SQLite schema 13 | `SqliteWorkspaceRepository` schema/table mappings and load/save paths; Integration project build | Pass (focused round-trip test remains follow-up) |
| Asset removal is blocked for current or historical source references | `AssetManagementService.RemoveAssetAsync` dependency guard | Pass |
| Editor exposes a keyboard-accessible Browse action and local path | `MockupTemplateEditorWindow.axaml`, injected `IAssetFilePicker`, `BrowseLocalSourceCommand`; App build | Pass |
| One named template can collect multiple color-specific source entries | `LocalSourceDrafts` collection, repeated Browse actions, collection ItemsControl, repeated source import for one created template, and reload of managed entries when editing | Pass for collection creation and reload; per-entry edit/mapping UI remains incomplete |
| Provider/API behavior remains deferred and no credentials are introduced | No provider calls in local source service; no secret fields added | Pass |
| Existing provider-candidate status behavior | Existing `product-supplier-setup` provider-state tests and source remains available for future adapters | Not applicable to local-only path |

## Commands

- `dotnet build .\src\FusionCanvas.App\FusionCanvas.App.csproj --no-restore -v minimal` — passed.
- `dotnet test .\tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~MockupTemplateSourcePolicy` — 3 passed.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore -v normal` — 189 passed.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore -v normal` — build/test process completed without errors.
- `openspec validate add-local-mockup-template-sources --strict` — passed before implementation; rerun after final edits.
- `dotnet test .\FusionCanvas.sln --no-restore -v minimal` — Domain 235 passed, Application 384 passed, Integration 189 passed; solution build/test target completed successfully.

## Review notes

The implementation intentionally excludes Printify/API retrieval, credential handling, drag-and-drop, rendering/export, and marketplace integration. The current UI uses a Browse picker; richer multi-entry draft editing and headless dialog coverage remain follow-up work before archive.
