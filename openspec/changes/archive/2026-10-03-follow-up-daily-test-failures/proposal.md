## Why

The full deterministic suite now runs daily rather than on every pull request, so a failure can otherwise remain unnoticed in the workflow history. Maintainers need an actionable, deduplicated follow-up signal without weakening the failed workflow result.

## What Changes

- Publish test-result artifacts and a concise failure summary when the daily baseline fails.
- Create or update one open GitHub issue for the active deterministic-test failure state.
- Comment subsequent failures on the existing issue instead of opening duplicates.
- Close the tracking issue automatically after a successful baseline run.
- Grant only the workflow permission required to manage the tracking issue.
- Keep the test job failed whenever the build or tests fail.

## Capabilities

### New Capabilities

- `ci-failure-follow-up`: actionable, deduplicated issue tracking and diagnostics for scheduled deterministic-test failures.

### Modified Capabilities

None.

## Impact

- `.github/workflows/ci.yml` will gain result publication and issue follow-up steps.
- GitHub Actions permissions will include narrowly scoped issue write access.
- The repository will create and later close a maintainer-visible tracking issue when the daily baseline changes state.
- No production code, application API, persistence, or user-facing runtime behavior is affected.
