## Why

FusionCanvas does not publish versioned releases yet, so requiring a version or commit prevents reporters without that information from submitting a bug report.

## What Changes

- Make the Bug Report form's version-or-commit field optional while retaining it as useful diagnostic information.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `github-issue-workflow`: Allow Bug Reports without version or commit information.

## Impact

- `.github/ISSUE_TEMPLATE/bug_report.yml` and the accepted GitHub Issue workflow specification.
- No application code, runtime behavior, or desktop UI changes; UX preflight is not applicable.
