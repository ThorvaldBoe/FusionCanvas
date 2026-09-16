## Why

Catalog archive actions currently rely on users discovering dependency order by trial and error. A sellable Variant can be archived only after every active Placeholder that references it has been handled, while Blueprint Offerings have no visible archive action at all. This makes a valid reversible lifecycle workflow look broken and makes larger catalog cleanup unnecessarily difficult.

## What Changes

- Add a focused, explicit archive workflow for Blueprint Offerings.
- Make offering archive eligibility and the complete active dependent set visible before confirmation.
- Support a confirmed reversible cascade for catalog-owned descendants in dependency order, preserving stable identities and archive state.
- Improve blocked Variant and other catalog-record messages so they identify the exact dependent record types and names that must be archived, reassigned, or removed first.
- Keep external or user-workflow relationships protected; do not silently orphan Items, listing configuration, or other records outside the catalog-owned cascade.
- Preserve restore semantics and make the resulting archived Offering and descendants visible in the existing archived/review states.

## Capabilities

### New Capabilities

- `catalog-archive-cascade`: User-facing archive planning, confirmation, and reversible cascade behavior for Blueprint Offerings and their catalog-owned descendants.

### Modified Capabilities

- `product-supplier-setup`: Clarify offering archive controls, dependent-record guidance, and safe catalog lifecycle behavior.
- `variant-management`: Replace generic blocked-archive guidance with concrete dependent-record information and the supported resolution path.

## Impact

- Application catalog lifecycle services and request/result contracts for dependent discovery and cascade mutation.
- Store Editor view models and Avalonia markup for offering-level archive controls, a confirmation summary, and actionable blocked guidance.
- Domain/persistence snapshot mutation and integration coverage for atomic archive cascades; no schema migration is expected because archive state and stable identities already exist.
- Focused application and Avalonia headless tests, plus the full solution test baseline and strict OpenSpec validation.
