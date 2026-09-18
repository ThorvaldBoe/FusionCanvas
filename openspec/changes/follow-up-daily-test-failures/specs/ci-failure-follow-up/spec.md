## ADDED Requirements

### Requirement: Daily deterministic failures produce actionable follow-up
The daily deterministic-test workflow SHALL preserve diagnostics and create or update a single maintainer-visible GitHub issue when the build or deterministic tests fail.

#### Scenario: The baseline fails
- **WHEN** the scheduled or manually dispatched deterministic test command fails
- **THEN** the workflow remains failed
- **AND** test-result artifacts are retained for inspection
- **AND** the job summary identifies the failed baseline and links to the workflow run
- **AND** one open tracking issue titled `CI: deterministic tests failing` exists or is created

#### Scenario: A later baseline fails while the tracking issue is open
- **WHEN** another scheduled or manually dispatched baseline fails while the tracking issue is open
- **THEN** the workflow adds the new run link and commit identity to that issue
- **AND** it does not create a duplicate open tracking issue

#### Scenario: The baseline recovers
- **WHEN** a scheduled or manually dispatched deterministic baseline completes successfully while the tracking issue is open
- **THEN** the workflow preserves the successful result
- **AND** the tracking issue is closed with a recovery reference

#### Scenario: Issue automation cannot complete
- **WHEN** GitHub issue creation, commenting, or closing fails
- **THEN** the deterministic test result remains authoritative
- **AND** the workflow remains failed when the test command failed
- **AND** the issue-operation error is visible in the workflow log or summary

### Requirement: Failure follow-up uses restricted repository automation
The failure-follow-up workflow SHALL use repository-controlled code and the minimum GitHub token permissions needed for diagnostics and issue tracking.

#### Scenario: The workflow executes
- **WHEN** the scheduled or manually dispatched job checks out code and performs issue follow-up
- **THEN** it checks out the repository default branch
- **AND** it has read access to repository contents and write access only to issues
- **AND** it does not require external services, credentials, or a third-party issue-management action
