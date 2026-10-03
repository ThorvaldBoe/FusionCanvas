## Why

Blueprint Offering archive cascades preserve catalog history, but the Blueprint detail surface currently hides archived Offerings and provides no supported way to restore or permanently remove one. This leaves archived catalog configuration difficult to review and prevents intentional cleanup after external Item or listing references have been resolved.

## What Changes

- Add an explicit, opt-in **Show archived Blueprint Offerings** filter to the Blueprint-scoped Offering list; it is unchecked by default and scoped to the selected Blueprint and Store.
- Keep active Offerings visible by default and distinguish archived Offering rows and details from active records.
- Add an archived-only **Restore Blueprint Offering** action that restores the Offering together with the catalog-owned descendants archived by its Offering archive cascade, preserving stable identities and relationships.
- Add an archived-only, explicitly confirmed **Delete permanently** action for Blueprint Offerings.
- Permanently delete the archived Offering and its catalog-owned descendants atomically, including Options, Option Values, Variants, Placeholders, Mockup Templates, and template-owned records.
- Block permanent deletion when Items, listing configurations, design-slot assignments, or other protected external records reference the Offering or its descendants; report the named blockers and leave all records unchanged.
- Preserve the existing reversible archive workflow and archived-Store read-only behavior.

## Capabilities

### New Capabilities

None. The behavior belongs to the existing Blueprint-scoped catalog and archive capabilities.

### Modified Capabilities

- `blueprint-offering-list`: add archived Offering visibility, archived-state lifecycle actions, default filtering, selection behavior, and focused confirmation UX.
- `catalog-archive-cascade`: define reviewable archived Offering state, cascade restore semantics, and the Offering-owned permanent-delete boundary.
- `product-supplier-setup`: define atomic permanent deletion of an archived Offering, named protected-reference blockers, and catalog-owned descendant cleanup.

## Impact

- Store Editor Blueprint detail view models and Avalonia markup for the archived filter, row state, restore action, and permanent-delete confirmation.
- Application catalog lifecycle contracts and services for cascade restore, permanent Offering deletion, dependency discovery, and atomic snapshot mutation.
- Domain/persistence snapshot handling for Offering-owned descendants and compatibility projection cleanup; no schema migration is expected because archive flags and stable identities already exist.
- Focused application, integration, and Avalonia headless tests, followed by the full `dotnet test .\\FusionCanvas.sln -m:1` baseline and strict OpenSpec validation.

