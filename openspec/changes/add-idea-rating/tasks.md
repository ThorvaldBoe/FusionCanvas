## 1. Rating contract and application state

- [x] 1.1 Add the canonical `idea.rating` metadata key and framework-free parse/normalize/validate behavior for missing, valid, invalid, and clear values.
- [x] 1.2 Extend Item inspector state and the immediate mutation path with rating validation, protected-item guards, atomic authoritative persistence, and recoverable error handling.
- [ ] 1.3 Add application tests covering defaults, valid 1–5 updates, clear-to-unrated, invalid values, protected Items, and preservation of unrelated drafts/metadata.

## 2. Persistence and transfer compatibility

- [ ] 2.1 Verify SQLite snapshot save/load and workspace transfer preserve metadata-backed ratings and interpret missing keys as unrated.
- [ ] 2.2 Inspect Item CSV import/export contracts; if they claim arbitrary Item metadata round-trip, add a documented rating field and compatibility handling, otherwise record the explicit non-goal in tests/docs.
- [ ] 2.3 Add isolated integration tests for rated/unrated local persistence and workspace package round-trips.

## 3. Navigation rating filtering

- [x] 3.1 Extend `WorkspaceTreeQuery`, projection matching, and `WorkspaceTreeViewModel` filter state with All/Unrated/1–5 exact rating semantics.
- [x] 3.2 Preserve existing ancestor context, archive inclusion, AND composition, empty-results state, expansion restoration, clear-all behavior, and canonical selection refresh.
- [ ] 3.3 Add application tests for exact scores, unrated items, combinations with tag/text/stage/status/scope/archive filters, no matches, and a mutation removing a row from the active filter.

## 4. Item Overview star control

- [x] 4.1 Add shared Overview rating properties/commands to `ItemInspectorViewModel`, including busy-state, read-only state, current-value announcements, and authoritative refresh.
- [x] 4.2 Add the compact five-star Avalonia control with clear behavior, theme-consistent selected/unselected visuals, tooltips, and accessible names; keep it visible across active workflow stages.
- [ ] 4.3 Add App view-model tests and deterministic Avalonia headless tests for construction, binding, enabled/read-only state, click/toggle behavior, and accessibility semantics.

## 5. Navigation filter UI

- [x] 5.1 Add the exact rating selector beside existing Stage/Status filters with an accessible label and immediate application.
- [x] 5.2 Ensure active-filter indication, clear-all, empty-results recovery, and keyboard navigation remain coherent with the new dimension.
- [ ] 5.3 Add focused headless coverage for selector binding/state and filter clear behavior; avoid pixel-based assertions.

## 6. Verification and delivery gates

- [ ] 6.1 Map every scenario in `specs/idea-rating/spec.md` to passing focused test evidence and correct any approved artifact drift discovered during implementation.
- [ ] 6.2 Run `openspec validate add-idea-rating` and resolve all validation findings.
- [ ] 6.3 Run `dotnet test .\FusionCanvas.sln` and confirm the complete deterministic baseline passes.
