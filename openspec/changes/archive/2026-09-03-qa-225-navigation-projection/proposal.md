# Extract navigation projection from the main-window view model

## Why

`MainWindowViewModel` owns window coordination, lifecycle commands, workspace selection, and navigation-tree projection. The navigation context projection is a cohesive, pure responsibility that can be independently named and tested.

## Scope

- Move navigation-context construction and recursive group projection into an App-owned factory.
- Preserve ordering, paths, workflow metadata, and public view-model behavior.

## Non-goals

- No user-facing behavior or navigation model changes.
- No broad rewrite of the remaining view-model responsibilities.

## Modified Capabilities

- `architecture-guidelines`: clarify that cohesive projection responsibilities may be extracted from oversized presentation types without behavior changes.

## Verification

- Existing main-window and navigation tests pass.
- Solution build and strict OpenSpec validation pass.
