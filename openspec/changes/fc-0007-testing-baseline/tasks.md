## 1. Baseline Verification

- [ ] 1.1 Review the current solution and test projects to confirm domain, application, integration, and app test projects mirror the production layer projects.
- [ ] 1.2 Confirm the baseline suite can be run from the repository root with the standard solution-level `dotnet test` command.
- [ ] 1.3 Remove or adjust any placeholder-only tests that do not provide useful baseline confidence once behavior-focused tests exist.

## 2. Domain and Application Coverage

- [ ] 2.1 Add or verify focused domain tests for Phase 0 core entity relationships, invariants, and workflow-relevant decisions.
- [ ] 2.2 Add or verify application tests for use-case orchestration and application contracts where current Phase 0 application behavior exists.
- [ ] 2.3 Ensure tests avoid UI framework, persistence engine, external service, and file system dependencies unless those dependencies are the behavior under test.

## 3. Integration and Asset Boundary Coverage

- [ ] 3.1 Add or verify isolated local tests for SQLite persistence boundaries, including save/load behavior, identities, relationships, and required fields.
- [ ] 3.2 Add or verify workspace file storage and asset reference tests that protect resource identity and reconnectable references.
- [ ] 3.3 Ensure integration tests use temporary local resources and leave no shared developer database or workspace state behind.

## 4. App and Navigation Coverage

- [ ] 4.1 Add or verify app-layer tests for UI-owned state, navigation decisions, or shell behavior that can be tested without full visual UI automation.
- [ ] 4.2 Avoid superficial tests for static Avalonia markup or framework-owned rendering behavior.

## 5. Contributor Guidance and Validation

- [ ] 5.1 Document the baseline test command and priority coverage expectations in the appropriate contributor-facing project documentation.
- [ ] 5.2 Align test names or grouping with important OpenSpec acceptance behavior where practical.
- [ ] 5.3 Run the full baseline test suite and fix any failures introduced by the baseline work.
- [ ] 5.4 Run OpenSpec validation for `fc-0007-testing-baseline` and resolve any spec formatting or requirement issues.
