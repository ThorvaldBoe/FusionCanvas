## Why

The Mockup Template editor exposes an unlabeled provider-image dropdown without explaining where images come from, whether local import works, or what users can do when no candidates appear. This ambiguity blocks a core setup decision even though the accepted workflow supports provider-catalog images only.

## Origin

- Primary issue: [#203](https://github.com/ThorvaldBoe/FusionCanvas/issues/203)

## What Changes

- Give provider mockup image selection a persistent visible label, accessible name, and nearby explanation that candidates come from the Offering's provider catalog.
- Explicitly state that local upload and drag/drop are not available in this workflow.
- Represent loading, available, empty, unavailable, and error states distinctly in App presentation state.
- Keep guidance visible in every state and provide a supported provider setup/sync next action when candidates are unavailable.
- Preserve existing candidate selection, placement, validation, and persistence behavior.
- Add focused ViewModel and Avalonia headless tests for the instructions and each state.

This is a cohesive, independently verifiable guidance module with no open integration dependency: it describes the current provider-catalog boundary and state already observable from `IProviderCatalogCandidateSource`; it does not require live synchronization to exist. Provider image selection is occasional but consequential and belongs beside the selector and preview in the existing focused editor.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `product-supplier-setup`: Make provider mockup image provenance, unsupported local-import behavior, selector accessibility, and load-state recovery guidance explicit.

## Impact

- App-only provider catalog presentation state in `CatalogSetupViewModel`.
- Mockup Template editor labels and guidance in `StoreEditorWindow.axaml` (and the focused dialog after #201 is merged).
- Focused ViewModel/headless tests; no Domain, Application contracts, Integration adapters, persistence, migration, package, upload, drag/drop, or external sync changes.
