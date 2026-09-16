## 1. Workflow

- [x] 1.1 Update `.github/workflows/ci.yml` to remove the pull-request full-suite trigger and add a daily UTC schedule plus `workflow_dispatch`.
- [x] 1.2 Preserve restore, serialized deterministic test execution, and non-zero failure behavior; add scoped concurrency/cache safeguards without hiding failures.

## 2. Documentation and contract

- [x] 2.1 Update contributor/CI documentation with the local focused-test and full-suite commands and the daily CI responsibility.
- [x] 2.2 Review the testing-baseline delta against the accepted requirement and correct any missing or contradictory scenarios before implementation.

## 3. Verification

- [x] 3.1 Verify workflow triggers, branch targeting, schedule, command, and failure semantics by inspection and available workflow validation.
- [x] 3.2 Run focused affected tests and the full deterministic solution baseline locally, recording any existing failures rather than suppressing them.
- [x] 3.3 Run `openspec validate schedule-deterministic-tests-daily --strict` and resolve all validation errors.
- [x] 3.4 Record criterion-level evidence for every scenario in the change verification record before completion.
