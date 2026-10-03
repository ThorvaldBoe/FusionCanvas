## MODIFIED Requirements

### Requirement: Pull requests do not require the canonical deterministic baseline
FusionCanvas SHALL retain the documented deterministic solution test command as the canonical full baseline, but pull-request automation SHALL NOT be required to run that full baseline.

#### Scenario: A pull request is opened
- **WHEN** pull-request automation executes
- **THEN** it does not run the full deterministic solution test command as a mandatory check
- **AND** absence of the full-suite result does not by itself make the pull request fail

#### Scenario: A contributor needs full-suite confidence before merging
- **WHEN** a contributor or maintainer needs to verify the complete deterministic baseline
- **THEN** they can run the documented local command from the repository root
- **AND** the result remains suitable for pre-merge verification

#### Scenario: Real-desktop automation remains separate
- **WHEN** the scheduled or manually triggered deterministic baseline executes
- **THEN** it does not require Appium, Windows Developer Mode, an interactive desktop, or execution of `FusionCanvas.UITests`

## ADDED Requirements

### Requirement: The canonical deterministic baseline runs on a daily schedule
FusionCanvas SHALL run the full deterministic solution test command through repository-controlled automation at least once every 24 hours on the default branch.

#### Scenario: The daily schedule executes
- **WHEN** the scheduled workflow starts
- **THEN** it restores required dependencies and runs `dotnet test .\\FusionCanvas.sln -m:1 --no-restore --nologo`
- **AND** the workflow result is failed when the build or any deterministic test fails

#### Scenario: A maintainer needs an immediate baseline run
- **WHEN** a maintainer manually dispatches the workflow
- **THEN** the workflow runs the same full deterministic baseline
- **AND** failures remain visible as failed workflow results

#### Scenario: The scheduled workflow is not silently stale
- **WHEN** the repository has a recent scheduled or manually dispatched baseline run
- **THEN** the workflow does not need to run an additional duplicate full baseline for every pull-request update
- **AND** the repository retains a visible daily run history for diagnosing regressions
