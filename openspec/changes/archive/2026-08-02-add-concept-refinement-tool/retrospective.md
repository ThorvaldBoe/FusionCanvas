# add-concept-refinement-tool Retrospective

## Outcome

The Concept stage now ships the issue-#85 refinement tool: a section inside the existing Concept stage surface with manual `Initialize from base idea`, per-corner `Fine tune`/`Change` AI actions gated on Concept-purpose AI availability, session-scoped history with rollback, a deterministic Domain completeness score, and a bundled placeholder design-triangle guidance document feeding prompt context. All 34 acceptance scenarios carry criterion-level evidence; both strict validations pass; 838/839 tests pass with the single failure a confirmed pre-existing main regression (VR-008, routed to a separate bugfix flow). The change also reconciled the contradictory `listing-inspector` explicit-save requirement, leaving automatic save as the sole accepted save model.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
|---|---|---|---|---|---|
| "Failed operation leaves drafts/history/score unchanged" covers all failures | fc-spec-reviewer SR-001: contradicts design D6 when the commit fails *after* AI application | Spec now scopes failure semantics by phase; post-application commit failure retains the appended entry | Missing requirement precision (specification defect caught at review) | Any spec describing multi-phase operations | Deferred: candidate specification-writing rule (see Learning Review) |
| Setting a VM `ErrorMessage` is enough for "recoverable inline error" | fc-verifier VR-001: no view element rendered it | Added visible inline error TextBlock + headless assertion | Ordinary implementation defect | Any "inline error" scenario | None (covered by existing UX guideline "explain why a requested action is blocked") |
| Disabled-button guidance works as a tooltip | fc-verifier VR-002/VR-009: Avalonia `ToolTip.ShowOnDisabled` defaults to false — tooltip never shows on the disabled control | Visible guidance TextBlock when disabled + `ShowOnDisabled="True"` + visual-tree test | Ordinary implementation defect carrying a reusable UI lesson | Every disabled-with-guidance control | Deferred: candidate `docs/ui-guidelines.md` rule (see Learning Review) |
| Manual-commit detection can compare against empty when history is empty | fc-verifier VR-003: phantom `Edited Concept fields` entries for items loaded with existing values | Baseline captured at session reset; compare against last entry or baseline | Ordinary implementation defect | Any session-delta detection | None (change-specific) |
| A passing regression test proves the fix | fc-verifier VR-010: `NonConceptCommit_AppendsNothing` passed even without the fix (empty-triangle fixture) | Fixture parametrized with pre-existing Concept values so it fails without the fix | Ordinary implementation defect (test), carrying a reusable fixture-design lesson | Any regression test | Deferred: candidate `testing-baseline` rule (see Learning Review) |
| Resolved creative context automatically reaches the prompt | fc-verifier VR-004: group name/metadata + descriptions resolved but never written (dead fields) | Prompt assembly includes topic + descriptions; grouped-item payload test | Ordinary implementation defect | Any prompt-assembly pipeline | None (covered by fixture lesson above) |

## Learning Review

- **Result:** reusable lessons identified (2 promoted candidates; both deferred).
- **Evidence reviewed:** final proposal, design (D1–D8), delta specs, tasks, `verification.md`; fc-spec-reviewer report (SR-001–SR-003); fc-verifier reports from three rounds (VR-001–VR-012); implementer slice reports; branch git history.
- **Promotions completed:** none — both candidates live outside the coordinator's edit scope (`docs/`) or require their own OpenSpec change (`testing-baseline` spec).
- **Deferred promotions:**
  1. `docs/ui-guidelines.md` (Specific UI Elements / Buttons): "Do not rely on `ToolTip` alone for blocked-state guidance on disabled controls — Avalonia does not show tooltips on disabled controls by default; surface the reason as visible text (optionally plus `ToolTip.ShowOnDisabled="True"`)." Rationale for deferral: docs/ is outside the coordinator's edit permission; hand to the user or a session with doc-edit rights.
  2. `openspec/specs/testing-baseline/spec.md`: "A regression test must be shown to fail without the fix (fixture must exercise the original defect's preconditions); a test that passes under the pre-fix code does not guard the regression." Rationale for deferral: editing an accepted spec requires its own OpenSpec change; propose as a small follow-up. (Echoes the archived tree-actions-toolbar fixture lesson VR-001 — recurrence strengthens the case.)
  3. Specification-writing rule (weakest candidate): "Multi-phase operations need per-phase failure semantics in acceptance scenarios." Rationale for deferral: single instance; revisit if it recurs.
- **Process validations (no promotion needed):** resolving the four high-impact decisions with the user before the proposal meant zero architecture escalations during implementation; establishing the solution test baseline before implementation let the team prove VR-008 was a pre-existing main regression rather than a change defect.
