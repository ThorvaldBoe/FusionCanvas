# Verification — make-bug-version-optional

| Acceptance scenario | Result | Evidence |
| --- | --- | --- |
| Bug report accepts omitted version or commit | Pass | The `version` field remains visible in `.github/ISSUE_TEMPLATE/bug_report.yml` but has no `validations.required` entry. |
| Other diagnostic fields remain required | Pass | Structural validation confirmed `operating-system`, `expected-behavior`, `actual-behavior`, `reproduction-steps`, and `frequency` each retain `required: true`. |
| Accepted change artifacts are valid | Pass | `openspec validate make-bug-version-optional --strict` passed. |
| Solution test baseline | Deferred | `dotnet test .\\FusionCanvas.sln` spawned stalled child processes and was terminated after exceeding its normal runtime. The test runner processes started by this attempt were stopped; no production or test code changed in this change. Re-run the baseline before archiving. |
