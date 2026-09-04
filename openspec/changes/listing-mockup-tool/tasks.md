## 1. Contracts and application model

- [x] 1.1 Add generation request, output summary, per-job diagnostic, and Listing generation state contracts using existing Item, Offering, Design, Mockup Template, Asset, and revision identities.
- [x] 1.2 Add application ports for mockup generation and raster composition, including cancellation and managed-file boundaries.

## 2. Generation implementation

- [x] 2.1 Implement eligibility and input resolution for selected Offering Colors, Design PNGs, ready template revisions, applicable source images, and valid mappings.
- [x] 2.2 Implement contain scaling and local raster composition in Integration, with deterministic error handling for unreadable or missing files.
- [x] 2.3 Persist successful outputs as managed Item-linked `MockupImage` Assets with template/revision/color/design metadata and clean up files when persistence fails.
- [x] 2.4 Wire the generation service and compositor into application startup without changing existing template-management persistence behavior.

## 3. Listing-stage UI

- [x] 3.1 Replace the Listing placeholder view model with load, selection, apply, busy, blocked, error, and generated-output state.
- [x] 3.2 Add the Listing mockup tool AXAML with accessible template selection, Apply mockup template action, diagnostics, and output gallery while preserving protected-item policy.
- [x] 3.3 Refresh Listing state after application, preserve prior outputs on partial failure, and keep template changes unapplied until explicit confirmation.

## 4. Verification and completion gates

- [x] 4.1 Add focused domain/application tests for matching, contain scaling, revision metadata, missing colors, partial success, and persistence failure cleanup.
- [x] 4.2 Add Integration raster/file tests and Avalonia headless Listing-stage tests for bindings, control state, read-only behavior, and keyboard-reachable actions.
- [x] 4.3 Run criterion-level verification for every scenario and record methods, results, evidence, and limitations in `verification.md`.
- [x] 4.4 Run `openspec validate`, `dotnet test .\FusionCanvas.sln`, review changed scope, then commit and push `codex/137-mockup-tool` without merging.
