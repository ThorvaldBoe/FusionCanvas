## 1. Asset Management Application Layer

- [ ] 1.1 Define asset context references (listing, niche, group, store), asset summaries with purpose/file/missing state, import/relabel/remove requests, results, and recoverable error contracts in `FusionCanvas.Application.Workspace`.
- [ ] 1.2 Implement context asset state loading: resolve the target context, project linked assets (or all store-owned assets with derived context labels for a store target), and detect missing managed files through `IWorkspaceFileStore.Exists` without modifying persisted state.
- [ ] 1.3 Implement import validation: target exists in the active workspace, belongs to the asset's store, and is active including ancestor chain via `GroupHierarchy`; reject archived or unavailable targets with actionable errors before any file copy.
- [ ] 1.4 Implement the deterministic extension-to-`AssetKind` pre-selection policy (source design, exported image, SVG, font, brush mappings, unknown fallback) as a pure application policy.
- [ ] 1.5 Implement import: copy the file through `IWorkspaceFileStore.ImportAsync`, create the store-owned `Asset` (original file name, workspace-relative path, original source path, timestamps) plus one `AssetLink`, reload-then-save once atomically, and best-effort delete the copied file when the save fails.
- [ ] 1.6 Implement purpose relabeling as its own atomic mutation that preserves file reference, context link, and metadata.
- [ ] 1.7 Implement confirmed removal: delete asset record and all its links in one atomic save, best-effort delete the managed file only after the save succeeds, and keep record and file intact on save failure.
- [ ] 1.8 Add application tests for context loading, store-level projection with unlinked assets, missing-file detection, import success and each failure path, extension mapping, relabel preservation, removal semantics, and repository-failure atomicity with a deterministic fake file store.

## 2. Persistence Verification

- [ ] 2.1 Confirm the existing SQLite schema and snapshot persistence cover asset and asset-link mutations without a database migration.
- [ ] 2.2 Add integration tests over a temporary SQLite database and temporary workspace root covering import copy-plus-save, reload fidelity, relabel, removal with managed-file deletion, and save-failure cleanup of the copied file.
- [ ] 2.3 Verify cancelled or failed operations persist no partial asset records, links, or managed files.

## 3. Assets Focused Surface

- [ ] 3.1 Add an `AssetsViewModel` and non-modal Assets window following the existing focused-editor pattern (store/group/listing editors), parameterized by target context (listing, niche, group, or store) with a context summary header.
- [ ] 3.2 Implement the asset list with name, purpose, managed file name, and distinct missing state; empty state explains import and keeps the action available; store context adds the linked-context label column with an unlinked indicator.
- [ ] 3.3 Implement the import flow: system file picker, purpose selector pre-selected from the extension policy, explicit confirm, busy state with duplicate-submission prevention, keyboard accessibility, and new-asset selection on success.
- [ ] 3.4 Implement per-asset purpose relabeling with immediate atomic persistence and inline recoverable errors.
- [ ] 3.5 Implement confirmed removal with a keyboard-accessible inline confirmation, cancellation that preserves state, and post-removal selection of a remaining asset or the empty state.
- [ ] 3.6 Apply compact action sizing, tooltips for icon-only commands, predictable tab order, busy/disabled coherence, and error presentation that preserves selection and window context.
- [ ] 3.7 Add view-model and app tests for state projection, import flow decisions, relabel, removal confirmation/cancellation, selection behavior, and error/busy states.

## 4. Entry Points and Wiring

- [ ] 4.1 Register `IAssetManagementService` through `AppWorkspaceFactory` and main-window coordination without exposing repository or file-store details to the UI.
- [ ] 4.2 Add an "Assets…" tree context-menu command for listing, niche, group, and store nodes that opens (or activates) the Assets window for that context while preserving canonical tree and document context.
- [ ] 4.3 Ensure the Assets window coexists with the reusable working tab model: opening it never creates, replaces, or closes document tabs.

## 5. Verification

- [ ] 5.1 Run `dotnet test .\FusionCanvas.sln` and resolve all regressions.
- [ ] 5.2 Perform the real desktop UI verification pass against a disposable database/workspace per the testing baseline: keyboard-only import with purpose override, import to listing/niche/group/store, unsupported type and missing source errors, empty state, missing-file display, relabel persistence across restart, removal confirmation/cancellation, listing move preserving assets, group deletion cascade leaving store-reachable unlinked assets, and focus/keyboard behavior; record build, environment, scenarios, results, isolation, limitations, and evidence.
- [ ] 5.3 Run strict OpenSpec validation and confirm every asset-management scenario is covered by implementation or automated tests.
