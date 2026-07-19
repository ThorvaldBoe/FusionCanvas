## 1. Domain Lifecycle Model

- [ ] 1.1 Redefine `ListingStatus` as `Draft`, `Published`, `Paused`, `Rejected` with a display-name helper, and add the persisted `WorkflowStage` fact to the `Listing` record; update all construction sites across src and tests.
- [ ] 1.2 Add domain tests for the fixed status vocabulary, stage/status independence, and display names.

## 2. Persistence Migration (Schema v4)

- [ ] 2.1 Add the `workflow_stage` column, translate persisted status integers to the new vocabulary, set the archive flag for old archive-valued rows, and backfill stage per the design mapping table; advance the schema version to 4 with a `MigrateToVersion4Async` step.
- [ ] 2.2 Round-trip the workflow stage through listing insert and load.
- [ ] 2.3 Add integration tests with v3-format fixture databases covering every mapping row (Active/Draft/Ready/Published/Archived), stage backfill, archive-flag mapping, reload behavior, and the newer-version refusal guard.

## 3. Application Lifecycle Operations

- [ ] 3.1 Extend `IListingManagementService` with set-status and move-stage operations following the reload-validate-save pattern (validate target values and listing existence; no transition rules; status change allowed on rejected listings).
- [ ] 3.2 Add an active-view marker to `WorkflowStageNavigatorEntry` and the navigator service build/navigate logic.
- [ ] 3.3 Build item workflow contexts from the persisted stage with availability limited to reached stages, and derive the inactive overlay (`IsInactive`/`InactiveLabel`) from rejected status or archive state.
- [ ] 3.4 Add application tests for status/stage mutations, persistence-failure atomicity, active-view marker behavior, inactive derivation, duplicate resetting to idea+draft, and preservation across move, archive, and restore.

## 4. Document Workspace UI

- [ ] 4.1 Remove `WorkflowStageForListing` and the always-available stage wiring from `MainWindowViewModel`; open listing documents at their persisted current stage.
- [ ] 4.2 Add the document card footer stage-move controls: advance right, regress left, adjacent target labels, disabled at boundaries and while inactive, view switches to the new stage on success, inline error and unchanged state on failure.
- [ ] 4.3 Add the compact lifecycle-status selector to the document card title row with immediate apply, revert-plus-inline-error on failure, and no unrelated-field requirements.
- [ ] 4.4 Render the navigator's current-versus-view emphasis (prominent current stage, secondary active-view marker) including the inactive overlay.
- [ ] 4.5 Add view-model and app tests for stage-move commands, boundary/inactive disabling, view-follows-move, status selector apply/revert, and navigator marker state.

## 5. Navigation Tree: Inactive Treatment and Filters

- [ ] 5.1 Present rejected listings in the tree with a subdued inactive treatment while keeping them visible and openable.
- [ ] 5.2 Add stage and status filter selectors to the navigation-pane filter area, combining with the text query (AND), preserving parent topics, expansion, and selection, and restoring normal browsing when cleared.
- [ ] 5.3 Add view-model tests for rejected styling, stage/status filtering, filter combination with text query, empty results, and clear/restore behavior.

## 6. Verification

- [ ] 6.1 Run `dotnet test .\FusionCanvas.sln` and resolve lifecycle and regression failures across all suites.
- [ ] 6.2 Run the desktop UI verification pass on a disposable workspace/database: stage advance/regress with boundary and inactive disabling, status quick-change and failure recovery, opening at the current stage, navigator current-versus-view emphasis, rejected reference and reactivation, stage/status filtering and clearing, persistence across restart, and migration of a v3-format database; record evidence.
- [ ] 6.3 Run strict OpenSpec validation and confirm every delta scenario is covered by implementation or automated tests.
