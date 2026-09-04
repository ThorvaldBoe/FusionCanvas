# Verification — make-bug-version-optional

| Acceptance scenario | Result | Evidence |
| --- | --- | --- |
| Bug report accepts omitted version or commit | Pass | The `version` field remains visible in `.github/ISSUE_TEMPLATE/bug_report.yml` but has no `validations.required` entry. |
| Other diagnostic fields remain required | Pass | Structural validation confirmed `operating-system`, `expected-behavior`, `actual-behavior`, `reproduction-steps`, and `frequency` each retain `required: true`. |
| Accepted change artifacts are valid | Pass | `openspec validate make-bug-version-optional --strict` passed. |
| Solution test baseline | Pass | 2026-08-06 deterministic rerun passed all projects without building: Domain 188, Application 325, Integration 129, and App/headless 366; 1,008 passed, 0 failed, 0 skipped. The projects were run serially because the aggregate command previously exceeded its sandbox time limit. |
