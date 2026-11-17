# Verification: Reactivate Archived Blueprints

| Acceptance scenario | Verification | Result | Evidence |
|---|---|---|---|
| Active Blueprint shows archive only | `StoreEditorHeadlessTests.ArchivedBlueprintCanBeReactivatedAfterConfirmation` | PASS | Active detail has no Reactivate action. |
| Archived Blueprint in editable Store shows Reactivate and Delete | `StoreEditorHeadlessTests.ArchivedBlueprintCanBeReactivatedAfterConfirmation`; existing permanent-delete headless test | PASS | Archived detail exposes Reactivate and Delete; Archive is hidden. |
| Confirm restores Blueprint and full catalog graph atomically | `CatalogSetupServiceTests.RestoresArchivedBlueprintAndItsOwnedCatalogGraphWithoutChangingReferences` | PASS | Blueprint, Offering, Option, Value, Variant, Placeholder, Template, Template Color, and source Image are active after one save. |
| Restore preserves identities, relationships, and Item references | `CatalogSetupServiceTests.RestoresArchivedBlueprintAndItsOwnedCatalogGraphWithoutChangingReferences` | PASS | Stable IDs, Variant membership, Placeholder compatibility, Template target, source-image ownership, and Item listing configuration are retained. |
| Cancel leaves graph unchanged | `StoreEditorHeadlessTests.ArchivedBlueprintCanBeReactivatedAfterConfirmation` | PASS | Cancel closes confirmation and Blueprint remains archived. |
| Archived Store cannot reactivate or delete | `CatalogSetupServiceTests.BlueprintRestoreRequiresConfirmationArchivedBlueprintAndWritableStore`; existing Store Editor archived-store read-only coverage | PASS | Service rejects archived Store and makes no save; Store Editor remains read-only. |

## Commands

- `dotnet build .\FusionCanvas.sln -m:1` — PASS; 0 errors. Package vulnerability-data lookup warnings occurred because NuGet was unreachable.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj -m:1 --no-restore` — PASS, 715 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj -m:1 --no-restore` — PASS, 1,041 tests.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore` — PASS, 2,539 tests across Domain, Application, Integration, App, and UI Description.
- `openspec validate reactivate-archived-blueprints --strict` — PASS
- `git diff --check` — PASS

## Supplemental desktop evidence

No real-desktop Appium journey is planned. The added state transition and confirmation are covered by deterministic application and Avalonia headless tests.
