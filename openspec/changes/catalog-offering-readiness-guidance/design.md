## Context

Store Management already projects Offering counts and the Application layer already evaluates individual Mockup Template readiness. The current presentation therefore has authoritative facts, but they are split across count-only Offering cards, template-level summaries, and stage-specific diagnostics. A creator can see “41 configured” while still not knowing that every template is blocked by missing source-image applicability or an invalid mapping.

This module is the first iteration of the broader Store → Design → Listing improvement program. It belongs in the focused Store Management surface because setup is occasional administrative work, while the main workspace should remain focused on creative production.

## Goals / Non-Goals

**Goals:**

- Produce one Offering-scoped projection that makes catalog completeness and Mockup Template readiness legible.
- Reuse the existing `MockupTemplateReadinessEvaluator` and `MockupTemplateReadinessPolicy`; do not create a second readiness rule set.
- Preserve progressive disclosure: show a compact status and counts first, with named issue guidance available in the Offering context.
- Keep the projection read-only, deterministic, store-scoped, and safe for archived/read-only review.
- Cover the derived policy with framework-free tests and cover material Store-editor binding/state behavior with deterministic headless tests.

**Non-Goals:**

- No persistence schema or migration.
- No automatic repair, navigation shortcut, dialog, Printify synchronization, or AI recommendation.
- No change to which templates Listing may select or which Design artwork operations are allowed.
- No Item-specific readiness claim; selected Colors, artwork files, and downstream Listing state remain separate concerns.

## Decisions

### 1. Add a catalog projection, not a new persistence entity

`OfferingReadinessSummary` and its structured issue records belong in the Application catalog query model. They are derived from `WorkspaceSnapshot` and are not stored. This keeps persistence stable and prevents stale readiness data after a catalog edit.

Alternative considered: storing a readiness flag on `BlueprintOffering`. Rejected because readiness depends on mutable template revisions, source images, applicability, and mappings and would require invalidation on many unrelated writes.

### 2. Reuse the existing Mockup Template policy

The summary builder will call the existing internal `MockupTemplateReadinessEvaluator` for each active template in the current Offering. It will count only `ReadyForUse` templates and retain each Draft template's complete blocker list. The new Offering-level issues cover only missing Variants, Design Areas, or Mockup Templates; template-specific issues remain the existing typed `MockupTemplateReadinessBlocker` values.

Alternative considered: infer readiness from counts or duplicate the policy in the Store view model. Rejected because counts cannot detect invalid mappings or source applicability and duplicated rules would drift from Listing eligibility.

### 3. Use precise status language

The projection will distinguish `Incomplete` (one or more required catalog collections are empty), `NeedsAttention` (catalog collections exist but no active template is ready), and `ReadyForMockupGeneration` (at least one active template is ready and the basic catalog collections are present). The UI will label this as catalog/mockup setup readiness and will not call the Offering Listing-ready.

### 4. Present compact summary plus actionable issue list

Offering cards will show counts and a short status. The focused Offering detail will show the status and issue list near the dependent sections; the list will name incomplete templates and translate existing blockers through the shared readiness-message translator. No new always-visible controls are added to the primary workspace.

### 5. Keep this iteration read-only

There is not yet a resolved product decision for automatic repair precedence, one-click option selection, or AI-generated setup plans. This module records the facts needed by those later iterations but does not guess or mutate user-owned catalog choices.

## Risks / Trade-offs

- **[Risk]** A snapshot may contain malformed legacy relationships. → Build the projection from the same filtered active records used by existing Offering queries; preserve existing synchronization/repair behavior and fail with a concise query error rather than inventing records.
- **[Risk]** The same blocker may be repeated across several templates. → Keep template names distinct in structured issues and let presentation formatting remain compact; do not deduplicate away per-template evidence.
- **[Risk]** “Ready for mockup generation” may be read as full Listing readiness. → Use explicit catalog/mockup wording and retain the non-goal in helper text; selected Item Colors and Design artwork remain outside the summary.
- **[Risk]** Existing callers construct `BlueprintOfferingSetupSummary` directly. → Add the readiness projection compatibly at the end of the record shape or update all constructors and tests in one bounded change; no persistence contract changes.

## Migration Plan

No database migration is required. The new projection is calculated on load. Rollback is a code rollback only: remove the projection fields and presentation bindings while leaving all persisted records untouched.

## Open Questions

None for this module. One-click repair, provider-import reconciliation, AI assistance, and cross-stage shared readiness are intentionally deferred to later iterations and must receive their own discovery decisions.

## Implementation Plan

1. Add application catalog projection types for Offering readiness state and structured issues, including template name plus typed `MockupTemplateReadinessBlocker` values for incomplete templates.
2. Add a focused `OfferingReadinessBuilder` (or equivalent query helper) in `FusionCanvas.Application.Catalog` that filters active records to one Offering, calls `MockupTemplateReadinessEvaluator`, computes counts/status, and never saves.
3. Extend `BlueprintOfferingSetupSummary` and `OfferingManagementService.ToSummary` to include the projection for both Offering overview and Offering detail loads. Keep `CatalogSetupQueries.CountSetup` and existing template eligibility behavior unchanged.
4. Update `BlueprintOfferingCardViewModel` and the focused Store Offering detail view model to expose short status text, counts, and translated issue guidance without putting mutation commands in the summary.
5. Update Store catalog XAML with a compact readiness panel/list, accessible text, and explicit empty/attention/ready visual states. Keep creation forms progressively disclosed and preserve existing navigation/draft safeguards.
6. Add application tests for missing collections, ready templates, multiple Draft templates with multiple blockers, archived/read-only review, and no-mutation behavior. Update presentation-model tests for status and issue text.
7. Add or extend deterministic Avalonia headless tests for Offering detail rendering/binding state where visibility or control state is material. Do not add a live desktop gate for this read-only projection.
8. Run criterion-level tests, `openspec validate catalog-offering-readiness-guidance --strict`, and the repository baseline `dotnet test .\FusionCanvas.sln` after restoring dependencies if required.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Ready Offering reports exact counts and no blockers | Application projection test with a ready local template; presentation-model assertion |
| Missing catalog prerequisites are named separately | Application projection test with empty active collections |
| Draft templates retain every blocker and name | Application projection test with two incomplete templates and focused presentation test |
| Summary does not mutate state | Repository fake/save-call assertion and snapshot equality test |
| Store detail shows status and guidance | Deterministic Avalonia headless view test for binding/visibility/accessibility plus view-model test |
| Existing Offering controls remain unchanged | Existing catalog/Offering test suite and solution baseline |
