# Design

Add parameter support to the repository’s existing async reader helper so relationship queries use the same binding path as writes. Add one strict identifier-quoting helper for migration-only table and column names; values and identifiers remain visibly distinct.

## Implementation plan

1. Replace the three typed-ID query interpolations with parameters.
2. Quote and validate migration/table identifiers.
3. Run integration persistence tests, build, and strict validation.
