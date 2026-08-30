## Context

The existing editor already owns per-image mapping text and dimensions; this module makes that state visible and reusable without changing persistence contracts.

## Functional Design

The selected image preview occupies the left side of the placement section and renders a scaled bitmap with a translucent mapping rectangle. The right side retains direct coordinate fields. A reuse selector lists only other images with explicit mappings. Metadata choices are built from option kind, and the source list uses a grid-like ItemsControl with a header and selected-row class.

StoreEditorWindow checks attachment/visibility before `ShowDialog`; if no valid owner exists it schedules the open after attachment or uses a non-modal fallback.

## Implementation Plan

1. Extend `LocalMockupSourceDraftViewModel`/`CatalogSetupViewModel` with preview path/state, mapped-source choices, and a command that copies only mapping values.
2. Recompose `MockupTemplateEditorWindow.axaml` placement and source table; add a bitmap preview/overlay control using existing workspace paths and safe failure state.
3. Filter `TemplateColorChoices` by `OptionKind.Color` and retain all non-color values in the secondary collection.
4. Harden `StoreEditorWindow.axaml.cs` owner lifecycle and preserve focus after opening.
5. Add focused ViewModel/domain tests and Avalonia headless tests for preview, reuse, filtering, table selection, and ownerless opening; update UI evidence and specs.

## Verification

Run focused App/ViewModel and headless tests, full `dotnet test .\\FusionCanvas.sln`, strict OpenSpec validation, and `git diff --check`.
