## 1. Specification and plan

- [x] 1.1 Record the archived-only lifecycle action and restoration acceptance scenarios.
- [x] 1.2 Define the cascade semantics, edge cases, test plan, and Appium decision.

## 2. Application restore operation

- [x] 2.1 Add a confirmed Store-scoped Blueprint restore request and service contract.
- [x] 2.2 Implement atomic restoration of the Blueprint and all owned catalog descendants, preserving identities, relationships, and external references.
- [x] 2.3 Add application tests for cascade success, guards, and archived Store protection.

## 3. Store Editor reactivation action

- [x] 3.1 Add archived-only reactivation command and confirmation state with correct Store and draft guards.
- [x] 3.2 Add the Reactivate Blueprint button and confirmation panel with accessible naming and consequence text.
- [x] 3.3 Add headless UI coverage for action visibility, confirmation cancellation/success, and selection after restore.

## 4. Verify and learn

- [x] 4.1 Run focused tests and the solution baseline.
- [x] 4.2 Build the solution and validate the OpenSpec change strictly.
- [x] 4.3 Record acceptance-scenario evidence and any limitations in `verification.md`.
- [x] 4.4 Review implementation lessons and archive the completed change.
