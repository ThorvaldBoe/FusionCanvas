# Audit — d0f9281c-e845-4531-a28a-ce66d6a48f6c

- Audit ID: d0f9281c-e845-4531-a28a-ce66d6a48f6c
- Status: BLOCKED
- Baseline readiness: PROVISIONAL
- Started at: 2026-10-04T00:00:00+02:00
- Ended at: 2026-10-04T00:00:00+02:00
- Audit owner/reviewer: Codex / pending human review
- Request scope: SELECTED_AXES
- Requested axis or axes: Software Factory audit measures for the app-wide terms and responsible-use consent change. Interpreted as a changed-scope cross-axis review across all six QA input axes; this is not a full-repository audit.
- Included axes and baseline versions: Architecture & Code Structure v2.0.0 Active / extract v2.0 Active; Functionality & Logic v1.2 Active / extract v1.2 Active; UX v1.1 Active / extract v1.2 Active; UI v1.3 Draft normative baseline / extract v1.4 Draft; Security & Privacy v1.0 Active / extract v1.2 Working; Values, Vision & Taste v1.1 Draft normative baseline / extract v1.2 Draft.
- Excluded axes and reasons: None within the selected scope. Unrelated repository changes, full-repository certification, and external issue creation are out of scope.
- Progress phase: FINALIZE
- Progress updated at: 2026-10-04T12:26:53+02:00
- Progress summary: Six targeted axis packets were extracted and assessed against the selected change. Two findings were recorded and resolved, and the isolated worktree now has clean deterministic verification. The audit remains provisional/blocked only for human review and release-gated policy approval.
- Progress estimate and basis: Approximately 100% of the planned changed-scope packet work; estimate is process coverage only, based on six axis extracts, routing records, criterion assessments, findings, challenges, and completion records. It is not a compliance score.
- Parent checkpoint / next action: Human review of findings/challenges and legal/product approval of the bundled policy before release certification.

## Axis runs

| Axis | Audit run ID | Standard/version/status | Definition/version/status | Source revision range | Extract identity/latest extension | Run status |
|---|---|---|---|---|---|---|
| Architecture & Code Structure | 2067e25c-bb6f-491c-8dd1-b489ce80babe | FC-ARCH v2.0.0 / Active | v2.0 / Active | 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 | ARCH-EXTRACT-001 / 1 | PROVISIONAL |
| Functionality & Logic | 07c56278-78a1-40de-8ad5-1a7850b697ca | FC-FUNC v1.2 / Active | v1.2 / Active | 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 | FUNC-EXTRACT-001 / 1 | BLOCKED |
| UX | f4618aa7-7db6-47dc-8185-1edf084ffbf4 | FC-UX v1.1 / Active | v1.2 / Active | 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 | UX-EXTRACT-001 / 1 | PROVISIONAL |
| UI | 90f133fe-51ba-4987-b2bd-ef319de68570 | FC-UI v1.3 / Draft normative baseline | v1.4 / Draft | 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 | UI-EXTRACT-001 / 1 | PROVISIONAL |
| Security & Privacy | cf66e03f-375a-49b9-8cf0-d3b6f64d4e3f | FC-SEC v1.0 / Active | v1.2 / Working | 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 | SEC-EXTRACT-001 / 1 | BLOCKED |
| Values, Vision & Taste | c43470b1-826b-4d34-882a-803741cb5d2a | FC-VVT v1.1 / Draft normative baseline | v1.2 / Draft | 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 | VVT-EXTRACT-001 / 1 | PROVISIONAL |

## Challenge review and standard changes

- `CH-ARCH-001`, `CH-FUNC-001`, `CH-UX-001`, `CH-UI-001`, `CH-SEC-001`, and `CH-VVT-001` remain pending human review where noted in the axis packets. No standard was changed during this audit.
- Verification was completed in an isolated consent worktree based on commit `70c1e1c3da564a51bd8ae6544e5a699e7b6842d7`; unrelated content-risk work was not included.
- GitHub issue creation: NOT REQUESTED. Findings remain valid without linked issues.

## Final coverage and certification summary

- Process coverage: All six selected axis packets have a source extract, routing outcome, criterion-level assessment register, limitations, and completion record.
- Product result: No release certification yet. Architecture and UI findings are resolved with clean verification. Legal/product approval of the `draft-0.1` policy and human review of the remaining challenges are still required.
- Baseline result: Deterministic verification PASS in the isolated worktree: Domain 273/273, Application 586/586, Integration 311/311, App 919/919, and UiDescription 29/29. Audit remains BLOCKED pending human/legal release approval; UI and Values baselines are Draft and Security & Privacy extraction is Working.

## Open findings, linked issues, and follow-up

1. `F-ARCH-001` — resolved by `TermsConsentService`; full solution verification passed.
2. `F-UI-001` — resolved by semantic token migration; full App/headless verification passed.
3. Obtain product/legal approval for the draft FusionCanvas terms before treating the startup gate as release-ready.
