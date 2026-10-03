## 1. Application contracts and catalog lifecycle

- [x] 1.1 Add explicit Offering cascade-restore and permanent-delete request/plan/result contracts beside the existing catalog archive contracts, including confirmation and named blocker data.
- [x] 1.2 Extend `ICatalogSetupService` and implement archived-Offering validation: writable Store, archived/active state gates, explicit confirmation, and archived-Store read-only rejection.
- [x] 1.3 Implement atomic Offering cascade restore for the Offering and all catalog-owned Options, Option Values, Variants, Placeholders, Mockup Templates, and template-owned records while preserving identities and relationships.
- [x] 1.4 Implement atomic permanent Offering deletion for normalized and compatibility-owned records, including template revisions, source images, color/mapping rows, and dependent projection records.
- [x] 1.5 Reuse and extend dependency discovery so Item listing configurations, design-slot assignments, and other protected external references produce named blockers and prevent partial mutation.
- [x] 1.6 Verify snapshot ownership, compatibility synchronization, and Store isolation for restore/delete without adding a schema migration.

## 2. Blueprint Offering list and editor behavior

- [x] 2.1 Update `IOfferingManagementService`/`OfferingManagementService` and catalog presentation state to support active-only default loading and explicit inclusion of archived Offerings for one Blueprint and Store.
- [x] 2.2 Add the opt-in **Show archived Blueprint Offerings** checkbox, archived row/status presentation, and selection reconciliation when filtering, changing context, or refreshing after mutation.
- [x] 2.3 Add state-aware Restore and Delete permanently commands to `CatalogSetupViewModel`, preserving the existing active Offering archive command and archived-Store read-only guards.
- [x] 2.4 Add or extend Store Editor confirmation surfaces for restore and permanent deletion with cascade summaries, irreversible-deletion warning, cancellation, keyboard focus, and named-blocker display.
- [x] 2.5 Refresh authoritative catalog state after success and select the restored Offering, a valid remaining active Offering, or the relevant empty state after deletion.

## 3. Focused regression coverage

- [x] 3.1 Add application tests for active-only/default and include-archived Offering queries, archived state summaries, selection-safe refresh, and state-specific command availability.
- [x] 3.2 Add application tests for restore success/cancel, descendant coverage, stable identities, unrelated-record preservation, and archived-Store rejection.
- [x] 3.3 Add application tests for permanent deletion confirmation, active-Offering rejection, complete catalog-owned cascade removal, sibling/Store isolation, and atomic no-mutation failure.
- [x] 3.4 Add blocker tests for Item listing configurations, design-slot assignments, and named recoverable guidance; retain the future remap behavior as out of scope.
- [x] 3.5 Add integration persistence tests that round-trip restored state and prove deleted Offering descendants and compatibility projections do not reappear after reload/synchronization.
- [x] 3.6 Add Avalonia headless tests for the archived filter, row state, active/archived action visibility, archived-Store read-only behavior, confirmation cancellation, and post-mutation selection.

## 4. Criterion-level verification and delivery gates

- [x] 4.1 Map every scenario in the three delta specs to a passing focused application, integration, or headless test and record evidence in `verification.md`.
- [x] 4.2 Run `openspec validate` and correct any delta-spec, scenario, or artifact errors before implementation handoff.
- [x] 4.3 Run the full deterministic baseline `dotnet test .\\FusionCanvas.sln -m:1` and record the result in `verification.md`.
- [x] 4.4 Review the final diff for scope drift, migration absence, Store isolation, external-reference safety, and unchanged non-goals before requesting implementation approval.
