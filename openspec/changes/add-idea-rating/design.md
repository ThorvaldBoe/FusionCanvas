## Context

Issue #184 adds a lightweight prioritization signal to the existing universal `Item` model. Items already carry optional workflow content in metadata, are loaded into authoritative `WorkspaceSnapshot` instances, and are projected through `WorkspaceTreeQuery`. The main window already has a shared Item Overview and a navigation filter surface. The feature must remain local-first, preserve old workspaces, and avoid weakening the existing guarded text-edit and filtered-selection behavior.

## Goals / Non-Goals

**Goals:**

- Persist and display an integer 0–5 idea-potential rating.
- Make rating edits immediate, independently durable, accessible, and safe for read-only Items.
- Filter the navigation tree by exact rating, including unrated Items.
- Preserve ratings through persistence and workspace transfer, with focused automated coverage.

**Non-Goals:**

- Rating concepts, designs, listings, tags, or marketplace performance independently of the Item.
- Sorting, weighted scoring, averages, AI-generated ratings, half-stars, or custom scales.
- A new database table, external service, or visual rating system outside the Item surface and navigation filter.

## Decisions

1. **Use Item metadata with a canonical `idea.rating` key.** A missing key represents 0; values are serialized as invariant decimal integers 1–5, and clearing removes the key. This is backward-compatible with existing snapshots and avoids a schema migration while filtering is performed over the in-memory snapshot. A first-class SQLite column was considered but would add migration and constructor churn without current query/index requirements.
2. **Treat rating as shared Item evaluation, not stage content.** Place it in the shared Item Overview so it remains visible and editable while the Item is active at any workflow stage. This avoids making a creator regress to Idea merely to reassess potential and keeps the value attached to the original Item.
3. **Use an exact single-choice rating filter.** Add All ratings, Unrated, and 1–5 options alongside existing stage/status selectors. A range or multi-select rating filter was considered but is not requested and would expand the filter UX and active-filter semantics.
4. **Use the existing immediate mutation boundary.** Add a focused application operation or equivalent inspector path that reloads the authoritative snapshot, validates the rating, writes one atomic repository snapshot, and raises the same refresh events used by tags and other Item mutations. Text drafts remain untouched.
5. **Represent stars with accessible text semantics.** Each star action exposes a name such as “Rate 4 stars” and the current state communicates “Unrated” or “Rated 4 of 5 stars.” The control uses existing theme resources and compact workspace spacing; no pixel-perfect visual contract is introduced.

## Risks / Trade-offs

- **[Risk] Free-form metadata contains malformed legacy values.** → Parse invalid, non-integer, or out-of-range values as 0 for presentation/filtering without silently rewriting unrelated metadata; valid user edits canonicalize the key.
- **[Risk] Immediate rating saves race with another Item mutation.** → Use the repository's authoritative-load/save pattern and existing mutation synchronization; return a recoverable error on stale or failed persistence and refresh from confirmed state.
- **[Risk] Filtered selection disappears after a rating edit.** → Reproject from authoritative state, preserve canonical identity, and expose the existing filtered-out indicator/clear-filter path.
- **[Risk] CSV surfaces omit the new field.** → Treat rating as metadata for workspace transfer; inspect CSV's declared contract and either add an explicit rating column with round-trip tests or document CSV as not claiming arbitrary metadata round-trip before implementation.

## Migration Plan

No schema migration is planned. Existing databases and transfer packages interpret a missing `idea.rating` metadata key as 0. If implementation discovers that an existing export format cannot safely carry the key, add a versioned explicit column/field before coding and update this design; do not silently lose ratings.

Rollback is application-code rollback: older versions ignore the optional metadata key and retain all other Item fields. A downgrade may hide ratings in the UI but must not corrupt the workspace.

## Open Questions

None for the proposed module. The rating is shared Item Overview state, exact-filtered, 0/unrated, and metadata-backed unless implementation evidence requires a documented migration decision.

## Implementation Plan

1. **Domain/application contract:** add rating constants, parse/normalize/validate helpers, and extend the Item inspector state/save or a focused rating mutation request without putting persistence logic in App. Keep invalid legacy values safe and canonical clear behavior explicit.
2. **Persistence/transfer:** ensure metadata-backed ratings are retained by SQLite snapshot save/load and workspace package filtering/import. Inspect Item CSV schemas; implement an explicit column only if that format promises metadata round-trip.
3. **Navigation query:** extend `WorkspaceTreeQuery` and `WorkspaceTreeProjector` with an optional exact rating value, parsing each Item's canonical/legacy metadata safely. Keep ancestor projection, archive rules, and AND semantics unchanged.
4. **App view model:** expose rating state and an immediate rating command on `ItemInspectorViewModel`; preserve busy/error handling, protected-item guards, authoritative refresh, and canonical selection. Add the rating selector state to `WorkspaceTreeViewModel` and its clear/filter property notifications.
5. **App views:** place the compact five-star control in the shared Overview and an exact rating selector beside Stage/Status filters. Add accessible names, tooltips, disabled/read-only guidance, and empty/filter-active state bindings following existing theme/layout conventions.
6. **Verification:** add framework-free application tests for normalization and mutation, application workspace-tree tests for exact/AND/empty behavior, integration round-trip tests, App view-model tests, and Avalonia headless tests for star bindings, enabled state, accessible semantics, and rating filter control state. Run strict OpenSpec validation and the full solution test baseline.

## Acceptance-to-Verification Map

| Acceptance area | Planned evidence |
| --- | --- |
| 0–5 semantics, defaults, invalid values | Application/domain unit tests |
| Star edit, clear, protected state, persistence across stages | Inspector view-model tests plus headless control tests |
| Exact rating and unrated filtering, AND/context/empty behavior | WorkspaceTreeProjector and WorkspaceTreeViewModel tests |
| Refresh after mutation and filtered selection | App/application synchronization tests |
| SQLite and workspace transfer round-trip | Integration tests with isolated temporary resources |
| Accessibility and binding/control state | Deterministic Avalonia headless view tests |
| Package/spec integrity | `openspec validate add-idea-rating` |
| Regression baseline | `dotnet test .\FusionCanvas.sln` |
