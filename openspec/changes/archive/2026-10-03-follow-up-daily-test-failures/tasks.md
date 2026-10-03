## 1. Workflow diagnostics

- [x] 1.1 Add restricted issue permissions and default-branch checkout to `.github/workflows/ci.yml`.
- [x] 1.2 Add TRX result logging, always-run artifact upload, and a concise job summary without weakening test failure behavior.

## 2. Failure follow-up

- [x] 2.1 Add stable-title issue lookup and create/comment behavior for failed baseline runs.
- [x] 2.2 Add recovery behavior that closes the active tracking issue after a successful baseline.
- [x] 2.3 Keep issue-operation errors visible and ensure they do not mask the deterministic test result.

## 3. Verification

- [x] 3.1 Inspect workflow permissions, triggers, branch targeting, conditional execution, and command failure semantics.
- [x] 3.2 Run `openspec validate follow-up-daily-test-failures --strict` and resolve all validation errors.
- [x] 3.3 Run `git diff --check` and the local deterministic solution baseline.
- [x] 3.4 Record criterion-level evidence for every acceptance scenario in `verification.md`.
