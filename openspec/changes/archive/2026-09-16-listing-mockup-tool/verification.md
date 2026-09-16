# Listing Mockup Tool Verification

## Criterion Results

| Acceptance scenarios | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Applicable template/design presentation; blocked and protected Listing states | `dotnet test .\FusionCanvas.sln --no-restore -m:1 --filter FullyQualifiedName~StageToolViewModelsTests`; solution build | Pass | Listing view-model regression passes; full Avalonia headless Listing-specific coverage is still limited. |
| Template selection remains unapplied; busy state prevents duplicate command | View-model command/state inspection and compiled App build | Pass | `CanApply` gates read-only, busy, blocked, and selection states. No live desktop focus observation was required. |
| Contain scaling, mapped placement, and template dimensions | `ImageSharpMockupRasterCompositorTests.ComposeAsync_PreservesTemplateDimensionsAndFitsDesignInMapping` | Pass | New Integration test verifies output dimensions and mapped design pixels. |
| Missing color, missing design/source, invalid mapping, partial success | Application service branch inspection plus existing solution tests | Pass by code path; not all branches have dedicated fixture tests | Diagnostics are accumulated per color and successful assets are saved independently; dedicated repository fixtures remain follow-up coverage. |
| Managed Item-linked output, metadata, replacement, and save-failure cleanup | Application service inspection and existing Asset/persistence regression suites | Pass by code path; no dedicated end-to-end fixture | Uses existing Asset/AssetLink persistence and deletes a newly managed file on save failure. |

## Commands

- `openspec validate listing-mockup-tool` — passed.
- `dotnet build .\FusionCanvas.sln --no-restore -m:1` — passed with the repository's existing warnings.
- `dotnet test .\FusionCanvas.sln --no-restore -m:1` — passed for the exercised solution suites; existing analyzer warnings remain.

## Limitations

- The output gallery currently lists generated managed files but does not yet render thumbnails or provide an in-app preview.
- Template ownership remains Offering-scoped, consistent with accepted catalog specs; a separate store-global template entity is deferred.
- The full issue's future drag-and-drop visual template authoring and marketplace publishing are out of scope.
