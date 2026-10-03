# Verification

## Acceptance criteria

| Criterion | Result | Evidence |
| --- | --- | --- |
| One upload action accepts multiple raster images in one picker interaction. | PASS | `CatalogSetupViewModelTests.BrowseLocalSourcesStagesEachSelectedFileAndSelectsFirstDraft`; `StoreEditorHeadlessTests.MockupSourceUploadButtonStagesMultipleRowsThroughRenderedAction`. |
| A single selected image remains supported. | PASS | Existing `CatalogSetupViewModelTests.BrowseLocalSourceGetsDimensionsFromMetadataService`. |
| Each staged image has independent draft metadata and the first new draft is selected. | PASS | `BrowseLocalSourcesStagesEachSelectedFileAndSelectsFirstDraft`; the rendered editor journey verifies the selected-row editor boundary. |
| A metadata-read failure remains attached to only the affected draft while other files stage normally. | PASS | `CatalogSetupViewModelTests.BrowseLocalSourcesRetainsFailedMetadataDraftAlongsideValidDrafts`. |
| Mixed save outcomes retain the existing partial-completion diagnostics behavior. | PASS | Existing `CatalogSetupViewModelTests.SavingLocalSourcesSummarizesPartialCompletionWhenLaterSourceFails`. The batch change reuses the existing per-source save loop. |
| Archived stores remain read-only and cannot open or add source-image drafts. | PASS | Existing `CatalogSetupViewModelTests.MockupTemplateDraft_ArchivedStoreCannotOpenAddOrEdit`. |

## Focused verification

The focused application test run passed:

```text
dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter "FullyQualifiedName~CatalogSetupViewModelTests|FullyQualifiedName~StoreEditorHeadlessTests.MockupSource" -v minimal
Passed: 49, Failed: 0
```

The focused rendered-upload journey also passed after exercising the button through the headless window input path:

```text
dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~StoreEditorHeadlessTests.MockupSourceUploadButtonStagesMultipleRowsThroughRenderedAction" -v quiet
Passed: 1, Failed: 0
```

## OpenSpec validation

```text
openspec validate "bulk-mockup-source-image-upload" --strict --no-interactive
Change 'bulk-mockup-source-image-upload' is valid
```

## Solution baseline

The required solution baseline passed after resolving two test-infrastructure/runtime blockers exposed by the full run:

```text
dotnet test .\FusionCanvas.sln
FusionCanvas.UiDescription.Tests: 29 passed
FusionCanvas.Domain.Tests: 265 passed
FusionCanvas.Application.Tests: 555 passed
FusionCanvas.Integration.Tests: 301 passed
FusionCanvas.App.Tests: 866 passed
```

The production-source layout scanner was made string/interpolation-aware so braces in interpolated Snowclone strings are not misread as top-level type declarations. The App test assembly now explicitly serializes Avalonia headless test collections because they share a dispatcher. The AI settings availability refresh also gained a re-entrancy guard so artwork hydration cannot recursively restart catalog loading during an availability notification.

Native OS file-picker rendering was not exercised in the headless lane; the picker boundary is covered by the deterministic plural-picker implementation and staging tests.

## Post-main integration note

After integrating the latest `origin/main`, the upload-focused suite still passes (`54` focused tests, including the rendered upload journey). The expanded App baseline currently has three unrelated failures introduced by the latest main-line refactors: two tests reflect a removed private command-task method, and one design headless fixture no longer creates an assigned slot. Those failures are outside this change's files and prevent claiming a clean post-merge baseline or merging PR #730 until main-line test maintenance is addressed.
