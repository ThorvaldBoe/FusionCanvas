# Security & Privacy Audit Run — cf66e03f-375a-49b9-8cf0-d3b6f64d4e3f

- Audit ID: d0f9281c-e845-4531-a28a-ce66d6a48f6c
- Audit run ID: cf66e03f-375a-49b9-8cf0-d3b6f64d4e3f
- Status: BLOCKED
- Baseline readiness: PROVISIONAL
- Started at: 2026-10-04T00:00:00+02:00
- Ended at: 2026-10-04T00:00:00+02:00
- Auditor/reviewer: Codex / pending human review
- Scope: Consent metadata persistence, fixed external policy links, startup failure handling, and privacy lifecycle.
- Audit profile: STANDARD_TARGETED
- Standard ID/version/status: FC-SEC / 1.0 / Active
- Source extract definition version/status: 1.2 / Working definition
- Jev classifier/configuration: NOT_APPLICABLE; two security surfaces and one privacy flow manually routed with mandatory input, failure, external-boundary, minimization, and deletion checks.
- Repository revision and working-tree state: 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 / dirty-after-consent-commit
- Append-only extract path and identity: `docs/software-factory/audits/d0f9281c-e845-4531-a28a-ce66d6a48f6c/cf66e03f-375a-49b9-8cf0-d3b6f64d4e3f/source-extract.yaml` / SEC-EXTRACT-001
- Latest extract extension revision: 1
- Progress phase: RECONCILE
- Progress updated at: 2026-10-04T00:00:00+02:00
- Source items: 3 / 3 / 3 / 0 / 3
- Checks: 8 planned / 8 completed at source-review level / 0 remaining; full baseline execution blocked
- Current item and position: PRIV-FLOW-consent-metadata / 3 of 3
- Current item relevant checks: minimization, retention/deletion, user control, external disclosure
- Progress estimate and basis: Approximately 85%; static security/privacy review is complete, but full solution verification and live OS link behavior remain unavailable.

## Extract history

| Extension revision | Source revision | Working-tree state | Standard/definition versions | Generated at | Change summary | Affected extract-entry IDs |
|---|---|---|---|---|---|---|
| 1 | 70c1e1c | dirty-after-consent-commit | FC-SEC 1.0 / extract 1.2 | 2026-10-04T00:00:00+02:00 | Initial security surface and privacy flow inventory. | SEC-ENTRY-001..003 |

## Item/criterion assessments

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Standard version | Criterion ID/heading | Result | Evidence references | Implementation references/dependencies | Finding IDs | Challenge IDs | Supersedes assessment ID |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A-SEC-001 | SEC-SURF-local-consent-persistence | SEC-ENTRY-001/1 | 70c1e1c | 1.0 | SEC-INPUT-001/003 and SEC-FAIL-001 safe local state | PASS | Record is versioned, minimally shaped, and malformed data falls back with a warning rather than silently becoming current consent. | `JsonApplicationSettingsStore.TryReadTermsConsent`; `TermsConsentPolicy.IsCurrent` | NONE | NONE | NONE |
| A-SEC-002 | SEC-SURF-local-consent-persistence | SEC-ENTRY-001/1 | 70c1e1c | 1.0 | PRIV-002 minimization and PRIV-005 external disclosure | PASS | Only policy versions and UTC acceptance time are persisted; no identity, IP, workspace content, or credentials are included or transmitted. | `TermsConsentRecord`; `ApplicationSettings`; JSON serializer | NONE | NONE | NONE |
| A-SEC-003 | SEC-SURF-external-policy-links | SEC-ENTRY-002/1 | 70c1e1c | 1.0 | SEC-INPUT-004 URL validation | N/A | Audited URLs are compile-time application constants, not user/file/provider-controlled URLs. If URLs become configurable, parsing, scheme/host allowlisting, and redirect limits become mandatory. | `TermsConsentPolicy` URL constants; `ExternalLinkLauncher` | NONE | NONE | NONE |
| A-SEC-004 | SEC-SURF-external-policy-links | SEC-ENTRY-002/1 | 70c1e1c | 1.0 | SEC-FAIL-002 accurate/actionable failure | UNKNOWN | The code path delegates to the OS and the current launcher has changed in the dirty tree; live launch failure behavior was not verified. | `ExternalLinkLauncher`; consent link commands | NONE | CH-SEC-001 | NONE |
| A-SEC-005 | PRIV-FLOW-consent-metadata | SEC-ENTRY-003/1 | 70c1e1c | 1.0 | PRIV-001/003 lifecycle inventory and user control | PASS | Creation, local storage, startup read, and Settings review are identifiable and understandable. | `TermsConsentStatus`; Settings terms section | NONE | NONE | NONE |
| A-SEC-006 | PRIV-FLOW-consent-metadata | SEC-ENTRY-003/1 | 70c1e1c | 1.0 | PRIV-004 deletion/reset | UNKNOWN | The feature supports review and re-acknowledgement, but explicit reset/deletion semantics for a stored acceptance timestamp are not defined. | `SettingsViewModel`; `JsonApplicationSettingsStore` | NONE | CH-SEC-001 | NONE |

## Findings

No confirmed security vulnerability was identified in the changed scope. The consent record is appropriately minimal for the stated purpose.

## Challenges

| Challenge ID | Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID | Uncertainty and competing interpretations | Review disposition | Owner/next action |
|---|---|---|---|---|---|---|---|---|
| CH-SEC-001 | A-SEC-006 | PRIV-FLOW-consent-metadata | SEC-ENTRY-003/1 | 70c1e1c | PRIV-004 deletion/reset and SEC-FAIL-002 external launch | The feature may only need a review/re-acknowledgement control, or privacy policy may require a clear reset/delete path. The dirty launcher refactor and blocked solution also prevent runtime confirmation. | Pending | Product/privacy review; define reset semantics and rerun security checks after the tree is reconciled. |

## Coverage, limitations, and certification status

The changed-scope security/privacy inventory and static assessments are complete, but this axis is BLOCKED from final certification because its extract definition is Working, the current full solution baseline is unavailable, and the OS external-link failure path was not exercised. GitHub issue creation: not requested.
