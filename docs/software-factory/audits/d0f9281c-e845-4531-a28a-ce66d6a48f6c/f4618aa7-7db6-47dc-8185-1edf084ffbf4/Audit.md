# UX Audit Run — f4618aa7-7db6-47dc-8185-1edf084ffbf4

- Audit ID: d0f9281c-e845-4531-a28a-ce66d6a48f6c
- Audit run ID: f4618aa7-7db6-47dc-8185-1edf084ffbf4
- Status: PROVISIONAL
- Baseline readiness: FINAL_ELIGIBLE for this axis; parent remains BLOCKED
- Started at: 2026-10-04T00:00:00+02:00
- Ended at: 2026-10-04T00:00:00+02:00
- Auditor/reviewer: Codex / pending human review
- Scope: First-launch consent workflow and Settings review workflow.
- Audit profile: STANDARD_TARGETED
- Standard ID/version/status: FC-UX / 1.1 / Active
- Source extract definition version/status: 1.2 / Active
- Jev classifier/configuration: NOT_APPLICABLE; two workflows manually routed across discoverability, states, recovery, interruption, and external consequence checks.
- Repository revision and working-tree state: 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 / dirty-after-consent-commit
- Append-only extract path and identity: `docs/software-factory/audits/d0f9281c-e845-4531-a28a-ce66d6a48f6c/f4618aa7-7db6-47dc-8185-1edf084ffbf4/source-extract.yaml` / UX-EXTRACT-001
- Latest extract extension revision: 1
- Progress phase: FINALIZE
- Progress updated at: 2026-10-04T00:00:00+02:00
- Source items: 2 / 2 / 2 / 0 / 2
- Checks: 10 planned / 10 completed / 0 remaining
- Current item and position: WF-terms-settings-review / 2 of 2
- Current item relevant checks: discoverability, outcome clarity, recovery, navigation
- Progress estimate and basis: 100% of the targeted workflow packet; runtime-only checks remain explicitly unknown.

## Extract history

| Extension revision | Source revision | Working-tree state | Standard/definition versions | Generated at | Change summary | Affected extract-entry IDs |
|---|---|---|---|---|---|---|
| 1 | 70c1e1c | dirty-after-consent-commit | FC-UX 1.1 / extract 1.2 | 2026-10-04T00:00:00+02:00 | Initial consent workflow inventory. | WF-terms-first-launch, WF-terms-settings-review |

## Item/criterion assessments

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Standard version | Criterion ID/heading | Result | Evidence references | Implementation references/dependencies | Finding IDs | Challenge IDs | Supersedes assessment ID |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A-UX-001 | WF-terms-first-launch | UX-ENTRY-001/1 | 70c1e1c | 1.1 | UX-D01/D02/D03 discoverability and understandable action | PASS | Dialog has heading, policy text, explicit required checkboxes, and disabled Agree action until complete. | `TermsConsentWindow.axaml` lines 14-63, 114-130 | NONE | NONE | NONE |
| A-UX-002 | WF-terms-first-launch | UX-ENTRY-001/1 | 70c1e1c | 1.1 | UX-F01/F04/F05 acknowledgement and failure next step | PASS | Save failure is retained in dialog state with an error message; incomplete state explains required selection. | `TermsConsentViewModel.AcceptAsync`; `TermsConsentWindow.axaml` error region | NONE | NONE | NONE |
| A-UX-003 | WF-terms-first-launch | UX-ENTRY-001/1 | 70c1e1c | 1.1 | UX-R01/R02/R04 recovery and retained work | PASS | Failed save leaves selections intact and permits retry; cancellation does not create consent. | focused `TermsConsentViewModelTests` | NONE | NONE | NONE |
| A-UX-004 | WF-terms-first-launch | UX-ENTRY-001/1 | 70c1e1c | 1.1 | UX-N02/C02/T09 back, cancel, interruption | UNKNOWN | Headless tests cover cancellation command behavior, but real focus return and interrupted startup behavior were not exercised on desktop. | `TermsConsentWindow.axaml.cs`; `App.InitializeStartupAsync` | NONE | CH-UX-001 | NONE |
| A-UX-005 | WF-terms-settings-review | UX-ENTRY-002/1 | 70c1e1c | 1.1 | UX-D01/N01 discoverable review route | PASS | Settings contains a dedicated Terms and responsible-use section with status and a review action. | `SettingsWindow.axaml` lines 111-137; `SettingsViewModel` lines 141-205 | NONE | NONE | NONE |
| A-UX-006 | WF-terms-settings-review | UX-ENTRY-002/1 | 70c1e1c | 1.1 | UX-F06/R08 truthful and non-leaky status | PASS | Status distinguishes current/missing/stale consent without exposing sensitive data. | `TermsConsentStatus`; Settings summary binding | NONE | NONE | NONE |

## Findings

No confirmed UX finding. The primary path is explicit, cancelable, retryable, and has a durable review route.

## Challenges

| Challenge ID | Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID | Uncertainty and competing interpretations | Review disposition | Owner/next action |
|---|---|---|---|---|---|---|---|---|
| CH-UX-001 | A-UX-004 | WF-terms-first-launch | UX-ENTRY-001/1 | 70c1e1c | UX-N02/C02/T09 | Headless evidence shows command behavior, but the standard may require real desktop confirmation of focus restoration and startup interruption semantics. | Pending | Optional desktop smoke check after solution baseline is repaired. |

## Coverage, limitations, and certification status

Targeted workflow coverage is complete. UX is PROVISIONAL at parent level only because the audit includes other Draft/Working baselines; runtime desktop accessibility/focus evidence remains unknown.
