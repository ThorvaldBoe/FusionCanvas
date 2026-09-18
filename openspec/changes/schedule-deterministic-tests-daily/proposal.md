## Why

The full deterministic solution test suite, including Avalonia headless tests, is currently a required pull-request check. Its runtime slows the merge loop, and the existing failing tests make the PR gate noisy while the baseline is being repaired. The project needs a predictable daily signal while contributors use the documented local suite for pre-merge verification.

## What Changes

- **BREAKING** Remove the requirement that pull-request automation run the full deterministic solution test command.
- Change CI to run the full deterministic solution test suite once per day on the default branch.
- Keep a manual workflow trigger available for maintainers who need an immediate full run.
- Document the local full-suite command as the contributor’s pre-merge verification path.
- Preserve failure visibility: daily or manually triggered test failures remain failed checks and are not converted into successful runs.
- Add workflow safeguards that prevent obsolete scheduled/manual runs from consuming unnecessary runner time where practical.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `testing-baseline`: change the CI cadence and remove the full deterministic suite from the mandatory pull-request gate while retaining the deterministic baseline and its local execution requirements.

## Impact

- `.github/workflows/ci.yml` will change its triggers and scheduling behavior.
- Contributor and testing documentation will be updated with the local verification workflow and the daily CI responsibility.
- The testing-baseline delta will change the accepted pull-request automation requirement and related scenarios.
- No production application code, runtime APIs, persistence, or user-facing UI behavior is affected.
