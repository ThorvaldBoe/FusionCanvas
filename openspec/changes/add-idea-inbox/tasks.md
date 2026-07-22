## 1. Application Layer

- [x] 1.1 Add the `idea.audience` key to `ListingMetadataCodec` and document it as a preserved idea-stage metadata key.
- [x] 1.2 Add audience to `ListingInspectorState` and `ListingInspectorSaveRequest`, persist it through the inspector save with normalization, omission-when-empty, and unknown-key and provenance preservation.
- [x] 1.3 Add application tests for audience round-trip, omission, unknown-key preservation, and preservation across unrelated edits.

## 2. Details Pane

- [x] 2.1 Expose audience on `ListingInspectorViewModel` with baseline tracking and include it in automatic-save commits.
- [x] 2.2 Add the Audience field to the inspector's Idea section in the view.
- [x] 2.3 Add view-model tests for audience editing, commit, and read-only inactive state.

## 3. Persistence Verification

- [x] 3.1 Add SQLite integration coverage for audience persistence and complete reload.
- [x] 3.2 Confirm no schema migration is required (metadata keys only).

## 4. Verification

- [x] 4.1 Run `dotnet test .\FusionCanvas.sln` and resolve regressions.
- [x] 4.2 Run strict OpenSpec validation for this change.
- [x] 4.3 Record verification evidence, including the not-applicable desktop UI pass under OpenCode.
