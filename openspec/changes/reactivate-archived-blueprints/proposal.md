# Reactivate Archived Blueprints

## Why

Blueprint archival is specified and implemented as reversible, but an archived Blueprint can currently only be reviewed or permanently deleted. This makes the reversible path one-way from the Store Editor and prevents creators from returning a Blueprint to active catalog use.

## Outcome

Creators can explicitly reactivate an archived Blueprint from its detail view. Reactivation restores the Blueprint and its owned catalog descendants together, preserving their stable identities and relationships.

## Scope

- Add an archived-only **Reactivate Blueprint** action and confirmation.
- Restore the Blueprint's Blueprint Offerings and their catalog-owned descendants in one atomic operation, using the existing Offering restore cascade as the behavioral precedent.
- Keep archived Store catalogs read-only and keep permanent deletion available for archived Blueprints.
- Cover service behavior, Store Editor command state, and the visible action in headless UI tests.

## Non-goals

- Changing which records the Blueprint archive cascade archives.
- Selective descendant restoration or recording per-descendant archive provenance.
- Changing Offering restore behavior or permanent deletion safeguards.

## Dependencies and decisions

- Existing `RestoreOfferingCascadeAsync` defines the established restore pattern: restore the selected parent and all owned catalog descendants while retaining identities and links.
- The Blueprint restore will follow that cascade semantics, including descendants that were already archived before Blueprint archival, matching current Offering restoration behavior. No provenance is recorded to distinguish those states.
- The action is unavailable when the Store is archived.

## Risks

- Restoring the full owned graph can reactivate descendants that were already archived before the Blueprint cascade. This is consistent with the current Offering restore implementation; selective restoration is outside scope.
- Existing protected Item references must remain intact and must not be required to be removed for reactivation.

## Verification

- Focused application tests prove atomic cascade restoration, identity/reference preservation, confirmation and state validation, and archived-Store protection.
- Store Editor headless tests prove archived-only visibility, confirmation/cancel behavior, active-store availability, and archived-Store read-only behavior.
- Run `dotnet test .\FusionCanvas.sln -m:1` and strict OpenSpec validation. Map every accepted scenario to test evidence in `verification.md`.
- No Appium journey is planned: this is one additional state-specific command on an existing editor surface, and the state transition plus rendered control and confirmation can be covered deterministically by application and Avalonia headless tests. Existing real-desktop coverage remains supplemental.

## Module fit

This is one cohesive lifecycle outcome: complete the reversible Blueprint archive loop. The service operation, confirmation, and state-based UI action share the same parent/descendant restore behavior and can be verified together.
