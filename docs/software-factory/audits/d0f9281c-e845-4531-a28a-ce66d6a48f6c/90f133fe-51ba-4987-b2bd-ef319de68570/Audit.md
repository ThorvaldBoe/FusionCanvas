# UI Audit Run — 90f133fe-51ba-4987-b2bd-ef319de68570

- Audit ID: d0f9281c-e845-4531-a28a-ce66d6a48f6c
- Audit run ID: 90f133fe-51ba-4987-b2bd-ef319de68570
- Status: PROVISIONAL
- Baseline readiness: PROVISIONAL
- Started at: 2026-10-04T00:00:00+02:00
- Ended at: 2026-10-04T00:00:00+02:00
- Auditor/reviewer: Codex / pending human review
- Scope: Consent dialog and Settings terms panel.
- Audit profile: STANDARD_TARGETED
- Standard ID/version/status: FC-UI / 1.3 / Draft normative baseline
- Source extract definition version/status: 1.4 / Draft
- Jev classifier/configuration: NOT_APPLICABLE; two surfaces manually routed across layout, states, dialog behavior, semantics, and accessibility.
- Repository revision and working-tree state: 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 / dirty-after-consent-commit
- Append-only extract path and identity: `docs/software-factory/audits/d0f9281c-e845-4531-a28a-ce66d6a48f6c/90f133fe-51ba-4987-b2bd-ef319de68570/source-extract.yaml` / UI-EXTRACT-001
- Latest extract extension revision: 1
- Progress phase: FINALIZE
- Progress updated at: 2026-10-04T00:00:00+02:00
- Source items: 2 / 2 / 2 / 0 / 2
- Checks: 8 planned / 8 completed / 0 remaining
- Current item and position: UI-SURF-settings-terms / 2 of 2
- Current item relevant checks: tokens, grouping, disabled state, dialog semantics, accessibility
- Progress estimate and basis: 100% of targeted source review; visual runtime and scaling checks remain unknown.

## Extract history

| Extension revision | Source revision | Working-tree state | Standard/definition versions | Generated at | Change summary | Affected extract-entry IDs |
|---|---|---|---|---|---|---|
| 1 | 70c1e1c | dirty-after-consent-commit | FC-UI 1.3 / extract 1.4 | 2026-10-04T00:00:00+02:00 | Initial consent surface inventory. | UI-ENTRY-001..002 |
| 2 | worktree-consent-audit-fixes | dirty | FC-UI 1.3 / extract 1.4 | 2026-10-04T12:05:00+02:00 | Re-extracted consent dialog after semantic-token migration. | UI-ENTRY-003 |

## Item/criterion assessments

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Standard version | Criterion ID/heading | Result | Evidence references | Implementation references/dependencies | Finding IDs | Challenge IDs | Supersedes assessment ID |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A-UI-001 | UI-SURF-terms-consent-dialog | UI-ENTRY-001/1 | 70c1e1c | 1.3 | FC-UI-016/024 grouping and semantic typography | PASS | Logical grouping and readable labels are present; headings use semantic roles. | `TermsConsentWindow.axaml` | NONE | NONE | NONE |
| A-UI-002 | UI-SURF-terms-consent-dialog | UI-ENTRY-001/1 | 70c1e1c | 1.3 | FC-UI-008/009/015/030 shared tokens and variants | FAIL | Dialog uses raw `Margin`, `Spacing`, `Padding`, `CornerRadius`, `FontSize`, and local `Button.primary` values despite existing DesignTokens resources. | `TermsConsentWindow.axaml` lines 14-35, 67-114, 134-141; `DesignTokens.axaml` | F-UI-001 | CH-UI-001 | NONE |
| A-UI-003 | UI-SURF-terms-consent-dialog | UI-ENTRY-001/1 | 70c1e1c | 1.3 | FC-UI-034/035/060 disabled/error states | PASS | Agree is disabled before all selections; save failure is visibly rendered and retry remains available. | `TermsConsentViewModel.CanAgree`; dialog action row/error region | NONE | NONE | NONE |
| A-UI-004 | UI-SURF-terms-consent-dialog | UI-ENTRY-001/1 | 70c1e1c | 1.3 | FC-UI-056/074-082 focus, keyboard, automation | UNKNOWN | Automation names are present for key controls, but real keyboard focus order, scaling, contrast, and announcements were not measured. | `TermsConsentWindow.axaml`; `TermsConsentWindowTests` | NONE | CH-UI-001 | NONE |
| A-UI-005 | UI-SURF-settings-terms | UI-ENTRY-002/1 | 70c1e1c | 1.3 | FC-UI-016/052 dialog/panel hierarchy | PASS | Terms content is grouped as a dedicated Settings section with a clear review action. | `SettingsWindow.axaml` lines 111-137 | NONE | NONE | NONE |
| A-UI-006 | UI-SURF-settings-terms | UI-ENTRY-002/1 | 70c1e1c | 1.3 | FC-UI-043/060 reason and recovery clarity | PASS | Status text and review action provide a next step for missing or stale consent. | `SettingsViewModel.TermsConsentSummary`; Settings panel | NONE | NONE | NONE |

## Findings

| Finding ID | Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID | Type/severity | Title | GitHub issue URL/status |
|---|---|---|---|---|---|---|---|---|
| F-UI-001 | A-UI-002 | UI-SURF-terms-consent-dialog | UI-ENTRY-001/1 | 70c1e1c | FC-UI-008/009/015/030 | UI consistency / Minor | Consent dialog bypasses shared semantic design tokens | Not created; issue creation not requested |

### F-UI-001 detail

The consent dialog contains repeated raw visual constants while the application already exposes semantic spacing and typography resources through `DesignTokens.axaml` and uses them in the design-system gallery. Replace the local values with existing tokens or add narrowly justified semantic tokens if the surface introduces a new pattern. This should be verified with a headless construction test plus a visual desktop check at the supported scaling range.

### Reassessment after extract extension 2

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID/heading | Result | Evidence | Supersedes |
|---|---|---|---|---|---|---|---|
| A-UI-007 | UI-SURF-terms-consent-dialog | UI-ENTRY-003/2 | worktree-consent-audit-fixes | FC-UI-008/009/015/030 shared tokens and variants | PASS | Consent dialog now uses semantic resources for page/card/indented/validation layout, spacing, typography, colors, radii, borders, controls, and button padding. | `src/FusionCanvas.App/TermsConsent/TermsConsentWindow.axaml`; `src/FusionCanvas.App/DesignSystem/DesignTokens.axaml` | A-UI-002 |

F-UI-001 is **resolved in the current worktree**, pending clean App/headless verification and commit-level reassessment.

## Challenges

| Challenge ID | Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID | Uncertainty and competing interpretations | Review disposition | Owner/next action |
|---|---|---|---|---|---|---|---|---|
| CH-UI-001 | A-UI-002 | UI-SURF-terms-consent-dialog | UI-ENTRY-001/1 | 70c1e1c | FC-UI-008/009/015/030 and FC-UI-074-082 | The UI baseline is Draft, and some existing product XAML also uses raw values; token adoption is clearly the intended direction but exact remediation may depend on the pending baseline. | Pending | Human UI review; decide token mapping and whether the Draft standard should be tightened. |

## Coverage, limitations, and certification status

The two consent surfaces were inventoried and assessed. UI result is PROVISIONAL because both the standard and extract definition are Draft. The prior token-consistency finding is resolved in the current worktree; clean App/headless verification remains pending.
