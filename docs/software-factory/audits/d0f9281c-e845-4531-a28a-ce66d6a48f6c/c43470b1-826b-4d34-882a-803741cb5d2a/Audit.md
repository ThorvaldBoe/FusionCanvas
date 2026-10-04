# Values, Vision & Taste Audit Run — c43470b1-826b-4d34-882a-803741cb5d2a

- Audit ID: d0f9281c-e845-4531-a28a-ce66d6a48f6c
- Audit run ID: c43470b1-826b-4d34-882a-803741cb5d2a
- Status: PROVISIONAL
- Baseline readiness: PROVISIONAL
- Started at: 2026-10-04T00:00:00+02:00
- Ended at: 2026-10-04T00:00:00+02:00
- Auditor/reviewer: Codex / pending human review
- Scope: Product decision to require app-wide acknowledgement of responsible-use and provider policies before first use.
- Audit profile: STANDARD_TARGETED
- Standard ID/version/status: FC-VVT / 1.1 / Draft normative baseline
- Source extract definition version/status: 1.2 / Draft
- Jev classifier/configuration: NOT_APPLICABLE; one product decision manually assessed against trust, safety/IP, artist-led creation, and quality-before-convenience criteria.
- Repository revision and working-tree state: 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 / dirty-after-consent-commit
- Append-only extract path and identity: `docs/software-factory/audits/d0f9281c-e845-4531-a28a-ce66d6a48f6c/c43470b1-826b-4d34-882a-803741cb5d2a/source-extract.yaml` / VVT-EXTRACT-001
- Latest extract extension revision: 1
- Progress phase: FINALIZE
- Progress updated at: 2026-10-04T00:00:00+02:00
- Source items: 1 / 1 / 1 / 0 / 1
- Checks: 4 planned / 4 completed / 0 remaining
- Current item and position: VVT-DEC-responsible-use-gate / 1 of 1
- Current item relevant checks: trust, safety/IP, artist-led creation, quality/convenience tradeoff
- Progress estimate and basis: 100% of the single-decision packet; interpretive result remains provisional by baseline status and requires product/legal judgment.

## Extract history

| Extension revision | Source revision | Working-tree state | Standard/definition versions | Generated at | Change summary | Affected extract-entry IDs |
|---|---|---|---|---|---|---|
| 1 | 70c1e1c | dirty-after-consent-commit | FC-VVT 1.1 / extract 1.2 | 2026-10-04T00:00:00+02:00 | Initial product-decision inventory for the consent gate. | VVT-ENTRY-001 |

## Item/criterion assessments

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Standard version | Criterion ID/heading | Result | Evidence references | Implementation references/dependencies | Finding IDs | Challenge IDs | Supersedes assessment ID |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A-VVT-001 | VVT-DEC-responsible-use-gate | VVT-ENTRY-001/1 | 70c1e1c | 1.1 | trust/transparency and safety/IP responsibility | PASS | Separate acknowledgements, explicit provider policy links, draft disclosure, and no claim that FusionCanvas controls provider terms. | `TermsConsentWindow.axaml`; bundled `FusionCanvasTermsOfUse.md` | NONE | CH-VVT-001 | NONE |
| A-VVT-002 | VVT-DEC-responsible-use-gate | VVT-ENTRY-001/1 | 70c1e1c | 1.1 | quality before convenience / artist-led creation | PASS | Gate is a deliberate friction point tied to a safety responsibility and does not add unrelated onboarding steps. | OpenSpec proposal/design; startup gate | NONE | NONE | NONE |
| A-VVT-003 | VVT-DEC-responsible-use-gate | VVT-ENTRY-001/1 | 70c1e1c | 1.1 | no unfinished core / release readiness | UNKNOWN | Terms content is explicitly marked draft and pending human/legal approval while the implementation gates startup on agreement. | `Assets/Legal/FusionCanvasTermsOfUse.md`; `TermsConsentPolicy.FusionCanvasTermsVersion` | NONE | CH-VVT-001 | NONE |
| A-VVT-004 | VVT-DEC-responsible-use-gate | VVT-ENTRY-001/1 | 70c1e1c | 1.1 | product taste: clarity without manipulative behavior | PASS | The user can inspect the policy, cancel, and revisit it from Settings; provider links are separated from the FusionCanvas policy. | Consent dialog and Settings terms section | NONE | NONE | NONE |

## Findings

No confirmed values/taste violation. The central concern is an unresolved release decision, recorded as a challenge because the standard interpretation and product authority are both involved.

## Challenges

| Challenge ID | Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID | Uncertainty and competing interpretations | Review disposition | Owner/next action |
|---|---|---|---|---|---|---|---|---|
| CH-VVT-001 | A-VVT-003 | VVT-DEC-responsible-use-gate | VVT-ENTRY-001/1 | 70c1e1c | no unfinished core / trust | The product may intentionally gate development builds on a clearly disclosed draft policy, or it may be unacceptable to ask users to agree to terms pending approval. The audit cannot choose the legal/product policy. | Pending | Product/legal owner must approve final text and decide release/dev-build gating. |

## Coverage, limitations, and certification status

The decision packet is complete, but the result is PROVISIONAL because the standard and extract definition are Draft and final terms approval is unavailable. No release-readiness claim should be made from this run alone. GitHub issue creation: not requested.
