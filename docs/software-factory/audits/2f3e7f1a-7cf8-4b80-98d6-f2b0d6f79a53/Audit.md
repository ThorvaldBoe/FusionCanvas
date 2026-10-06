# Software Factory Audit — 2f3e7f1a-7cf8-4b80-98d6-f2b0d6f79a53

- Status: PROVISIONAL
- Baseline readiness: PROVISIONAL; UI and Values, Vision & Taste baselines are Draft.
- Started at: 2026-10-06T10:00:00+02:00
- Ended at: 2026-10-06T10:00:00+02:00
- Auditor/reviewer: Codex / pending human review
- Request scope: SELECTED_AXES — changed-scope cross-axis audit of commits `a25aa3b` and `239aed6`.
- Included change: enable all test projects for `dotnet test`, then remove duplicate test-project metadata.
- Excluded: stale `codex/explore-listing-tool` feature branch; it was not merged because it replaces the current Listing-stage mockup tool and is based on an older persistence schema.
- Verification: `dotnet test .\FusionCanvas.sln -m:1 --no-restore -p:UsedAvaloniaProducts= -v minimal` — 2,245 passed, 0 failed, 0 skipped; `openspec validate --all` — 83 passed, 0 failed.
- GitHub issue creation: NOT REQUESTED.

## Axis runs

| Axis | Run ID | Result |
|---|---|---|
| Architecture & Code Structure | `arch-4ad2d8ce-0a6a-4b62-9b17-7e92b1f4b001` | PASS |
| Functionality & Logic | `func-4ad2d8ce-0a6a-4b62-9b17-7e92b1f4b002` | PASS; finding resolved |
| UX | `ux-4ad2d8ce-0a6a-4b62-9b17-7e92b1f4b003` | PASS / no impact |
| UI | `ui-4ad2d8ce-0a6a-4b62-9b17-7e92b1f4b004` | PROVISIONAL / no impact |
| Security & Privacy | `sec-4ad2d8ce-0a6a-4b62-9b17-7e92b1f4b005` | PASS / no impact |
| Values, Vision & Taste | `vvt-4ad2d8ce-0a6a-4b62-9b17-7e92b1f4b006` | PROVISIONAL / no impact |

## Findings and resolution

- `F-FUNC-001` — Before the audit correction, four test projects lacked effective test-project metadata and `dotnet test` skipped their tests. Resolved by adding one `<IsTestProject>true</IsTestProject>` property to each affected project and removing duplicate declarations found during reassessment.
- No remaining findings were identified in the changed scope.

## Certification note

The committed changes are verified and have no remaining changed-scope findings. Overall status remains PROVISIONAL because the repository’s UI and Values, Vision & Taste standards are Draft rather than Final, and human review is still required by the Software Factory process.
