## Why

Store Management currently exposes useful catalog counts, but counts do not tell a creator whether an Offering can support the next Design or Listing action. The same underlying setup facts are already evaluated in lower-level mockup and workflow services, so the first iteration should surface those facts coherently at the Offering boundary before adding automated fixes or broader AI assistance.

## What Changes

- Add an application-level readiness summary for each active Blueprint Offering that combines its active Variant, Design Area, and Mockup Template state.
- Report actionable, creator-facing blockers for incomplete or draft Mockup Templates while preserving the existing authoritative readiness policy.
- Show the summary in the Store catalog Offering overview/detail, alongside existing counts, with clear empty, ready, and attention-needed states.
- Keep the summary read-only and derived from the current workspace snapshot; it must not alter catalog data or loosen Design/Listing eligibility.
- Add focused domain/application tests and deterministic Avalonia coverage for the new state presentation where bindings and control visibility are material.

## Capabilities

### New Capabilities

- `catalog-offering-readiness-guidance`: Provides an Offering-scoped, actionable readiness summary for Store Management.

### Modified Capabilities

- `product-supplier-setup`: The Store catalog Offering surface additionally presents the current setup/readiness state and named next-step guidance without changing catalog ownership or validation rules.

## Impact

- **Application:** Extend the catalog/Offering query projection with a stable readiness summary and blocker mapping that reuses the existing Mockup Template readiness evaluator.
- **Domain:** No new persistence entities or eligibility rules are introduced; existing catalog and mockup invariants remain authoritative.
- **App:** Update Offering cards/detail presentation and accessible text for empty, attention-needed, and ready states. Keep the information in the focused Store Management surface rather than permanently expanding the main workspace.
- **Tests:** Add framework-free projection/policy tests, presentation-model tests, and headless view coverage only for meaningful binding/state behavior.
- **Non-goals:** Printify import changes, one-click repair dialogs, AI recommendations, automatic catalog mutation, Design-stage artwork readiness changes, Listing-stage selection changes, and database migrations.
