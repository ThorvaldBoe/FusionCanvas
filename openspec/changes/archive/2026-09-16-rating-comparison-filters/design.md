## Context

The navigation pane exposes rating filtering through a `ComboBox` bound to `WorkspaceTreeViewModel.IdeaRatingFilterIndex`. The current index is translated to an integer and `WorkspaceTreeProjector` performs exact matching, with zero representing unrated items. Ratings remain stored in item metadata as integers from 0 through 5.

## Goals / Non-Goals

**Goals:**

- Represent exact, unrated, greater-than, and less-than choices in one stable dropdown.
- Keep comparison semantics strict and deterministic.
- Preserve immediate refresh, active-filter behavior, and existing persisted data.

**Non-Goals:**

- Changing rating storage or the allowed rating range.
- Adding inclusive comparisons, ranges, sorting, or new filter surfaces.
- Changing the existing exact-rating or unrated behavior.

## Decisions

- Use a small query value object for the rating comparison rather than overloading an integer with undocumented index ranges. This keeps comparison semantics explicit in the Application layer; adding separate query properties was considered but would allow invalid combinations.
- Keep the dropdown as the existing compact navigation control and append the eight choices after the current seven entries. A separate flyout was considered but would add workspace complexity for an infrequent, lightweight choice.
- Use strict numeric comparisons against parsed ratings. Unrated is represented by zero only for the existing explicit Unrated choice; comparison choices require a positive stored rating.

## Risks / Trade-offs

- [Index coupling] The view-model index must stay aligned with XAML item order → centralize the mapping in the view-model and cover all entries with tests.
- [Filter regressions] New query semantics could alter exact/unrated behavior → retain existing assertions and add a full threshold matrix.

## Migration Plan

No migration is required. Existing metadata values and saved workspaces remain compatible. The change can be rolled back by reverting the UI, query, projector, tests, and change package.

## Open Questions

None. The requested labels, strict comparisons, and retained existing choices are resolved.

## Implementation Plan

1. Add an explicit rating filter model in `src/FusionCanvas.Application/WorkspaceTree/WorkspaceTreeQuery.cs` that can express exact, unrated, greater-than, and less-than modes while preserving the query's active-state behavior.
2. Update `WorkspaceTreeProjector` to evaluate the model for Item nodes only; keep ancestor context behavior unchanged and exclude unrated Items from comparisons.
3. Update `WorkspaceTreeViewModel` index bounds and query mapping for the eight appended choices, preserving clear/reset behavior.
4. Add the eight `ComboBoxItem` labels to `src/FusionCanvas.App/Views/MainWindow.axaml`.
5. Extend `tests/FusionCanvas.Application.Tests/WorkspaceTree/WorkspaceTreeTests.cs` with exact, unrated, greater-than, less-than, and boundary coverage; extend `tests/FusionCanvas.App.Tests/WorkspaceTreeViewModelTests.cs` for index mapping and immediate refresh if existing test seams support it.
6. Run focused tests, strict OpenSpec validation, and `dotnet test .\FusionCanvas.sln`; record each acceptance scenario in `verification.md`.

### Acceptance-to-verification mapping

| Scenario | Verification |
| --- | --- |
| Greater-than rating filters match strict thresholds | Application projector test covering all four thresholds and boundaries |
| Less-than rating filters match strict thresholds | Application projector test covering all four thresholds and boundaries |
| Comparison choices apply immediately | View-model test and deterministic solution test baseline |

