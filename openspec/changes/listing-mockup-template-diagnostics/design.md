## Context

The Listing stage currently asks the application service for eligible templates. The service correctly filters out Draft and archived templates, but the returned readiness blockers are only used for a requested-template failure and are discarded for the normal empty-selector path. Store settings already derives creator-facing messages from the same domain readiness policy.

The primary workflow is an occasional but high-friction recovery action: a creator opens Listing to generate mockups and needs to understand what to fix. The main workspace should show a compact blocked/empty explanation; detailed template editing remains in the focused Store settings editor. Progressive disclosure is therefore a short summary plus per-template blocker lines, without embedding the Store editor in Listing.

## Goals / Non-Goals

**Goals:**

- Preserve the authoritative ready-only eligibility gate.
- Carry candidate readiness summaries from the application service to Listing.
- Distinguish no active templates, incomplete templates, and eligibility-load errors.
- Show exact blockers per template with a clear Store settings destination.
- Keep text keyboard- and screen-reader-readable and retain existing selection/application behavior.

**Non-Goals:**

- No changes to readiness rules, persistence, template editing, navigation architecture, or mockup rendering.
- No automatic creation, repair, or selection of Draft templates.
- No new persistent diagnostic data.

## Decisions

1. **Return candidate readiness summaries from eligibility.** Extend the existing eligibility result with summaries for active candidates. This keeps diagnosis beside the authoritative policy evaluation and avoids duplicating readiness logic in the UI.

2. **Format diagnostics in the Listing view model.** The application result remains domain-oriented (`MockupTemplateReadinessBlocker` and template identity); creator-facing wording stays in the App layer, matching Store settings conventions.

3. **Use a compact diagnostic collection.** Listing receives a `TemplateDiagnostics` collection and a blocked summary. Each entry contains the template name and formatted blockers. The view renders the collection as ordinary text; no modal or new navigation command is introduced.

4. **Treat errors separately from empty results.** If eligibility returns an error, Listing shows that error and does not report that no templates exist. If eligibility succeeds with zero eligible templates, Listing can safely distinguish zero candidates from Draft candidates.

## Risks / Trade-offs

- [Long diagnostic lists] → Show one line per template and one indented blocker summary; the existing stage scroll area handles overflow.
- [Stale diagnostics after Store settings changes] → Reload Listing state through the existing document load path; no caching is introduced.
- [Wording drift between Store and Listing] → Reuse the same blocker-to-message mapping text and cover all enum values in tests.

## Migration Plan

No data migration is required. The result contract gains presentation data with no persistence impact. Existing callers receive an empty diagnostics collection through the default-compatible construction path.

## Open Questions

None. The scope and interaction are intentionally limited to actionable read-only guidance.

## Implementation Plan

1. Extend `EligibleMockupTemplateResult` with active candidate readiness summaries and populate them in `MockupTemplateSetupService.GetEligibleTemplatesAsync`.
2. Extend `MockupGenerationState` and `MockupGenerationService.LoadAsync` to carry diagnostics while preserving the existing generic blocked state for colors/no Offering.
3. Add a small immutable Listing diagnostic presentation type and map every `MockupTemplateReadinessBlocker` to the existing creator-facing wording.
4. Update `ListingStageToolViewModel` and `MainWindow.axaml` to expose/render the diagnostic collection with accessible text and clear empty-state wording.
5. Add application tests for candidate summaries and App tests for no-template, Draft-template, and ready-template states; run OpenSpec validation and the full solution test baseline.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Verification |
| --- | --- |
| No mockup templates are configured | Application service test plus Listing view-model test |
| Configured templates are incomplete | Application service test plus Listing view-model presentation test |
| A template becomes ready | Existing readiness/eligibility tests plus Listing state test |
| Readiness diagnostics are unavailable | Application service error/state test |
| Diagnostic state does not weaken eligibility | Eligibility regression test and full solution baseline |
