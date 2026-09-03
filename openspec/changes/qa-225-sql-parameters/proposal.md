# Parameterize SQLite data values

## Why

The SQLite repository already binds most persisted values, but migration and relationship queries still interpolate typed identifiers directly into SQL. Consistent binding removes avoidable injection and quoting risk; the few unavoidable dynamic identifiers should be validated before interpolation.

## Scope

- Bind relationship-query identifiers as SQLite parameters.
- Validate and quote dynamic table/column identifiers used by migrations and row-count checks.
- Preserve schema migration and persistence behavior.

## Non-goals

- No schema change or migration behavior change.
- No rewrite of already parameterized statements.

## Modified Capabilities

- `local-sqlite-persistence`: require data values to be bound and unavoidable identifiers to be constrained.

## Verification

- Integration persistence tests pass, including migrations and round trips.
- Source inspection confirms the targeted data-value interpolations are removed.
